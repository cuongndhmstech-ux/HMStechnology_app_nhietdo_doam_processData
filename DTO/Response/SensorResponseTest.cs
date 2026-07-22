namespace HMS_NewProject_Temp_Humdity_processdata.DTO.Response
{
	public class SensorResponseTest
	{
		public string NameSensor { get; set; } = null!;
		public double Temperature { get; set; }
		public double Humidity { get; set; }

		public double? TemperatureMin { get; set; }
		public double? TemperatureMax { get; set; }
		public int? HumidityMin { get; set; }
		public int? HumidityMax { get; set; }
	}
}
