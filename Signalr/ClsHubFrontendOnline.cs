using Microsoft.AspNetCore.SignalR;

namespace HMS_NewProject_Temp_Humdity_processdata.Signalr
{

	public class ClsHubFrontendOnline : Hub
	{
		private readonly ILogger<ClsHubFrontendOnline> _logger;

		public ClsHubFrontendOnline(ILogger<ClsHubFrontendOnline> logger)
		{
			_logger = logger;
		}

		public override async Task OnConnectedAsync()
		{
			var userId = GetUserIdFromToken();

			if (string.IsNullOrEmpty(userId))
			{
				_logger.LogWarning("Kết nối không có userId hợp lệ trong token. ConnectionId={ConnId}", Context.ConnectionId);
				Context.Abort(); // ngắt kết nối luôn nếu không xác định được user
				return;
			}

			// Tự động join group ngay khi kết nối — KHÔNG cần client tự gọi JoinUserGroup nữa
			await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));

			_logger.LogInformation("User {UserId} đã kết nối và join group. ConnectionId={ConnId}",
				userId, Context.ConnectionId);

			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			var userId = GetUserIdFromToken();
			_logger.LogInformation("User {UserId} đã ngắt kết nối. ConnectionId={ConnId}",
				userId, Context.ConnectionId);

			// SignalR tự dọn connection khỏi group khi disconnect, không cần remove thủ công
			await base.OnDisconnectedAsync(exception);
		}

		private string? GetUserIdFromToken()
		{
			// Khớp với claim bạn dùng lúc phát hành token: JwtRegisteredClaimNames.Sub
			return Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
		}

		public static string GroupName(string userId) => $"user:{userId}";
	}
}