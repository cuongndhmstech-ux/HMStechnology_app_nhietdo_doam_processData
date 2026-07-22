using HMS_NewProject_Temp_Humdity_processdata.Models;

namespace HMS_NewProject_Temp_Humdity_processdata.Database.Interface
{
	public interface IDAODeviceLog
	{
		//Task<List<clsDeviceLogModel>> GetAllAsync(FilterDefinition<clsDeviceLogModel>? filter = null);

		Task CreateAsync(clsDeviceLogModel device);

		Task<List<clsDeviceLogModel>> GetLatestByImeisAsync(IEnumerable<string> imeis);
		//Task<Dictionary<string, clsDeviceLogModel>> GetLatestTimestampAllAsync();
	}
}
