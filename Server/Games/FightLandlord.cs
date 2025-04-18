namespace Server.Games
{
    public class FightLandlord
    {
        public List<Card> AllCards { get; set; } = new List<Card>();
        public List<Player> Players { get; set; } = new List<Player>();

        public FightLandlord(List<ClientHandler> clients)
        {
            foreach (ClientHandler client in clients)
            {
                Players.Add(new Player(client));
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
            var ortherPlayers = Players.Where(x => x.Index != player.Index).ToList();
            player.client.SendMessage(new string('=', 50) + "\n");
            player.client.SendMessage("||                                  ||\n");
            player.client.SendMessage($"|| {ortherPlayers[0].Name + ":" + ortherPlayers[0].Cards.Count}" + "" + $"{ortherPlayers[1].Name + ":" + ortherPlayers[1].Cards.Count} ||\n");
            player.client.SendMessage("||                                  ||\n");
            ShowCards(player);
            player.client.SendMessage(new string('=', 50) + "\n");
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
            player.client.SendMessage(line + "\n");
            player.client.SendMessage(line2 + "\n");
            player.client.SendMessage(line3 + "\n");
            player.client.SendMessage(line4 + "\n");
            player.client.SendMessage(line5 + "\n");
            player.client.SendMessage(line + "\n");
        }

        public void GameStart()
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
        }
    }

    #region Class

    public class Player
    {
        public ClientHandler client;
        public string Name { get; set; }
        public List<Card> Cards { get; set; }
        public Role role { get; set; }
        public int Score { get; set; }
        public int Index { get; set; }

        public Player(ClientHandler client)
        {
            this.client = client;
            Name = client._clientName;
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