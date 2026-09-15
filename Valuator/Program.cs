using StackExchange.Redis; //

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        //
        string redisAddress = builder.Configuration.GetConnectionString("Redis")!; // получение адреса

        var redis = ConnectionMultiplexer.Connect(redisAddress); // подключение к redis

        builder.Services.AddSingleton<IConnectionMultiplexer>(redis); // подключение доступно всей приложухе
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

        app.Run();
    }
}
