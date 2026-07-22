using HMS_NewProject_Temp_Humdity_processdata.Models;

namespace HMS_NewProject_Temp_Humdity_processdata.Service.Interface
{
	public interface IManagerApiClient
	{
		//Task<DeviceInfoDTO?> GetDeviceByIMEIAsync(string imei);
		Task<List<LocationResponse>?> GetListClsDeviceCacheModelAsync();
	}
}
