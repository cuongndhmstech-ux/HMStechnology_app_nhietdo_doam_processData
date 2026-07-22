using HMS_NewProject_Temp_Humdity_processdata.Database.Interface;
using HMS_NewProject_Temp_Humdity_processdata.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HMS_NewProject_Temp_Humdity_processdata.Database
{
	public class DAODeviceLog : IDAODeviceLog
	{
		private readonly MongodbContext _mongo;

		public DAODeviceLog(MongodbContext mongoService)
		{
			_mongo = mongoService;
		}

		public async Task<List<clsDeviceLogModel>> GetAllAsync(FilterDefinition<clsDeviceLogModel>? filter = null)
		{
			filter ??= FilterDefinition<clsDeviceLogModel>.Empty;
			return await _mongo.DeviceReadingLogs.Find(filter).ToListAsync();
		}
		public async Task CreateAsync(clsDeviceLogModel device)
		{
			try
			{
				await _mongo.DeviceReadingLogs.InsertOneAsync(device);
			}
			catch (MongoException)
			{
				throw;
			}
		}
		public async Task<List<clsDeviceLogModel>> GetLatestByImeisAsync(IEnumerable<string> imeis)
		{
			try
			{
				var imeiList = imeis.Distinct().ToList();
				if (imeiList.Count == 0)
					return new List<clsDeviceLogModel>();

				var pipeline = new[]
				{
				new BsonDocument("$match", new BsonDocument(
					"a1", new BsonDocument("$in", new BsonArray(imeiList)))),

				new BsonDocument("$sort", new BsonDocument("a3", -1)), // LastUpdate giảm dần

                new BsonDocument("$group", new BsonDocument
				{
					{ "_id", "$a1" },
					{ "doc", new BsonDocument("$first", "$$ROOT") }
				}),

				new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$doc"))
			};

				return await _mongo.DeviceReadingLogs
					.Aggregate<clsDeviceLogModel>(pipeline)
					.ToListAsync();
			}
			catch (MongoException)
			{
				throw;
			}
		}
	}
}
