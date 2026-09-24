using ProtoKey.Storage;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://127.0.0.1:7777");

builder.Services.AddSingleton<StoreCommandQueue>();
builder.Services.AddSingleton<PersistenceQueue>();
builder.Services.AddSingleton<CommandLog>();
builder.Services.AddSingleton<StoreDispatcher>();
builder.Services.AddHostedService<StoreWorker>();
builder.Services.AddHostedService<PersistenceWorker>();

var app = builder.Build();

app.MapPost("/set", async (
    SetRequest request,
    StoreDispatcher store,
    CancellationToken cancellationToken) =>
{
    if (!KeyValidator.IsValidKey(request.Key))
    {
        return Results.BadRequest(new
        {
            error = "Key must contain 1-1000 characters: a-z, A-Z, 0-9, _, - or ."
        });
    }

    await store.SetAsync(request.Key!, request.Value, cancellationToken);
    return Results.Ok(new { request.Key, request.Value });
});

app.MapGet("/get/{key}", async (
    string key,
    StoreDispatcher store,
    CancellationToken cancellationToken) =>
{
    if (!KeyValidator.IsValidKey(key))
    {
        return Results.BadRequest(new { error = "Invalid key." });
    }

    int value = await store.GetAsync(key, cancellationToken);
    return Results.Ok(value);
});

app.MapGet("/keys", async (
    string? prefix,
    StoreDispatcher store,
    CancellationToken cancellationToken) =>
{
    prefix ??= string.Empty;
    if (!KeyValidator.IsValidPrefix(prefix))
    {
        return Results.BadRequest(new { error = "Invalid prefix." });
    }

    IReadOnlyList<string> keys = await store.KeysAsync(prefix, cancellationToken);
    return Results.Ok(keys);
});

app.MapGet("/", () => Results.Ok(new
{
    service = "ProtoKey",
    commands = new[] { "POST /set", "GET /get/{key}", "GET /keys?prefix=" }
}));

app.Run();

public sealed record SetRequest(string? Key, int Value);

public partial class Program;
