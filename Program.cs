using System.IdentityModel.Tokens.Jwt;
using System.Text;
using HMS_NewProject_Temp_Humdity_processdata.BGroundService;
using HMS_NewProject_Temp_Humdity_processdata.Database;
using HMS_NewProject_Temp_Humdity_processdata.Database.Interface;
using HMS_NewProject_Temp_Humdity_processdata.Models.Config;
using HMS_NewProject_Temp_Humdity_processdata.Service;
using HMS_NewProject_Temp_Humdity_processdata.Service.Interface;
using HMS_NewProject_Temp_Humdity_processdata.Signalr;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var port = builder.Configuration.GetValue<int>("PortService:Port");

builder.WebHost.ConfigureKestrel(options =>
{
	options.ListenAnyIP(port);
});

// Add services to the container.

var mongoConnectionString = builder.Configuration.GetSection("MongoDB:ConnectionString").Value;
var mongoDatabaseName = builder.Configuration.GetSection("MongoDB:DatabaseName").Value;

var mongoClient = new MongoClient(mongoConnectionString);
var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseName);
builder.Services.AddCors(options =>
{
	options.AddPolicy("SignalRPolicy", policy =>
	{
		policy.WithOrigins("https://domain-cua-server-kia.com") // domain server sẽ invoke vào
			  .AllowAnyHeader()
			  .AllowAnyMethod()
			  .AllowCredentials(); // SignalR cần dòng này
	});
});
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);

builder.Services.AddSingleton<MongodbContext>();
builder.Services.AddSingleton<IDAODeviceLog, DAODeviceLog>();
builder.Services.AddSingleton<DeviceSensorQueue>();
var appConfig = builder.Configuration.Get<clsAppConfig>();
builder.Services.AddSingleton(appConfig);


builder.Services.AddSingleton<DeviceSensorService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IDeviceRedisService, DeviceRedisService>();

//builder.Services.AddSingleton<IHubDeviceMonitor, HubDeviceMonitor>();
//builder.Services.AddSingleton<IDAOAlarmEvent, DAOAlarmEvent>();

// BackgroundService
builder.Services.AddHostedService<DeviceSensorWorker>();
builder.Services.AddHostedService<DeviceCacheManager>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
if (builder.Environment.IsDevelopment())
{
	builder.Host.UseSerilog((context, services, configuration) =>
	{
		configuration.ReadFrom.Configuration(context.Configuration);
	});
}

builder.Services.AddHttpClient<IManagerApiClient, ManagerApiClient>(client =>
{
	client.BaseAddress = new Uri(builder.Configuration["ManagerApi:BaseUrl"]!);

	client.DefaultRequestHeaders.Add(
		"X-Api-Key",
		builder.Configuration["InternalApi:ApiKey"]);
});

// Redis-------------------------------------------

builder.Services.AddSingleton<IConnectionMultiplexer>(
	ConnectionMultiplexer.Connect(builder.Configuration["Redis:Connection"]!));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = builder.Configuration["Jwt:Issuer"],
			ValidAudience = builder.Configuration["Jwt:Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
			NameClaimType = JwtRegisteredClaimNames.Sub
		};

		options.Events = new JwtBearerEvents
		{
			OnMessageReceived = context =>
			{
				var accessToken = context.Request.Query["access_token"];
				var path = context.HttpContext.Request.Path;

				if (!string.IsNullOrEmpty(accessToken) &&
					path.StartsWithSegments("/hubs"))
				{
					context.Token = accessToken;
				}
				return Task.CompletedTask;
			},

			// THÊM MỚI - bắt buộc để debug 401
			OnAuthenticationFailed = context =>
			{
				Console.WriteLine($"[JWT FAILED] {context.Exception.GetType().Name}: {context.Exception.Message}");
				return Task.CompletedTask;
			},
			OnChallenge = context =>
			{
				Console.WriteLine($"[JWT CHALLENGE] Error={context.Error}, Description={context.ErrorDescription}");
				return Task.CompletedTask;
			},
			OnTokenValidated = context =>
			{
				Console.WriteLine("[JWT OK] Token hợp lệ.");
				return Task.CompletedTask;
			}
		};
	});
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}
app.UseCors("SignalRPolicy");


app.UseAuthentication(); // BẮT BUỘC ở trên
app.UseAuthorization();

app.MapHub<ClsHubFrontendOnline>("/hubs/frontend-online");
app.MapHub<ClsHubManagerEvents>("/hubs/manager-events");  // hub mới, KHÔNG có [Authorize]

app.MapControllers();

app.Run();
