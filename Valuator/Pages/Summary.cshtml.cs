using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Valuator.Pages;

public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;

    private readonly IDatabase _db;

    public SummaryModel(
    ILogger<SummaryModel> logger,
    IConnectionMultiplexer redis)
    {
        _logger = logger;
        _db = redis.GetDatabase();
    }

    public double? Rank { get; set; }
    public double Similarity { get; set; }
    public string TextId { get; set; } = string.Empty;

    public void OnGet(string id)
    {
        _logger.LogDebug(id);
        TextId = id;

        // TODO: (pa1) проинициализировать свойства Rank и Similarity значениями из БД (Redis)
        string rankKey = "RANK-" + id;
        string similarityKey = "SIMILARITY-" + id;

        RedisValue rankValue = _db.StringGet(rankKey);
        Rank = rankValue.HasValue ? (double)rankValue : null;
        Similarity = (double)_db.StringGet(similarityKey);
    }
}
