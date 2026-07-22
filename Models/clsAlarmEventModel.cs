using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace HMS_NewProject_Temp_Humdity_processdata.Models
{
	public class clsAlarmEventModel
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public string? Id { get; set; }

		public string? IMEI { get; set; }
		//public WarningType Type { get; set; }  // 0: Đột nhập, 1: Báo cháy, 2: Nhiệt độ bất thường
		public DateTime CreatedAt { get; set; }
		public string? Description { get; set; }
	}
}
