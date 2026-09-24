using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using StackExchange.Redis;

namespace Valuator.Security;

public sealed partial class UserStore : IDisposable
{
    private const int Iterations = 120_000;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public UserStore(string address)
    {
        _redis = ConnectionMultiplexer.Connect(address);
        _db = _redis.GetDatabase();
    }

    public static bool IsValidLogin(string login) => LoginPattern().IsMatch(login);

    public async Task<bool> RegisterAsync(string login, string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = HashPassword(password, salt);
        var record = new UserRecord(
            login,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));

        return await _db.StringSetAsync(
            Key(login),
            JsonSerializer.Serialize(record),
            when: When.NotExists);
    }

    public async Task<bool> ValidateAsync(string login, string password)
    {
        RedisValue value = await _db.StringGetAsync(Key(login));
        if (!value.HasValue)
        {
            return false;
        }

        UserRecord? record = JsonSerializer.Deserialize<UserRecord>(value.ToString());
        if (record is null)
        {
            return false;
        }

        byte[] expected = Convert.FromBase64String(record.Hash);
        byte[] actual = HashPassword(password, Convert.FromBase64String(record.Salt));
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public async Task<string?> GetCanonicalLoginAsync(string login)
    {
        RedisValue value = await _db.StringGetAsync(Key(login));
        UserRecord? record = value.HasValue
            ? JsonSerializer.Deserialize<UserRecord>(value.ToString())
            : null;
        return record?.Login;
    }

    private static byte[] HashPassword(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            32);

    private static string Key(string login) => $"USER-{login.Trim().ToLowerInvariant()}";

    public void Dispose() => _redis.Dispose();

    [GeneratedRegex("^[A-Za-z0-9_.-]{3,50}$")]
    private static partial Regex LoginPattern();

    private sealed record UserRecord(string Login, string Salt, string Hash);
}
