using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HMS_NewProject_Temp_Humdity_processdata.Models
{
	public class clsDeviceLogModel
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public string? Id { get; set; }
		public required string Imei { get; set; }
		public required List<Sensor> Sensors { get; set; }
		public DeviceStatus status { get; set; }
		public ConnectivityStatus connectivity { get; set; }
		public DateTime? LastUpdate { get; set; } = DateTime.UtcNow;
		public double Threshold { get; set; }

	}
	public enum DeviceStatus { online, offline, unknown }

	public enum ConnectivityStatus
	{
		strong, // 4G tốt
		medium, // 4G trung bình
		weak, // 4G yếu
		none, // Không kết nối
	}
	public class Sensor
	{

		public string? NameSensor { get; set; }

		public string? humidity { get; set; }

		public string? temperature { get; set; }

	}


}
