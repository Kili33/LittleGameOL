using Server.Games;

namespace Server.Room
{
    public class GameRoom : AbstractRoom
    {
        public override long Id { get; set; }
        public override string Name { get; set; }
        public override int maxUserNum { get; set; } = 3;
        public override RoomType RoomType { get; set; }
        public override RoomState State { get; set; } = RoomState.Waiting;
        public override List<User> Roommates { get; set; } = new List<User>();
        public override GameServer _server { get; set; }

        public GameRoom(string name, RoomType roomType, long id)
        {
            Name = name;
            RoomType = roomType;
            Id = id;
            HandleRoom();
        }

        public override bool AddUser(User user)
        {
            if (Roommates.Count >= maxUserNum)
            {
                user.SendMessage("房间已满，无法加入！");
                return false;
            }
            user.CurrentRoom = this;
            Roommates.Add(user);
            user._isInRoom = true;
            var strMembers = string.Join(",", Roommates.Select(c => c._userName));
            BroadcastMessage($"{user._userName}加入房间！当前房间人员：{strMembers}");
            user.SendMessage("输入‘y’准备，输入‘exit’可退出当前房间！\n");
            var command = user.ReceiveMessage();
            if (command == "y")
            {
                user._isReady = true;
                var readyCount = Roommates.Where(o => o._isReady).Count();
                BroadcastMessage($"当前准备人数{readyCount},满3人开始！");
                return true;
            }
            else
            {
                RemoveUser(user);
                return false;
            }
        }

        public override void RemoveUser(User user)
        {
            Roommates.Remove(user);
            user.resetState();
            user.SendMessage("您已退出房间！");
            BroadcastMessage($"{user._userName}退出此房间！", user);
        }

        public override void HandleRoom()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var readyCount = Roommates.Where(o => o != null && o._isReady).Count();
                    if (readyCount == maxUserNum)
                    {
                        BroadcastMessage($"所有人已准备，即将开始游戏！");
                        State = RoomState.Playing;
                        FightLandlord fightLandlord = new FightLandlord(Roommates, this);
                        fightLandlord.GameStart();
                    }

                }
            });

        }

    }


}