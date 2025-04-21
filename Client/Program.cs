using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    public class GameClient
    {
        private readonly string _serverIp;
        private readonly int _port;
        private readonly string _clientName;
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public GameClient(string serverIp, int port, string clientName)
        {
            _serverIp = serverIp;
            _port = port;
            _clientName = clientName;
        }

        public async Task StartAsync()
        {
            // 1. 连接
            _client = new TcpClient();
            await _client.ConnectAsync(_serverIp, _port);
            _stream = _client.GetStream();

            // 2. 发送登录消息
            await SendMessageAsync(_clientName);
            Console.WriteLine($"[Info] 已连接到服务器，登录名：{_clientName}");

            // 3. 并行启动：接收循环 与 输入循环
            var receiveTask = ReceiveLoopAsync(_cts.Token);
            var inputTask = InputLoopAsync(_cts.Token);

            // 4. 等待任意一端结束后，取消另一端并断开
            await Task.WhenAny(receiveTask, inputTask);
            _cts.Cancel();

            // 等待两端都结束
            await Task.WhenAll(receiveTask, inputTask);
            Disconnect();
        }

        /// <summary>
        /// 展示内容，后续将优化这里
        /// </summary>
        /// <param name="json"></param>
        public void HandleMessage(JsonElement? json)
        {
            if (!json.HasValue) return;
            var root = json.Value;
            if (root.TryGetProperty("data", out var _data))
            {
                var data = _data.GetString();
                Console.WriteLine(data);
            }
        }

        /// <summary>
        /// 持续读取并解析 JSON 消息
        /// </summary>
        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var json = await ReceiveJsonAsync(token);
                    if (json == null) break;
                    // 这里根据实际业务解析
                    HandleMessage(json);
                }
            }
            catch (OperationCanceledException) { /* 主动取消 */ }
            catch (Exception ex)
            {
                Console.WriteLine("[Error] 接收消息异常: " + ex.Message);
            }
        }

        /// <summary>
        /// 从控制台读取用户输入，校验为 JSON 后发送
        /// </summary>
        private async Task InputLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    string line = Console.ReadLine();
                    if (line == null) break;

                    var body = Encoding.UTF8.GetBytes(line);
                    await _stream.WriteAsync(body, 0, body.Length);

                    //// 自动封装为 JSON 结构
                    //var jsonMessage = new
                    //{
                    //    type = "message",
                    //    data = line
                    //};

                    //await SendJsonAsync(jsonMessage);
                }
            }
            catch (OperationCanceledException) { /* 主动取消 */ }
            catch (Exception ex)
            {
                Console.WriteLine("[Error] 发送消息异常: " + ex.Message);
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (_stream == null) throw new InvalidOperationException("Not connected");
            var body = Encoding.UTF8.GetBytes(message);
            await _stream.WriteAsync(body, 0, body.Length);
        }

        /// <summary>
        /// 发送任意对象，会被序列化为 JSON
        /// </summary>
        public async Task SendJsonAsync(object data)
        {
            if (_stream == null) throw new InvalidOperationException("Not connected");
            string json = JsonSerializer.Serialize(data);
            await SendRawJsonStringAsync(json);
        }

        /// <summary>
        /// 将 JSON 字符串按长度前缀协议写入底层流
        /// </summary>
        public async Task SendRawJsonStringAsync(string jsonString)
        {
            var body = Encoding.UTF8.GetBytes(jsonString);
            var lenPrefix = BitConverter.GetBytes(body.Length);
            await _stream.WriteAsync(lenPrefix, 0, 4);
            await _stream.WriteAsync(body, 0, body.Length);
        }

        /// <summary>
        /// 按长度前缀协议读取一条完整的 JSON 消息
        /// </summary>
        public async Task<JsonElement?> ReceiveJsonAsync(CancellationToken token)
        {
            // 1) 读 4 字节长度前缀
            var lenBuf = new byte[4];
            int r = await ReadExactAsync(lenBuf, 0, 4, token);
            if (r < 4) return null;

            int bodyLen = BitConverter.ToInt32(lenBuf, 0);
            if (bodyLen <= 0) return null;

            // 2) 读 bodyLen 字节 JSON
            var bodyBuf = new byte[bodyLen];
            r = await ReadExactAsync(bodyBuf, 0, bodyLen, token);
            if (r < bodyLen) return null;

            // 3) 解析
            string json = Encoding.UTF8.GetString(bodyBuf);
            // 先验证JSON是否有效

            if (JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            }) is JsonDocument doc)
            {
                var root = doc.RootElement;
                // 处理数据...
                return JsonSerializer.Deserialize<JsonElement>(root);
            }
            else
                return null;
        }

        /// <summary>
        /// 保证读取到指定长度的数据
        /// </summary>
        private async Task<int> ReadExactAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            int total = 0;
            while (total < count)
            {
                int n = await _stream.ReadAsync(buffer, offset + total, count - total, token);
                if (n == 0) break;  // 对端关闭
                total += n;
            }
            return total;
        }

        private void Disconnect()
        {
            Console.WriteLine("[Info] 已断开连接。");
            _stream?.Close();
            _client?.Close();
        }
    }

    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // 假设你的 Login 窗口是 WinForms / WPF，同步调用即可：
            var login = new Login();
            login.ShowDialog();

            var client = new GameClient(login.Ip, 8888, login.Name);
            await client.StartAsync();
        }
    }
}