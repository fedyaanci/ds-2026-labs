using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace Valuator.Realtime;

public sealed class ValuationHub : Hub
{
    private readonly IDatabase _db;

    public ValuationHub(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task JoinText(string textId)
    {
        string groupName = GroupName(textId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        // Закрывает гонку, если расчет завершился до подключения браузера.
        RedisValue rankValue = await _db.StringGetAsync($"RANK-{textId}");
        if (rankValue.HasValue)
        {
            await Clients.Caller.SendAsync(
                "rankCalculated", textId, (double)rankValue);
        }
    }

    public static string GroupName(string textId) => $"text:{textId}";
}
