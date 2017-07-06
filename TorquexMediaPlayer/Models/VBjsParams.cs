using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorquexMediaPlayer.Models
{

    public class Words : ICloneable
    {
        public object Clone()
        {
            return this.MemberwiseClone();
        }
        public int p { get; set; }
        public int s { get; set; }
        public float c { get; set; }
        public int e { get; set; }
        public string w { get; set; }
        public string m { get; set; }
    }

    public class Diarization
    {
        public string speakerlabel { get; set; }
        public string band { get; set; }
        public int start { get; set; }
        public int length { get; set; }
        public string gender { get; set; }
        public string env { get; set; }
    }


    public class Latest
    {
        public Diarization[] diarization { get; set; }
        public Words[] words { get; set; }
    }


    public class Transcripts
    {
        public Latest latest { get; set; }
    }

    public class keyt
    {
        public float[] unknown { get; set; }
    }

    public class Wordskey
    {
        public string name { get; set; }
        public keyt t { get; set; }
        public float relevance { get; set; }
        public string[] internalName { get; set; }
        public float score { get; set; }
    }

    public class Latestkey
    {
        public Wordskey[] words { get; set; }
    }

    public class Keywords
    {
        public Latestkey latest { get; set; }
    }

    public class TopicsTop
    {
        public float score { get; set; }
        public Wordskey[] keywords { get; set; }
        public string name { get; set; }

    }

    public class LatestTop
    {
        public TopicsTop[] topics { get; set; }
    }

    public class Topics
    {
        public LatestTop latest { get; set; }
    }

    public class Length
    {
        public int milliseconds { get; set; }
        public string descriptive { get; set; }
    }

    public class Metadata
    {
        public Length length { get; set; }
    }

    public class MediaResponse
    {
        public string mediaId { get; set; }
        public string status { get; set; }
        public Transcripts transcripts { get; set; }
        public Keywords keywords { get; set; }
        public Topics topics { get; set; }
        public Metadata metadata { get; set; }
        //       public string message { get; set; }
    }

    public class Media
    {
        //        public string media { get; set; }
        public MediaResponse media { get; set; }
    }


}