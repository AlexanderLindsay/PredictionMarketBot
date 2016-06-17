using PredictionMarketBot.InfoModels;
using PredictionMarketBot.MarketModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PredictionMarketBot
{
    public class MarketSimulator
    {
        private Market Market { get; set; }
        private MarketContext Context { get; set; }

        private IMarketScoringRule Rule { get; set; }

        public MarketSimulator(MarketContext context, int marketId)
            : this(new LogarithmicMarketScoringRule(), context, marketId)
        { }

        public MarketSimulator(IMarketScoringRule rule, MarketContext context, int marketId)
        {
            Rule = rule;

            Context = context;
            Market = Context.Markets.FirstOrDefault(m => m.Id == marketId);
            if (Market == null)
            {
                throw new ArgumentException($"No marker found with Id {marketId}");
            }
        }

        public MarketSimulator(MarketContext context, Market market)
            : this(new LogarithmicMarketScoringRule(), context, market)
        { }

        public MarketSimulator(IMarketScoringRule rule, MarketContext context, Market market)
        {
            Rule = rule;
            Context = context;
            Market = market;
        }

        public async Task<bool> Start()
        {
            if (Market.IsRunning)
                return false;

            Market.IsRunning = true;
            await Context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Stop()
        {
            if (!Market.IsRunning)
                return false;

            Market.IsRunning = false;
            await Context.SaveChangesAsync();
            return true;
        }

        public async Task<StockInfo> AddStockAsync(StockInfo stock)
        {
            if (Market.IsRunning)
            {
                throw new InvalidOperationException("Can't add stocks once the market is running");
            }

            //If duplicate stock names within a market
            var existing = Market.Stocks.FirstOrDefault(st => st.Name == stock.Name);
            if(existing != null)
            {
                return ToInfo(existing);
            }

            var s = new Stock
            {
                Name = stock.Name,
                MarketId = Market.Id
            };

            Market.Stocks.Add(s);
            Context.Stocks.Add(s);
            await Context.SaveChangesAsync();
            await Context.Entry(s).ReloadAsync();
            await Context.Entry(Market).ReloadAsync();
            return ToInfo(s);
        }

        public async Task<PlayerInfo> AddPlayerAsync(PlayerInfo player, string discordId)
        {
            var p = new Player
            {
                MarketId = Market.Id,
                Name = player.Name,
                Money = Market.SeedMoney,
                DiscordId = discordId
            };

            Market.Players.Add(p);
            Context.Players.Add(p);
            await Context.SaveChangesAsync();
            await Context.Entry(p).ReloadAsync();
            await Context.Entry(Market).ReloadAsync();
            return ToInfo(p);
        }

        private PlayerInfo ToInfo(Player player)
        {
            if (player == null)
                return null;

            return new PlayerInfo
            {
                Id = player.Id,
                Name = player.Name,
                Money = player.Money,
                Shares = player.Shares.Select(share => new ShareInfo
                {
                    Player = player.Name,
                    StockId = share.StockId,
                    Stock = share.Stock.Name,
                    Amount = share.Amount
                }).ToList()
            };
        }

        private StockInfo ToInfo(Stock stock)
        {
            if (stock == null)
                return null;

            return new StockInfo
            {
                Id = stock.Id,
                Name = stock.Name
            };
        }

        public PlayerInfo GetDiscordPlayer(string discordId)
        {
            var player = Market.Players.FirstOrDefault(p => p.DiscordId == discordId);
            return ToInfo(player);
        }

        public PlayerInfo GetPlayerByName(string name)
        {
            var player = Market.Players.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return ToInfo(player);
        }

        private Player GetPlayer(int playerId)
        {
            var player = Market.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
            {
                throw new ArgumentException($"No player found with Id {playerId}");
            }

            return player;
        }

        public StockInfo GetStockByName(string name)
        {
            var stock = Market.Stocks.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return ToInfo(stock);
        }

        private Stock GetStock(int stockId)
        {
            var stock = Market.Stocks.FirstOrDefault(s => s.Id == stockId);
            if (stock == null)
            {
                throw new ArgumentException($"No stock found with Id {stockId}");
            }

            return stock;
        }

        public async Task<TransactionResult> Buy(int playerId, int stockId, int amount)
        {
            if (!Market.IsRunning)
            {
                throw new InvalidOperationException("The market must be running to buy stocks");
            }

            var player = GetPlayer(playerId);
            var stock = GetStock(stockId);

            return await TransactionAsync(player, stock, amount);
        }

        public async Task<TransactionResult> Sell(int playerId, int stockId, int amount)
        {
            if (!Market.IsRunning)
            {
                throw new InvalidOperationException("The market must be running to sell stocks");
            }

            var player = GetPlayer(playerId);
            var stock = GetStock(stockId);

            return await TransactionAsync(player, stock, -1 * amount);
        }

        private async Task<TransactionResult> TransactionAsync(Player player, Stock stock, int amount)
        {
            var result = new TransactionResult
            {
                Player = player.Name,
                Stock = stock.Name
            };

            if(amount == 0)
            {
                result.Success = false;
                result.Message = "Amount to sell or buy can't be zero";
            }

            var baseHoldings = Market.Stocks.Select(s => new { Stock = s, NumberSold = s.Shares.Sum(h => h.Amount) });
            var startingHoldings = baseHoldings.Select(s => s.NumberSold);
            var endingHoldings = baseHoldings.Select(s =>
            {
                if (s.Stock.Id == stock.Id)
                {
                    return s.NumberSold + amount;
                }
                else
                {
                    return s.NumberSold;
                }
            });

            var cost = Rule.CalculateChange(startingHoldings, endingHoldings, Market.Liquidity);

            if (player.Money < cost)
            {
                result.Success = false;
                result.Message = $"Not enough money to buy shares. Cost: {cost}";
                return result;
            }

            var currentShare = player.Shares.FirstOrDefault(s => s.StockId == stock.Id);
            if (currentShare == null)
            {
                currentShare = new Share { StockId = stock.Id, Amount = 0 };
                player.Shares.Add(currentShare);
            }

            if(currentShare.Amount + amount < 0)
            {
                result.Success = false;
                result.Message = $"Not enough holding shares to sell {amount}.";
                return result;
            }

            currentShare.Amount += amount;
            player.Money -= cost;

            await Context.SaveChangesAsync();
            await Context.Entry(Market).ReloadAsync();
            result.Success = true;
            result.Value = cost;
            return result;
        }

        public MarketInfo GetMarketInfo()
        {
            return new MarketInfo
            {
                Name = Market.Name,
                Description = Market.Description
            };
        }

        public IEnumerable<StockInfo> ListStocks()
        {
            var holdings = Market.Stocks.Select(stock => stock.Shares.Sum(s => s.Amount));
            var prices = Rule.CurrentPrices(holdings, Market.Liquidity);
            var probability = Rule.Probabilities(holdings, Market.Liquidity);

            return Market.Stocks
                .Zip(prices, (stock, price) => new StockInfo
                {
                    Id = stock.Id,
                    Name = stock.Name,
                    NumberSold = stock.Shares.Sum(s => s.Amount),
                    CurrentPrice = price
                })
                .Zip(probability, (stock, p) =>
                {
                    stock.CurrentProbability = p;
                    return stock;
                });
        }

        public IEnumerable<PlayerInfo> ListPlayers()
        {
            return Market.Players.Select(player => ToInfo(player));
        }
    }
}
