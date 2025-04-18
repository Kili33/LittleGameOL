using System.Text;
using Server.Room;

namespace Server.Games
{
    public class FightLandlord
    {
        public List<Card> AllCards { get; set; } = new List<Card>();
        public List<Player> Players { get; set; } = new List<Player>();
        public GameRoom room { get; set; }
        private byte[] _buffer = new byte[4096];

        public FightLandlord(List<User> users, GameRoom a_room)
        {
            room = a_room;
            foreach (User user in users)
            {
                Players.Add(new Player(user));
            }
            for (int i = 0; i < 13; i++)
            {
                AllCards.Add(new Card() { Value = (CardValue)i, Suit = Suit.Spade });
                AllCards.Add(new Card() { Value = (CardValue)i, Suit = Suit.Heart });
                AllCards.Add(new Card() { Value = (CardValue)i, Suit = Suit.Club });
                AllCards.Add(new Card() { Value = (CardValue)i, Suit = Suit.Diamond });
            }
            AllCards.Add(new Card() { Value = CardValue.SmallJoker, Suit = Suit.Spade });
            AllCards.Add(new Card() { Value = CardValue.BigJoker, Suit = Suit.Spade });
            Shuffle();
        }

        /// <summary>
        /// 洗牌
        /// </summary>
        public void Shuffle()
        {
            Random random = new Random();
            for (int i = 0; i < AllCards.Count; i++)
            {
                int j = random.Next(i, AllCards.Count);
                Card temp = AllCards[i];
                AllCards[i] = AllCards[j];
                AllCards[j] = temp;
            }
        }

        /// <summary>
        /// 发牌
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<Card> GetCards(int count)
        {
            List<Card> cards = new List<Card>();
            for (int i = 0; i < count; i++)
            {
                cards.Add(AllCards[i]);
            }
            AllCards.RemoveRange(0, count); ;
            return cards;
        }

        public void ShowTable(Player player)
        {
            var ortherPlayers = Players.Where(x => x != player).ToList();
            player.user.SendMessage(new string('=', 50) + "\n");
            player.user.SendMessage("||                                  ||\n");
            player.user.SendMessage($"|| {ortherPlayers[0].Name + ":" + ortherPlayers[0].Cards.Count}" + "" + $"{ortherPlayers[1].Name + ":" + ortherPlayers[1].Cards.Count} ||\n");
            player.user.SendMessage("||                                  ||\n");
            ShowCards(player);
            player.user.SendMessage(new string('=', 50) + "\n");
        }

        public void ShowCards(Player player, List<Card> cards = null)
        {
            if (cards == null && player != null)
                cards = player.Cards;
            cards = cards.OrderByDescending(o => o.Value).ToList();
            List<string> cardShow = new List<string>()
            {
                "3","4","5","6","7","8","9","0","J","Q","K","A","2","S","B"
            };
            List<string> suitShow = new List<string>()
            {
                "♠","♥","♣","♦"
            };
            var length = (cards.Count - 1) * 2 + 4;
            var line = new string('—', length);
            var line2 = "|| ";
            var line3 = "|| ";
            var line4 = "|| ";
            var line5 = "|| ";
            for (int i = 0; i < cards.Count; i++)
            {
                var card = (int)cards[i].Value;
                line2 += "|" + cardShow[(int)cards[i].Value];
                if (card < 14)
                {
                    var suit = (int)cards[i].Suit;
                    line3 += "|" + suitShow[(int)cards[i].Suit];
                    line4 += "| ";
                    line5 += "| ";
                }
                else
                {
                    line3 += "|" + "J";
                    line4 += "|" + "O";
                    line5 += "|" + "K";
                }
            }
            line2 += " | ||";
            line3 += " | ||";
            line4 += " | ||";
            line5 += " | ||";
            player.user.SendMessage(line + "\n");
            player.user.SendMessage(line2 + "\n");
            player.user.SendMessage(line3 + "\n");
            player.user.SendMessage(line4 + "\n");
            player.user.SendMessage(line5 + "\n");
            player.user.SendMessage(line + "\n");
        }

        public void GameStart()
        {
            try
            {
                var index = new List<int> { 0, 1, 2 };
                var random = new Random();
                foreach (var player in Players)
                {
                    player.Cards = GetCards(17);
                    player.Index = index[random.Next(0, index.Count)];
                    index.Remove(player.Index);
                    ShowTable(player);
                }
                CallLandlord();
                HandleGame();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with FightLandlord: {ex.Message}");
            }
            finally
            {
            }
        }
        /// <summary>
        /// 叫地主
        /// </summary>
        public void CallLandlord()
        {
            #region 叫地主

            Dictionary<int, int> scores = new Dictionary<int, int>();
            for (int i = 0; i < 3; i++)
            {
                var player = Players.Where(o => o.Index == i).First();
                string message;
                {
                    player.user.SendMessage("叫地主：0，1，2");
                    message = player.user.ReceiveMessage();
                } while (message != "0" && message != "1" && message != "2") ;

                if (message == "0" || message == "1" || message == "2")
                {
                    scores.Add(i, int.Parse(message));
                    player.user.CurrentRoom.BroadcastMessage(player.Name + $":{message}分", player.user);
                }


            }
            var maxScore = scores.Values.Max();
            var startIndex = scores.Where(x => x.Value == maxScore).Select(x => x.Key).First();
            var landlord = Players.Where(o => o.Index == startIndex).First();
            landlord.user.CurrentRoom.BroadcastMessage($"{landlord.Name}成为地主！");

            #endregion 叫地主
        }

        /// <summary>
        /// 游戏主逻辑
        /// </summary>
        public void HandleGame()
        {
            while (true)
            {

                foreach (var player in Players)
                {
                    ShowTable(player);
                    var cards = player.user.ReceiveMessage();
                    if (player.Cards.Count > 0)
                    {
                        player.Cards.RemoveAt(0);
                    }
                    ShowTable(player);
                }


            }
        }
    }

    #region Class

    public class Player
    {
        public User user;
        public string Name { get; set; }
        public List<Card> Cards { get; set; }
        public Role role { get; set; }
        public int Score { get; set; }
        public int Index { get; set; }

        public Player(User client)
        {
            this.user = client;
            Name = client._userName;
            Cards = new List<Card>();
        }
    }

    public class Card
    {
        public CardValue Value { get; set; }
        public Suit Suit { get; set; }
    }

    public enum Role
    {
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
        Single,
        Pair,
        Triple,
        TripleWithOne,
        TripleWithTwo,
        Straight,
        StraightPair,
        StraightTriple,
        StraightTripleWithOne,
        StraightTripleWithTwo,
        FourWithTwo,
        Bomb,
        Rocket
    }

    #endregion Class
}