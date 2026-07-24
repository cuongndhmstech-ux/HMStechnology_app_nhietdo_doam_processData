using HMS_NewProject_Temp_Humdity_processdata.Models;
using HMS_NewProject_Temp_Humdity_processdata.Models.Config;
using HMS_NewProject_Temp_Humdity_processdata.Service.Interface;
using HMS_NewProject_Temp_Humdity_processdata.Signalr;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;

namespace HMS_NewProject_Temp_Humdity_processdata.BGroundService
{
	public class DeviceCacheManager : BackgroundService
	{
		private readonly IServiceScopeFactory _serviceScopeFactory;
		private readonly ILogger<DeviceCacheManager> _logger;
		private readonly clsAppConfig _config;
		private readonly string _hubUrl;
		private Microsoft.AspNetCore.SignalR.Client.HubConnection? _connection;
		private readonly IHubContext<ClsHubFrontendOnline> _hub;

		public DeviceCacheManager(IServiceScopeFactory scopeFactory, ILogger<DeviceCacheManager> logger, clsAppConfig config)
		{
			_serviceScopeFactory = scopeFactory;
			_logger = logger;
			_config = config;
			_hubUrl = _config.SignalR.HubUrlManager;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await LoadDevicesToRedis(stoppingToken);

			await ConnectionSignalr(stoppingToken);
		}

		// chưa reset theo thời gian
		private async Task LoadDevicesToRedis(CancellationToken stoppingToken)
		{
			try
			{
				using var scope = _serviceScopeFactory.CreateScope();

				var managerApiClient = scope.ServiceProvider.GetRequiredService<IManagerApiClient>();
				var redis = scope.ServiceProvider.GetRequiredService<IDeviceRedisService>();

				var locations = await managerApiClient.GetListClsDeviceCacheModelAsync();

				if (locations == null || !locations.Any())
					return;

				foreach (var location in locations)
				{
					if (stoppingToken.IsCancellationRequested)
						break;

					try
					{
						// ===========================================================
						// Location "UNASSIGNED" chỉ dùng để gom dữ liệu từ Manager API.
						// Không lưu Location này vào Redis.
						// ===========================================================

						bool isUnassigned =
							string.Equals(location.LocationId,
								"UNASSIGNED",
								StringComparison.OrdinalIgnoreCase);

						if (!isUnassigned)
						{
							await redis.SetLocationAsync(new LocationResponse
							{
								LocationId = location.LocationId,
								UserId = location.UserId,
								Name = location.Name,
								Devices = null
							});

							await redis.AddLocationToUser(
								location.UserId,
								location.LocationId);
						}

						if (location.Devices == null || !location.Devices.Any())
							continue;

						foreach (var device in location.Devices)
						{
							// =======================================================
							// Nếu Device thuộc Location thật thì cập nhật LocationId.
							// Nếu là nhóm UNASSIGNED thì giữ nguyên LocationId=""
							// theo dữ liệu gốc trong Database.
							// =======================================================

							if (!isUnassigned)
							{
								device.LocationId = location.LocationId;
							}

							if (!isUnassigned &&
								device.UserId != location.UserId)
							{
								_logger.LogWarning(
									"Device {IMEI} có UserId={DeviceUserId} khác với chủ Location {LocationId} (UserId={LocationUserId})",
									device.Imei,
									device.UserId,
									location.LocationId,
									location.UserId);
							}
							// dữ liệu mẫu////////////////////////////////////////////////
							// =======================================================
							// Sinh dữ liệu mẫu Device Status và Connectivity
							// =======================================================

							int hash = Math.Abs(device.Imei?.GetHashCode() ?? 0);

							device.status = (hash % 4) switch
							{
								0 => DeviceStatus.online,
								1 => DeviceStatus.offline,
								2 => DeviceStatus.unknown,
								_ => DeviceStatus.online
							};

							device.connectivity = (hash % 4) switch
							{
								0 => ConnectivityStatus.strong,
								1 => ConnectivityStatus.medium,
								2 => ConnectivityStatus.weak,
								_ => ConnectivityStatus.none
							};

							// =======================================================
							// Sinh dữ liệu mẫu Sensor
							// =======================================================




							if (device.Sensors != null)
							{
								for (int i = 0; i < device.Sensors.Count; i++)
								{
									var sensor = device.Sensors[i];

									switch (i % 4)
									{
										// Bình thường
										case 0:
											sensor.temperature =
												((device.TemperatureMin + device.TemperatureMax) / 2)
												.ToString("0");

											sensor.humidity =
												((device.HumidityMin + device.HumidityMax) / 2)
												.ToString("0");
											break;

										// Nhiệt độ vượt max
										case 1:
											sensor.temperature =
												(device.TemperatureMax + 5)
												.ToString("0");

											sensor.humidity =
												((device.HumidityMin + device.HumidityMax) / 2)
												.ToString("0");
											break;

										// Độ ẩm thấp hơn min
										case 2:
											sensor.temperature =
												((device.TemperatureMin + device.TemperatureMax) / 2)
												.ToString("0");

											sensor.humidity =
												(device.HumidityMin - 10)
												.ToString("0");
											break;

										// Cả nhiệt độ và độ ẩm vượt ngưỡng
										case 3:
											sensor.temperature =
												(device.TemperatureMax + 5)
												.ToString("0");

											sensor.humidity =
												(device.HumidityMax + 10)
												.ToString("0");
											break;
									}
								}
							}
							// dữ liệu mẫu//////////////////////////////////////////////////
							// Cache thông tin Device
							await redis.SetDeviceAsync(device.Imei, device);

							// Mapping User -> Device
							await redis.AddDeviceToUser(
								device.UserId,
								device.Imei);

							// =======================================================
							// Chỉ Device có Location thật mới thêm vào
							// location:{locationId}:devices
							// =======================================================

							if (!isUnassigned)
							{
								await redis.AddDeviceToLocation(
									location.LocationId,
									device.Imei);
							}

							_logger.LogInformation(
								"Cached device {IMEI} -> {Location}",
								device.Imei,
								isUnassigned
									? "UNASSIGNED"
									: location.LocationId);
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(
							ex,
							"Failed to cache location {LocationId}",
							location.LocationId);
					}
				}

				_logger.LogInformation("Finished caching locations.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "LoadDevicesToRedis failed.");
			}
		}

		private async Task ConnectionSignalr(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Creating SignalR connection to {HubUrl}...", _hubUrl);

			// Build connection kiểu ASP.NET Core SignalR
			_connection = new HubConnectionBuilder()
				.WithUrl(_hubUrl)
				.WithAutomaticReconnect(new[]
				{
					TimeSpan.Zero,
					TimeSpan.FromSeconds(2),
					TimeSpan.FromSeconds(5),
					TimeSpan.FromSeconds(10)
				})
				.Build();

			// Đăng ký log khi tự động reconnect
			_connection.Reconnecting += error =>
			{
				_logger.LogWarning(error, "SignalR đang reconnect...");
				return Task.CompletedTask;
			};

			_connection.Reconnected += connectionId =>
			{
				_logger.LogInformation("SignalR reconnected. ConnectionId={ConnectionId}", connectionId);
				return Task.CompletedTask;
			};

			_connection.Closed += error =>
			{
				_logger.LogWarning(error, "SignalR connection closed.");
				return Task.CompletedTask;
			};

			// Đăng ký lắng nghe event "SendMessage" từ Hub — thay cho _proxy.On<string,string>(...

			_connection.On<string, string>("SendMessage", async (name, noiDung) =>
			{
				_logger.LogDebug("SignalR message received. Name={Name}", name);
				using var scope = _serviceScopeFactory.CreateScope();
				var redis = scope.ServiceProvider.GetRequiredService<IDeviceRedisService>();
				switch (name)
				{
					case "DeviceDeleted":
						{
							var device = JsonConvert.DeserializeObject<clsDeviceCacheModel>(noiDung);
							if (device == null)
							{
								_logger.LogInformation("Deserialize device failed.");
								return;
							}
							_logger.LogInformation("Deleting device: {IMEI}", device.Imei);
							await redis.DeleteDeviceAsync(device.Imei);

							await redis.RemoveDeviceFromUser(device.UserId, device.Imei);

							if (!string.IsNullOrWhiteSpace(device.LocationId))
							{
								await redis.RemoveDeviceFromLocation(
									device.LocationId,
									device.Imei);
							}


							break;
						}

					case "LocationDeleted":
						{
							var location = JsonConvert.DeserializeObject<LocationResponse>(
								noiDung,
								new JsonSerializerSettings
								{
									ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
								});

							if (location == null)
							{
								_logger.LogInformation("Deserialize location failed.");
								return;
							}
							_logger.LogInformation("Deleting location: {LocationId}", location.LocationId);
							await redis.DeleteLocationAsync(location.LocationId, location.UserId);
							break;
						}

					case "LocationCreated":
					case "LocationUpdated":
						{
							var location = JsonConvert.DeserializeObject<LocationResponse>(
								noiDung,
								new JsonSerializerSettings
								{
									ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
								});

							if (location == null)
							{
								_logger.LogInformation("Deserialize location failed.");
								return;
							}

							if (string.IsNullOrEmpty(location.LocationId) || string.IsNullOrEmpty(location.UserId))
							{
								_logger.LogWarning(
									"Location thiếu LocationId hoặc UserId, bỏ qua. LocationId={LocationId}, UserId={UserId}",
									location.LocationId, location.UserId);
								return;
							}

							_logger.LogInformation(
								"Updating location. LocationId={LocationId}, UserId={UserId}, Name={Name}",
								location.LocationId,
								location.UserId,
								location.Name);

							// Nếu location đã tồn tại và đổi chủ (UserId khác) -> gỡ khỏi user cũ trước khi gán user mới
							var oldLocation = await redis.GetLocationAsync(location.LocationId);
							if (oldLocation != null
								&& !string.IsNullOrEmpty(oldLocation.UserId)
								&& oldLocation.UserId != location.UserId)
							{
								await redis.RemoveLocationFromUser(oldLocation.UserId, location.LocationId);

								_logger.LogInformation(
									"Location {LocationId} đổi chủ từ {OldUserId} -> {NewUserId}",
									location.LocationId, oldLocation.UserId, location.UserId);
							}

							// Không set Devices ở đây để tránh ghi đè danh sách device đang có trong Redis
							await redis.SetLocationAsync(new LocationResponse
							{
								LocationId = location.LocationId,
								UserId = location.UserId,
								Name = location.Name,
								Devices = null
							});
							await redis.AddLocationToUser(location.UserId, location.LocationId);

							_logger.LogDebug("Redis updated successfully. LocationId={LocationId}", location.LocationId);

							break;
						}

					case "DeviceCreated":
					case "DeviceUpdated":
					default:
						{
							var device = JsonConvert.DeserializeObject<clsDeviceCacheModel>(
								noiDung,
								new JsonSerializerSettings
								{
									ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
								});

							if (device == null)
							{
								_logger.LogInformation("Deserialize device failed.");
								return;
							}

							_logger.LogInformation(
								"Updating device. IMEI={IMEI}, UserId={UserId}, LocationId={LocationId}",
								device.Imei,
								device.UserId,
								device.LocationId);

							// ===========================================================
							// Lấy thông tin Device cũ trong Redis để kiểm tra
							// có đổi User hoặc đổi Location hay không.
							// ===========================================================
							var oldDevice = await redis.GetDeviceAsync(device.Imei);

							// ===========================================================
							// Nếu Device đổi User
							// -> Xóa khỏi danh sách Device của User cũ.
							// ===========================================================
							if (oldDevice != null &&
								!string.Equals(oldDevice.UserId, device.UserId, StringComparison.Ordinal))
							{
								await redis.RemoveDeviceFromUser(
									oldDevice.UserId,
									device.Imei);

								_logger.LogInformation(
									"Device {IMEI} moved from User {OldUser} -> {NewUser}",
									device.Imei,
									oldDevice.UserId,
									device.UserId);
							}

							// ===========================================================
							// Nếu Device đổi Location
							// -> Xóa khỏi Location cũ.
							// ===========================================================
							if (oldDevice != null &&
								!string.Equals(oldDevice.LocationId, device.LocationId, StringComparison.Ordinal) &&
								!string.IsNullOrWhiteSpace(oldDevice.LocationId))
							{
								await redis.RemoveDeviceFromLocation(
									oldDevice.LocationId,
									device.Imei);

								_logger.LogInformation(
									"Device {IMEI} removed from old Location {LocationId}",
									device.Imei,
									oldDevice.LocationId);
							}

							// ===========================================================
							// Cập nhật Device vào Redis
							// ===========================================================
							await redis.SetDeviceAsync(device.Imei, device);

							// ===========================================================
							// Mapping User -> Device
							// ===========================================================
							if (!string.IsNullOrWhiteSpace(device.UserId))
							{
								await redis.AddDeviceToUser(
									device.UserId,
									device.Imei);
							}

							// ===========================================================
							// Mapping Location -> Device
							// Chỉ thêm nếu Device đã được gán Location.
							// Device chưa gán Location chỉ nằm trong
							// user:{userId}:devices
							// ===========================================================
							if (!string.IsNullOrWhiteSpace(device.LocationId))
							{
								await redis.AddDeviceToLocation(
									device.LocationId,
									device.Imei);
							}

							_logger.LogInformation(
								"Redis updated successfully. IMEI={IMEI}",
								device.Imei);

							break;
						}
				}
			});

			await StartWithRetryAsync(stoppingToken);
		}

		private async Task StartWithRetryAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					_logger.LogInformation("Connecting to SignalR...");

					await _connection!.StartAsync(stoppingToken);

					_logger.LogInformation("SignalR connected.");

					// Chờ tới khi bị cancel hoặc connection đóng hẳn (WithAutomaticReconnect sẽ tự nối lại các lần rớt tạm thời)
					await Task.Delay(Timeout.Infinite, stoppingToken);
				}
				catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
				{
					break;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Kết nối SignalR thất bại, thử lại sau 5 giây...");
					await Task.Delay(5000, stoppingToken);
				}
			}
		}
	}
}