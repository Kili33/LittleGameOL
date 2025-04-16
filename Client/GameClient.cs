using System.Net.Sockets;
using System.Text;

namespace MultiplayerGameClient
{
    public class GameClient
    {
        private readonly TcpClient _client;
        private NetworkStream _stream;
        private bool _isRunning;
        private string _clientName;

        public GameClient(string serverIp, int port, string clientName)
        {
            _client = new TcpClient();
            _clientName = clientName;
            _client.Connect(serverIp, port);
            _stream = _client.GetStream();
        }

        public void Start()
        {
            _isRunning = true;

            // 发送客户端名称
            byte[] nameData = Encoding.UTF8.GetBytes(_clientName);
            _stream.Write(nameData, 0, nameData.Length);

            // 启动接收消息的线程
            Thread receiveThread = new Thread(ReceiveMessages);
            receiveThread.Start();

            // 主线程处理用户输入
            while (_isRunning)
            {
                string input = Console.ReadLine();

                try
                {
                    byte[] data = Encoding.UTF8.GetBytes(input);
                    _stream.Write(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending message: {ex.Message}");
                    Stop();
                    break;
                }
            }
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                while (_isRunning)
                {
                    bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                if (_isRunning)
                    Console.WriteLine($"Error receiving message: {ex.Message}");
            }
            finally
            {
                Stop();
            }
        }

        public void Stop()
        {
            if (_isRunning)
            {
                _isRunning = false;
                _stream?.Close();
                _client?.Close();
                Console.WriteLine("Disconnected from server.");
            }
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.Write("Enter your name: ");
            string name = Console.ReadLine();

            GameClient client = new GameClient("127.0.0.1", 8888, name);
            Console.WriteLine($"Connected to server as {name}.");

            client.Start();
        }
    }
}