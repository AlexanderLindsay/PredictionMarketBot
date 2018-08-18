using PredictionMarketBot.MarketModels;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace PredictionMarketBot
{
    class Program
    {
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            using (var context = new MarketContext())
            {
                var manager = new MarketsManager(context);
                var token = ConfigurationManager.AppSettings["token"];

                using (var bot = new Bot("PredictiveMarket", manager))
                {
                    await bot.Start(token);
                }
            }
        }
    }
}
