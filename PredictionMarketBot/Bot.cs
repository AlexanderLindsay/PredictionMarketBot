using Discord;
using Discord.Commands;
using PredictionMarketBot.MarketModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictionMarketBot
{
    public class Bot : IDisposable
    {
        private DiscordClient Client { get; set; }
        private MarketSimulator Simulator { get; set; }

        public Bot(string appName, MarketSimulator simulator)
        {
            Simulator = simulator;

            Client = new DiscordClient(c =>
            {
                c.AppName = appName;
                c.MessageCacheSize = 0;
                c.LogLevel = LogSeverity.Info;
                c.LogHandler = OnLogMessage;
            });

            Client.UsingCommands(c =>
            {
                c.PrefixChar = '$';
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

        private void CreateCommands()
        {
            var service = Client.GetService<CommandService>();

            service.CreateCommand("list")
                .Description("prints the list players and stocks.")
                .Do(async (e) =>
                {
                    Func<string, Task> reply = async (msg) =>
                    {
                        await Client.Reply(e, msg);
                    };
                    await ListStocks(reply);
                    await ListPlayers(reply);
                });

            service.CreateCommand("list players")
                .Description("prints the list of players.")
                .Do(async (e) =>
                {
                    Func<string, Task> reply = async (msg) =>
                    {
                        await Client.Reply(e, msg);
                    };
                    await ListPlayers(reply);
                });

            service.CreateCommand("list stocks")
                .Description("prints the list of stocks.")
                .Do(async (e) =>
                {
                    Func<string, Task> reply = async (msg) =>
                    {
                        await Client.Reply(e, msg);
                    };
                    await ListStocks(reply);
                });

            service.CreateCommand("open")
                .Description("Opens the market for buying and selling, but the stocks can no longer be changed.")
                .AddCheck((command, user, channel) => user.Server.Owner.Id == user.Id,
                    "Only the server owner can open or close the market.")
                .Do(async (e) =>
                {
                    var result = await Simulator.Start();
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
                    var result = await Simulator.Start();
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
                .Parameter("name", ParameterType.Required)
                .AddCheck((command, user, channel) => user.Server.Owner.Id == user.Id,
                    "Only the server owner can add stocks")
                .Do(async (e) =>
                {
                    var stock = new Stock
                    {
                        Name = e.GetArg("name")
                    };

                    await Simulator.AddStockAsync(stock);

                    await Client.Reply(e, $"Stock {stock.Name} ({stock.Id}) Added.");
                });

            service.CreateCommand("buy")
                .Description("Buys the given amount of the given stock.")
                .Parameter("stock", ParameterType.Required)
                .Parameter("amount", ParameterType.Required)
                .Do(async (e) =>
                {
                    int stockId;
                    var validStock = int.TryParse(e.GetArg("stock"), out stockId);

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

                    await Buy(reply, e.User, stockId, amount);
                });

            service.CreateCommand("sell")
                .Description("Sells the given amount of the given stock.")
                .Parameter("stock", ParameterType.Required)
                .Parameter("amount", ParameterType.Required)
                .Do(async (e) =>
                {
                    int stockId;
                    var validStock = int.TryParse(e.GetArg("stock"), out stockId);

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

                    await Sell(reply, e.User, stockId, amount);
                });
        }

        private async Task ListStocks(Func<string, Task> reply)
        {
            var builder = new StringBuilder();

            var stocks = Simulator.ListStocks();

            if (!stocks.Any())
            {
                await reply("No Stocks");
                return;
            }

            foreach (var stock in stocks)
            {
                builder.AppendFormat("**Stock** {0} ({1}):\n", stock.Name, stock.Id);
                builder.AppendFormat("**Shares Owned** {0}\n", stock.NumberSold);
                builder.AppendFormat("**Current Price** {0:C}\n", stock.CurrentPrice);
                builder.AppendFormat("**Probability** {0:F4}\n", stock.CurrentProbability);
                builder.AppendLine();
            }

            await reply(builder.ToString());
        }

        private async Task ListPlayers(Func<string, Task> reply)
        {
            var builder = new StringBuilder();

            var players = Simulator.ListPlayers();

            if (!players.Any())
            {
                await reply("No Players");
                return;
            }

            foreach (var player in players)
            {
                builder.AppendFormat("**Player** {0}\n", player.Name);
                builder.AppendFormat("**Funds** {0:C}\n", player.Money);
                builder.AppendFormat("**Shares** {0}\n", player.Shares
                    .Aggregate("", (str, share) => str += string.Format("{0}:{1}|", share.Stock.Name, share.Amount))
                    .Trim('|'));
                builder.AppendLine();
            }

            await reply(builder.ToString());
        }

        private async Task Buy(Func<string, Task> reply, User user, int stockId, int amount)
        {
            var player = Simulator.GetDiscordPlayer(user.Id.ToString());
            if (player == null)
            {
                player = new Player
                {
                    Name = user.Name,
                    DiscordId = user.Id.ToString(),
                    Money = 500
                };
                await Simulator.AddPlayerAsync(player);
            }

            var result = await Simulator.Buy(player.Id, stockId, amount);
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

        private async Task Sell(Func<string, Task> reply, User user, int stockId, int amount)
        {
            var player = Simulator.GetDiscordPlayer(user.Id.ToString());
            if (player == null)
            {
                player = new Player
                {
                    Name = user.Name,
                    DiscordId = user.Id.ToString(),
                    Money = 500
                };
                await Simulator.AddPlayerAsync(player);
            }

            var result = await Simulator.Sell(player.Id, stockId, amount);
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
