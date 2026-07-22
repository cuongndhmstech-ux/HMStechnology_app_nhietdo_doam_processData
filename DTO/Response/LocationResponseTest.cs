namespace HMS_NewProject_Temp_Humdity_processdata.DTO.Response
{
	public class LocationResponseTest
	{
		public string LocationId { get; set; } = null!;
		public string Name { get; set; } = null!;
		public List<DeviceResponseTest> Devices { get; set; } = new();
	}
}
