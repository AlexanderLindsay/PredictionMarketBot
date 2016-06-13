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

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Share>()
                .HasRequired(s => s.Stock)
                .WithMany(s => s.Shares)
                .HasForeignKey(k => k.StockId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Share>()
                .HasRequired(s => s.Player)
                .WithMany(p => p.Shares)
                .HasForeignKey(k => k.PlayerId)
                .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}
