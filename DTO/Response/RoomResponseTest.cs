namespace HMS_NewProject_Temp_Humdity_processdata.DTO.Response
{
	public class RoomResponseTest
	{
		public string Id { get; set; } = null!;
		public string Name { get; set; } = null!;
		public string? Description { get; set; }
		public int TotalDevices { get; set; }
		public int OnlineDevices { get; set; }
		public int AlertCount { get; set; }
		public DateTime CreatedAt { get; set; }
        public List<DeviceResponseTest> Devices { get; set; } = new();
    }
}