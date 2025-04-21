using Server.Room;
using System.Net.Sockets;
using System.Text;

namespace Server.Games
{
    public class FightLandlord
    {
        public List<Card> AllCards { get; set; } = new List<Card>();
        public List<Player> Players { get; set; } = new List<Player>();
        private Player _currentLandlord;
        public GameRoom room { get; set; }
        public PlayRecord _lastPlay { get; set; }

        private CancellationTokenSource gameLoopCTS;
        public int CurrentPlayerIndex { get; protected set; }
        private GamePhase _phase = GamePhase.Prepareing;

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

        public async Task ShowTable()
        {
            foreach (Player player in Players)
            {
                //var ortherPlayers = Players.Where(x => x != player).ToList();
                //player.user.SendMessage(new string('=', 50) + "\n");
                //player.user.SendMessage("||                                  ||\n");
                //player.user.SendMessage($"|| {ortherPlayers[0].Name + ":" + ortherPlayers[0].Cards.Count}" + "" + $"{ortherPlayers[1].Name + ":" + ortherPlayers[1].Cards.Count} ||\n");
                //player.user.SendMessage("||                                  ||\n");
                //ShowCards(player);
                //player.user.SendMessage(new string('=', 50) + "\n");


                foreach (Player player2 in Players)
                {
                    await player2.user.SendMessage($"{player.Name}有{player.Cards.Count}张牌");
                }

            }
        }

        public void ShowCards(Player player, List<Card> cards = null)
        {
            if (cards == null && player != null)
                cards = player.Cards;
            cards = cards.OrderByDescending(o => o.Value).ToList();
            StringBuilder stringBuilder = new StringBuilder();
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
            stringBuilder.AppendLine(line);
        }

        public async Task GameStart()
        {
            try
            {
                await PrepareGame();
                await CallLandlord();
                await HandleGame();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with FightLandlord: {ex.Message}");
            }
            finally
            {
            }
        }

        private async Task PrepareGame()
        {
            // 初始化牌堆并洗牌
            var index = new List<int> { 0, 1, 2 };
            var random = new Random();
            foreach (var player in Players)
            {
                player.Cards = GetCards(17);
                player.Index = index[random.Next(0, index.Count)];
                index.Remove(player.Index);
            }
            await ShowTable();
        }
        /// <summary>
        /// 叫地主
        /// </summary>
        public async Task CallLandlord()
        {
            #region 叫地主
            _phase = GamePhase.Bidding;
            Dictionary<int, int> scores = new Dictionary<int, int>();
            for (int i = 0; i < 3; i++)
            {
                var player = Players.Where(o => o.Index == i).First();
                string message;
                do
                {
                    await player.user.SendMessage("叫地主：0，1，2");
                    message = await player.user.ReceiveMessageAsync();
                } while (message != "0" && message != "1" && message != "2");

                if (message == "0" || message == "1" || message == "2")
                {
                    scores.Add(i, int.Parse(message));
                    await room.BroadcastMessage(player.Name + $":{message}分");
                }


            }
            var maxScore = scores.Values.Max();
            var startIndex = scores.Where(x => x.Value == maxScore).Select(x => x.Key).First();
            _currentLandlord = Players.Where(o => o.Index == startIndex).First();
            await room.BroadcastMessage($"{_currentLandlord.Name}成为地主！");
            _currentLandlord.role = Role.Landlord;
            _currentLandlord.Cards.AddRange(GetCards(3));
            var ortherPlayers = Players.Where(x => x != _currentLandlord).ToList();
            ortherPlayers.ForEach(ortherPlayers =>
            {
                ortherPlayers.role = Role.Pauper;
            });

            #endregion 叫地主
        }

        /// <summary>
        /// 游戏主逻辑
        /// </summary>
        public async Task HandleGame()
        {
            _phase = GamePhase.Playing;
            int currentPlayerIndex = Players.IndexOf(_currentLandlord);
            while (!CheckGameEnd())
            {
                var currentPlayer = Players[currentPlayerIndex];

                await ShowTable();
                var cards = await WaitForPlay(currentPlayer, 3);
                if (ValidatePlay(currentPlayer, cards))
                {
                    //UpdateGameState(currentPlayer, playedCards);
                    if (currentPlayer.Cards.Count > 0)
                    {
                        currentPlayer.Cards.RemoveAt(0);
                    }
                    currentPlayerIndex = (currentPlayerIndex + 1) % Players.Count;
                }

                await Task.Delay(100);
            }
        }

        private async Task<List<Card>> WaitForPlay(Player currentPlayer, double timeout)
        {

            try
            {
                // 发送出牌提示和当前桌面状态
                await room.BroadcastMessage($"--------到{currentPlayer.Name}出牌了，限制时间{timeout}s------------");
                while (true)
                {
                    // 异步等待输入

                    var rawMessage = await currentPlayer.user.ReceiveMessageAsync(timeout);

                    // 解析指令
                    if (TryParsePlayCommand(currentPlayer, rawMessage, out var playedCards))
                    {
                        return playedCards;
                    }

                    await currentPlayer.user.SendMessage("Invalid card combination");
                }
            }
            catch (OperationCanceledException)
            {
                //await BroadcastToAll($"TIMEOUT|{currentPlayer.Id}");
                return new List<Card>(); // 返回空列表表示跳过
            }
            catch (Exception ex) when (ex is IOException || ex is SocketException)
            {
                //await HandleDisconnect(currentPlayer);
                throw;
            }
        }

        private bool TryParsePlayCommand(Player player, string rawInput, out List<Card> playedCards)
        {
            playedCards = new List<Card>();

            try
            {
                // 指令格式示例: 暂定 
                if (rawInput == "")
                {
                    return ValidatePass(player); // 验证是否可以跳过
                }
                else
                {
                    // playedCards = xxx;

                    // 验证玩家是否拥有这些牌
                    //if (!player.Cards.ContainsAll(playedCards))
                    //{
                    //    throw new InvalidOperationException("Don't own these cards");
                    //}

                    // 验证牌型有效性
                    return ValidateCardCombination(playedCards);
                }
            }
            catch
            {
                return false;
            }
        }
        private bool ValidatePass(Player player)
        {
            // 如果是首轮出牌不能跳过
            //if (_lastPlay == null) return false;

            // 如果上家是队友不能跳过（根据斗地主规则调整）
            //if (player.Role == _lastPlayPlayer.Role) return false;

            return true;
        }
        private bool ValidateCardCombination(List<Card> cards)
        {
            // 牌型验证逻辑示例：
            //var type = AnalyzeCardType(cards);

            //// 与上家牌型比较
            //if (_lastPlay != null)
            //{
            //    return type == _lastPlay.Type &&
            //           cards.Count == _lastPlay.Cards.Count &&
            //           cards.Max() > _lastPlay.Cards.Max();
            //}

            return true;
        }
        private bool ValidatePlay(Player player, List<Card> playedCards)
        {
            // 验证是否拥有这些牌
            //if (!player.Cards.Contains(playedCards)) return false;

            // 验证牌型有效性
            //var type = AnalyzeCardType(playedCards);
            return true;
        }

        public bool CheckGameEnd()
        {
            return false;
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

        public void Play(string message)
        {

        }
    }

    public class Card
    {
        public CardValue Value { get; set; }
        public Suit Suit { get; set; }
    }

    // 辅助类型打牌记录
    public class PlayRecord
    {
        public CardGroup Type { get; }
        public List<Card> Cards { get; }
        public User Player { get; }

        public string ToMessage() =>
            $"{Player._userName}|{(int)Type}|{string.Join(",", Cards.Select(c => c))}";
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