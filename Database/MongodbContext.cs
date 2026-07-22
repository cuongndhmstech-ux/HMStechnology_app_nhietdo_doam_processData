using HMS_NewProject_Temp_Humdity_processdata.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HMS_NewProject_Temp_Humdity_processdata.Database
{
	public class MongodbContext
	{
		protected readonly IMongoDatabase _database;
		private readonly ILogger<MongodbContext> _logger;

		public MongodbContext(IMongoDatabase database, ILogger<MongodbContext> logger)
		{
			_database = database;
			_logger = logger;
		}

		public IMongoDatabase mongoDatabase => _database;

		public IMongoCollection<clsDeviceLogModel> DeviceReadingLogs => _database.GetCollection<clsDeviceLogModel>("DeviceLog");

		//public IMongoCollection<clsAlarmEventModel> AlarmEvents => _database.GetCollection<clsAlarmEventModel>("AlarmEvent");

		public async Task<bool> IsConnected()
		{
			try
			{
				await _database.RunCommandAsync<BsonDocument>(
					new BsonDocument("ping", 1));
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "MongoDB connection error: {Message}", ex.Message);
				return false;
			}
		}
	}
}
