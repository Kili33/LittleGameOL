using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Server.Room;

namespace Server
{
    public class GameServer
    {
        private readonly TcpListener _listener;
        private readonly List<User> _allUsers = new List<User>();
        public readonly List<GameRoom> _rooms = new List<GameRoom>();
        //public readonly Dictionary<int, GameRoom> _rooms = new Dictionary<int, GameRoom>();
        public LobbyRoom lobbyRoom;
        private bool _isRunning;
        private static int MaxRoom = 6;

        public GameServer(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            Start();
        }

        public void Start()
        {
            _isRunning = true;
            InitRooms();
            Console.WriteLine("Room created.");
            _listener.Start();
            Console.WriteLine("Server started. Waiting for connections...");

            // 启动接受客户端连接的线程
            Thread acceptThread = new Thread(AcceptClients);
            acceptThread.Start();

            // 主线程处理服务器命令
            while (_isRunning)
            {
                string command = Console.ReadLine();
            }
        }

        private async void AcceptClients()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    User user = new User(client);
                    _allUsers.Add(user);
                    // 将用户加入大厅房间
                    _ = lobbyRoom.AddUser(user);

                    Console.WriteLine($"New client connected. Total clients: {_allUsers.Count}");
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        public void BroadcastMessage(string message, User sender = null)
        {
            foreach (var client in _allUsers)
            {
                if (client != sender) // 可选：不发送给消息发送者
                {
                    client.SendMessage(message);
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            foreach (var user in _allUsers)
            {
                user.CurrentRoom.RemoveUser(user);
                user.Disconnect();
            }
            _allUsers.Clear();
            Console.WriteLine("Server stopped.");
        }

        public void InitRooms()
        {
            lobbyRoom = new LobbyRoom(this);
            for (int i = 0; i < MaxRoom; i++)
            {
                var _type = i % (int)RoomType.COUNT;
                var group = i / (int)RoomType.COUNT + 1;
                CreateRoom($"{(RoomType)_type}{group}", (RoomType)_type, i);
            }
        }

        public void CreateRoom(string name, RoomType roomType, long id)
        {
            GameRoom room = new(name, roomType, id, this);
            _rooms.Add(room);
        }


    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            GameServer server = new GameServer(8888);
        }
    }
}