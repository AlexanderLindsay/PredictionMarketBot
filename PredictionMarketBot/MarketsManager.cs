using PredictionMarketBot.MarketModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PredictionMarketBot
{
    public class MarketsManager
    {
        private MarketContext Context { get; set; }

        public MarketsManager(MarketContext context)
        {
            Context = context;
        }

        public MarketSimulator GetMarket(string serverId)
        {
            var active = Context.ActiveMarkets.FirstOrDefault(ma => ma.ServerId == serverId);
            if (active == null)
            {
                return null;
            }

            return new MarketSimulator(Context, active.Market);
        }

        private Market GetMarketByName(string serverId, string name)
        {
            var market = Context.Markets
                .Where(m => m.ServerId == serverId)
                .FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            return market;
        }

        public MarketSimulator CreateMarket(string serverId, string name, string description, double money, double liquidity = 100.0)
        {
            var market = new Market
            {
                ServerId = serverId,
                Name = name,
                Description = description,
                SeedMoney = money,
                Liquidity = liquidity
            };
            Context.Markets.Add(market);

            return new MarketSimulator(Context, market);
        }

        public async Task SetActiveMarket(string serverId, string name)
        {
            var market = GetMarketByName(serverId, name);

            if (market == null)
                throw new InvalidOperationException("No market found by that name.");

            var active = Context.ActiveMarkets.FirstOrDefault(ma => ma.ServerId == serverId);
            if (active == null)
            {
                active = new ActiveMarket
                {
                    ServerId = serverId,
                };
                Context.ActiveMarkets.Add(active);
            }

            active.MarketId = market.Id;

            await Context.SaveChangesAsync();
        }
    }
}
