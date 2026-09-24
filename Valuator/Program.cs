using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using ShardStore;
using Valuator.Messaging;
using Valuator.Security;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        string? keyDirectory = builder.Configuration["DATA_PROTECTION_KEYS"];
        IDataProtectionBuilder dataProtection = builder.Services
            .AddDataProtection()
            .SetApplicationName("Valuator");
        if (!string.IsNullOrWhiteSpace(keyDirectory))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
        }

        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Login";
                options.AccessDeniedPath = "/Denied";
                options.Cookie.Name = "Valuator.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });
        builder.Services.AddAuthorization();
        //
        builder.Services.AddSingleton(new ShardedRedisStore(
            builder.Configuration["DB_MAIN"] ?? "localhost:6000",
            builder.Configuration["DB_RU"] ?? "localhost:6001",
            builder.Configuration["DB_EU"] ?? "localhost:6002",
            builder.Configuration["DB_ASIA"] ?? "localhost:6003"));
        builder.Services.AddSingleton(new UserStore(
            builder.Configuration["DB_AUTH"] ?? "localhost:6004"));
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

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}
