using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorquexMediaPlayer.Models
{
    public class Event
    {
        public int id { get; set; }
        public string UserName { get; set; }
        public string IPaddress { get; set; }
        public string SessionId { get; set; }
        public DateTime ActionDate { get; set; }
        public string EventName { get; set; }
        public int? TranscriptId { get; set; }
        public string SearchTerm { get; set; }
        public string OldWord { get; set; }
        public string NewWord { get; set; }
    }
}