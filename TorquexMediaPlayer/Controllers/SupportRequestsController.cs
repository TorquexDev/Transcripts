using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TorquexMediaPlayer.Models;
using System.Net.Mail;
using System.Configuration;

namespace TorquexMediaPlayer.Controllers
{
    public class SupportRequestsController : Controller
    {
        private TranscriptDBContext db = new TranscriptDBContext();

        // GET: SupportRequests
        public ActionResult Index()
        {
            return View(db.SupportRequests.ToList());
        }

        // GET: SupportRequests/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SupportRequest supportRequest = db.SupportRequests.Find(id);
            if (supportRequest == null)
            {
                return HttpNotFound();
            }
            return View(supportRequest);
        }

        // GET: SupportRequests/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: SupportRequests/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Subject,Details,Attachment,CreateBy,CreateDate,Email")] SupportRequest supportRequest)
        {
            if (ModelState.IsValid)
            {
                supportRequest.CreateBy = User.Identity.Name;
                supportRequest.CreateDate = DateTime.Now;
                db.SupportRequests.Add(supportRequest);
                db.SaveChanges();
                SmtpClient client = new SmtpClient(ConfigurationManager.AppSettings["AWS-SES-host"], 587);
                client.Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["AWS-SES-username"], ConfigurationManager.AppSettings["AWS-SES-password"]);
                client.EnableSsl = true;
                try
                {
                    client.Send("support@torquex.com.au", "simon.brooks@torquex.com.au", supportRequest.Subject, supportRequest.Details);
                } catch (Exception ex)
                {

                }


                return RedirectToAction("Index");
            }

            return View(supportRequest);
        }

        // GET: SupportRequests/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SupportRequest supportRequest = db.SupportRequests.Find(id);
            if (supportRequest == null)
            {
                return HttpNotFound();
            }
            return View(supportRequest);
        }

        // POST: SupportRequests/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Subject,Details,Attachment,CreateBy,CreateDate,Email")] SupportRequest supportRequest)
        {
            if (ModelState.IsValid)
            {
                db.Entry(supportRequest).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(supportRequest);
        }

        // GET: SupportRequests/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SupportRequest supportRequest = db.SupportRequests.Find(id);
            if (supportRequest == null)
            {
                return HttpNotFound();
            }
            return View(supportRequest);
        }

        // POST: SupportRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            SupportRequest supportRequest = db.SupportRequests.Find(id);
            db.SupportRequests.Remove(supportRequest);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
