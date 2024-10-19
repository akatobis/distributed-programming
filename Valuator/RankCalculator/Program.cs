using System;
using System.Text;
using NATS.Client;
using Newtonsoft.Json;
using StackExchange.Redis;

public class MessageModel
{
    public string Id { get; set; }
}

class RankCalculator
{
    private static readonly IConnectionMultiplexer _redis = ConnectionMultiplexer.Connect("127.0.0.1:6379");
    private static readonly IConnection _natsConnection = new ConnectionFactory().CreateConnection("127.0.0.1:4222");
    
    public static void Main()
    {
        var s = _natsConnection.SubscribeAsync("text.processing", (sender, args) =>
        {
            byte[] data = args.Message.Data;
            IDatabase db = _redis.GetDatabase();

            var messageObject = JsonConvert.DeserializeObject<MessageModel>(Encoding.UTF8.GetString(data));

            string id = messageObject.Id;
            string text = db.StringGet("TEXT-" + id);
            
            string rankKey = "RANK-" + id;
            double rank = CalculateRank(text);
            db.StringSet(rankKey, rank.ToString());
        });

        s.Start();

        Console.WriteLine("Press Enter to exit");
        Console.ReadLine();

        s.Unsubscribe();

        _natsConnection.Drain();
        _natsConnection.Close();
    }
    
    static double CalculateRank(string? text)
    {
        if (text == null) return 0;
        
        int numOfLetters = 0;
        foreach (char ch in text)
        {
            if (Char.IsLetter(ch))
            {
                numOfLetters++;
            }
        }

        double rank = (double)(text.Length - numOfLetters) / text.Length;

        return rank;
    }
}