using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Room
{
    public abstract class AbstractRoom
    {
        public abstract long Id { get; set; }
        public abstract string Name { get; set; }
        public abstract int maxUserNum { get; set; }
        public abstract RoomType RoomType { get; set; }
        public abstract RoomState State { get; set; }
        public abstract List<User> Roommates { get; set; }
        public abstract GameServer _server { get; set; }


        public abstract Task<bool> AddUser(User user);
        public abstract Task RemoveUser(User user);
        public abstract Task<bool> CheckReady(User user);

        public abstract Task HandleRoom();
        //public abstract void BroadcastMessage(string message, User sender);

        public virtual async Task BroadcastMessage(string message, User sender = null)
        {
            foreach (var user in Roommates)
            {
                if (user != sender) // 可选：不发送给消息发送者
                {
                    await user.SendMessage(message);
                }
            }
        }
    }
    public enum RoomType
    {
        大富翁 = 0,
        斗地主 = 1,
        聊天室 = 2,
        COUNT = 3,
        LOBBY = 99
    }

    public enum RoomState
    {
        Waiting,
        Playing,
    }
}
