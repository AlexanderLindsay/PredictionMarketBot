namespace PredictionMarketBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialMigration : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Markets",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Liquidity = c.Double(nullable: false),
                        IsRunning = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Players",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MarketId = c.Int(nullable: false),
                        DiscordId = c.String(),
                        Name = c.String(),
                        Money = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Markets", t => t.MarketId, cascadeDelete: true)
                .Index(t => t.MarketId);
            
            CreateTable(
                "dbo.Shares",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StockId = c.Int(nullable: false),
                        PlayerId = c.Int(nullable: false),
                        Amount = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Players", t => t.PlayerId)
                .ForeignKey("dbo.Stocks", t => t.StockId)
                .Index(t => t.StockId)
                .Index(t => t.PlayerId);
            
            CreateTable(
                "dbo.Stocks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MarketId = c.Int(nullable: false),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Markets", t => t.MarketId, cascadeDelete: true)
                .Index(t => t.MarketId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Shares", "StockId", "dbo.Stocks");
            DropForeignKey("dbo.Stocks", "MarketId", "dbo.Markets");
            DropForeignKey("dbo.Shares", "PlayerId", "dbo.Players");
            DropForeignKey("dbo.Players", "MarketId", "dbo.Markets");
            DropIndex("dbo.Stocks", new[] { "MarketId" });
            DropIndex("dbo.Shares", new[] { "PlayerId" });
            DropIndex("dbo.Shares", new[] { "StockId" });
            DropIndex("dbo.Players", new[] { "MarketId" });
            DropTable("dbo.Stocks");
            DropTable("dbo.Shares");
            DropTable("dbo.Players");
            DropTable("dbo.Markets");
        }
    }
}
