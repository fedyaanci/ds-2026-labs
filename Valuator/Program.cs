using StackExchange.Redis; //
using Valuator.Messaging;
using Valuator.Realtime;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddSignalR();
        //
        string redisAddress = builder.Configuration.GetConnectionString("Redis")!; // получение адреса

        var redis = ConnectionMultiplexer.Connect(redisAddress); // подключение к redis

        builder.Services.AddSingleton<IConnectionMultiplexer>(redis); // подключение доступно всей приложухе
        builder.Services.AddSingleton<MessagePublisher>();
        builder.Services.AddHostedService<RankEventsNotifier>();
        //

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();
        app.MapHub<ValuationHub>("/valuationHub");

        app.Run();
    }
}
