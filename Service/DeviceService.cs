using HMS_NewProject_Temp_Humdity_processdata.Database.Interface;
using HMS_NewProject_Temp_Humdity_processdata.Models;
using HMS_NewProject_Temp_Humdity_processdata.Service.Interface;

namespace HMS_NewProject_Temp_Humdity_processdata.Service
{
	public class DeviceService : IDeviceService
	{
		private readonly ILogger<DeviceService> _logger;
		private readonly IDAODeviceLog _dAODeviceLog;
		private readonly IManagerApiClient _managerApiClient;



		public DeviceService(ILogger<DeviceService> dataLogger, IDAODeviceLog dAODeviceLog, IManagerApiClient managerApiClient)
		{
			_logger = dataLogger;
			_dAODeviceLog = dAODeviceLog;
			_managerApiClient = managerApiClient;
		}

		public async Task ProcessSensorDataAsync(clsDeviceLogModel dto)
		{
			try
			{
				_logger.LogInformation("Bắt đầu xử lý dữ liệu IMEI: {IMEI}", dto.Imei);

				//var device = await _managerApiClient.GetDeviceByIMEIAsync(dto.Imei]]]);
				//if (device == null)
				//{
				//	_logger.LogWarning("Không tìm thấy thiết bị IMEI: {IMEI}", dto.Imei);
				//	return;
				//}

				_logger.LogInformation("Tìm thấy thiết bị: {DeviceName} - IMEI: {IMEI}",
					 dto.Imei);

				await _dAODeviceLog.CreateAsync(dto);

				_logger.LogInformation("Lưu dữ liệu thành công IMEI: {IMEI}", dto.Imei);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi xử lý IMEI: {IMEI}", dto.Imei);
			}
		}

		public async Task<Dictionary<string, clsDeviceLogModel>> GetClsDeviceLogLastUpdateByImeis(IEnumerable<string> imeis)
		{
			var imeiList = imeis?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new List<string>();
			if (imeiList.Count == 0)
				return new Dictionary<string, clsDeviceLogModel>();

			var list = await _dAODeviceLog.GetLatestByImeisAsync(imeiList);
			return list.ToDictionary(x => x.Imei, x => x);
		}

		private clsDeviceLogModel ParseReading(clsDeviceCacheModel dto)
		{

			return new clsDeviceLogModel
			{
				Imei = dto.Imei,
				Sensors = dto.Sensors,
				LastUpdate = dto.LastUpdate
			};
		}


	}
}
