using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    public class GameClient
    {
        private readonly TcpClient _client;
        private NetworkStream _stream;
        private bool _isRunning;
        private string _clientName;
        private byte[] _buffer = new byte[4096];

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
                    Disconnect();
                    break;
                }
            }
        }

        private void ReceiveMessages()
        {
            int bytesRead;
            try
            {
                while (_isRunning)
                {
                    bytesRead = _stream.Read(_buffer, 0, _buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(_buffer, 0, bytesRead);
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
                Disconnect();
            }
        }

        private void Disconnect()
        {
            if (_isRunning)
            {
                Console.WriteLine("Disconnected from server");
                _stream?.Close();
                _client?.Close();
                Environment.Exit(0);
            }
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var login = new Login();
            login.ShowDialog();

            GameClient client = new GameClient(login.Ip, 8888, login.Name);
            Console.WriteLine($"Connected to server as {login.Name}.");

            client.Start();
        }
    }
}