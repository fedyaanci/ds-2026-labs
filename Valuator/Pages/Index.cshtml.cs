using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShardStore;
using Valuator.Messaging;

namespace Valuator.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    private readonly ShardedRedisStore _store;
    private readonly MessagePublisher _messages;

    public IndexModel(
    ILogger<IndexModel> logger,
    ShardedRedisStore store,
    MessagePublisher messages)
    {
        _logger = logger;
        _store = store;
        _messages = messages;
    }

    public void OnGet()
    {

    }

    public async Task<IActionResult> OnPostAsync(string text, string country)
    {
        _logger.LogDebug(text);

        if (!CountryRegions.TryGetRegion(country, out string region))
        {
            ModelState.AddModelError(nameof(country), "Выберите страну из списка.");
            return Page();
        }

        string id = Guid.NewGuid().ToString();
        await _store.Main.StringSetAsync($"SHARD-{id}", region);
        _logger.LogInformation("LOOKUP: {TextId}, {Region}", id, region);
        ShardContext shard = _store.ForRegion(region);

        string textKey = "TEXT-" + id;
        await shard.Database.StringSetAsync(textKey, text);
        await shard.Database.StringSetAsync("COUNTRY-" + id, country);
        await shard.Database.StringSetAsync(
            "AUTHOR-" + id,
            User.Identity?.Name ?? throw new InvalidOperationException("User is not authenticated"));

        string similarityKey = "SIMILARITY-" + id;

        // TODO: (pa1) посчитать similarity и сохранить в БД (Redis) по ключу similarityKey
        bool textWasAdded = await shard.Database.SetAddAsync("PROCESSED-TEXTS", text);

        double similarity;

        if (textWasAdded)
        {
            similarity = 0;
        }
        else
        {
            similarity = 1;
        }

        await shard.Database.StringSetAsync(similarityKey, similarity);
        _messages.PublishSimilarityCalculated(id, similarity);

        // В сообщении передаётся только ID. Сам текст RankCalculator прочитает из Redis.
        _messages.Publish(id);

        return Redirect($"summary?id={id}");
    }
}
