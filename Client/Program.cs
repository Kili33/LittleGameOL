using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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
            _ = Task.Run(async () =>
            {
                await ReceiveMessages();
            });

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


        public async Task handleJson()
        {
            var jsonResponse = await ReceiveJsonAsync();
            if (jsonResponse.HasValue)
            {
                var root = jsonResponse.Value;
                if (root.TryGetProperty("data", out var _data))
                {
                    var data = _data.GetString();
                    Console.WriteLine(data);
                }
            }
        }

        private async Task ReceiveMessages()
        {

            try
            {
                while (_isRunning)
                {

                    await handleJson();

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

        public async Task<JsonElement?> ReceiveJsonAsync()
        {
            if (_stream == null) throw new InvalidOperationException("Not connected");

            byte[] lengthBuffer = new byte[4];
            int read = await _stream.ReadAsync(lengthBuffer, 0, 4);
            if (read < 4)
                return null;

            int length = BitConverter.ToInt32(lengthBuffer, 0);
            byte[] buffer = new byte[length];
            int totalRead = 0;

            while (totalRead < length)
            {
                int bytesRead = await _stream.ReadAsync(buffer, totalRead, length - totalRead);
                if (bytesRead == 0) break;
                totalRead += bytesRead;
            }

            string json = Encoding.UTF8.GetString(buffer);
            return JsonSerializer.Deserialize<JsonElement>(json); // 支持任意 JSON 结构
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