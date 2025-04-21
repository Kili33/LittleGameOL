using System.Text;

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
            _ = HandleRoom();
        }

        public override async Task<bool> AddUser(User user)
        {
            user.CurrentRoom = this;
            Roommates.Add(user);
            await RequestJoinRoom(user);
            return true;
        }
        public override async Task<bool> CheckReady(User user)
        {
            return true;
        }

        public override async Task RemoveUser(User user)
        {
            Roommates.Remove(user);
        }

        public override async Task HandleRoom()
        {

        }

        public async Task RequestJoinRoom(User user)
        {
            // TODO 有bug

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("请选择你要加入的房间\n======================================");

            int i = 1;
            foreach (var room in _server._rooms)
            {
                stringBuilder.AppendLine($"||  {i++}.{room.Name} 当前人数：{room.Roommates.Count} {room.State}  ||");
            }
            stringBuilder.AppendLine("======================================");

            while (true)
            {


                await user.SendMessage(stringBuilder.ToString());
                var message = await user.ReceiveMessageAsync();

                if (int.TryParse(message, out int roomId))
                {
                    roomId -= 1;
                    if (roomId < _server._rooms.Count)
                    {
                        var room = _server._rooms[roomId];
                        bool success = await user.JoinRoom(room);
                        if (success)
                        {
                            break;
                        }
                        else
                        {
                            user.CurrentRoom = this;
                            Roommates.Add(user);
                        }

                    }
                    else
                    {
                        await user.SendMessage("请输入正确的房间号！");

                    }
                }
                else
                {
                    await user.SendMessage("请输入正确的房间号！");
                }
            }
        }


    }


}