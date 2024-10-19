using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;
using NATS.Client;
using Newtonsoft.Json;

namespace Valuator.Pages;

public class MessageModel
{
    public string? Id { get; set; }
    public double Data { get; set; }
}

public class IndexModel : PageModel
{
    private IDatabase _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IndexModel> _logger;
    private readonly IConnection _natsConnection;
    
    private const string NatsUrlConnection = "127.0.0.1:4222";

    private const string SimilarityCalculatedSubject = "SimilarityCalculated";
    private const string BeginTextKey = "TEXT-";
	
    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
        _db = _redis.GetDatabase();
        
        try
        {
            Options options = ConnectionFactory.GetDefaultOptions();
            options.Url = NatsUrlConnection;

            _natsConnection = new ConnectionFactory().CreateConnection(options);
        }
        catch (Exception e)
        {
            _logger.LogError($"Error when trying to connect to NATS {e.Message}");
        }
    }

    public void OnGet()
    {

    }

    private int CalcSimilarity(string id, string text, IDatabase db)
    {
        var keys = db.Multiplexer.GetServer(db.Multiplexer.GetEndPoints().First()).Keys();
        var keysSimilarity = keys.Where(_ => _.ToString().StartsWith(BeginTextKey));
        var isSimilarity = keysSimilarity.Any(_ => db.StringGet(_.ToString()) == text);
        return isSimilarity ? 1 : 0;
    }

    public IActionResult OnPost(string text, string region)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Redirect($"/");
        }
        
        _logger.LogDebug(text);
        var id = Guid.NewGuid().ToString();
        Console.WriteLine($"LOOKUP: {id}, {region}.");
        _db.StringSet(id, region);
        
        string? redisConnection = Environment.GetEnvironmentVariable($"DB_{region}");
        if (redisConnection == null) return Redirect($"/");
        IDatabase regionDb = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(redisConnection)).GetDatabase();

        //TEXT
        var textKey = "TEXT-" + id;
        regionDb.StringSet(textKey, text);

        //RANK
        var messageObject = new
        {
            Id = id,
        };
        var textMessage = JsonConvert.SerializeObject(messageObject);
        var data = Encoding.UTF8.GetBytes(textMessage);
        _natsConnection.Publish("text.processing", data);

        //SIMILARITY
        var similarityKey = "SIMILARITY-" + id;
        var similarity = CalcSimilarity(id, text, regionDb);
        regionDb.StringSet(similarityKey, similarity.ToString());
        
        var similarityMessageObject = new MessageModel()
        {
            Id = $"TEXT-{id}",
            Data = similarity
        };
        var similarityMessageString = JsonConvert.SerializeObject(similarityMessageObject);
        var similarityMessageData = Encoding.UTF8.GetBytes(similarityMessageString);
        _natsConnection!.Publish(SimilarityCalculatedSubject, similarityMessageData);

        return Redirect($"summary?id={id}");
    }
}
