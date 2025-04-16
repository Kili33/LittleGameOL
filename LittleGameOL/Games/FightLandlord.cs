using System.Net.Sockets;
using System.Text;

namespace Server.Games
{
    public class FightLandlord
    {
        private readonly TcpClient _client;
        private readonly GameServer _server;
        private readonly Room _room;
        private NetworkStream _stream;
        private string _clientName;

        public FightLandlord(Room room, GameServer server, TcpClient client, string clientName)
        {
            _room = room;
            _server = server;
            _client = client;
            _clientName = clientName;
        }

        public void Start()
        {
            int readyPlayers = 0;
            _server.BroadcastMessage($"当前人数已满，请准备：y/n", _room);
            while (readyPlayers != 3)
            {
                try
                {
                    _stream = _client.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (Encoding.UTF8.GetString(buffer, 0, bytesRead).ToLower() == "y")
                    {
                        _server.BroadcastMessage($"{_clientName}已准备", _room);
                        readyPlayers += 1;
                    }
                    if (readyPlayers == 3)
                    {
                        _server.BroadcastMessage($"游戏开始", _room);
                        // 处理客户端消息
                        while (_client.Connected)
                        {
                            bytesRead = _stream.Read(buffer, 0, buffer.Length);
                            if (bytesRead == 0) break;

                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error with client {_clientName}: {ex.Message}");
                }
                finally
                {
                }
            }
            while (readyPlayers == 3)
            {
                try
                {
                    _stream = _client.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    _server.BroadcastMessage($"游戏开始", _room);
                    // 处理客户端消息
                    while (_client.Connected)
                    {
                        bytesRead = _stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break;

                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error with client {_clientName}: {ex.Message}");
                }
                finally
                {
                }
            }
        }
    }
}