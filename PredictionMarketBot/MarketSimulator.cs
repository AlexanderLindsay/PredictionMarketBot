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

        public async Task AddStockAsync(Stock stock)
        {
            if (Market.IsRunning)
            {
                throw new InvalidOperationException("Can't add stocks once the market is running");
            }
            Market.Stocks.Add(stock);
            stock.MarketId = Market.Id;
            Context.Stocks.Add(stock);
            await Context.SaveChangesAsync();
            await Context.Entry(stock).ReloadAsync();
            await Context.Entry(Market).ReloadAsync();
        }

        public async Task AddPlayerAsync(Player player)
        {
            Market.Players.Add(player);
            player.MarketId = Market.Id;
            Context.Players.Add(player);
            await Context.SaveChangesAsync();
            await Context.Entry(player).ReloadAsync();
            await Context.Entry(Market).ReloadAsync();
        }

        public Player GetDiscordPlayer(string discordId)
        {
            var player = Market.Players.FirstOrDefault(p => p.DiscordId == discordId);
            return player;
        }

        public Player GetPlayerByName(string name)
        {
            var player = Market.Players.FirstOrDefault(p => p.Name == name);
            return player;
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

            var startingHoldings = Market.Stocks.Select(s => s.NumberSold);
            var endingHoldings = Market.Stocks.Select(s =>
            {
                if (s.Id == stock.Id)
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

            currentShare.Amount += amount;
            player.Money -= cost;

            await Context.SaveChangesAsync();
            await Context.Entry(Market).ReloadAsync();
            result.Success = true;
            result.Value = cost;
            return result;
        }

        public IEnumerable<Stock> ListStocks()
        {
            var holdings = Market.Stocks.Select(stock => stock.NumberSold);
            var prices = Rule.CurrentPrices(holdings, Market.Liquidity);
            var probability = Rule.Probabilities(holdings, Market.Liquidity);

            return Market.Stocks
                .Zip(prices, (stock, price) => new Stock
                {
                    Id = stock.Id,
                    Name = stock.Name,
                    Shares = stock.Shares.ToList(),
                    CurrentPrice = price
                })
                .Zip(probability, (stock, p) =>
                {
                    stock.CurrentProbability = p;
                    return stock;
                });
        }

        public IEnumerable<Player> ListPlayers()
        {
            return Market.Players.Select(player => new Player
            {
                Id = player.Id,
                Name = player.Name,
                Money = player.Money,
                Shares = player.Shares.Select(share => new Share
                {
                    Id = share.Id,
                    StockId = share.StockId,
                    Stock = share.Stock,
                    Amount = share.Amount
                }).ToList()
            });
        }
    }
}
