using StackExchange.Redis;

namespace ShardStore;

public sealed record ShardContext(string Region, IDatabase Database);

public sealed class ShardedRedisStore : IDisposable
{
    private readonly IConnectionMultiplexer _main;
    private readonly Dictionary<string, IConnectionMultiplexer> _shards;

    public ShardedRedisStore(
        string mainAddress,
        string ruAddress,
        string euAddress,
        string asiaAddress)
    {
        _main = ConnectionMultiplexer.Connect(mainAddress);
        _shards = new Dictionary<string, IConnectionMultiplexer>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["RU"] = ConnectionMultiplexer.Connect(ruAddress),
            ["EU"] = ConnectionMultiplexer.Connect(euAddress),
            ["ASIA"] = ConnectionMultiplexer.Connect(asiaAddress)
        };
    }

    public IDatabase Main => _main.GetDatabase();

    public ShardContext ForRegion(string region)
    {
        if (!_shards.TryGetValue(region, out IConnectionMultiplexer? connection))
        {
            throw new ArgumentOutOfRangeException(nameof(region), region, "Unknown region");
        }

        return new ShardContext(region.ToUpperInvariant(), connection.GetDatabase());
    }

    public async Task<ShardContext?> LookupAsync(string textId)
    {
        RedisValue regionValue = await Main.StringGetAsync($"SHARD-{textId}");
        return regionValue.HasValue ? ForRegion(regionValue.ToString()) : null;
    }

    public void Dispose()
    {
        _main.Dispose();
        foreach (IConnectionMultiplexer connection in _shards.Values)
        {
            connection.Dispose();
        }
    }
}

public static class CountryRegions
{
    private static readonly IReadOnlyDictionary<string, string> Values =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Russia"] = "RU",
            ["France"] = "EU",
            ["Germany"] = "EU",
            ["UAE"] = "ASIA",
            ["India"] = "ASIA"
        };

    public static bool TryGetRegion(string? country, out string region) =>
        Values.TryGetValue(country ?? string.Empty, out region!);
}
