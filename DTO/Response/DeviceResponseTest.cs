namespace HMS_NewProject_Temp_Humdity_processdata.DTO.Response
{
	public class DeviceResponseTest
	{
		public string Id { get; set; } = null!;
		public string Name { get; set; } = null!;
		public string RoomId { get; set; } = null!;
		public string? RoomName { get; set; }
		public string? SerialNumber { get; set; }
		public DeviceStatus Status { get; set; }
		public ConnectivityStatus Connectivity { get; set; }
		public double? CurrentTemperature { get; set; }
		public double? CurrentHumidity { get; set; }
		public ThresholdResponseTest? Threshold { get; set; }
		public DateTime? LastUpdatedAt { get; set; }
	}
}
