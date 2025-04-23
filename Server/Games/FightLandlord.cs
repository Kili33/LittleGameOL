using Server.Room;
using System.Net.Sockets;
using System.Text;
using static Server.User;

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
        public List<Card> GetCards(int count, Player player = null)
        {
            List<Card> cards = new List<Card>();
            if (player != null)
                cards = player.Cards;
            for (int i = 0; i < count; i++)
            {
                cards.Add(AllCards[i]);
            }
            AllCards.RemoveRange(0, count); ;
            cards = cards.OrderByDescending(o => o.Value).ToList();
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
                ShowCards(player);
            }
        }

        public async void ShowCards(Player player, List<Card> playCards = null)
        {
            var cards = new List<Card>();
            if (playCards == null && player != null)
                cards = player.Cards;
            else
                cards = playCards;
            StringBuilder stringBuilder = new StringBuilder();
            List<string> cardShow = new List<string>()
            {
                "3","4","5","6","7","8","9","T","J","Q","K","A","2","S","B"
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
            stringBuilder.AppendLine(line2);
            stringBuilder.AppendLine(line3);
            stringBuilder.AppendLine(line4);
            stringBuilder.AppendLine(line5);
            stringBuilder.AppendLine(line);
            if (playCards != null)
                await room.BroadcastMessage(stringBuilder.ToString(), player.user);
            else
                await player.user.SendMessage(stringBuilder.ToString());
        }

        public async Task GameStart()
        {
            try
            {
                await PrepareGame();
                await CallLandlord();
                await HandleGame();
                await Restart();
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
            double timeout = 30;
            Dictionary<int, int> scores = new Dictionary<int, int>();
            for (int i = 0; i < 3; i++)
            {
                var player = Players.Where(o => o.Index == i).First();
                var recMessage = new ReceiveResult();
                string message;
                try
                {
                    do
                    {
                        await player.user.SendMessage($"叫地主：0，1，2,剩余时间{timeout}s");
                        await room.BroadcastMessage($"--------到{player.Name}叫地主了，限制时间{timeout}s------------", player.user);
                        recMessage = await player.user.ReceiveMessageAsync(timeout);
                        message = recMessage.Message;
                        timeout = recMessage.RemainingTime.TotalSeconds;
                    } while (message != "0" && message != "1" && message != "2");
                }
                catch (OperationCanceledException)
                {
                    message = "0";
                }
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
            GetCards(3, _currentLandlord);
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
                var cards = await WaitForPlay(currentPlayer, 30);

                UpdateGameState(currentPlayer, cards);
                currentPlayerIndex = (currentPlayerIndex + 1) % Players.Count;

                await Task.Delay(100);
            }
        }

        public async Task Restart()
        {
            foreach (var player in Players)
            {
                await room.CheckReady(player.user);
            }
        }

        private async Task<List<Card>> WaitForPlay(Player currentPlayer, double timeout)
        {
            try
            {
                // 发送出牌提示和当前桌面状态
                await room.BroadcastMessage($"--------到{currentPlayer.Name}出牌了，限制时间{timeout}s------------", currentPlayer.user);
                var canPass = "";
                if (ValidatePass(currentPlayer))
                    canPass = ",输入n不出";
                await currentPlayer.user.SendMessage($"请输入您要出的牌{canPass}");
                while (true)
                {
                    // 异步等待输入

                    var rawMessage = await currentPlayer.user.ReceiveMessageAsync(timeout);

                    // 解析指令
                    if (TryParsePlayCommand(currentPlayer, rawMessage.Message, out var playedCards))
                    {
                        return playedCards;
                    }
                    timeout = rawMessage.RemainingTime.TotalSeconds;
                    await currentPlayer.user.SendMessage($"输入有误，请重新输入！剩余时间{(int)timeout}s");
                }
            }
            catch (OperationCanceledException)
            {
                if (ValidatePass(currentPlayer))
                    return new List<Card>(); // 可跳过超时返回空列表
                else
                    return new List<Card>() { currentPlayer.Cards.Last() };//不可跳过超时返回最后一张牌
            }
            catch (Exception ex) when (ex is IOException || ex is SocketException)
            {
                //await HandleDisconnect(currentPlayer);
                throw;
            }
        }

        /// <summary>
        /// 分析玩家输入
        /// </summary>
        /// <param name="player"></param>
        /// <param name="rawInput"></param>
        /// <param name="playedCards"></param>
        /// <returns></returns>
        private bool TryParsePlayCommand(Player player, string rawInput, out List<Card> playedCards)
        {
            playedCards = new List<Card>();
            rawInput = rawInput.ToLower();
            try
            {
                // 指令格式示例: 暂定
                if (rawInput == "n")
                {
                    return ValidatePass(player); // 验证是否可以跳过
                }
                else
                {
                    foreach (var card in rawInput)
                    {
                        switch (card)
                        {
                            case '3':
                                playedCards.Add(new Card() { Value = CardValue.Three });
                                break;

                            case '4':
                                playedCards.Add(new Card() { Value = CardValue.Four });
                                break;

                            case '5':
                                playedCards.Add(new Card() { Value = CardValue.Five });
                                break;

                            case '6':
                                playedCards.Add(new Card() { Value = CardValue.Six });
                                break;

                            case '7':
                                playedCards.Add(new Card() { Value = CardValue.Seven });
                                break;

                            case '8':
                                playedCards.Add(new Card() { Value = CardValue.Eight });
                                break;

                            case '9':
                                playedCards.Add(new Card() { Value = CardValue.Nine });
                                break;

                            case 't':
                                playedCards.Add(new Card() { Value = CardValue.Ten });
                                break;

                            case 'j':
                                playedCards.Add(new Card() { Value = CardValue.Jack });
                                break;

                            case 'q':
                                playedCards.Add(new Card() { Value = CardValue.Queen });
                                break;

                            case 'k':
                                playedCards.Add(new Card() { Value = CardValue.King });
                                break;

                            case 'a':
                                playedCards.Add(new Card() { Value = CardValue.Ace });
                                break;

                            case '2':
                                playedCards.Add(new Card() { Value = CardValue.Two });
                                break;

                            case 's':
                                playedCards.Add(new Card() { Value = CardValue.SmallJoker });
                                break;

                            case 'b':
                                playedCards.Add(new Card() { Value = CardValue.BigJoker });
                                break;

                            default:
                                break;
                        }
                    }

                    return ValidatePlay(player, playedCards);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检测玩家能否跳过出牌
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private bool ValidatePass(Player player)
        {
            // 如果是首轮出牌不能跳过
            if (_lastPlay == null) return false;

            // 如果上家是自己不能跳过
            if (player == _lastPlay.Player) return false;

            return true;
        }

        #region 分析牌型

        private CardGroup AnalyzeCardType(List<Card> cards)
        {
            // 按牌值排序
            cards = cards.OrderBy(c => c.Value).ToList();

            switch (cards.Count)
            {
                case 1:
                    return CardGroup.Single;

                case 2:
                    if (cards[0].Value == cards[1].Value)
                        return CardGroup.Pair;
                    else if (IsRocket(cards))
                        return CardGroup.Rocket;
                    else
                        return CardGroup.Wrong;

                case 3:
                    if (cards[0].Value == cards[1].Value && cards[0].Value == cards[2].Value)
                        return CardGroup.Triple;
                    else
                        return CardGroup.Wrong;

                case 4:
                    if (cards[0].Value == cards[1].Value &&
                        cards[0].Value == cards[2].Value &&
                        cards[0].Value == cards[3].Value)
                        return CardGroup.Bomb;
                    else if (IsTripleWithOne(cards))
                        return CardGroup.TripleWithOne;
                    else
                        return CardGroup.Wrong;

                case 5:
                    if (IsStraight(cards))
                        return CardGroup.Straight;
                    else if (IsTripleWithPair(cards))
                        return CardGroup.TripleWithPair;
                    else
                        return CardGroup.Wrong;

                default:
                    if (IsBomb(cards))
                        return CardGroup.Bomb;
                    else if (IsStraight(cards))
                        return CardGroup.Straight;
                    else if (IsPairStraight(cards))
                        return CardGroup.PairStraight;
                    else if (IsAirplane(cards))
                        return CardGroup.Airplane;
                    else if (IsAirplaneWithSingle(cards))
                        return CardGroup.AirplaneWithSingle;
                    else if (IsAirplaneWithPair(cards))
                        return CardGroup.AirplaneWithPair;
                    else if (IsFourWithTwo(cards))
                        return CardGroup.FourWithTwo;
                    else
                        return CardGroup.Wrong;
            }
        }

        // 辅助方法
        private bool IsRocket(List<Card> cards)
        {
            return cards.Count == 2 &&
                   cards.Any(c => c.Value == CardValue.SmallJoker) &&
                   cards.Any(c => c.Value == CardValue.BigJoker);
        }

        private bool IsTripleWithOne(List<Card> cards)
        {
            if (cards.Count != 4) return false;

            var groups = cards.GroupBy(c => c.Value);
            return groups.Any(g => g.Count() == 3) && groups.Count() == 2;
        }

        private bool IsTripleWithPair(List<Card> cards)
        {
            if (cards.Count != 5) return false;

            var groups = cards.GroupBy(c => c.Value);
            return groups.Any(g => g.Count() == 3) && groups.Any(g => g.Count() == 2);
        }

        private bool IsStraight(List<Card> cards)
        {
            if (cards.Count < 5 || cards.Count > 12) return false;

            // 不能包含2或王
            if (cards.Any(c => c.Value == CardValue.Two ||
                               c.Value == CardValue.SmallJoker ||
                               c.Value == CardValue.BigJoker))
                return false;

            // 检查是否连续
            for (int i = 1; i < cards.Count; i++)
            {
                if (cards[i].Value - cards[i - 1].Value != 1)
                    return false;
            }

            return true;
        }

        private bool IsPairStraight(List<Card> cards)
        {
            if (cards.Count < 6 || cards.Count % 2 != 0) return false;

            var groups = cards.GroupBy(c => c.Value)
                              .OrderBy(g => g.Key)
                              .ToList();

            // 每组必须都是对子
            if (groups.Any(g => g.Count() != 2))
                return false;

            // 不能包含2或王
            if (groups.Any(g => g.Key == CardValue.Two ||
                                g.Key == CardValue.SmallJoker ||
                                g.Key == CardValue.BigJoker))
                return false;

            // 检查是否连续
            for (int i = 1; i < groups.Count; i++)
            {
                if (groups[i].Key - groups[i - 1].Key != 1)
                    return false;
            }

            return true;
        }

        private bool IsAirplane(List<Card> cards)
        {
            if (cards.Count < 6 || cards.Count % 3 != 0) return false;

            var triples = cards.GroupBy(c => c.Value)
                               .Where(g => g.Count() == 3)
                               .OrderBy(g => g.Key)
                               .ToList();

            // 三张的数量必须匹配
            if (triples.Count != cards.Count / 3)
                return false;

            // 不能包含2或王
            if (triples.Any(g => g.Key == CardValue.Two ||
                                 g.Key == CardValue.SmallJoker ||
                                 g.Key == CardValue.BigJoker))
                return false;

            // 检查是否连续
            for (int i = 1; i < triples.Count; i++)
            {
                if (triples[i].Key - triples[i - 1].Key != 1)
                    return false;
            }

            return true;
        }

        private bool IsAirplaneWithSingle(List<Card> cards)
        {
            if (cards.Count < 8 || cards.Count % 4 != 0) return false;

            var groups = cards.GroupBy(c => c.Value).ToList();
            var triples = groups.Where(g => g.Count() == 3)
                                .OrderBy(g => g.Key)
                                .ToList();

            // 三张的数量必须匹配
            int tripleCount = cards.Count / 4;
            if (triples.Count != tripleCount)
                return false;

            // 不能包含2或王
            if (triples.Any(g => g.Key == CardValue.Two ||
                                 g.Key == CardValue.SmallJoker ||
                                 g.Key == CardValue.BigJoker))
                return false;

            // 检查三张是否连续
            for (int i = 1; i < triples.Count; i++)
            {
                if (triples[i].Key - triples[i - 1].Key != 1)
                    return false;
            }

            // 检查带牌是否符合要求
            int singleCount = tripleCount;
            var singles = groups.Where(g => g.Count() == 1).ToList();
            var pairs = groups.Where(g => g.Count() == 2).ToList();

            // 带牌可以是单张或对子拆开
            return (singles.Count + pairs.Count * 2) >= singleCount;
        }

        private bool IsAirplaneWithPair(List<Card> cards)
        {
            if (cards.Count < 10 || cards.Count % 5 != 0) return false;

            var groups = cards.GroupBy(c => c.Value).ToList();
            var triples = groups.Where(g => g.Count() == 3)
                                .OrderBy(g => g.Key)
                                .ToList();

            // 三张的数量必须匹配
            int tripleCount = cards.Count / 5;
            if (triples.Count != tripleCount)
                return false;

            // 不能包含2或王
            if (triples.Any(g => g.Key == CardValue.Two ||
                                 g.Key == CardValue.SmallJoker ||
                                 g.Key == CardValue.BigJoker))
                return false;

            // 检查三张是否连续
            for (int i = 1; i < triples.Count; i++)
            {
                if (triples[i].Key - triples[i - 1].Key != 1)
                    return false;
            }

            // 检查带牌是否都是对子
            int pairCount = tripleCount;
            var pairs = groups.Where(g => g.Count() == 2).ToList();

            return pairs.Count >= pairCount;
        }

        private bool IsFourWithTwo(List<Card> cards)
        {
            if (cards.Count != 6) return false;

            var groups = cards.GroupBy(c => c.Value).ToList();
            return groups.Any(g => g.Count() == 4) && groups.Count(g => g.Count() == 1) == 2;
        }

        private bool IsBomb(List<Card> cards)
        {
            if (cards.Count != 4) return false;
            return cards.All(c => c.Value == cards[0].Value);
        }

        #endregion 分析牌型

        /// <summary>
        /// 验证牌型是否通过
        /// </summary>
        /// <param name="cards"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        private bool ValidateCardCombination(List<Card> cards, Player player)
        {
            // 牌型验证逻辑示例：
            var type = AnalyzeCardType(cards);

            if (type == CardGroup.Wrong) return false;
            // 与上家牌型比较,上家是自己则跳过验证
            if (_lastPlay != null && (_lastPlay.Player != player || _lastPlay.Type == CardGroup.Wrong))
            {
                return type == _lastPlay.Type &&
                       cards.Count == _lastPlay.Cards.Count &&
                       cards.Max(o => o.Value) > _lastPlay.Cards.Max(o => o.Value);
            }

            return true;
        }

        /// <summary>
        /// 验证出牌是否正确
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playedCards"></param>
        /// <returns></returns>
        private bool ValidatePlay(Player player, List<Card> playedCards)
        {
            var groups = playedCards.GroupBy(o => o.Value);
            // 验证是否拥有这些牌
            foreach (var group in groups)
            {
                if (player.Cards.Where(o => o.Value == group.Key).Count() < group.Count()) return false;
            }

            // 验证牌型有效性
            return ValidateCardCombination(playedCards, player);
        }

        /// <summary>
        /// 更新游戏状态
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playedCards"></param>
        private async void UpdateGameState(Player player, List<Card> playedCards)
        {
            if (playedCards.Count > 0)
            {
                foreach (var card in playedCards)
                {
                    player.Cards.Remove(player.Cards.Where(o => o.Value == card.Value).First());
                }
                await room.BroadcastMessage($"{player.Name} :");
                ShowCards(player, playedCards);

                _lastPlay = new PlayRecord()
                {
                    Type = AnalyzeCardType(playedCards),
                    Player = player,
                    Cards = playedCards
                };
                if (player.Cards.Count == 0)
                {
                    await room.BroadcastMessage($"{player.Name} 获得胜利！");
                    _phase = GamePhase.Ended;
                }
            }
            else
            {
                await room.BroadcastMessage($"{player.Name}：不出");
            }
        }

        /// <summary>
        /// 检查游戏是否结束
        /// </summary>
        /// <returns></returns>
        public bool CheckGameEnd()
        {
            if (Players.Where(o => o.Cards.Count() == 0).Count() > 0)
                return true;
            else
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
        public CardGroup Type { get; set; }
        public List<Card> Cards { get; set; }
        public Player Player { get; set; }

        public string ToMessage() =>
            $"{Player.user._userName}|{(int)Type}|{string.Join(",", Cards.Select(c => c))}";
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

    #endregion Class
}