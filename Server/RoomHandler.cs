using Server.Games;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class RoomHandler
    {
        private readonly ClientHandler _client;
        private GameServer _server => _client._server;
        private Room _room => _client._room;
        private string _clientName => _client._clientName;
        private NetworkStream _stream => _client._stream;
        private List<ClientHandler> roommates;

        public RoomHandler(ClientHandler client)
        {
            _client = client;
            roommates = _room.Clients;
        }

        public void HandleRoom()
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                var strMembers = string.Join(",", _client._room.Clients.Select(c => c._clientName));
                _server.BroadcastMessage($"{_clientName}加入房间！当前房间人员：{strMembers}", _room);
                _client.SendMessage("输入‘y’准备，输入‘exit’可退出当前房间！\n");
                while (true)
                {
                    bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    if (message == "y")
                        _client._isReady = true;
                    switch (_room.RoomType)
                    {
                        case RoomType.大富翁:
                            break;

                        case RoomType.斗地主:
                            var readyCount = roommates.Where(o => o._isReady).Count();
                            if (readyCount == 3)
                            {
                                _server.BroadcastMessage($"所有人已准备，即将开始游戏！", _room);
                                _room.State = RoomState.Playing;
                                FightLandlord fightLandlord = new FightLandlord(roommates);
                                fightLandlord.GameStart();
                            }
                            else
                            {
                                _server.BroadcastMessage($"当前准备人数{readyCount},满3人开始！", _room);
                                return;
                            }
                            break;

                        case RoomType.聊天室:
                            _server.BroadcastMessage($"{DateTime.Now.ToString("HH:mm:ss")}  {_clientName}:{message}", _room, _client);
                            break;

                        default:
                            break;
                    }

                    if (message.ToLower() == "exit")
                    {
                        Disconnect();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_client._client.Connected)
                {
                    _client._isInRoom = false;
                    _client._isReady = false;
                    _room.Clients.Remove(_client);
                    _client.SendMessage("您已退出房间！");
                    _server.BroadcastMessage($"{_clientName}退出此房间！", _room);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting client {_clientName}: {ex.Message}");
            }
        }
    }
}