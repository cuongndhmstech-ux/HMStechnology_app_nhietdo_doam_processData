using HMS_NewProject_Temp_Humdity_processdata.Models;

namespace HMS_NewProject_Temp_Humdity_processdata.DTO.Response
{
	public class UserDashboardResponseTest
	{
		public string UserId { get; set; } = null!;
		public string? FullName { get; set; }
		public List<LocationResponseTest> Locations { get; set; } = new();
		public List<DeviceResponseTest> UnassignedDevices { get; set; } = new();
	}
}
