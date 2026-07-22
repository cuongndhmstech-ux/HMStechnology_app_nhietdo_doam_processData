using HMS_NewProject_Temp_Humdity_processdata.Services;
using Microsoft.AspNetCore.Mvc;

namespace HMS_NewProject_Temp_Humdity_processdata.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class DashboardController : ControllerBase
	{
		private readonly DashboardMockService _service;

		public DashboardController(DashboardMockService service)
		{
			_service = service;
		}

		// GET: api/dashboard/U000001/rooms
		// Trả về List<RoomEntity>
		[HttpGet("{userId}/rooms")]
		public async Task<IActionResult> GetRooms(string userId)
		{
			var result = await _service.GetRoomsAsync(userId);
			if (result == null)
				return NotFound(new { message = $"Không tìm thấy user '{userId}'" });

			return Ok(result);
		}

		// GET: api/dashboard/U000001/devices
		// GET: api/dashboard/U000001/devices?roomId=L000001
		// GET: api/dashboard/U000001/devices?roomId=unassigned
		// Trả về List<DeviceEntity>
		[HttpGet("{userId}/devices")]
		public async Task<IActionResult> GetDevices(string userId, [FromQuery] string? roomId)
		{
			var result = await _service.GetDevicesAsync(userId, roomId);
			if (result == null)
			{
				var message = string.IsNullOrWhiteSpace(roomId)
					? $"Không tìm thấy user '{userId}'"
					: $"Không tìm thấy phòng '{roomId}' của user '{userId}'";

				return NotFound(new { message });
			}

			return Ok(result);
		}
	}
}