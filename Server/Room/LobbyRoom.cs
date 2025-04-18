using System.IO;
using System.Text;
using System.Xml;

namespace Server.Room
{
    public class LobbyRoom : AbstractRoom
    {
        public override long Id { get; set; }
        public override string Name { get; set; }
        public override int maxUserNum { get; set; } = 99;
        public override RoomType RoomType { get; set; }
        public override RoomState State { get; set; } = RoomState.Waiting;
        public override List<User> Roommates { get; set; } = new List<User>();
        public override GameServer _server { get; set; }


        public LobbyRoom(GameServer server)
        {
            _server = server;
            Name = "LOBBY";
            RoomType = RoomType.LOBBY;
            Id = 99;
            HandleRoom();
        }

        public override bool AddUser(User user)
        {
            user.CurrentRoom = this;
            Roommates.Add(user);
            RequestJoinRoom(user);
            return true;
        }

        public override void RemoveUser(User user)
        {
            Roommates.Remove(user);
        }

        public override void HandleRoom()
        {

        }

        public void RequestJoinRoom(User user)
        {
            // TODO 有bug

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("请选择你要加入的房间\n======================================");

            int i = 1;
            foreach (var room in _server._rooms)
            {
                stringBuilder.AppendLine($"||  {i++}.{room.Name} 当前人数：{room.Roommates.Count} {room.State}  ||\n");
            }
            stringBuilder.AppendLine("======================================\n");

            user.SendMessage(stringBuilder.ToString());
            var message = user.ReceiveMessage();

            if (int.TryParse(message, out int roomId))
            {
                roomId -= 1;
                if (roomId < _server._rooms.Count)
                {
                    user.CurrentRoom.RemoveUser(user);
                    var room = _server._rooms[roomId];
                    bool success = room.AddUser(user);

                    if (!success)
                    {
                        AddUser(user); // 如果加入失败，返回大厅
                    }

                    //RoomHandler roomHandler = new RoomHandler(this);
                    //// 启动房间线程
                    //roomHandler.HandleRoom();
                }
                else
                    user.SendMessage("请输入正确的房间号！");
            }
            else
                user.SendMessage("请输入正确的房间号！");

        }
    }


}