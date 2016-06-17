namespace PredictionMarketBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RequirePlayerName : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Players", "Name", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Players", "Name", c => c.String());
        }
    }
}
