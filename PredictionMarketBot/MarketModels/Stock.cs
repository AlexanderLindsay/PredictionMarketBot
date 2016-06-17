using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PredictionMarketBot.MarketModels
{
    public class Stock
    {
        public int Id { get; set; }

        [Index("IX_Market_StockName", 1, IsUnique = true)]
        public int MarketId { get; set; }
        public virtual Market Market { get; set; }

        [Required, Index("IX_Market_StockName", 2, IsUnique = true), MaxLength(100)]
        public string Name { get; set; }

        private ICollection<Share> _shares;
        public virtual ICollection<Share> Shares
        {
            get { return _shares ?? (_shares = new HashSet<Share>()); }
            protected set { _shares = value; }
        }
    }
}
