using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HMS_NewProject_Temp_Humdity_processdata.Models
{
	public class clsDeviceLogModel
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public string? Id { get; set; }
		[JsonPropertyName("a1")] public required string Imei { get; set; }
		[JsonPropertyName("a2")] public required List<Sensor> Sensors { get; set; }
		[JsonPropertyName("a3")] public DateTime? LastUpdate { get; set; } = DateTime.UtcNow;

	}


	public class Sensor
	{
		public string? humidity { get; set; }

		public string? temperature { get; set; }
	}
}
