using HMS_NewProject_Temp_Humdity_processdata.Models;
using HMS_NewProject_Temp_Humdity_processdata.Service.Interface;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace HMS_NewProject_Temp_Humdity_processdata.Service
{
	public class DeviceRedisService : IDeviceRedisService
	{
		private readonly IConnectionMultiplexer? _connectionMultiplexer;
		public IDatabase? db => _connectionMultiplexer?.GetDatabase();
		private readonly ILogger<DeviceRedisService> _logger;

		private readonly IDeviceService _deviceService;
		public DeviceRedisService(IConnectionMultiplexer connectionMultiplexer, ILogger<DeviceRedisService> logger, IDeviceService deviceService)
		{
			_logger = logger;
			_connectionMultiplexer = connectionMultiplexer;
			_deviceService = deviceService;
		}

		private bool IsRedisAvailable()
		{
			var multiplexerNull = _connectionMultiplexer == null;
			var isConnected = _connectionMultiplexer?.IsConnected ?? false;
			var dbNull = db == null;
			return !multiplexerNull && isConnected && !dbNull;
		}

		// ================== KEY HELPERS ==================
		private static string DeviceKey(string IMEI) => $"device_TH:{IMEI}";
		private static string LocationKey(string locationId) => $"location:{locationId}";
		private static string LocationDevicesKey(string locationId) => $"location:{locationId}:devices";
		private static string UserLocationsKey(string userId) => $"user:{userId}:locations";
		private static string DeviceLocationKey(string imei) => $"device:{imei}:location";
		private static string LocationOwnerKey(string locationId) => $"location:{locationId}:owner";

		// ================== DEVICE ==================
		public async Task SetDeviceAsync(string IMEI, clsDeviceCacheModel deviceCacheModel)
		{
			if (!IsRedisAvailable()) return;
			try
			{
				var json = JsonConvert.SerializeObject(deviceCacheModel);
				await db!.StringSetAsync(DeviceKey(IMEI), json);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis set device IMEI={IMEI}", IMEI);
			}
		}

		public async Task<clsDeviceCacheModel?> GetDeviceAsync(string IMEI)
		{
			if (!IsRedisAvailable()) return null;
			try
			{
				var value = await db!.StringGetAsync(DeviceKey(IMEI));
				if (!value.HasValue) return null;
				var json = value.ToString();
				if (!json.StartsWith("{")) return null;
				return JsonConvert.DeserializeObject<clsDeviceCacheModel>(json);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis lấy device IMEI={IMEI}", IMEI);
				return null;
			}
		}

		public async Task DeleteDeviceAsync(string IMEI)
		{
			if (!IsRedisAvailable()) return;
			try
			{
				await db!.KeyDeleteAsync(DeviceKey(IMEI)); // fix: xóa đúng key có prefix
				_logger.LogInformation("Xóa device IMEI={IMEI} khỏi Redis", IMEI);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis xóa device IMEI={IMEI}", IMEI);
			}
		}

		public async Task<List<clsDeviceCacheModel>> GetAllDevicesAsync()
		{
			if (!IsRedisAvailable())
				return new List<clsDeviceCacheModel>();
			try
			{
				var server = _connectionMultiplexer!.GetServer(_connectionMultiplexer.GetEndPoints().First());
				// fix: chỉ lấy đúng key của device, không lấy nhầm key location/user
				var keys = server.Keys(pattern: "device_TH:*").ToList();
				var result = new List<clsDeviceCacheModel>();
				foreach (var key in keys)
				{
					var imei = key.ToString().Replace("device_TH:", "");
					var device = await GetDeviceAsync(imei);
					if (device != null)
						result.Add(device);
				}
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis lấy tất cả thiết bị");
				return new List<clsDeviceCacheModel>();
			}
		}

		public async Task<List<LocationResponse>> GetLocationsWithDevicesByUserAsync(string userId)
		{
			if (!IsRedisAvailable())
				return new List<LocationResponse>();

			try
			{
				_logger.LogInformation("Getting locations + devices from Redis. UserId={UserId}", userId);

				// 1. Lấy danh sách LocationId thuộc user (từ Set: user:{userId}:locations)
				var locationIds = await db!.SetMembersAsync(UserLocationsKey(userId));
				if (locationIds.Length == 0)
				{
					_logger.LogInformation("User {UserId} không có location nào.", userId);
					return new List<LocationResponse>();
				}

				// 2. Lấy trước toàn bộ imei theo từng location (để gộp query Mongo 1 lần)
				var locationImeiMap = new Dictionary<string, List<string>>();
				var allImeis = new HashSet<string>();

				foreach (var locId in locationIds)
				{
					var locationId = locId.ToString();
					var imeis = await db!.SetMembersAsync(LocationDevicesKey(locationId));
					var imeiStrings = imeis.Select(x => x.ToString()).ToList();

					locationImeiMap[locationId] = imeiStrings;
					foreach (var imei in imeiStrings)
						allImeis.Add(imei);
				}

				// 3. Gọi Mongo DUY NHẤT 1 lần cho toàn bộ imei của user (thay vì gọi lặp trong foreach)
				var deviceDataMap = allImeis.Count > 0
					? await _deviceService.GetClsDeviceLogLastUpdateByImeis(allImeis)
					: new Dictionary<string, clsDeviceLogModel>();

				var result = new List<LocationResponse>();

				foreach (var locId in locationIds)
				{
					var locationId = locId.ToString();

					// 4. Lấy thông tin location (từ String: location:{locationId})
					var location = await GetLocationAsync(locationId);
					if (location == null)
					{
						_logger.LogWarning("Location {LocationId} có trong Set nhưng không tìm thấy dữ liệu.", locationId);
						continue;
					}

					var imeis = locationImeiMap[locationId];
					var devices = new List<clsDeviceCacheModel>();

					foreach (var imei in imeis)
					{
						var device = await GetDeviceAsync(imei);
						if (device != null)
						{
							if (deviceDataMap.TryGetValue(imei, out var log))
							{
								device.Sensors = log.Sensors;
							}

							devices.Add(device);
						}
					}

					_logger.LogInformation(
						"Location {LocationId} ({Name}) có {DeviceCount} devices.",
						location.LocationId,
						location.Name,
						devices.Count);

					location.Devices = devices;
					result.Add(location);
				}

				_logger.LogInformation("Trả về {LocationCount} locations cho user {UserId}.", result.Count, userId);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis lấy location + devices theo UserId={UserId}", userId);
				return new List<LocationResponse>();
			}
		}

		// ================== LOCATION ==================
		public async Task SetLocationAsync(LocationResponse location)
		{
			if (!IsRedisAvailable()) return;
			try
			{
				var json = JsonConvert.SerializeObject(location);
				await db!.StringSetAsync(LocationKey(location.LocationId), json);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis set location {LocationId}", location.LocationId);
			}
		}

		public async Task<LocationResponse?> GetLocationAsync(string locationId)
		{
			if (!IsRedisAvailable()) return null;
			try
			{
				var value = await db!.StringGetAsync(LocationKey(locationId));
				if (!value.HasValue) return null;
				var json = value.ToString();
				if (!json.StartsWith("{")) return null;
				return JsonConvert.DeserializeObject<LocationResponse>(json);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis lấy location {LocationId}", locationId);
				return null;
			}
		}

		// ================== LOCATION <-> DEVICE ==================
		public async Task AddDeviceToLocation(string locationId, string IMEI)
		{
			if (!IsRedisAvailable()) return;
			try
			{
				await db!.SetAddAsync(LocationDevicesKey(locationId), IMEI);

				await db!.StringSetAsync(DeviceLocationKey(IMEI), locationId);

			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis thêm thiết bị {IMEI} vào location {LocationId}", IMEI, locationId);
			}
		}

		public async Task RemoveDeviceFromLocation(string locationId, string IMEI)
		{
			if (!IsRedisAvailable()) return;
			try
			{
				await db!.SetRemoveAsync(LocationDevicesKey(locationId), IMEI);
				await db!.KeyDeleteAsync(DeviceLocationKey(IMEI));

			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis xóa thiết bị {IMEI} khỏi location {LocationId}", IMEI, locationId);
			}
		}

		public async Task<List<clsDeviceCacheModel>> GetDevicesByLocationAsync(string locationId)
		{
			if (!IsRedisAvailable()) return new List<clsDeviceCacheModel>();
			try
			{
				var imeis = await db!.SetMembersAsync(LocationDevicesKey(locationId));
				var result = new List<clsDeviceCacheModel>();
				foreach (var imei in imeis)
				{
					var device = await GetDeviceAsync(imei!);
					if (device != null)
						result.Add(device);
				}
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis lấy device theo location {LocationId}", locationId);
				return new List<clsDeviceCacheModel>();
			}
		}

		// ================== USER <-> LOCATION ==================
		public async Task AddLocationToUser(string userId, string locationId)
		{
			if (!IsRedisAvailable()) return;
			try
			{
				await db!.SetAddAsync(UserLocationsKey(userId), locationId);

				await db!.StringSetAsync(LocationOwnerKey(locationId), userId);

			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis thêm location {LocationId} vào user {UserId}", locationId, userId);
			}
		}

		public async Task RemoveLocationFromUser(string userId, string locationId)
		{
			if (!IsRedisAvailable()) return;
			try
			{
				await db!.SetRemoveAsync(UserLocationsKey(userId), locationId);

			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis xóa location {LocationId} khỏi user {UserId}", locationId, userId);
			}
		}

		public async Task<List<LocationResponse>> GetLocationsByUserAsync(string userId)
		{
			if (!IsRedisAvailable()) return new List<LocationResponse>();
			try
			{
				var locationIds = await db!.SetMembersAsync(UserLocationsKey(userId));
				var result = new List<LocationResponse>();
				foreach (var locId in locationIds)
				{
					var loc = await GetLocationAsync(locId!);
					if (loc != null)
						result.Add(loc);
				}
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis lấy location theo user {UserId}", userId);
				return new List<LocationResponse>();
			}
		}

		public async Task<List<LocationWithDevicesResponse>> GetFullTreeByUserAsync(string userId)
		{
			var result = new List<LocationWithDevicesResponse>();
			if (!IsRedisAvailable()) return result;

			var locations = await GetLocationsByUserAsync(userId);
			foreach (var location in locations)
			{
				var devices = await GetDevicesByLocationAsync(location.LocationId);
				result.Add(new LocationWithDevicesResponse
				{
					Location = location,
					Devices = devices
				});
			}
			return result;
		}

		public async Task UpdateSensorDataAsync(string IMEI, List<Sensor>? sensors, DateTime? lastUpdate)
		{
			if (!IsRedisAvailable()) return;
			try
			{
				var existing = await GetDeviceAsync(IMEI);
				if (existing == null)
				{
					_logger.LogWarning("Không tìm thấy device trong cache khi update sensor. IMEI={IMEI}", IMEI);
					return;
				}

				existing.Sensors = sensors;
				existing.LastUpdate = lastUpdate;

				var json = JsonConvert.SerializeObject(existing);
				await db!.StringSetAsync(DeviceKey(IMEI), json);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis update sensor data IMEI={IMEI}", IMEI);
			}
		}

		public async Task DeleteLocationAsync(string locationId, string userId)
		{
			if (!IsRedisAvailable()) return;
			try
			{
				// 1. Xóa key thông tin location (String: location:{locationId})
				await db!.KeyDeleteAsync(LocationKey(locationId));

				// 2. Xóa luôn Set chứa danh sách device của location này (location:{locationId}:devices)
				await db!.KeyDeleteAsync(LocationDevicesKey(locationId));

				// 3. Gỡ locationId khỏi Set của user (user:{userId}:locations)
				await db!.SetRemoveAsync(UserLocationsKey(userId), locationId);

				_logger.LogInformation(
					"Đã xóa hoàn toàn location {LocationId} khỏi Redis (bao gồm devices set và user set).",
					locationId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis xóa location {LocationId}", locationId);
			}
		}
		public async Task<string?> GetUserIdByImeiAsync(string imei)
		{
			if (!IsRedisAvailable()) return null;
			try
			{
				var locationId = await db!.StringGetAsync(DeviceLocationKey(imei));
				if (!locationId.HasValue) return null;

				var userId = await db!.StringGetAsync(LocationOwnerKey(locationId!));
				return userId.HasValue ? userId.ToString() : null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "lỗi redis tra cứu userId theo IMEI={IMEI}", imei);
				return null;
			}
		}
	}

	public class LocationWithDevicesResponse
	{
		public LocationResponse Location { get; set; } = null!;
		public List<clsDeviceCacheModel> Devices { get; set; } = new();
	}
}