using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using ShardStore;
using StackExchange.Redis;

namespace Valuator.Pages;

public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;

    private readonly ShardedRedisStore _store;

    public SummaryModel(
    ILogger<SummaryModel> logger,
    ShardedRedisStore store)
    {
        _logger = logger;
        _store = store;
    }

    public double? Rank { get; set; }
    public double Similarity { get; set; }
    public string Country { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string id)
    {
        _logger.LogDebug(id);

        // TODO: (pa1) проинициализировать свойства Rank и Similarity значениями из БД (Redis)
        ShardContext? shard = await _store.LookupAsync(id);
        if (shard is null)
        {
            return NotFound();
        }

        _logger.LogInformation("LOOKUP: {TextId}, {Region}", id, shard.Region);
        string rankKey = "RANK-" + id;
        string similarityKey = "SIMILARITY-" + id;

        RedisValue rankValue = await shard.Database.StringGetAsync(rankKey);
        Rank = rankValue.HasValue ? (double)rankValue : null;
        Similarity = (double)await shard.Database.StringGetAsync(similarityKey);
        Country = (await shard.Database.StringGetAsync("COUNTRY-" + id)).ToString();
        return Page();
    }
}
