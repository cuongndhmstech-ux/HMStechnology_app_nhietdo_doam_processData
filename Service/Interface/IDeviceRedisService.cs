using HMS_NewProject_Temp_Humdity_processdata.Models;

namespace HMS_NewProject_Temp_Humdity_processdata.Service.Interface
{
	public interface IDeviceRedisService
	{
		Task SetDeviceAsync(string IMEI, clsDeviceCacheModel deviceCacheModel);

		Task<clsDeviceCacheModel?> GetDeviceAsync(string IMEI);

		Task DeleteDeviceAsync(string IMEI);

		Task<List<clsDeviceCacheModel>> GetAllDevicesAsync();

		Task<DataResponseForUser> GetLocationsWithDevicesByUserAsync(string userId);

		Task SetLocationAsync(LocationResponse location);

		Task<LocationResponse?> GetLocationAsync(string locationId);

		Task AddDeviceToLocation(string locationId, string IMEI);

		Task RemoveDeviceFromLocation(string locationId, string IMEI);

		Task<List<clsDeviceCacheModel>> GetDevicesByLocationAsync(string locationId);

		Task AddLocationToUser(string userId, string locationId);

		Task RemoveLocationFromUser(string userId, string locationId);

		Task<List<LocationResponse>> GetLocationsByUserAsync(string userId);

		Task<List<LocationWithDevicesResponse>> GetFullTreeByUserAsync(string userId);

		Task UpdateSensorDataAsync(string IMEI, List<Sensor>? sensors, DateTime? lastUpdate);

		Task DeleteLocationAsync(string locationId, string userId);
		Task<string?> GetUserIdByImeiAsync(string imei);

		Task AddDeviceToUser(string userId, string imei);

		Task RemoveDeviceFromUser(string userId, string imei);


	}
}
