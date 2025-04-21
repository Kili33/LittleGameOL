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
            }
        }

        public void ShowCards(Player player, List<Card> cards = null)
        {
            if (cards == null && player != null)
                cards = player.Cards;
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
                    message = (await player.user.ReceiveMessageAsync()).Message;
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
                    await currentPlayer.user.SendMessage($"输入有误，请重新输入！剩余时间{timeout}s");
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

        private bool TryParsePlayCommand(Player player, string rawInput, out List<Card> playedCards)
        {
            playedCards = new List<Card>();

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

                            case 'T':
                                playedCards.Add(new Card() { Value = CardValue.Ten });
                                break;

                            case 'J':
                                playedCards.Add(new Card() { Value = CardValue.Jack });
                                break;

                            case 'Q':
                                playedCards.Add(new Card() { Value = CardValue.Queen });
                                break;

                            case 'K':
                                playedCards.Add(new Card() { Value = CardValue.King });
                                break;

                            case 'A':
                                playedCards.Add(new Card() { Value = CardValue.Ace });
                                break;

                            case '2':
                                playedCards.Add(new Card() { Value = CardValue.Two });
                                break;

                            case 'S':
                                playedCards.Add(new Card() { Value = CardValue.SmallJoker });
                                break;

                            case 'B':
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

        private bool ValidatePass(Player player)
        {
            // 如果是首轮出牌不能跳过
            if (_lastPlay == null) return false;

            // 如果上家是自己不能跳过
            if (player == _lastPlay.Player) return false;

            return true;
        }

        private CardGroup AnalyzeCardType(List<Card> cards)
        {
            switch (cards.Count)
            {
                case 1:
                    return CardGroup.Single;

                default:
                    return CardGroup.Wrong;
            }
        }

        private bool ValidateCardCombination(List<Card> cards, Player player)
        {
            // 牌型验证逻辑示例：
            var type = AnalyzeCardType(cards);

            if (type == CardGroup.Wrong) return false;
            // 与上家牌型比较,上家是自己则跳过验证
            if (_lastPlay != null && _lastPlay.Player != player)
            {
                return type == _lastPlay.Type &&
                       cards.Count == _lastPlay.Cards.Count &&
                       cards.Max(o => o.Value) > _lastPlay.Cards.Max(o => o.Value);
            }

            return true;
        }

        private bool ValidatePlay(Player player, List<Card> playedCards)
        {
            // 验证是否拥有这些牌
            foreach (Card card in playedCards)
            {
                if (!player.Cards.Contains(card)) return false;
            }

            // 验证牌型有效性
            return ValidateCardCombination(playedCards, player);
        }

        private void UpdateGameState(Player player, List<Card> playedCards)
        {
            if (playedCards.Count > 0)
            {
                foreach (var card in playedCards)
                {
                    player.Cards.Remove(card);
                }
            }
            _lastPlay = new PlayRecord()
            {
                Type = AnalyzeCardType(playedCards),
                Player = player,
                Cards = playedCards
            };
        }

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
        Rocket,
        Wrong
    }

    #endregion Class
}