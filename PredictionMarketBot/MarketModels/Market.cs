using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PredictionMarketBot.MarketModels
{
    public class Market
    {
        public int Id { get; set; }
        [Required, MaxLength(32), Index("IX_MarketName", 1, IsUnique = true)]
        public string ServerId { get; set; }
        [Required, MaxLength(100), Index("IX_MarketName", 2, IsUnique = true)]
        public string Name { get; set; }
        public string Description { get; set; }
        public double SeedMoney { get; set; }
        public double Liquidity { get; set; }
        public bool IsRunning { get; set; }

        private ICollection<Stock> _stocks;
        public virtual ICollection<Stock> Stocks
        {
            get { return _stocks ?? (_stocks = new HashSet<Stock>()); }
            protected set { _stocks = value; }
        }

        private ICollection<Player> _players;
        public virtual ICollection<Player> Players
        {
            get { return _players ?? (_players = new HashSet<Player>()); }
            protected set { _players = value; }
        }
    }
}
