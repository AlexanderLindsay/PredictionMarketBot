namespace PredictionMarketBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddServerId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Markets", "ServerId", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Markets", "ServerId");
        }
    }
}
