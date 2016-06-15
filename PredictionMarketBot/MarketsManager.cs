using PredictionMarketBot.MarketModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var market = Context.Markets.FirstOrDefault(m => m.ServerId == serverId);
            if(market == null)
            {
                return null;
            }

            return new MarketSimulator(Context, market);
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
    }
}
