using System.Collections.Concurrent;
using HMS_NewProject_Temp_Humdity_processdata.DTO.Response;

namespace HMS_NewProject_Temp_Humdity_processdata.Services
{
	public class DashboardMockService
	{
		private static readonly Random _random = new();

		private static readonly ConcurrentDictionary<string, (double Temp, double Humid)> _lastReadings = new();

		private class DeviceSeed
		{
			public string Id = null!;
			public string Name = null!;
			public string RoomId = null!; // rỗng "" nếu chưa gán phòng
			public string? SerialNumber;
			public bool IsOffline;      // true => Connectivity luôn offline, không sinh sensor
			public bool ForceWarning;   // true => Status luôn warning bất kể sensor
			public double TempMin, TempMax, HumidMin, HumidMax;
		}

		private class RoomSeed
		{
			public string Id = null!;
			public string Name = null!;
			public string? Description;
			public DateTime CreatedAt;
			public List<DeviceSeed> Devices = new();
		}

		private class UserSeed
		{
			public List<RoomSeed> Rooms = new();
			public List<DeviceSeed> UnassignedDevices = new();
		}

		private Dictionary<string, UserSeed>? _seedData;

		private Dictionary<string, UserSeed> GetSeedData()
		{
			if (_seedData != null) return _seedData;

			_seedData = new Dictionary<string, UserSeed>
			{
				// ================= USER 1 =================
				["U000001"] = new UserSeed
				{
					Rooms = new List<RoomSeed>
					{
						new RoomSeed
						{
							Id = "L000001", Name = "Phòng Server", Description = "Khu vực đặt máy chủ",
							CreatedAt = new DateTime(2025, 01, 10),
							Devices = new List<DeviceSeed>
							{
								new() { Id = "D000001", Name = "ESP32-01", RoomId = "L000001", SerialNumber = "868123456789001", TempMin = 18, TempMax = 27, HumidMin = 40, HumidMax = 60 },
								new() { Id = "D000002", Name = "ESP32-02", RoomId = "L000001", SerialNumber = "868123456789002", TempMin = 18, TempMax = 27, HumidMin = 40, HumidMax = 60 },
								new() { Id = "D000003", Name = "ESP32-03", RoomId = "L000001", SerialNumber = "868123456789003", IsOffline = true, TempMin = 18, TempMax = 27, HumidMin = 40, HumidMax = 60 },
								new() { Id = "D000021", Name = "ESP32-21-FAULTY", RoomId = "L000001", SerialNumber = "868123456789021", ForceWarning = true, TempMin = 18, TempMax = 27, HumidMin = 40, HumidMax = 60 }
							}
						},
						new RoomSeed
						{
							Id = "L000002", Name = "Kho A", Description = "Kho chứa hàng khu A",
							CreatedAt = new DateTime(2025, 02, 15),
							Devices = new List<DeviceSeed>
							{
								new() { Id = "D000004", Name = "ESP32-04", RoomId = "L000002", SerialNumber = "868123456789004", TempMin = 20, TempMax = 32, HumidMin = 50, HumidMax = 80 },
								new() { Id = "D000005", Name = "ESP32-05", RoomId = "L000002", SerialNumber = "868123456789005", TempMin = 20, TempMax = 32, HumidMin = 50, HumidMax = 80 }
							}
						}
					},
					UnassignedDevices = new List<DeviceSeed>
					{
						new() { Id = "D000006", Name = "ESP32-06", RoomId = "", SerialNumber = "868123456789006", TempMin = 20, TempMax = 32, HumidMin = 50, HumidMax = 80 }
					}
				},

				// ================= USER 2 =================
				["U000002"] = new UserSeed
				{
					Rooms = new List<RoomSeed>
					{
						new RoomSeed
						{
							Id = "L000003", Name = "Văn Phòng", Description = "Khu vực làm việc chính",
							CreatedAt = new DateTime(2025, 01, 20),
							Devices = new List<DeviceSeed>
							{
								new() { Id = "D000007", Name = "ESP32-07", RoomId = "L000003", SerialNumber = "868123456789007", TempMin = 22, TempMax = 28, HumidMin = 40, HumidMax = 65 },
								new() { Id = "D000008", Name = "ESP32-08", RoomId = "L000003", SerialNumber = "868123456789008", TempMin = 22, TempMax = 28, HumidMin = 40, HumidMax = 65 },
								new() { Id = "D000009", Name = "ESP32-09", RoomId = "L000003", SerialNumber = "868123456789009", TempMin = 22, TempMax = 28, HumidMin = 40, HumidMax = 65 }
							}
						},
						new RoomSeed
						{
							Id = "L000004", Name = "Kho B", Description = "Kho chứa hàng khu B",
							CreatedAt = new DateTime(2025, 02, 05),
							Devices = new List<DeviceSeed>
							{
								new() { Id = "D000010", Name = "ESP32-10", RoomId = "L000004", SerialNumber = "868123456789010", TempMin = 20, TempMax = 32, HumidMin = 50, HumidMax = 80 },
								new() { Id = "D000011", Name = "ESP32-11", RoomId = "L000004", SerialNumber = "868123456789011", TempMin = 20, TempMax = 32, HumidMin = 50, HumidMax = 80 }
							}
						},
						new RoomSeed
						{
							Id = "L000005", Name = "Xưởng Sản Xuất", Description = "Khu vực sản xuất",
							CreatedAt = new DateTime(2025, 03, 01),
							Devices = new List<DeviceSeed>
							{
								new() { Id = "D000012", Name = "ESP32-12", RoomId = "L000005", SerialNumber = "868123456789012", TempMin = 24, TempMax = 36, HumidMin = 45, HumidMax = 75 }
							}
						}
					},
					UnassignedDevices = new List<DeviceSeed>
					{
						new() { Id = "D000013", Name = "ESP32-13", RoomId = "", SerialNumber = "868123456789013", IsOffline = true, TempMin = 20, TempMax = 32, HumidMin = 50, HumidMax = 80 },
						new() { Id = "D000014", Name = "ESP32-14", RoomId = "", SerialNumber = "868123456789014", TempMin = 20, TempMax = 32, HumidMin = 50, HumidMax = 80 }
					}
				},

				// ================= USER 3 =================
				["U000003"] = new UserSeed
				{
					Rooms = new List<RoomSeed>
					{
						new RoomSeed
						{
							Id = "L000006", Name = "Nhà Kính 1", Description = "Khu trồng rau sạch",
							CreatedAt = new DateTime(2025, 01, 05),
							Devices = new List<DeviceSeed>
							{
								new() { Id = "D000015", Name = "ESP32-15", RoomId = "L000006", SerialNumber = "868123456789015", TempMin = 22, TempMax = 34, HumidMin = 60, HumidMax = 90 },
								new() { Id = "D000016", Name = "ESP32-16", RoomId = "L000006", SerialNumber = "868123456789016", TempMin = 22, TempMax = 34, HumidMin = 60, HumidMax = 90 },
								new() { Id = "D000017", Name = "ESP32-17", RoomId = "L000006", SerialNumber = "868123456789017", TempMin = 22, TempMax = 34, HumidMin = 60, HumidMax = 90 }
							}
						},
						new RoomSeed
						{
							Id = "L000007", Name = "Nhà Kính 2", Description = "Khu trồng hoa",
							CreatedAt = new DateTime(2025, 02, 10),
							Devices = new List<DeviceSeed>
							{
								new() { Id = "D000018", Name = "ESP32-18", RoomId = "L000007", SerialNumber = "868123456789018", TempMin = 22, TempMax = 34, HumidMin = 60, HumidMax = 90 },
								new() { Id = "D000019", Name = "ESP32-19", RoomId = "L000007", SerialNumber = "868123456789019", TempMin = 22, TempMax = 34, HumidMin = 60, HumidMax = 90 }
							}
						}
					},
					UnassignedDevices = new List<DeviceSeed>
					{
						new() { Id = "D000020", Name = "ESP32-20", RoomId = "", SerialNumber = "868123456789020", TempMin = 20, TempMax = 32, HumidMin = 50, HumidMax = 80 }
					}
				}
			};

			return _seedData;
		}

		// ================= API 1: List<RoomEntity> =================
		public Task<List<RoomResponseTest>?> GetRoomsAsync(string userId)
		{
			var seed = GetSeedData();
			if (!seed.TryGetValue(userId, out var userSeed))
				return Task.FromResult<List<RoomResponseTest>?>(null);

			var result = userSeed.Rooms.Select(room =>
			{
				var devices = room.Devices.Select(d => BuildDeviceResponse(d, room.Name)).ToList();

				return new RoomResponseTest
				{
					Id = room.Id,
					Name = room.Name,
					Description = room.Description,
					CreatedAt = room.CreatedAt,
					TotalDevices = devices.Count,
					OnlineDevices = devices.Count(d => d.Connectivity == ConnectivityStatus.online),
					AlertCount = devices.Count(d => d.Status == DeviceStatus.warning)
				};
			}).ToList();

			return Task.FromResult<List<RoomResponseTest>?>(result);
		}

		// ================= API 2: List<DeviceEntity>, lọc theo roomId (optional) =================
		// roomId = null/rỗng  -> tất cả thiết bị (trong phòng + chưa gán)
		// roomId = "unassigned" -> chỉ thiết bị chưa gán phòng
		// roomId = "L000001"    -> chỉ thiết bị thuộc phòng đó
		public Task<List<DeviceResponseTest>?> GetDevicesAsync(string userId, string? roomId = null)
		{
			var seed = GetSeedData();
			if (!seed.TryGetValue(userId, out var userSeed))
				return Task.FromResult<List<DeviceResponseTest>?>(null);

			List<DeviceResponseTest> result;

			if (string.IsNullOrWhiteSpace(roomId))
			{
				// Không lọc -> trả tất cả thiết bị của user (mọi phòng + chưa gán)
				var fromRooms = userSeed.Rooms
					.SelectMany(room => room.Devices.Select(d => BuildDeviceResponse(d, room.Name)));

				var fromUnassigned = userSeed.UnassignedDevices
					.Select(d => BuildDeviceResponse(d, roomName: null));

				result = fromRooms.Concat(fromUnassigned).ToList();
			}
			else if (roomId.Equals("unassigned", StringComparison.OrdinalIgnoreCase))
			{
				result = userSeed.UnassignedDevices
					.Select(d => BuildDeviceResponse(d, roomName: null))
					.ToList();
			}
			else
			{
				var room = userSeed.Rooms.FirstOrDefault(r => r.Id == roomId);
				if (room == null)
					return Task.FromResult<List<DeviceResponseTest>?>(null); // phòng không tồn tại

				result = room.Devices
					.Select(d => BuildDeviceResponse(d, room.Name))
					.ToList();
			}

			return Task.FromResult<List<DeviceResponseTest>?>(result);
		}

		// ================= HELPERS =================

		private DeviceResponseTest BuildDeviceResponse(DeviceSeed seed, string? roomName)
		{
			if (seed.IsOffline)
			{
				return new DeviceResponseTest
				{
					Id = seed.Id,
					Name = seed.Name,
					RoomId = seed.RoomId,
					RoomName = roomName,
					SerialNumber = seed.SerialNumber,
					Status = DeviceStatus.normal,
					Connectivity = ConnectivityStatus.offline,
					CurrentTemperature = null,
					CurrentHumidity = null,
					Threshold = new ThresholdResponseTest
					{
						MinTemperature = seed.TempMin,
						MaxTemperature = seed.TempMax,
						MinHumidity = seed.HumidMin,
						MaxHumidity = seed.HumidMax
					},
					LastUpdatedAt = null
				};
			}

			var (temp, humid) = GenerateDrifted(seed.Id, seed.TempMin, seed.TempMax, seed.HumidMin, seed.HumidMax);

			var status = seed.ForceWarning
				? DeviceStatus.warning
				: (temp < seed.TempMin || temp > seed.TempMax || humid < seed.HumidMin || humid > seed.HumidMax
					? DeviceStatus.warning
					: DeviceStatus.normal);

			return new DeviceResponseTest
			{
				Id = seed.Id,
				Name = seed.Name,
				RoomId = seed.RoomId,
				RoomName = roomName,
				SerialNumber = seed.SerialNumber,
				Status = status,
				Connectivity = ConnectivityStatus.online,
				CurrentTemperature = temp,
				CurrentHumidity = humid,
				Threshold = new ThresholdResponseTest
				{
					MinTemperature = seed.TempMin,
					MaxTemperature = seed.TempMax,
					MinHumidity = seed.HumidMin,
					MaxHumidity = seed.HumidMax
				},
				LastUpdatedAt = DateTime.UtcNow
			};
		}

		/// <summary>
		/// Sinh giá trị mới dựa trên giá trị lần trước (drift nhẹ ±0.5°C / ±1.5%),
		/// mô phỏng cảm biến thật thay vì random giật cục mỗi lần poll 10s.
		/// Luôn kẹp trong khoảng [Min, Max].
		/// </summary>
		private (double Temp, double Humid) GenerateDrifted(
			string deviceId, double tempMin, double tempMax, double humidMin, double humidMax)
		{
			if (!_lastReadings.TryGetValue(deviceId, out var last))
			{
				var initTemp = Math.Round(tempMin + _random.NextDouble() * (tempMax - tempMin), 1);
				var initHumid = Math.Round(humidMin + _random.NextDouble() * (humidMax - humidMin), 1);
				_lastReadings[deviceId] = (initTemp, initHumid);
				return (initTemp, initHumid);
			}

			var newTemp = Clamp(last.Temp + (_random.NextDouble() * 1.0 - 0.5), tempMin, tempMax);
			var newHumid = Clamp(last.Humid + (_random.NextDouble() * 3.0 - 1.5), humidMin, humidMax);

			newTemp = Math.Round(newTemp, 1);
			newHumid = Math.Round(newHumid, 1);

			_lastReadings[deviceId] = (newTemp, newHumid);
			return (newTemp, newHumid);
		}

		private double Clamp(double value, double min, double max)
			=> Math.Max(min, Math.Min(max, value));
	}
}