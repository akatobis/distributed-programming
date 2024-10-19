using System.Text;
using NATS.Client;
using Newtonsoft.Json;

public class MessageModel
{
    public string? Id { get; set; }
    public double Data { get; set; }
}

class EventLogger
{
    private static readonly IConnection NatsConnection = new ConnectionFactory().CreateConnection("127.0.0.1:4222");
    
    private const string RankCalculatedSubject = "RankCalculated";
    private const string SimilarityCalculatedSubject = "SimilarityCalculated";
    
    public static void Main()
    {
        var similaritySubscriber = NatsConnection.SubscribeAsync(SimilarityCalculatedSubject, "event_logger", (sender, args) =>
        {
            var messageData = args.Message.Data;

            var receiveMessageObject = JsonConvert.DeserializeObject<MessageModel>(Encoding.UTF8.GetString(messageData))!;

            var id = receiveMessageObject.Id;
            var data = receiveMessageObject.Data;
            
            Console.WriteLine(SimilarityCalculatedSubject);
            Console.WriteLine(id);
            Console.WriteLine(data);
        });
        similaritySubscriber.Start();
        
        var rankSubscriber = NatsConnection.SubscribeAsync(RankCalculatedSubject, "event_logger", (sender, args) =>
        {
            var messageData = args.Message.Data;

            var receiveMessageObject = JsonConvert.DeserializeObject<MessageModel>(Encoding.UTF8.GetString(messageData))!;

            var id = receiveMessageObject.Id;
            var data = receiveMessageObject.Data;
            
            Console.WriteLine(RankCalculatedSubject);
            Console.WriteLine(id);
            Console.WriteLine(data);
        });
        rankSubscriber.Start();

        Console.WriteLine("EventLogger running");
        Console.WriteLine("Press Enter to exit");
        Console.ReadLine();

        similaritySubscriber.Unsubscribe();
        rankSubscriber.Unsubscribe();

        NatsConnection.Drain();
        NatsConnection.Close();
    }
}