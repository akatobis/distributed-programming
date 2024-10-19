using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client;

class Program
{
    private const int MaxMessageLength = 1024;
    public static void StartClient(string host, int port, string message)
    {
        try
        {
            // Разрешение сетевых имён
            IPAddress ipAddress = Dns.GetHostAddresses(host)[1];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // CREATE
            Socket sender = new Socket(
                ipAddress.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);
            
            try
            {
                // CONNECT
                sender.Connect(remoteEP);

                Console.WriteLine("Удалённый адрес подключения сокета: {0}",
                    sender.RemoteEndPoint!.ToString());

                // Подготовка данных к отправке
                byte[] msg = Encoding.UTF8.GetBytes($"{message}<EOF>");

                // SEND
                int bytesSent = sender.Send(msg);

                // RECEIVE
                byte[] buf = new byte[MaxMessageLength];
                StringBuilder responseBuilder = new StringBuilder();
                while (true)
                {
                    int bytesRec = sender.Receive(buf);
                    string responsePart = Encoding.UTF8.GetString(buf, 0, bytesRec);
                    responseBuilder.Append(responsePart);

                    // Проверка на маркер конца сообщения
                    if (responsePart.IndexOf("<EOF>") > -1)
                    {
                        break;
                    }
                }

                var response = responseBuilder.ToString().Replace("<EOF>", "");
                Console.WriteLine("Ответ:\n{0}", response);

                // RELEASE
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    static void Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Invalid parameters");
            Console.WriteLine("Usage: dotnet run <host> <port> <message>");
            return;
        }
        
        string host = args[0];
        int port = int.Parse(args[1]);
        string message = args[2];
        
        StartClient(host, port, message);
    }
}
