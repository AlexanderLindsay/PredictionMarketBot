namespace PredictionMarketBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIsRunningFlag : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Markets", "IsRunning", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Markets", "IsRunning");
        }
    }
}
