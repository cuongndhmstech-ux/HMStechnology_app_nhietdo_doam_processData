using HMS_NewProject_Temp_Humdity_processdata.Database;
using HMS_NewProject_Temp_Humdity_processdata.Models.Config;
using Microsoft.AspNetCore.SignalR.Client;

namespace HMS_NewProject_Temp_Humdity_processdata.BGroundService
{
	public class DeviceSensorService
	{
		private readonly DeviceSensorQueue _queue;
		private readonly clsAppConfig _config;
		private readonly string _hubUrl;
		private HubConnection? _vehicleHubConnection;
		private readonly ILogger<DeviceSensorService> _logger;
		private readonly MongodbContext _mongoContext;

		public DeviceSensorService(
			DeviceSensorQueue queue,
			clsAppConfig config,
			ILogger<DeviceSensorService> log,
			MongodbContext mongoContext)
		{
			_queue = queue;
			_config = config;
			_hubUrl = _config.SignalR.HubUrlGialap;
			_logger = log;
			_mongoContext = mongoContext;
		}

		public async Task StartAsync()
		{
			try
			{
				_logger.LogInformation("Checking MongoDB connection...");
				bool connected = await _mongoContext.IsConnected();
				if (!connected)
				{
					_logger.LogError("MongoDB connection failed. SignalR client will not start.");
					return;
				}
				_logger.LogInformation("MongoDB connected successfully.");

				_logger.LogInformation("Connecting to SignalR Hub: {HubUrl}", _hubUrl);

				_vehicleHubConnection = new HubConnectionBuilder()
					.WithUrl(_hubUrl)
					.WithAutomaticReconnect(new[]
					{
						TimeSpan.Zero,
						TimeSpan.FromSeconds(2),
						TimeSpan.FromSeconds(5),
						TimeSpan.FromSeconds(10)
					})
					.Build();

				_vehicleHubConnection.On<string, string>(
					"SendMessage",
					(name, message) => DeviceReceive(name, message));

				_vehicleHubConnection.Closed += async (error) =>
				{
					_logger.LogWarning(error, "SignalR disconnected. Retry connecting manually...");
					await ManualReconnectAsync();
				};

				_vehicleHubConnection.Reconnecting += (error) =>
				{
					_logger.LogWarning(error, "SignalR reconnecting...");
					return Task.CompletedTask;
				};

				_vehicleHubConnection.Reconnected += (connectionId) =>
				{
					_logger.LogInformation("SignalR reconnected successfully. ConnectionId={ConnectionId}", connectionId);
					return Task.CompletedTask;
				};

				await _vehicleHubConnection.StartAsync();
				_logger.LogInformation("SignalR connected successfully.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to start SignalR client.");
			}
		}


		// và sự kiện Closed sẽ bắn ra -> cần tự vòng lặp reconnect thủ công ở đây.
		private async Task ManualReconnectAsync()
		{
			if (_vehicleHubConnection == null) return;

			while (_vehicleHubConnection.State == HubConnectionState.Disconnected)
			{
				try
				{
					_logger.LogInformation("Reconnecting to SignalR...");
					await Task.Delay(2000);
					await _vehicleHubConnection.StartAsync();
					_logger.LogInformation("SignalR reconnected successfully (manual).");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Reconnect failed.");
				}
			}
		}

		private async void DeviceReceive(string Name, string jsonData)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(jsonData))
				{
					return;
				}
				await _queue.DeviceSensors.Writer.WriteAsync(jsonData);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi ghi dữ liệu vào queue. Imei/Name={Name}", Name);
			}
		}
	}
}