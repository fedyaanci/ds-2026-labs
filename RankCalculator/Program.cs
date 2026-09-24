using RankCalculator;
using ShardStore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(new ShardedRedisStore(
    builder.Configuration["DB_MAIN"] ?? "localhost:6000",
    builder.Configuration["DB_RU"] ?? "localhost:6001",
    builder.Configuration["DB_EU"] ?? "localhost:6002",
    builder.Configuration["DB_ASIA"] ?? "localhost:6003"));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
