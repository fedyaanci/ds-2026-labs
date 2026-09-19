using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    private readonly IDatabase _db;

    public IndexModel(
    ILogger<IndexModel> logger,
    IConnectionMultiplexer redis)
    {
        _logger = logger;
        _db = redis.GetDatabase();
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

        string rankKey = "RANK-" + id;

        // TODO: (pa1) посчитать rank и сохранить в БД (Redis) по ключу rankKey
        int nonAlphabeticCount = 0;

        foreach (char symbol in text)
        {
            bool isLatin =
                (symbol >= 'A' && symbol <= 'Z') ||
                (symbol >= 'a' && symbol <= 'z');

            bool isRussian =
                (symbol >= 'А' && symbol <= 'Я') ||
                (symbol >= 'а' && symbol <= 'я') ||
                symbol == 'Ё' ||
                symbol == 'ё';

            if (!isLatin && !isRussian)
            {
                nonAlphabeticCount++;
            }
        }

        double rank;

        if (text.Length == 0)
        {
            rank = 0;
        }
        else
        {
            rank = (double)nonAlphabeticCount / text.Length;
        }

        _db.StringSet(rankKey, rank);
        //
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

        return Redirect($"summary?id={id}");
    }
}
