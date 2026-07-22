namespace HMS_NewProject_Temp_Humdity_processdata.DTO.Response
{
	public class LocationSummaryResponseTest
	{
		public string LocationId { get; set; } = null!;
		public string Name { get; set; } = null!;
		public int TotalDevices { get; set; }
		public int OnlineCount { get; set; }
		public int OfflineCount { get; set; }
		public int WarningCount { get; set; }
	}
}
