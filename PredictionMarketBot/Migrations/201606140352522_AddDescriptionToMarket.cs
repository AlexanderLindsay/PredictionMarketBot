namespace PredictionMarketBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDescriptionToMarket : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Markets", "Description", c => c.String());
            AddColumn("dbo.Markets", "SeedMoney", c => c.Double(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Markets", "SeedMoney");
            DropColumn("dbo.Markets", "Description");
        }
    }
}
