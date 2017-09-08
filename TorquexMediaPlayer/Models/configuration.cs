using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorquexMediaPlayer.Models
{

    public class cfgingest
    {
        public bool diarization { get; set; }
        public cfgchannels channels { get; set; }
    }

    public class cfgtranscripts
    {
        public string engine { get; set; }
        public IList<cfgvocabs> vocabularies { get; set; }
        
    }

    public class cfgvocabs
    {
        public IList<string> terms { get; set; }
    }

    public class cfgchannels
    {
        public cfgleft left { get; set; }
        public cfgright right { get; set; }
    }

    public class cfgleft
    {
        public string speaker { get; set; }
    }

    public class cfgright
    {
        public string speaker { get; set; }
    }

    public class detections
    {
        public string model { get; set; }
    }

    public class configuration
    {
        public string executor { get; set; }
        public string language { get; set; }
        public cfgtranscripts transcripts { get; set; }
        public cfgingest ingest { get; set; }
        public List<detections> detections { get; set; }

    }

    public class configwrapper
    {
        public configuration configuration { get; set; }
    }


}