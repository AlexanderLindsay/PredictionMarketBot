using System.Collections.Generic;

namespace PredictionMarketBot.MarketModels
{
    public class Player
    {
        public int Id { get; set; }
        public int MarketId { get; set; }
        public virtual Market Market { get; set; }
        public string Name { get; set; }
        public double Money { get; set; }
        public virtual ICollection<Share> Shares { get; set; }
    }
}
