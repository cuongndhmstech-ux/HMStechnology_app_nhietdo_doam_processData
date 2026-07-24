namespace HMS_NewProject_Temp_Humdity_processdata.Models
{
	public class clsLocationModel
	{
		public string? LocationId { get; set; }

		public string? UserId { get; set; }

		public string? Name { get; set; }

	}

	public class LocationResponse
	{
		public string? LocationId { get; set; }

		public string? UserId { get; set; }

		public string? Name { get; set; }
		public DateTime? CreatedAt { get; set; }

		public List<clsDeviceCacheModel>? Devices { get; set; }
	}

	public class DataResponseForUser()
	{
		public List<LocationResponse> Locations { get; set; } = new();
		public List<clsDeviceCacheModel> DeviceUnassgin { get; set; } = new();

	}
}
