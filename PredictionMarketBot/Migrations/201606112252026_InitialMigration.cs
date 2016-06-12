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
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Players",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MarketId = c.Int(nullable: false),
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
                        Amount = c.Int(nullable: false),
                        Player_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Stocks", t => t.StockId, cascadeDelete: true)
                .ForeignKey("dbo.Players", t => t.Player_Id)
                .Index(t => t.StockId)
                .Index(t => t.Player_Id);
            
            CreateTable(
                "dbo.Stocks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MarketId = c.Int(nullable: false),
                        Name = c.String(),
                        NumberSold = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Markets", t => t.MarketId, cascadeDelete: true)
                .Index(t => t.MarketId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Shares", "Player_Id", "dbo.Players");
            DropForeignKey("dbo.Shares", "StockId", "dbo.Stocks");
            DropForeignKey("dbo.Stocks", "MarketId", "dbo.Markets");
            DropForeignKey("dbo.Players", "MarketId", "dbo.Markets");
            DropIndex("dbo.Stocks", new[] { "MarketId" });
            DropIndex("dbo.Shares", new[] { "Player_Id" });
            DropIndex("dbo.Shares", new[] { "StockId" });
            DropIndex("dbo.Players", new[] { "MarketId" });
            DropTable("dbo.Stocks");
            DropTable("dbo.Shares");
            DropTable("dbo.Players");
            DropTable("dbo.Markets");
        }
    }
}
