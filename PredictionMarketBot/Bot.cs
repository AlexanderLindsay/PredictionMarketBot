using Discord;
using Discord.Commands;
using PredictionMarketBot.InfoModels;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PredictionMarketBot
{
    public class Bot : IDisposable
    {
        private DiscordClient Client { get; set; }
        private MarketsManager Manager { get; set; }

        public Bot(string appName, MarketsManager manager)
        {
            Manager = manager;

            Client = new DiscordClient(c =>
            {
                c.AppName = appName;
                c.MessageCacheSize = 0;
                c.LogLevel = LogSeverity.Info;
                c.LogHandler = OnLogMessage;
            });

            Client.UsingCommands(c =>
            {
                c.CustomPrefixHandler = (msg) =>
                {
                    if (msg.User.IsBot)
                        return -1;

                    var isMatch = Regex.IsMatch(msg.Text, @"^\$market");
                    if (isMatch)
                        return 7;

                    return -1;
                };
                c.AllowMentionPrefix = true;
                c.HelpMode = HelpMode.Public;
                c.ExecuteHandler = OnCommandExecuted;
                c.ErrorHandler = OnCommandError;
            });

            CreateCommands();
        }

        public void Start(string token)
        {
            Client.ExecuteAndWait(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Client.Connect(token);
                        Client.SetGame("Discord.Net");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Client.Log.Error($"Login Failed", ex);
                        await Task.Delay(Client.Config.FailedReconnectDelay);
                    }
                }
            });
        }

        private MarketSimulator GetSimulator(CommandEventArgs cea)
        {
            return Manager.GetMarket(cea.Server.Id.ToString());
        }

        private void CreateCommands()
        {
            var service = Client.GetService<CommandService>();

            service.CreateCommand("info")
                .Description("displays information about the bot.")
                .Do(async (e) =>
                {
                    var botName = "Prediction Market Bot v0.1.0";
                    var discordVersion = typeof(DiscordClient).Assembly.GetName().Version;
                    var discordInfo = $"Build using Discord.NET {discordVersion}";
                    var helpInfo = "type `$market help` for information on how to use the bot";
                    var aboutInfo = "type `$market about` for a description of what the bot does";

                    var msg = $"{botName}\n{discordInfo}\n{aboutInfo}\n{helpInfo}";
                    await Client.Reply(e, msg);
                });

            service.CreateCommand("about")
                .Description("describes what the bot does.")
                .Do(async (e) =>
                {
                    var msg = "Runs a prediction market(https://en.wikipedia.org/wiki/Prediction_market) and allows users to buy and sell stocks on the market.";
                    await Client.Reply(e, msg);
                });

            service.CreateCommand("current")
                .Description("displays information about the current market.")
                .Do(async (e) =>
                {
                    var simulator = GetSimulator(e);

                    var market = simulator.GetMarketInfo();
                    var msg = $"**{market.Name}**\n{market.Description}";
                    await Client.Reply(e, msg);
                    await ListStocks(async (m) => await Client.Reply(e, m), simulator);
                });

            service.CreateCommand("predict")
                .Description("predicts the outcome of an event.")
                .Do(async (e) =>
                {
                    var simulator = GetSimulator(e);
                    var market = simulator.GetMarketInfo();

                    var result = simulator.Predict();

                    var msg = $"**{market.Description}** {result.Name} ({result.CurrentProbability:P})";
                    await Client.Reply(e, msg);
                });

            service.CreateCommand("list")
                .Description("prints the list players and stocks.")
                .Do(async (e) =>
                {
                    var simulator = GetSimulator(e);

                    Func<string, Task> reply = async (msg) =>
                    {
                        await Client.Reply(e, msg);
                    };
                    await ListStocks(reply, simulator);
                    await ListPlayers(reply, simulator);
                });

            service.CreateCommand("list players")
                .Description("prints the list of players.")
                .Do(async (e) =>
                {
                    var simulator = GetSimulator(e);

                    Func<string, Task> reply = async (msg) =>
                    {
                        await Client.Reply(e, msg);
                    };
                    await ListPlayers(reply, simulator);
                });

            service.CreateCommand("list stocks")
                .Description("prints the list of stocks.")
                .Do(async (e) =>
                {
                    var simulator = GetSimulator(e);

                    Func<string, Task> reply = async (msg) =>
                    {
                        await Client.Reply(e, msg);
                    };
                    await ListStocks(reply, simulator);
                });

            service.CreateCommand("player")
                .Description("prints info about a player.")
                .Parameter("name", ParameterType.Optional)
                .Do((e) =>
                {
                    var simulator = GetSimulator(e);

                    PlayerInfo player;

                    var name = e.GetArg("name");
                    if (!string.IsNullOrEmpty(name))
                    {
                        player = simulator.GetPlayerByName(name);
                    }
                    else
                    {
                        player = simulator.GetDiscordPlayer(e.User.Id.ToString());
                    }

                    if (player == null)
                    {
                        Client.Reply(e, "No such player.");
                        return;
                    }

                    var playerInfo = DisplayPlayerInfo(player);
                    Client.Reply(e, playerInfo);
                });

            service.CreateCommand("open")
                .Description("Opens the market for buying and selling, but the stocks can no longer be changed.")
                .AddCheck((command, user, channel) => user.Server.Owner.Id == user.Id,
                    "Only the server owner can open or close the market.")
                .Do(async (e) =>
                {
                    var simulator = GetSimulator(e);

                    var result = await simulator.Start();
                    if (result)
                    {
                        await Client.Reply(e, "Market is open.");
                    }
                    else
                    {
                        await Client.Reply(e, "Market was already open.");
                    }
                });

            service.CreateCommand("close")
                .Description("Closes the market.")
                .AddCheck((command, user, channel) => user.Server.Owner.Id == user.Id,
                    "Only the server owner can open or close the market.")
                .Do(async (e) =>
                {
                    var simulator = GetSimulator(e);

                    var result = await simulator.Stop();
                    if (result)
                    {
                        await Client.Reply(e, "Market is closed.");
                    }
                    else
                    {
                        await Client.Reply(e, "Market was already closed.");
                    }
                });

            service.CreateCommand("add stock")
                .Description("Adds a stock to the current market. Only works if the market is closed.")
                .Parameter("name", ParameterType.Unparsed)
                .AddCheck((command, user, channel) => user.Server.Owner.Id == user.Id,
                    "Only the server owner can add stocks")
                .Do(async (e) =>
                {
                    var simulator = GetSimulator(e);

                    var stock = new StockInfo
                    {
                        Name = e.GetArg("name")
                    };

                    var result = await simulator.AddStockAsync(stock);

                    await Client.Reply(e, $"Stock {result.Name} ({result.Id}) Added.");
                });

            service.CreateCommand("buy")
                .Description("Buys the given amount of the given stock.")
                .Parameter("amount", ParameterType.Required)
                .Parameter("stock", ParameterType.Unparsed)
                .Do(async (e) =>
                {
                    var simulator = GetSimulator(e);

                    var stockName = e.GetArg("stock");
                    var stock = simulator.GetStockByName(stockName);

                    var validStock = stock != null;

                    int amount;
                    var validAmount = int.TryParse(e.GetArg("amount"), out amount);

                    if (!validStock)
                    {
                        await Client.Reply(e, $"Can't find a stock by that name.");
                    }

                    if (!validAmount)
                    {
                        await Client.Reply(e, "Expects an integer for the amount.");
                    }

                    if (!validStock || !validAmount)
                    {
                        return;
                    }

                    Func<string, Task> reply = async (msg) =>
                    {
                        await Client.Reply(e, msg);
                    };

                    await Buy(reply, simulator, e.User, stock.Id, amount);
                });

            service.CreateCommand("sell")
                .Description("Sells the given amount of the given stock.")
                .Parameter("amount", ParameterType.Required)
                .Parameter("stock", ParameterType.Unparsed)
                .Do(async (e) =>
                {
                    var simulator = GetSimulator(e);

                    var stockName = e.GetArg("stock");
                    var stock = simulator.GetStockByName(stockName);

                    var validStock = stock != null;

                    int amount;
                    var validAmount = int.TryParse(e.GetArg("amount"), out amount);

                    if (!validStock)
                    {
                        await Client.Reply(e, "Expects an integer for the stock id.");
                    }

                    if (!validAmount)
                    {
                        await Client.Reply(e, "Expects an integer for the amount.");
                    }

                    if (!validStock || !validAmount)
                    {
                        return;
                    }

                    Func<string, Task> reply = async (msg) =>
                    {
                        await Client.Reply(e, msg);
                    };

                    await Sell(reply, simulator, e.User, stock.Id, amount);
                });
        }

        private async Task ListStocks(Func<string, Task> reply, MarketSimulator simulator)
        {
            var builder = new StringBuilder();

            var stocks = simulator.ListStocks();

            if (!stocks.Any())
            {
                await reply("No Stocks");
                return;
            }

            foreach (var stock in stocks)
            {
                builder.AppendLine($"**Stock** {stock.Name}");
                builder.AppendLine($"**Shares Owned** {stock.NumberSold}");
                builder.AppendLine($"**Current Price** {stock.CurrentPrice:C}");
                builder.AppendLine($"**Probability** {stock.CurrentProbability:F4}");
                builder.AppendLine();
            }

            await reply(builder.ToString());
        }

        private string DisplayPlayerInfo(PlayerInfo player)
        {
            if (player == null)
                return "";

            var shares = player.Shares
                    .Aggregate("", (str, share) => str += $"{share.Stock}:{share.Amount}|")
                    .Trim('|');

            return $"**Player** {player.Name}\n**Funds** {player.Money:C}\n**Shares** {shares}";
        }

        private async Task ListPlayers(Func<string, Task> reply, MarketSimulator simulator)
        {
            var builder = new StringBuilder();

            var players = simulator.ListPlayers();

            if (!players.Any())
            {
                await reply("No Players");
                return;
            }

            foreach (var player in players)
            {
                builder.AppendLine(DisplayPlayerInfo(player));
                builder.AppendLine();
            }

            await reply(builder.ToString());
        }

        private async Task Buy(Func<string, Task> reply, MarketSimulator simulator, User user, int stockId, int amount)
        {
            var player = simulator.GetDiscordPlayer(user.Id.ToString());
            if (player == null)
            {
                player = new PlayerInfo
                {
                    Name = user.Name
                };
                await simulator.AddPlayerAsync(player, user.Id.ToString());
            }

            var result = await simulator.Buy(player.Id, stockId, amount);
            string msg = "";

            var shares = amount == 1 ? "share" : "shares";

            if (result.Success)
            {
                msg = $"Player {result.Player} bought {amount} {shares} of stock {result.Stock} for {result.Value:C}";
            }
            else
            {
                msg = $"Player {result.Player} failed to buy {amount} {shares} of stock {result.Stock} because: {result.Message}";
            }

            await reply(msg);
        }

        private async Task Sell(Func<string, Task> reply, MarketSimulator simulator, User user, int stockId, int amount)
        {
            var player = simulator.GetDiscordPlayer(user.Id.ToString());
            if (player == null)
            {
                player = new PlayerInfo
                {
                    Name = user.Name
                };
                await simulator.AddPlayerAsync(player, user.Id.ToString());
            }

            var result = await simulator.Sell(player.Id, stockId, amount);
            string msg = "";

            var shares = amount == 1 ? "share" : "shares";

            if (result.Success)
            {
                msg = $"Player {result.Player} sold {amount} {shares} of stock {result.Stock} for {result.Value:C}";
            }
            else
            {
                msg = $"Player {result.Player} failed to sell {amount} {shares} of stock {result.Stock} because: {result.Message}";
            }

            await reply(msg);
        }

        private void OnCommandError(object sender, CommandErrorEventArgs e)
        {
            string msg = e.Exception?.Message;
            if (msg == null) //No exception - show a generic message
            {
                switch (e.ErrorType)
                {
                    case CommandErrorType.Exception:
                        msg = "Unknown error.";
                        break;
                    case CommandErrorType.BadPermissions:
                        msg = "You do not have permission to run this command.";
                        break;
                    case CommandErrorType.BadArgCount:
                        msg = "You provided the incorrect number of arguments for this command.";
                        break;
                    case CommandErrorType.InvalidInput:
                        msg = "Unable to parse your command, please check your input.";
                        break;
                    case CommandErrorType.UnknownCommand:
                        msg = "Unknown command.";
                        break;
                }
            }
            if (msg != null)
            {
                Client.ReplyError(e, msg);
                Client.Log.Error("Command", msg);
            }
        }
        private void OnCommandExecuted(object sender, CommandEventArgs e)
        {
            Client.Log.Info("Command", $"{e.Command.Text} ({e.User.Name})");
        }

        private void OnLogMessage(object sender, LogMessageEventArgs e)
        {
            //Color
            ConsoleColor color;
            switch (e.Severity)
            {
                case LogSeverity.Error: color = ConsoleColor.Red; break;
                case LogSeverity.Warning: color = ConsoleColor.Yellow; break;
                case LogSeverity.Info: color = ConsoleColor.White; break;
                case LogSeverity.Verbose: color = ConsoleColor.Gray; break;
                case LogSeverity.Debug: default: color = ConsoleColor.DarkGray; break;
            }

            //Exception
            string exMessage;
            Exception ex = e.Exception;
            if (ex != null)
            {
                while (ex is AggregateException && ex.InnerException != null)
                    ex = ex.InnerException;
                exMessage = ex.Message;
            }
            else
                exMessage = null;

            //Source
            string sourceName = e.Source?.ToString();

            //Text
            string text;
            if (e.Message == null)
            {
                text = exMessage ?? "";
                exMessage = null;
            }
            else
                text = e.Message;

            //Build message
            StringBuilder builder = new StringBuilder(text.Length + (sourceName?.Length ?? 0) + (exMessage?.Length ?? 0) + 5);
            if (sourceName != null)
            {
                builder.Append('[');
                builder.Append(sourceName);
                builder.Append("] ");
            }
            for (int i = 0; i < text.Length; i++)
            {
                //Strip control chars
                char c = text[i];
                if (!char.IsControl(c))
                    builder.Append(c);
            }
            if (exMessage != null)
            {
                builder.Append(": ");
                builder.Append(exMessage);
            }

            text = builder.ToString();
            Console.ForegroundColor = color;
            Console.WriteLine(text);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Client.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
