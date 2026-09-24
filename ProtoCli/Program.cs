using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

return await ProtoCliApp.RunAsync(args);

internal static class ProtoCliApp
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        string baseUrl = Environment.GetEnvironmentVariable("PROTOKEY_URL")
            ?? "http://127.0.0.1:7777";
        using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "set" => await SetAsync(client, args),
                "get" => await GetAsync(client, args),
                "keys" => await KeysAsync(client, args),
                _ => UnknownCommand()
            };
        }
        catch (HttpRequestException exception)
        {
            Console.Error.WriteLine($"HTTP error: {exception.Message}");
            return 2;
        }
    }

    private static async Task<int> SetAsync(HttpClient client, string[] args)
    {
        if (args.Length != 3 ||
            !int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            Console.Error.WriteLine("Usage: set <key> <int32-value>");
            return 1;
        }

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/set",
            new { key = args[1], value });
        return await PrintResultAsync(response, "OK");
    }

    private static async Task<int> GetAsync(HttpClient client, string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: get <key>");
            return 1;
        }

        HttpResponseMessage response = await client.GetAsync(
            $"/get/{Uri.EscapeDataString(args[1])}");
        if (!response.IsSuccessStatusCode)
        {
            return await PrintErrorAsync(response);
        }

        int value = await response.Content.ReadFromJsonAsync<int>();
        Console.WriteLine(value.ToString(CultureInfo.InvariantCulture));
        return 0;
    }

    private static async Task<int> KeysAsync(HttpClient client, string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: keys <prefix>");
            return 1;
        }

        HttpResponseMessage response = await client.GetAsync(
            $"/keys?prefix={Uri.EscapeDataString(args[1])}");
        if (!response.IsSuccessStatusCode)
        {
            return await PrintErrorAsync(response);
        }

        string[] keys = await response.Content.ReadFromJsonAsync<string[]>() ?? [];
        foreach (string key in keys)
        {
            Console.WriteLine(key);
        }

        return 0;
    }

    private static async Task<int> PrintResultAsync(
        HttpResponseMessage response,
        string successText)
    {
        if (!response.IsSuccessStatusCode)
        {
            return await PrintErrorAsync(response);
        }

        Console.WriteLine(successText);
        return 0;
    }

    private static async Task<int> PrintErrorAsync(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        string message;
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            message = document.RootElement.TryGetProperty("error", out JsonElement error)
                ? error.GetString() ?? body
                : body;
        }
        catch (JsonException)
        {
            message = body;
        }

        Console.Error.WriteLine($"{(int)response.StatusCode}: {message}");
        return 2;
    }

    private static int UnknownCommand()
    {
        Console.Error.WriteLine("Unknown command.");
        PrintUsage();
        return 1;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("ProtoCli commands:");
        Console.WriteLine("  set <key> <value>");
        Console.WriteLine("  get <key>");
        Console.WriteLine("  keys <prefix>");
    }
}
