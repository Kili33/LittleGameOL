using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Server.Room;

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
        public User(TcpClient client, AbstractRoom room)
        {
            CurrentRoom = room;
            _stream = client.GetStream();
            var userName = ReceiveMessage();
            Console.WriteLine($"{userName} has joined the server.");
            _userName = userName;
            room.AddUser(this);
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
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int bytesRead = _stream.EndRead(ar);
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(_buffer, 0, bytesRead);
                    CurrentRoom.BroadcastMessage(message, this);
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

        public void SendMessage(string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                _stream.BeginWrite(data, 0, data.Length, SendCallback, null);
            }
            catch
            {
                Disconnect();
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                _stream.EndWrite(ar);
            }
            catch
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {

            CurrentRoom.RemoveUser(this);
            _stream?.Close();
            _client?.Close();
        }
    }
}
