namespace TorquexMediaPlayer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class changeId : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Transcripts", "createby", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Transcripts", "createby", c => c.Int(nullable: false));
        }
    }
}
