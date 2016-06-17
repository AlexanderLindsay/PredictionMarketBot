namespace PredictionMarketBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddActiveMarkets : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ActiveMarkets",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ServerId = c.String(),
                        MarketId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Markets", t => t.MarketId, cascadeDelete: true)
                .Index(t => t.MarketId);
            
            AlterColumn("dbo.Markets", "ServerId", c => c.String(nullable: false, maxLength: 32));
            AlterColumn("dbo.Markets", "Name", c => c.String(nullable: false, maxLength: 100));
            CreateIndex("dbo.Markets", new[] { "ServerId", "Name" }, unique: true, name: "IX_MarketName");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ActiveMarkets", "MarketId", "dbo.Markets");
            DropIndex("dbo.Markets", "IX_MarketName");
            DropIndex("dbo.ActiveMarkets", new[] { "MarketId" });
            AlterColumn("dbo.Markets", "Name", c => c.String());
            AlterColumn("dbo.Markets", "ServerId", c => c.String());
            DropTable("dbo.ActiveMarkets");
        }
    }
}
