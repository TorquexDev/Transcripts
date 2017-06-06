using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace TorquexMediaPlayer.Models
{
    public class Transcript
    {
        public int Id { get; set; }
        public string Filename { get; set; }
        public string mediaId { get; set; }
        public Nullable<System.DateTime> Processtime { get; set; }

/*       [DisplayFormat(DataFormatString = "dd/MM/yyyy hh:mm")]*/
        public Nullable<System.DateTime> CreateTime { get; set; }
        public string DirDate { get; set; }

        [DisplayName("Status")]
        public string VBstatus { get; set; }
        public string JSON { get; set; }
        public string Text_Plain { get; set; }
        public string Text_Sort { get; set; }
        public string Project { get; set; }
        public string Link { get; set; }
        public string createby { get; set; }
        public string Language { get; set; }
        public string Channels { get; set; }
        public string Vocabs { get; set; }
    }

    public class TranscriptDBContext : DbContext
    {
        public DbSet<Transcript> Transcripts { get; set; }
    }
}