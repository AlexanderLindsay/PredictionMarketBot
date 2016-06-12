using System.Collections.Generic;

namespace PredictionMarketBot.MarketModels
{
    public class Market
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Liquidity { get; set; }
        public bool IsRunning { get; set; }

        public virtual ICollection<Stock> Stocks { get; set; }
        public virtual ICollection<Player> Players { get; set; }

    }
}
