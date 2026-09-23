using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using Valuator.Messaging;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    private readonly IDatabase _db;
    private readonly MessagePublisher _messages;

    public IndexModel(
    ILogger<IndexModel> logger,
    IConnectionMultiplexer redis,
    MessagePublisher messages)
    {
        _logger = logger;
        _db = redis.GetDatabase();
        _messages = messages;
    }

    public void OnGet()
    {

    }

    public IActionResult OnPost(string text)
    {
        _logger.LogDebug(text);

        string id = Guid.NewGuid().ToString();

        string textKey = "TEXT-" + id;
        _db.StringSet(textKey, text); // TODO: (pa1) сохранить в БД (Redis) text по ключу textKey

        string similarityKey = "SIMILARITY-" + id;

        // TODO: (pa1) посчитать similarity и сохранить в БД (Redis) по ключу similarityKey
        bool textWasAdded = _db.SetAdd("PROCESSED-TEXTS", text);

        double similarity;

        if (textWasAdded)
        {
            similarity = 0;
        }
        else
        {
            similarity = 1;
        }

        _db.StringSet(similarityKey, similarity);
        _messages.PublishSimilarityCalculated(id, similarity);

        // В сообщении передаётся только ID. Сам текст RankCalculator прочитает из Redis.
        _messages.Publish(id);

        return Redirect($"summary?id={id}");
    }
}
