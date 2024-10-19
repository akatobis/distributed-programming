using System;
using System.Text;
using NATS.Client;
using Newtonsoft.Json;
using StackExchange.Redis;

public class ReceiveMessageModel
{
    public string? Id { get; set; }
}

public class SendMessageModel
{
    public string? Id { get; set; }
    public double Data { get; set; }
}

class RankCalculator
{
    private static readonly IConnectionMultiplexer Redis = ConnectionMultiplexer.Connect("127.0.0.1:6379");
    private static readonly IConnection NatsConnection = new ConnectionFactory().CreateConnection("127.0.0.1:4222");

    private const string Subject = "RankCalculated";

    public static void Main()
    {
        var s = NatsConnection.SubscribeAsync("text.processing", (sender, args) =>
        {
            var data = args.Message.Data;

            var receiveMessageObject = JsonConvert.DeserializeObject<ReceiveMessageModel>(Encoding.UTF8.GetString(data))!;

            var id = receiveMessageObject.Id;
            var region = Redis.GetDatabase().StringGet(id);
            
            var redisConnection = Environment.GetEnvironmentVariable($"DB_{region}");
            if (redisConnection == null) return;
            IDatabase regionDb = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(redisConnection)).GetDatabase();

            var text = regionDb.StringGet("TEXT-" + id)!;
            
            var rankKey = "RANK-" + id;
            var rank = CalculateRank(text);
            Console.WriteLine($"LOOKUP: {id}, {region}.");
            regionDb.StringSet(rankKey, rank.ToString());

            var sendMessageObject = new SendMessageModel()
            {
                Id = $"TEXT-{id}",
                Data = rank
            };

            var sendMessageString = JsonConvert.SerializeObject(sendMessageObject);
            var sendData = Encoding.UTF8.GetBytes(sendMessageString);
            
            NatsConnection.Publish(Subject, sendData);
        });

        s.Start();

        Console.WriteLine("RankCalculator running");
        Console.WriteLine("Press Enter to exit");
        Console.ReadLine();

        s.Unsubscribe();

        NatsConnection.Drain();
        NatsConnection.Close();
    }
    
    static double CalculateRank(string? text)
    {
        if (text == null) return 0;
        
        var numOfLetters = text.Count(ch => Char.IsLetter(ch));

        var rank = (double)(text.Length - numOfLetters) / text.Length;

        return rank;
    }
}