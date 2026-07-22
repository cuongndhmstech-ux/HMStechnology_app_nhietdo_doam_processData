using System.Text.Json;
using System.Threading.Channels;
using HMS_NewProject_Temp_Humdity_processdata.Models;
using HMS_NewProject_Temp_Humdity_processdata.Service.Interface;
using HMS_NewProject_Temp_Humdity_processdata.Signalr;
using Microsoft.AspNetCore.SignalR;

namespace HMS_NewProject_Temp_Humdity_processdata.BGroundService
{
	public class DeviceSensorWorker : BackgroundService
	{
		private readonly DeviceSensorService _service;
		private readonly DeviceSensorQueue _queue;
		private readonly ILogger<DeviceSensorWorker> _logger;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly IHubContext<ClsHubFrontendOnline> _Hub;

		public DeviceSensorWorker(DeviceSensorService service, DeviceSensorQueue queue, ILogger<DeviceSensorWorker> logger, IServiceScopeFactory scopeFactory, IHubContext<ClsHubFrontendOnline> Hubs)
		{
			_service = service;
			_queue = queue;
			_logger = logger;
			_scopeFactory = scopeFactory;
			_Hub = Hubs;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await Task.Delay(1000, stoppingToken);
			await _service.StartAsync();

			try
			{
				await ProcessQueue(_queue.DeviceSensors.Reader, stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				//  Shutdown bình thường
			}
			catch (Exception ex)
			{
				_logger.LogCritical(ex, "DeviceSensorWorker crashed");
				throw;
			}
		}

		private async Task ProcessQueue(ChannelReader<string> reader, CancellationToken stoppingToken)
		{
			using var scope = _scopeFactory.CreateScope();
			var _redis = scope.ServiceProvider.GetRequiredService<IDeviceRedisService>();
			var _deviceSerivce = scope.ServiceProvider.GetRequiredService<IDeviceService>();

			await foreach (var json in reader.ReadAllAsync(stoppingToken))
			{
				try
				{
					if (string.IsNullOrWhiteSpace(json)) continue;
					_logger.LogInformation("Raw JSON: {Json}", json);

					var data = JsonSerializer.Deserialize<clsDeviceLogModel>(json,
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

					if (data == null || string.IsNullOrWhiteSpace(data.Imei))
					{
						_logger.LogWarning("Dữ liệu sensor không hợp lệ hoặc thiếu IMEI. Json={Json}", json);
						continue;
					}

					// Xử lý nghiệp vụ (lưu DB, tính toán, ...)
					await ProcessSensorData(data, _deviceSerivce);

					// Cảnh báo nhiệt độ (đang comment, khi cần bật lại)
					//if (double.TryParse(data.Temperature, out double temp) && temp > 40)
					//{
					//	var _alarmSerivce = scope.ServiceProvider.GetRequiredService<IAlarmEventService>();
					//	await _alarmSerivce.SaveAbnormalTemperatureAlarm(data.IMEI, "cảnh báo nhiệt độ > 40");
					//}

					//===================== Cập nhật nhiệt độ/độ ẩm liên tục lên Redis =====================//
					await _redis.UpdateSensorDataAsync(data.Imei, data.Sensors, data.LastUpdate);

					var userId = await _redis.GetUserIdByImeiAsync(data.Imei);
					if (userId == null)
					{
						_logger.LogWarning("Không tìm thấy chủ sở hữu (userId) cho IMEI={Imei}");
						continue;
					}

					// 4. CHỈ gửi cho đúng group của user đó, không broadcast toàn bộ
					await _Hub.Clients
						.Group(ClsHubFrontendOnline.GroupName(userId))
						.SendAsync("DeviceUpdated", data.Imei, json, stoppingToken);

				}
				catch (JsonException ex)
				{
					_logger.LogWarning("Lỗi parse JSON: {Message}", ex.Message);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Lỗi xử lý sensor data");
				}
			}
		}

		private async Task ProcessSensorData(clsDeviceLogModel data, IDeviceService _deviceSerivce)
		{
			try
			{
				await _deviceSerivce.ProcessSensorDataAsync(data);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi ProcessSensorData IMEI");
			}
		}
	}
}
