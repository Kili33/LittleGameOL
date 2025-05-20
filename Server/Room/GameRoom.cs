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

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private TaskCompletionSource<bool> _allReadyTaskSource;

        public GameRoom(string name, RoomType roomType, long id, GameServer server)
        {
            Name = name;
            RoomType = roomType;
            Id = id;
            _server = server;
        }

        public override async Task<bool> AddUser(User user)
        {
            await _lock.WaitAsync();
            try
            {
                if (Roommates.Count >= maxUserNum)
                {
                    await user.SendMessage("房间已满，无法加入！");
                    return false;
                }
                user.CurrentRoom = this;
                Roommates.Add(user);
                user._isInRoom = true;
                var strMembers = string.Join(",", Roommates.Select(c => c._userName));
                await BroadcastMessage($"{user._userName}加入房间！当前房间人员：{strMembers}");
                return true;
            }
            finally
            {
                _lock.Release();
            }
        }

        public override async Task<bool> CheckReady(User user)
        {
            await user.SendMessage("输入‘y’准备，输入其他退出当前房间！\n");
            var command = await user.ReceiveMessageAsync();
            if (command.Message == "y")
            {
                user._isReady = true;
                var readyCount = Roommates.Where(o => o._isReady).Count();
                await BroadcastMessage($"当前准备人数{readyCount},满{maxUserNum}人开始！");
                OnPlayerReadyChanged();
                return true;
            }
            else
            {
                await RemoveUser(user);
                return false;
            }
        }

        public override async Task RemoveUser(User user)
        {
            Roommates.Remove(user);
            user.resetState();
            await user.SendMessage("您已退出房间！");
            await BroadcastMessage($"{user._userName}退出此房间！", user);
        }

        // 玩家准备状态变化时调用此方法
        public async void OnPlayerReadyChanged()
        {
            var readyCount = Roommates.Count(o => o != null && o._isReady);
            if (readyCount == maxUserNum && State != RoomState.Playing)
            {
                _ = HandleRoom();
            }
        }

        public override async Task HandleRoom()
        {
            await _lock.WaitAsync();
            try
            {
                await BroadcastMessage($"GAME START", null, MessageType.Figlet);
                State = RoomState.Playing;
                FightLandlord fightLandlord = new FightLandlord(Roommates, this);
                await fightLandlord.GameStart();
            }
            finally { _lock.Release(); }
        }
    }
}