using System.Security.Claims;
using HMS_NewProject_Temp_Humdity_processdata.DTO;
using HMS_NewProject_Temp_Humdity_processdata.Models;
using HMS_NewProject_Temp_Humdity_processdata.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS_NewProject_Temp_Humdity_processdata.Controllers
{
	[ApiController]
	[Route("device-online")]
	[Authorize]
	public class DeviceOnlineController : Controller
	{
		private readonly IDeviceRedisService _redis;
		private readonly ILogger<DeviceOnlineController> _logger;

		public DeviceOnlineController(IDeviceRedisService redis, ILogger<DeviceOnlineController> logger)
		{
			_redis = redis;
			_logger = logger;
		}



		[HttpPost("query")]
		public async Task<IActionResult> Query([FromBody] DeviceRequestModel request)
		{
			// Lấy userId từ claim "sub" trong token (khớp với JwtRegisteredClaimNames.Sub lúc generate token)
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			foreach (var claim in User.Claims)
			{
				_logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
			}

			if (string.IsNullOrEmpty(userId))
			{
				return Unauthorized("Không xác định được người dùng từ token.");
			}

			var result = request.Type switch
			{
				DeviceQueryType.GetAll =>
					Ok(new ApiResponse<List<LocationResponse>>
					{
						Success = true,
						Message = "Lấy danh sách thiết bị thành công",
						Data = await _redis.GetLocationsWithDevicesByUserAsync(userId)
					}),
				_ => null // xử lý bên dưới
			};

			if (result is null)
			{
				return BadRequest("Invalid query type");
			}

			return result;
		}
	}
}