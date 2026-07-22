namespace HMS_NewProject_Temp_Humdity_processdata.Models.Config
{
	public class clsAppConfig
	{
		public SignalRConfig? SignalR { get; set; }
	}

	public class SignalRConfig
	{
		public string? HubUrl { get; set; }

		public string? HubUrlGialap { get; set; }
		public string? HubUrlManager { get; set; }

	}
}
