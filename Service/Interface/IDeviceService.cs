using HMS_NewProject_Temp_Humdity_processdata.Models;

namespace HMS_NewProject_Temp_Humdity_processdata.Service.Interface
{
	public interface IDeviceService
	{
		Task ProcessSensorDataAsync(clsDeviceLogModel dto);

		Task<Dictionary<string, clsDeviceLogModel>> GetClsDeviceLogLastUpdateByImeis(IEnumerable<string> imeis);
		//Task<Dictionary<string, clsDeviceLogModel?>> GetLatestTimestampDevices();
	}
}
