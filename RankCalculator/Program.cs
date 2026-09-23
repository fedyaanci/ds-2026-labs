using RankCalculator;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);
string redisAddress = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis is required");
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisAddress));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
