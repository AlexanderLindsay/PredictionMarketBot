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
                var manager = new MarketsManager(context);
                var token = ConfigurationManager.AppSettings["token"];

                using (var bot = new Bot("PredictiveMarket", manager))
                {
                    bot.Start(token);
                }
            }
        }
    }
}
