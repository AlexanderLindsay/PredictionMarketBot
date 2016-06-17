namespace PredictionMarketBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MakeStocksUnique : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Stocks", new[] { "MarketId" });
            AlterColumn("dbo.Stocks", "Name", c => c.String(nullable: false, maxLength: 100));
            CreateIndex("dbo.Stocks", new[] { "MarketId", "Name" }, unique: true, name: "IX_Market_StockName");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Stocks", "IX_Market_StockName");
            AlterColumn("dbo.Stocks", "Name", c => c.String());
            CreateIndex("dbo.Stocks", "MarketId");
        }
    }
}
