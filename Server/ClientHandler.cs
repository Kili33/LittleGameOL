using Server.Games;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class ClientHandler
    {
        public readonly TcpClient _client;
        private readonly GameServer _server;
        private NetworkStream _stream;
        public string _clientName;
        private bool _isInRoom = false;
        private bool _isPlaying = false;
        private Room _room;

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
                while (_client.Connected && !_isPlaying)
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
                                var strMembers = string.Join(",", _room.Clients.Select(c => c._clientName));
                                _server.BroadcastMessage($"{_clientName}加入房间！当前房间人员：{strMembers}", _room);
                                SendMessage("输入‘exit’可退出当前房间！\n");
                                switch (_room.RoomType)
                                {
                                    case RoomType.大富翁:
                                        break;

                                    case RoomType.斗地主:
                                        if (_room.Clients.Count == 3)
                                        {
                                            _server.BroadcastMessage($"人数已满，即将开始游戏！", _room);
                                            FightLandlord fightLandlord = new FightLandlord();
                                        }
                                        break;

                                    default:
                                        break;
                                }
                            }
                            else
                                SendMessage("请输入正确的房间号！");
                        }
                        else
                            SendMessage("请输入正确的房间号！");
                    }
                    else
                    {
                        if (message.ToLower() == "exit")
                        {
                            _isInRoom = false;
                            _room.Clients.Remove(this);
                            SendMessage("您已退出房间！");
                            JionRoom();
                            _server.BroadcastMessage($"{_clientName}退出此房间！", _room);
                            break;
                        }
                        switch (_room.RoomType)
                        {
                            case RoomType.大富翁:
                                break;

                            case RoomType.斗地主:
                                break;

                            case RoomType.聊天室:
                                _server.BroadcastMessage($"{DateTime.Now.ToString("HH:mm:ss")}  {_clientName}:{message}", _room, this);
                                break;

                            default:
                                break;
                        }
                    }
                    Console.WriteLine($"{_clientName}: {message}");

                    // 广播消息给所有客户端
                    //_server.BroadcastMessage($"{_clientName}: {message}", this);
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
        public void ShowTable(Room room)
        {
            foreach (var player in room.Clients)
            {
                player.SendMessage("======================================\n");
                player.SendMessage("||                                  ||\n");
                player.SendMessage("||                                  ||\n");
                player.SendMessage("||                                  ||\n");
                player.SendMessage("======================================\n");
            }

        }
        public void ShowCards(List<Card> cards)
        {
            SendMessage("======================================\n");
            int i = 1;
            foreach (var room in _server._rooms.Where(o => o.State == RoomState.Waiting))
            {
                SendMessage($"||       {i++}.{room.Name} 当前人数：{room.Clients.Count}      ||\n");
            }
            SendMessage("======================================\n");
        }
    }
}