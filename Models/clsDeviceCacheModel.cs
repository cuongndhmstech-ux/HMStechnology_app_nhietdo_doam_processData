namespace HMS_NewProject_Temp_Humdity_processdata.Models
{


	public class clsDeviceCacheModel
	{
		public int? DeviceId { get; set; }

		public string? Imei { get; set; }

		public string? CompanyId { get; set; }

		public string? UserId { get; set; }
		public string? Name { get; set; }

		public string? LocationId { get; set; }

		public DateTime? TimeStamp { get; set; } = DateTime.Now;

		public DeviceStatus status { get; set; }
		public ConnectivityStatus connectivity { get; set; }
		public List<Sensor>? Sensors { get; set; }
		public DateTime? LastUpdate { get; set; } = DateTime.UtcNow;
	}

	public class DeviceRequestModel
	{
		public DeviceQueryType Type { get; set; }
	}

	public enum DeviceQueryType
	{
		GetAll = 1,

	}
}

