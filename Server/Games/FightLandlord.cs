using Server.Room;
using Shared.Class;
using Spectre.Console;
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
            Console.OutputEncoding = System.Text.Encoding.UTF8;
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
                var tableShowDto = new TableShowDto();

                var ortherPlayers = Players.Where(x => x != player).ToList();
                var listPlayers = new List<string>();
                foreach (Player ortherPlayer in ortherPlayers)
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine(ortherPlayer.Cards.Count.ToString());
                    stringBuilder.AppendLine(ortherPlayer.Name);
                    listPlayers.Add(stringBuilder.ToString());
                }
                tableShowDto.OtherPlayers = listPlayers;
                if (_lastPlay != null)
                {
                    tableShowDto.LastPlayedCards = ShowCards(_lastPlay.Player, _lastPlay.Cards);
                    tableShowDto.LastPlayerName = _lastPlay.Player.Name;
                }
                // 显示玩家牌
                tableShowDto.SelfCards = ShowCards(player);
                await player.user.SendMessage(Newtonsoft.Json.JsonConvert.SerializeObject(tableShowDto), MessageType.FightLandlordTableShow);
            }
        }

        public List<string> ShowCards(Player player, List<Card> playCards = null)
        {
            var cards = new List<Card>();
            var panels = new List<string>();
            if (playCards == null && player != null)
                cards = player.Cards;
            else
                cards = playCards;
            List<string> cardShow = new List<string>()
            {
                "3","4","5","6","7","8","9","T","J","Q","K","A","2","J","J"
            };
            List<string> suitShow = new List<string>()
            {
                ":spade_suit:",":heart_suit:",":club_suit:",":diamond_suit:"
            };

            for (int i = 0; i < cards.Count; i++)
            {
                var stringBuilder = new StringBuilder();
                var card = (int)cards[i].Value;
                if (card < 13)
                {
                    var suit = (int)cards[i].Suit;
                    // 黑桃|梅花
                    if (suit % 2 == 0)
                    {
                        stringBuilder.AppendLine(cardShow[card]);
                        stringBuilder.AppendLine(suitShow[suit]);
                    }
                    // 红桃|方块
                    else
                    {
                        stringBuilder.AppendLine("[red]" + cardShow[card] + "[/]");
                        stringBuilder.AppendLine("[red]" + suitShow[suit] + "[/]");
                    }
                    stringBuilder.AppendLine("");
                }
                // 小王
                else if (card == 13)
                {
                    stringBuilder.AppendLine(cardShow[card]);
                    stringBuilder.AppendLine("O");
                    stringBuilder.AppendLine("K");
                }
                // 大王
                else if (card == 14)
                {
                    stringBuilder.AppendLine("[red]" + cardShow[card] + "[/]");
                    stringBuilder.AppendLine("[red]O[/]");
                    stringBuilder.AppendLine("[red]K[/]");
                }
                panels.Add(stringBuilder.ToString());
            }
            return panels;
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
            _phase = GamePhase.Bidding;
            Dictionary<int, int> scores = new Dictionary<int, int>();
            for (int i = 0; i < 3; i++)
            {
                double timeout = 30;
                var player = Players.Where(o => o.Index == i).First();
                var recMessage = new ReceiveResult();
                string message;
                try
                {
                    do
                    {
                        await player.user.SendMessage($"叫地主：0，1，2,剩余时间{timeout}s");
                        await room.BroadcastMessage($"到{player.Name}叫地主了，限制时间{timeout}s", player.user, MessageType.Rule);
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
            _currentLandlord.Cards = GetCards(3, _currentLandlord);
            var ortherPlayers = Players.Where(x => x != _currentLandlord).ToList();
            ortherPlayers.ForEach(ortherPlayer =>
            {
                ortherPlayer.role = Role.Pauper;
            });
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
                await room.BroadcastMessage($"到{currentPlayer.Name}出牌了，限制时间{timeout}s", currentPlayer.user, MessageType.Rule);
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

    // 辅助类型打牌记录
    public class PlayRecord
    {
        public CardGroup Type { get; set; }
        public List<Card> Cards { get; set; }
        public Player Player { get; set; }

        public string ToMessage() =>
            $"{Player.user._userName}|{(int)Type}|{string.Join(",", Cards.Select(c => c))}";
    }

    #endregion Class
}