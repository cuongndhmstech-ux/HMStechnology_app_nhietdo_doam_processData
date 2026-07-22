using System.Text.Json;
using HMS_NewProject_Temp_Humdity_processdata.DTO;
using HMS_NewProject_Temp_Humdity_processdata.Models;
using HMS_NewProject_Temp_Humdity_processdata.Service.Interface;

namespace HMS_NewProject_Temp_Humdity_processdata.Service
{
	public class ManagerApiClient : IManagerApiClient
	{
		private readonly HttpClient _httpClient;
		private readonly ILogger<ManagerApiClient> _logger;

		public ManagerApiClient(HttpClient httpClient, ILogger<ManagerApiClient> logger)
		{
			_httpClient = httpClient;
			_logger = logger;
		}


		public async Task<List<LocationResponse>?> GetListClsDeviceCacheModelAsync()
		{
			try
			{
				_logger.LogInformation("lấy dữ liệu cho redis test");
				var response = await _httpClient.PostAsJsonAsync("device/query", new { Type = 3 });
				_logger.LogInformation("Status: {StatusCode}", response.StatusCode);

				var json = await response.Content.ReadAsStringAsync();
				_logger.LogInformation("Raw: {Raw}", json);

				if (!response.IsSuccessStatusCode) return null;
				// lấy qua bọc
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var result = JsonSerializer.Deserialize<ApiResponse<List<LocationResponse>>>(json, options);

				_logger.LogInformation("Deserialize được {Count} thiết bị", result?.Data?.Count);
				return result?.Data;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi gọi Manager API lấy danh sách");

				return null;
			}
		}

	}
}
