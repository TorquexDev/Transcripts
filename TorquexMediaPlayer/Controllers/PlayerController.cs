using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TorquexMediaPlayer.Models;

namespace TorquexMediaPlayer.Controllers
{
    public class PlayerController : Controller
    {
        private TranscriptDBContext db = new TranscriptDBContext();

        // GET: Player
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult EventLog(JsonEventLog sEvent)
        {
            var query = from s in db.Transcripts select s;
            query = query.Where(s => s.mediaId.Equals(sEvent.mediaId));
            Transcript transcript = query.FirstOrDefault();
            EventLoad.LogEvent(User.Identity.Name, transcript.Id, sEvent.eventType, sEvent.eventValue, null, null, transcript.ProjectId);
            return Json(new { status = "SUCCESS" });
        }
    }
}