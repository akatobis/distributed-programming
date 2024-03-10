using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private IDatabase _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IndexModel> _logger;

    private const string BeginTextKey = "TEXT-";
	
    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
        _db = _redis.GetDatabase();
    }

    public void OnGet()
    {

    }

    private double CalcRang(string text)
    {
        if (text == null)
        {
            return 0;
        }
        return text.Count(c => !char.IsLetter(c)) / (double)text.Length;
    }

    private int CalcSimilarity(string text)
    {
        var keys = _redis.GetServer(_redis.GetEndPoints().First()).Keys();
        var keysSimilarity = keys.Where(_ => _.ToString().StartsWith(BeginTextKey));
        var isSimilarity = keysSimilarity.Any(_ => _db.StringGet(_.ToString()) == text);
        return isSimilarity ? 1 : 0;
    }

    public IActionResult OnPost(string text)
    {
        _logger.LogDebug(text);

        string id = Guid.NewGuid().ToString();

        string similarityKey = "SIMILARITY-" + id;
        var similarity = CalcSimilarity(text);
        _db.StringSet(similarityKey, similarity);
        
        string textKey = BeginTextKey + id;
        _db.StringSet(textKey, text);

        string rankKey = "RANK-" + id;
        var rang = CalcRang(text);
        _db.StringSet(rankKey, rang);

        return Redirect($"summary?id={id}");
    }
}
