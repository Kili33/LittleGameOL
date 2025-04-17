using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class ClientHandler
    {
        public readonly TcpClient _client;
        public readonly GameServer _server;
        public NetworkStream _stream;
        public string _clientName;
        public Room _room;
        public bool _isInRoom = false;
        public bool _isReady = false;

        public ClientHandler(TcpClient client, GameServer server)
        {
            _client = client;
            _server = server;
            _stream = client.GetStream();
        }

        public void HandleClient()
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                // 获取客户端名称
                bytesRead = _stream.Read(buffer, 0, buffer.Length);
                _clientName = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"{_clientName} has joined the server.");

                // 欢迎消息
                SendMessage($"Welcome to the server, {_clientName}!\n");
                JionRoom();

                // 处理客户端消息
                while (_client.Connected)
                {
                    bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    if (!_isInRoom)
                    {
                        if (int.TryParse(message, out int roomId))
                        {
                            roomId -= 1;
                            if (roomId < _server._rooms.Count)
                            {
                                _room = _server._rooms[roomId];
                                _room.Clients.Add(this);
                                _isInRoom = true;
                                RoomHandler roomHandler = new RoomHandler(this);
                                // 启动房间线程
                                roomHandler.HandleRoom();
                            }
                            else
                                SendMessage("请输入正确的房间号！");
                        }
                        else
                            SendMessage("请输入正确的房间号！");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with client {_clientName}: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        public void SendMessage(string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                _stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message to {_clientName}: {ex.Message}");
                Disconnect();
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_client.Connected)
                {
                    _client.Close();
                    _stream.Close();
                    _server.RemoveClient(this);
                    Console.WriteLine($"{_clientName} has left the server.");
                    if (_isInRoom)
                    {
                        _server.BroadcastMessage($"{_clientName}断开连接！", _room);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting client {_clientName}: {ex.Message}");
            }
        }

        public void JionRoom()
        {
            SendMessage("请选择你要加入的房间\n======================================\n");
            int i = 1;
            foreach (var room in _server._rooms.Where(o => o.State == RoomState.Waiting))
            {
                SendMessage($"||       {i++}.{room.Name} 当前人数：{room.Clients.Count}      ||\n");
            }
            SendMessage("======================================\n");
        }
    }
}