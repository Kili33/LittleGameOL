using Server.Room;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Server
{
    public class User
    {
        public readonly TcpClient _client;
        public NetworkStream _stream;
        private byte[] _buffer = new byte[4096];

        //public readonly GameServer _server;
        public AbstractRoom CurrentRoom;

        public string _userName;
        public bool _isInRoom = false;
        public bool _isReady = false;

        // 锁
        private readonly SemaphoreSlim _receiveLock = new SemaphoreSlim(1, 1);

        private CancellationTokenSource _cts = new CancellationTokenSource();

        public User(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
            _ = InitializeAsync();
        }

        public async Task<bool> JoinRoom(AbstractRoom room)
        {
            if (CurrentRoom != null)
            {
                await CurrentRoom.RemoveUser(this);
            }
            bool success = await room.AddUser(this);
            return success && await CurrentRoom.CheckReady(this);
        }

        public async Task<bool> QuitRoom()
        {
            if (CurrentRoom != null)
            {
                await CurrentRoom.RemoveUser(this);
                resetState();
                return true;
            }
            return false;
        }

        private async Task InitializeAsync()
        {
            try
            {
                // 异步接收用户名
                _userName = (await ReceiveMessageAsync()).Message;
                Console.WriteLine($"{_userName} has joined the server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化失败: {ex.Message}");
                Disconnect();
            }
        }

        public async Task<ReceiveResult> ReceiveMessageAsync(double seconds = 0)
        {
            await _receiveLock.WaitAsync();
            var startTime = DateTime.UtcNow;
            TimeSpan timeout = seconds > 0 ? TimeSpan.FromSeconds(seconds) : Timeout.InfiniteTimeSpan;
            _cts = new CancellationTokenSource(timeout);

            try
            {
                var readTask = _stream.ReadAsync(_buffer, 0, _buffer.Length, _cts.Token);
                var delayTask = Task.Delay(timeout, _cts.Token);

                var completedTask = await Task.WhenAny(readTask, delayTask);

                if (completedTask == delayTask)
                {
                    _cts.Cancel(); // 确保取消
                    throw new OperationCanceledException("Receive operation timed out.");
                }

                int bytesRead = await readTask; // 确保读取完成（或抛出异常）

                // 计算剩余时间
                var elapsed = DateTime.UtcNow - startTime;
                var remainingTime = timeout - elapsed;
                if (remainingTime < TimeSpan.Zero) remainingTime = TimeSpan.Zero;

                return new ReceiveResult
                {
                    Message = Encoding.UTF8.GetString(_buffer, 0, bytesRead),
                    RemainingTime = remainingTime
                };
            }
            finally
            {
                _receiveLock.Release();
                _cts?.Dispose();
            }
        }

        public string ReceiveMessage()
        {
            int bytesRead;
            bytesRead = _stream.Read(_buffer, 0, _buffer.Length);
            var message = Encoding.UTF8.GetString(_buffer, 0, bytesRead);
            return message;
        }

        public void resetState()
        {
            _isInRoom = false;
            _isReady = false;
        }

        public void ReceiveAndBroadcast()
        {
            _stream.BeginRead(_buffer, 0, _buffer.Length, ReceiveCallback, null);
        }

        private async void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int bytesRead = _stream.EndRead(ar);
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(_buffer, 0, bytesRead);
                    await CurrentRoom.BroadcastMessage(message, this);
                    ReceiveMessage();
                }
                else
                {
                    Disconnect();
                }
            }
            catch
            {
                Disconnect();
            }
        }

        public async Task SendMessage(string message, MessageType type = MessageType.Default)
        {
            try
            {
                var data = new
                {
                    type = type,
                    data = message,
                    time = DateTime.Now
                };
                var json = JsonSerializer.Serialize(data);

                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

                // 可加上消息长度前缀，便于对方识别边界
                byte[] lengthPrefix = BitConverter.GetBytes(jsonBytes.Length);
                await _stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
                await _stream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
            }
            catch
            {
                Disconnect();
            }
        }

        public class ReceiveResult
        {
            public string Message { get; set; }
            public TimeSpan RemainingTime { get; set; }
        }

        public void Disconnect()
        {
            CurrentRoom.RemoveUser(this);
            _stream?.Close();
            _client?.Close();
        }
    }

    public enum MessageType
    {
        // 普通文本
        Default,

        // 告示文本
        Figlet,

        // 方框
        Panel,

        // 横线
        Rule,

        // 斗地主牌桌
        FightLandlordTableShow,
    }
}