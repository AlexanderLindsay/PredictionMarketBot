using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PredictionMarketBot.InfoModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictionMarketBot
{
    public class InfoModule : ModuleBase
    {
        [Command("info"), Summary("displays information about the bot")]
        public async Task GetInfoAsync()
        {
            var botName = "Prediction Market Bot v0.1.0";
            var discordVersion = typeof(IDiscordClient).Assembly.GetName().Version;
            var discordInfo = $"Build using Discord.NET {discordVersion}";
            var aboutInfo = "type `$market about` for a description of what the bot does";

            var msg = $"{botName}\n{discordInfo}\n{aboutInfo}";
            await ReplyAsync(msg);
        }

        [Command("about"), Summary("describes what the bot does")]
        public async Task GetAboutMessageAsync()
        {
            var msg = "Runs a prediction market(https://en.wikipedia.org/wiki/Prediction_market) and allows users to buy and sell stocks on the market.";
            await ReplyAsync(msg);
        }
    }

    public class MarketModule : ModuleBase
    {
        private readonly MarketsManager Manager;

        public MarketModule(MarketsManager manager)
        {
            Manager = manager;
        }

        private string GetGuildId()
        {
            return Context.Guild.Id.ToString();
        }

        private MarketSimulator GetSimulator()
        {
            var id = GetGuildId();
            var sim = Manager.GetMarket(id);
            if (sim == null)
            {
                ReplyAsync("No valid market; add an active market");
            }

            return sim;
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

        private async Task ListMarkets(Func<string, Task> reply, IEnumerable<MarketInfo> markets)
        {
            var builder = new StringBuilder();

            if (!markets.Any())
            {
                await reply("No Markets");
                return;
            }

            foreach (var market in markets)
            {
                builder.AppendLine($"**{market.Name}** {market.Description}");
            }

            await reply(builder.ToString());
        }

        private async Task<PlayerInfo> GetPlayer(MarketSimulator simulator, IUser user)
        {
            var player = simulator.GetDiscordPlayer(user.Id.ToString());
            if (player == null)
            {
                player = new PlayerInfo
                {
                    Name = user.Username
                };
                player = await simulator.AddPlayerAsync(player, user.Id.ToString());
            }

            return player;
        }

        private async Task Buy(Func<string, Task> reply, MarketSimulator simulator, IUser user, int stockId, int amount)
        {
            var player = await GetPlayer(simulator, user);

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

        private async Task Sell(Func<string, Task> reply, MarketSimulator simulator, IUser user, int stockId, int? amount)
        {
            var player = await GetPlayer(simulator, user);

            if (amount == null)
            {
                var stock = player.Shares.FirstOrDefault(s => s.StockId == stockId);
                if (stock != null)
                {
                    amount = stock.Amount;
                }
            }

            var result = await simulator.Sell(player.Id, stockId, amount ?? 0);
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

        [Command("current"), Summary("displays information about the current market")]
        public async Task GetCurrentMarketAsync()
        {
            var simulator = GetSimulator();
            if (simulator == null)
                return;

            var market = simulator.GetMarketInfo();
            var prediction = simulator.Predict();

            var msg = $"**{market.Name}**\n{market.Description}\n**Prediction** {prediction.Name} ({prediction.CurrentProbability:P})";
            await ReplyAsync(msg);
            await ListStocks(async (m) => await ReplyAsync(m), simulator);
        }

        [Command("create market"), Summary("creates a new market")]
        public async Task CreateMarketAsync(
            [Summary("name of the market")] string name,
            [Summary("describe the market")] string description,
            [Summary("the amount of money each player starts with")] double money)
        {
            var result = await Manager.CreateMarket(GetGuildId(), name, description, money);

            var msg = result ? "Market created successfully" : "A market by that name already exists";
            await ReplyAsync(msg);
        }

        [Command("switch market"),
            Summary("Changes the active market"),
            Alias("change market"),
            RequireOwner()]
        public async Task ChangeMarketAsync(
            [Summary("The name of the market to switch to")] string name
            )
        {
            var result = await Manager.SetActiveMarket(GetGuildId(), name);

            var msg = result ? "Active market changed successfully" : "A market by that name already exists";
            await ReplyAsync(msg);
        }

        [Command("predict"),
            Summary("predicts the outcome of an event")]
        public async Task PredictAsync()
        {
            var simulator = GetSimulator();
            if (simulator == null)
                return;

            var market = simulator.GetMarketInfo();

            var result = simulator.Predict();

            var msg = $"**{market.Description}** {result.Name} ({result.CurrentProbability:P})";
            await ReplyAsync(msg);
        }

        [Command("list"),
            Summary("prints the list of players and stocks")]
        public async Task ListPlayersAndStocksAsync()
        {
            var simulator = GetSimulator();
            if (simulator == null)
                return;

            async Task reply(string msg)
            {
                await ReplyAsync(msg);
            }

            await ListPlayers(reply, simulator);
            await ListStocks(reply, simulator);
        }

        [Command("list players"),
            Summary("prints the list of players")]
        public async Task ListPlayersAsync()
        {
            var simulator = GetSimulator();
            if (simulator == null)
                return;

            async Task reply(string msg)
            {
                await ReplyAsync(msg);
            }

            await ListPlayers(reply, simulator);
        }

        [Command("list stocks"),
            Summary("prints the list of stocks")]
        public async Task ListStocksAsync()
        {
            var simulator = GetSimulator();
            if (simulator == null)
                return;

            async Task reply(string msg)
            {
                await ReplyAsync(msg);
            }

            await ListStocks(reply, simulator);
        }

        [Command("list markets"),
            Alias("markets"),
            Summary("prints the list of markets")]
        public async Task ListMarketsAsync()
        {
            var markets = Manager.ListMarkets(GetGuildId());
            async Task reply(string msg)
            {
                await ReplyAsync(msg);
            }

            await ListMarkets(reply, markets);
        }

        [Command("player"),
            Summary("prints info about a player")]
        public async Task GetPlayerInfoAsync(
            [Summary("The (optional) user to get info for")] IUser user = null)
        {
            var simulator = GetSimulator();
            if (simulator == null)
                return;

            PlayerInfo player;

            if (user != null)
            {
                player = simulator.GetPlayerByName(user.Username);
            }
            else
            {
                player = simulator.GetDiscordPlayer(Context.User.Id.ToString());
            }

            if (player == null)
            {
                await ReplyAsync("No such player.");
                return;
            }

            var playerInfo = DisplayPlayerInfo(player);
            await ReplyAsync(playerInfo);
        }

        [Command("open"),
            Summary("opens the market for buying and selling, but the stocks can no longer be changed"),
            RequireOwner()]
        public async Task OpenMarketAsync()
        {
            var simulator = GetSimulator();
            if (simulator == null)
                return;

            var result = await simulator.Start();
            if (result)
            {
                await ReplyAsync("Market is open.");
            }
            else
            {
                await ReplyAsync("Market was already open.");
            }
        }

        [Command("close"),
            Summary("closes the market"),
            RequireOwner()]
        public async Task ClosesMarketAsync()
        {
            var simulator = GetSimulator();
            if (simulator == null)
                return;

            var result = await simulator.Stop();
            if (result)
            {
                await ReplyAsync("Market is closed.");
            }
            else
            {
                await ReplyAsync("Market was already closed.");
            }
        }

        [Command("add stock"),
            Summary("adds a stock to the current market. Only works if the market is closed"),
            RequireOwner()]
        public async Task AddStockAsync(
            [Remainder, Summary("name of the stock")] string name)
        {
            var simulator = GetSimulator();
            if (simulator == null)
                return;

            var stock = new StockInfo
            {
                Name = name
            };

            var result = await simulator.AddStockAsync(stock);

            await ReplyAsync($"Stock {result.Name} Added.");
        }

        [Command("buy"),
            Summary("buys the given amount of the given stock")]
        public async Task BuyStockAsync(int amount, string stockName)
        {
            var simulator = GetSimulator();
            if (simulator == null)
                return;

            var stock = simulator.GetStockByName(stockName);

            var validStock = stock != null;

            if (!validStock)
            {
                await ReplyAsync("Can't find a stock by that name.");
                return;
            }

            async Task reply(string msg)
            {
                await ReplyAsync(msg);
            }

            await Buy(reply, simulator, Context.User, stock.Id, amount);
        }

        [Command("buy all"),
            Summary("buys the maximum a player can afford of the given stock")]
        public async Task BuyMaxStockAsync(string stockName)
        {
            var simulator = GetSimulator();
            if (simulator == null)
                return;

            var stock = simulator.GetStockByName(stockName);

            var validStock = stock != null;

            if (!validStock)
            {
                await ReplyAsync("Can't find a stock by that name.");
                return;
            }

            async Task reply(string msg)
            {
                await ReplyAsync(msg);
            }

            var player = await GetPlayer(simulator, Context.User);
            var amount = simulator.GetAffordableAmount(player.Id, stock.Id);

            await Buy(reply, simulator, Context.User, stock.Id, amount);
        }

        [Command("sell"),
            Summary("sells the given amount of the given stock")]
        public async Task SellStockAsync(int amount, string stockName)
        {
            var simulator = GetSimulator();
            if (simulator == null)
                return;

            var stock = simulator.GetStockByName(stockName);

            var validStock = stock != null;

            if (!validStock)
            {
                await ReplyAsync("Expects an integer for the stock id.");
                return;
            }

            async Task reply(string msg)
            {
                await ReplyAsync(msg);
            }

            await Sell(reply, simulator, Context.User, stock.Id, amount);
        }

        [Command("sell all"),
            Summary("sells all the shared  of the given stock that the player owns")]
        public async Task SellAllStockAsync(string stockName)
        {
            var simulator = GetSimulator();
            if (simulator == null)
                return;

            var stock = simulator.GetStockByName(stockName);

            var validStock = stock != null;

            if (!validStock)
            {
                await ReplyAsync("Expects an integer for the stock id.");
                return;
            }

            async Task reply(string msg)
            {
                await ReplyAsync(msg);
            }

            await Sell(reply, simulator, Context.User, stock.Id, null);
        }
    }
}
