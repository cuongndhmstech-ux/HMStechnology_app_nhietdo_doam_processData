using System.Text.Json.Serialization;

namespace HMS_NewProject_Temp_Humdity_processdata.DTO.Response
{
	[JsonConverter(typeof(JsonStringEnumConverter<DeviceStatus>))]
	public enum DeviceStatus
	{
		normal,
		warning,
		maintenance
	}
	[JsonConverter(typeof(JsonStringEnumConverter<ConnectivityStatus>))]
	public enum ConnectivityStatus
	{
		online,
		offline,
		unstable
	}
}