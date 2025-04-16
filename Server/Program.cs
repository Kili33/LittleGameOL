using System.Net;
using System.Net.Sockets;

namespace Server
{
    public class GameServer
    {
        private readonly TcpListener _listener;
        private readonly List<ClientHandler> _clients = new List<ClientHandler>();
        public readonly List<Room> _rooms = new List<Room>();
        private bool _isRunning;
        private static int MaxRoom = 6;

        public GameServer(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
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
                //if (command?.ToLower() == "exit")
                //{
                //    Stop();
                //}
            }
        }

        private void AcceptClients()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    ClientHandler handler = new ClientHandler(client, this);
                    _clients.Add(handler);
                    Thread clientThread = new Thread(handler.HandleClient);
                    clientThread.Start();
                    Console.WriteLine($"New client connected. Total clients: {_clients.Count}");
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        public void BroadcastMessage(string message, Room room, ClientHandler sender = null)
        {
            foreach (var client in room.Clients)
            {
                if (client != sender) // 可选：不发送给消息发送者
                {
                    client.SendMessage(message);
                }
            }
        }

        public void RemoveClient(ClientHandler client)
        {
            _clients.Remove(client);
            Console.WriteLine($"Client disconnected. Total clients: {_clients.Count}");
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            foreach (var client in _clients)
            {
                client.Disconnect();
            }
            _clients.Clear();
            Console.WriteLine("Server stopped.");
        }

        public void InitRooms()
        {
            for (int i = 0; i < MaxRoom; i++)
            {
                var _type = i % (int)RoomType.COUNT;
                var group = i / (int)RoomType.COUNT + 1;
                CreateRoom($"{(RoomType)_type}{group}", (RoomType)_type, i);
            }
        }

        public void CreateRoom(string name, RoomType roomType, long id)
        {
            Room room = new Room(name, roomType, id);
            _rooms.Add(room);
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            GameServer server = new GameServer(8888);
            server.Start();
        }
    }
}