#nullable enable

using System.Collections.Generic;

namespace Shared.Class
{
    public class FightLandlordClass
    {
    }

    public class TableShowDto
    {
        public List<string> OtherPlayers { get; set; }
        public List<string> SelfCards { get; set; }
        public List<string> LastPlayedCards { get; set; }
        public string? LastPlayerName { get; set; }
    }

    public class Card
    {
        public CardValue Value { get; set; }
        public Suit Suit { get; set; }
    }

    public enum GamePhase
    {
        Prepareing,
        Bidding,
        Playing,
        Ended
    }

    public enum Role
    {
        None,
        Landlord,
        Pauper
    }

    public enum Suit
    {
        Spade,
        Heart,
        Club,
        Diamond
    }

    public enum CardValue
    {
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King,
        Ace,
        Two,
        SmallJoker,
        BigJoker
    }

    public enum CardGroup
    {
        Wrong,          // 错误牌型
        Single,         // 单张
        Pair,           // 对子
        Triple,         // 三张
        TripleWithOne,  // 三带一
        TripleWithPair, // 三带对
        Bomb,           // 炸弹
        Rocket,         // 王炸
        Straight,       // 顺子
        PairStraight,   // 连对
        Airplane,       // 飞机(不带翅膀)
        AirplaneWithSingle, // 飞机带单张
        AirplaneWithPair,   // 飞机带对子
        FourWithTwo,    // 四带二
    }
}