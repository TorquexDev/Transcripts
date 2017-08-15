using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace TorquexMediaPlayer.Models
{
    public class Transcript
    {
        public int Id { get; set; }

        [DisplayName("File Name")]
        public string Filename { get; set; }
        public string PlayFile { get; set; }
        public string mediaId { get; set; }
        public Nullable<System.DateTime> Processtime { get; set; }

        [DisplayName("Submitted")]
        public Nullable<System.DateTime> CreateTime { get; set; }
        public string DirDate { get; set; }

        [DisplayName("Status")]
        public string VBstatus { get; set; }
        public string JSON { get; set; }
        [DisplayName("Transcript")]
        public string Text_Plain { get; set; }
        public string Text_Sort { get; set; }
        public string Text_Vtt { get; set; }
        public string Project { get; set; }
        public int? ProjectId { get; set; }
        public string Link { get; set; }

        [DisplayName("Submitted By")]
        public string createby { get; set; }
        public string Language { get; set; }
        public string Channels { get; set; }
        public string Vocabs { get; set; }
        public bool Diarization { get; set; }
        public int? Duration { get; set; }
        public int? WordCount {get; set;}
        public bool Active { get; set; }
    }

    public class WordChange
    {
        public int id { get; set; }
        public int TranscriptId { get; set; }
        public int p { get; set;}
        public int s { get; set; }
        public string m { get; set; }
        public string oldWord { get; set; }
        public string newWord { get; set; }
        public string changeBy { get; set; }
        public DateTime changeDate { get; set; }
    }

    public class TranscriptDBContext : DbContext
    {
        public DbSet<Transcript> Transcripts { get; set; }
        public DbSet<WordChange> WordChanges { get; set; }
        public DbSet<SupportRequest> SupportRequests { get; set; }
        public DbSet<TorquexMediaPlayer.Models.Event> Events { get; set; }

        public System.Data.Entity.DbSet<TorquexMediaPlayer.Models.Project> Projects { get; set; }
    }

    public class SrtFile : ICloneable
    {
        public object Clone()
        {
            return this.MemberwiseClone();
        }
        public string pos { get; set; }
        public string time { get; set; }
        public int s { get; set; }
        public int e { get; set; }
        public string content { get; set; }
    }
}