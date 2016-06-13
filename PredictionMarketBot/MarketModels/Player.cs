using System.Collections.Generic;

namespace PredictionMarketBot.MarketModels
{
    public class Player
    {
        public int Id { get; set; }
        public int MarketId { get; set; }
        public virtual Market Market { get; set; }
        public string DiscordId { get; set; }
        public string Name { get; set; }
        public double Money { get; set; }

        private ICollection<Share> _shares;
        public virtual ICollection<Share> Shares
        {
            get { return _shares ?? (_shares = new HashSet<Share>()); }
            set { _shares = value; }
        }
    }
}
