using Microsoft.AspNetCore.SignalR;

namespace HMS_NewProject_Temp_Humdity_processdata.Signalr
{
	public class ClsHubManagerEvents : Hub
	{
		private readonly ILogger<ClsHubManagerEvents> _logger;

		public ClsHubManagerEvents(ILogger<ClsHubManagerEvents> logger)
		{
			_logger = logger;
		}

		public override async Task OnConnectedAsync()
		{
			_logger.LogInformation("[ManagerEvents] Client connected: {ConnId}", Context.ConnectionId);
			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			_logger.LogWarning("[ManagerEvents] Client disconnected: {ConnId}", Context.ConnectionId);
			await base.OnDisconnectedAsync(exception);
		}
	}
}
