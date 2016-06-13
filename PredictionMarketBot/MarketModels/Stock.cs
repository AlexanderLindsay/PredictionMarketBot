using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace PredictionMarketBot.MarketModels
{
    public class Stock
    {
        public int Id { get; set; }
        public int MarketId { get; set; }
        public virtual Market Market { get; set; }
        public string Name { get; set; }

        private ICollection<Share> _shares;
        public virtual ICollection<Share> Shares
        {
            get { return _shares ?? (_shares = new HashSet<Share>()); }
            set { _shares = value; }
        }

        [NotMapped]
        public int NumberSold { get { return Shares.Sum(s => s.Amount); } }

        [NotMapped]
        public double CurrentPrice { get; set; }
        [NotMapped]
        public double CurrentProbability { get; set; }
    }
}
