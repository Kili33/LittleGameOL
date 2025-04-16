namespace Server
{
    public class Room
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public RoomType RoomType { get; set; }
        public RoomState State { get; set; } = RoomState.Waiting;
        public List<ClientHandler> Clients { get; set; } = new List<ClientHandler>();

        public Room(string name, RoomType roomType, long id)
        {
            Name = name;
            RoomType = roomType;
            Id = id;
        }
    }

    public enum RoomType
    {
        大富翁 = 0,
        斗地主 = 1,
        聊天室 = 2,
        COUNT = 3,
    }

    public enum RoomState
    {
        Waiting,
        Playing,
    }
}