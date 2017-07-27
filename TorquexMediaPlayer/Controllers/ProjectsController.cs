using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TorquexMediaPlayer.Models;
using System.Data.SqlClient;

namespace TorquexMediaPlayer.Controllers
{
    public class ProjectsController : Controller
    {
        private TranscriptDBContext db = new TranscriptDBContext();

        // GET: Projects
        public ActionResult Index()
        {
            var projects = from s in db.Projects select s;
            projects = projects.Where(s => s.CreateBy.Equals(User.Identity.Name));
            return View(projects.ToList());
        }

        // GET: Projects/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // GET: Projects/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Projects/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProjectUpload formdata)
        {

            if (ModelState.IsValid)
            { 

                var project = new Project();
                project.CreateBy = User.Identity.Name;
                project.CreateDate = DateTime.Now;
                project.PageFooter = formdata.PageFooter;
                project.PageTitle = formdata.PageTitle;
                project.ProjectName = formdata.ProjectName;
                project.DeptName = formdata.DeptName;
                string ext = Path.GetExtension(formdata.file.FileName);
                string fn = Path.GetFileNameWithoutExtension(formdata.file.FileName);
                string random = "_" + Path.GetRandomFileName().Replace(".", "").Substring(0, 8);
                string saveFile = Server.MapPath("~/Content/ProjectLogos") + "\\" + fn + random + ext;
                project.PageLogo = fn + random + ext;
                formdata.file.SaveAs(saveFile);
                db.Projects.Add(project);
                db.SaveChanges();
                ViewBag.Message = "File has been uploaded successfully";
                ModelState.Clear();
                return RedirectToAction("Index");
            }
                return View();
        }

        // GET: Projects/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);


            if (project == null)
            {
                return HttpNotFound();
            }

            var projectEdit = new ProjectUpload();
            projectEdit.DeptName = project.DeptName;
            projectEdit.PageFooter = project.PageFooter;
            projectEdit.PageLogo = "/Content/ProjectLogos/" + project.PageLogo;
            projectEdit.PageTitle = project.PageTitle;
            projectEdit.ProjectName = project.ProjectName;
            projectEdit.ID = project.ID;

            return View(projectEdit);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProjectUpload formdata)
        {
            if (ModelState.IsValid)
            {

                Project project = db.Projects.Find(formdata.ID);

                project.PageFooter = formdata.PageFooter;
                project.PageTitle = formdata.PageTitle;
                project.ProjectName = formdata.ProjectName;
                project.DeptName = formdata.DeptName;

                if (formdata.file != null)
                { 
                    string ext = Path.GetExtension(formdata.file.FileName);
                    string fn = Path.GetFileNameWithoutExtension(formdata.file.FileName);
                    string random = "_" + Path.GetRandomFileName().Replace(".", "").Substring(0, 8);
                    string saveFile = Server.MapPath("~/Content/ProjectLogos") + "\\" + fn + random + ext;
                    project.PageLogo = fn + random + ext;
                    formdata.file.SaveAs(saveFile);
                }
                db.Entry(project).State = EntityState.Modified;
                db.SaveChanges();
                ViewBag.Message = "File has been uploaded successfully";
                ModelState.Clear();
                return RedirectToAction("Index");
            }
            return View(formdata);
        }


    // GET: Projects/Unit/5
    [AllowAnonymous]
        public ActionResult Unit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);


            if (project == null)
            {
                return HttpNotFound();
            }

            var projectUnit = new PublicProject();
            projectUnit.DeptName = project.DeptName;
            projectUnit.PageFooter = project.PageFooter;
            projectUnit.PageLogo = "/Content/ProjectLogos/" + project.PageLogo;
            projectUnit.PageTitle = project.PageTitle;
            projectUnit.ProjectName = project.ProjectName;
            projectUnit.ID = project.ID;

            var transcripts = from s in db.Transcripts select s;
            transcripts = transcripts.Where(s => s.ProjectId == id);

            projectUnit.Transcripts = transcripts.ToList();

            return View(projectUnit);
        }

        [AllowAnonymous]
        // GET: Transcripts/Details/5
        public ActionResult Play(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Transcript transcript = db.Transcripts.Find(id);
            if (transcript == null)
            {
                return HttpNotFound();
            }

            var PlayProject = new ProjectTranscript();

            Project project = db.Projects.Find(transcript.ProjectId);

            PlayProject.transcript = transcript;
            PlayProject.PageLogo = "/Content/ProjectLogos/" + project.PageLogo;

            return View(PlayProject);
        }


        // GET: Projects/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Project project = db.Projects.Find(id);
            db.Projects.Remove(project);
            db.SaveChanges();
            var sId = new SqlParameter("@id", id);
            db.Database.ExecuteSqlCommand("update Transcripts set ProjectId = null, Project = null where ProjectId = @id;", sId);
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
