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
        public int? ProjectId { get; set; }
        public string SearchTerm { get; set; }
        public string OldWord { get; set; }
        public string NewWord { get; set; }
    }

    public class JsonEventLog
    {
        public string eventType { get; set; }
        public string mediaId { get; set; }
        public string eventValue { get; set; }
    }


    public class EventLoad
    {

        public static Boolean LogEvent(string UserName, int? TranscriptId,string EventName, string SearchTerm, string OldWord, string NewWord, int? ProjectId)
        {
            TranscriptDBContext db = new TranscriptDBContext();
            Event ev = new Event();
            ev.ActionDate = DateTime.Now;
            ev.SessionId = HttpContext.Current.Session.SessionID;
            ev.IPaddress = HttpContext.Current.Request.UserHostAddress;
            ev.TranscriptId = TranscriptId;
            ev.UserName = UserName;
            if (ProjectId == null) {
                if (TranscriptId != null)
                {
                    Transcript transcript = db.Transcripts.Find(TranscriptId);
                    ev.ProjectId = transcript.ProjectId;
                }
            } else ev.ProjectId = ProjectId;
            ev.EventName = EventName;
            ev.SearchTerm = SearchTerm;
            ev.OldWord = OldWord;
            ev.NewWord = NewWord;
            db.Events.Add(ev);
            db.SaveChanges();
            
            return true;
        }

    }

}