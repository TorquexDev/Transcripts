namespace TorquexMediaPlayer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class upate1 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Transcripts", "Language", c => c.String());
            AddColumn("dbo.Transcripts", "Channels", c => c.String());
            AddColumn("dbo.Transcripts", "Vocabs", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Transcripts", "Vocabs");
            DropColumn("dbo.Transcripts", "Channels");
            DropColumn("dbo.Transcripts", "Language");
        }
    }
}
