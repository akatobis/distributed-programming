using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Chain
{
    class Program
    {
        private static async Task<Socket> CreateSenderSocket(string host, int port)
        {
            IPAddress ipAddress = host == "localhost" ? IPAddress.Loopback : IPAddress.Parse(host);
            IPEndPoint remoteEp = new IPEndPoint(ipAddress, port);
            Socket sender = new Socket(
                ipAddress.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);
                
            while (true)
            {
                try
                {
                    await sender.ConnectAsync(remoteEp);
                    break;
                }
                catch 
                {
                    // ignored
                }
            }

            return sender;
        }
        
        private static Socket CreateReceiverSocket(int port)
        {
            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint remoteEp = new IPEndPoint(ipAddress, port);
            Socket receiver = new Socket(
                ipAddress.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);
            receiver.Bind(remoteEp);
            receiver.Listen(10);

            return receiver;
        }

        private static void Send(Socket sender, int x)
        {
            byte[] msg = BitConverter.GetBytes(x);
            int bytesSent = sender.Send(msg);
        }
        
        private static int Receive(Socket receiver)
        {
            byte[] msg = new byte[1024];
            receiver.Receive(msg);
            return BitConverter.ToInt32(msg);
        }
        
        static async Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: <listening-port> <next-host> <next-port> [true]");
                return;
            }

            int listeningPort = int.Parse(args[0]);
            string nextHost = args[1];
            int nextPort = int.Parse(args[2]);
            bool isInitiator = args.Length > 3 && args[3].ToLower() == "true";

            Socket receiver = CreateReceiverSocket(listeningPort);
            Socket sender = await CreateSenderSocket(nextHost, nextPort);
                
            Console.WriteLine("Введите число");
            if (!int.TryParse(Console.ReadLine(), out var x))
            {
                Console.WriteLine("Invalid input number");
                return;
            }
            
            Socket connectionHandler = await receiver.AcceptAsync();
            
            if (isInitiator)
            {
                Send(sender, x);

                x = Receive(connectionHandler);

                Send(sender, x);
                
                x = Receive(connectionHandler);
            }
            else
            {
                var y = Receive(connectionHandler);

                var max = Math.Max(x, y);
                
                Send(sender, max);

                x = Receive(connectionHandler);
                
                Send(sender, x);
            }
            
            Console.WriteLine($"Максимально число из всех потоков: {x}");
            connectionHandler.Shutdown(SocketShutdown.Both);
            sender.Shutdown(SocketShutdown.Both);
                
            connectionHandler.Close();
            sender.Close();
            receiver.Close();
        }
    }
}