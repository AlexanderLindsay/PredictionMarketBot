using PredictionMarketBot.MarketModels;
using System.Configuration;
using System.Linq;

namespace PredictionMarketBot
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var context = new MarketContext())
            {
                var market = context.Markets.FirstOrDefault();
                if (market == null)
                {
                    market = new Market() { Liquidity = 100.0, SeedMoney = 500.0, IsRunning = false };
                    context.Markets.Add(market);
                    context.SaveChanges();
                    context.Entry(market).Reload();
                }

                var simulator = new MarketSimulator(context, market.Id);
                var token = ConfigurationManager.AppSettings["token"];

                using (var bot = new Bot("PredictiveMarket", simulator))
                {
                    bot.Start(token);
                }
            }
        }
    }
}
