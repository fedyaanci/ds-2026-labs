using ShardStore;
using Valuator.Messaging;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        //
        builder.Services.AddSingleton(new ShardedRedisStore(
            builder.Configuration["DB_MAIN"] ?? "localhost:6000",
            builder.Configuration["DB_RU"] ?? "localhost:6001",
            builder.Configuration["DB_EU"] ?? "localhost:6002",
            builder.Configuration["DB_ASIA"] ?? "localhost:6003"));
        builder.Services.AddSingleton<MessagePublisher>();
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
