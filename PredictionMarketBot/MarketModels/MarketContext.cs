using System.Data.Entity;

namespace PredictionMarketBot.MarketModels
{
    public class MarketContext : DbContext
    {
        public DbSet<Market> Markets { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<Share> Shares { get; set; }

        public MarketContext() : base("DefaultConnection") { }
    }
}
