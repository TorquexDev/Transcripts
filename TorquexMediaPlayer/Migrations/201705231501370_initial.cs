namespace TorquexMediaPlayer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Transcripts",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Filename = c.String(),
                        mediaId = c.String(),
                        Processtime = c.DateTime(),
                        CreateTime = c.DateTime(),
                        DirDate = c.String(),
                        VBstatus = c.String(),
                        JSON = c.String(),
                        Text_Plain = c.String(),
                        Text_Sort = c.String(),
                        Project = c.String(),
                        Link = c.String(),
                        createby = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Transcripts");
        }
    }
}
