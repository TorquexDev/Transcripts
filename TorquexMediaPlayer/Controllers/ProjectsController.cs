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
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using TorquexUtilities;
using System.Text;
using System.Web.Script.Serialization;
using Novacode;

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
            EventLoad.LogEvent(User.Identity.Name, null, "List_Projects", null, null, null, null);
            return View(projects.ToList());

        }

        [AllowAnonymous]
        public Boolean downloadTranscript(int projectid)
        {
                var transcripts = from s in db.Transcripts select s;
                transcripts = transcripts.Where(s => s.ProjectId  == projectid);
                string filepath = Server.MapPath("~/temp/");
                string fname;
                string filename;

                foreach (var transcript in transcripts)
                {
                    fname = System.IO.Path.GetFileNameWithoutExtension(transcript.Filename);
                    filename = filepath + fname + ".docx";
                    CreateFormatWordDoc(filename, transcript.JSON);
                }

            return true;
        }

        private Boolean CreateFormatWordDoc(string filename, string jsonTranscript)
        {
            var json_serializer = new JavaScriptSerializer();
            int splitTime = 1000;
            int end = 500000;  // set large so we don't put a paragraph split at the start of the file.

            Media VBJson = json_serializer.Deserialize<Media>(jsonTranscript);

            DocX doc = DocX.Create(filename);
            Paragraph p;

            p = doc.InsertParagraph();

            foreach (Words word in VBJson.media.transcripts.latest.words)
            {
                if (word.m != null)
                {
                    if (word.m == "turn")
                    {
                        p.Append("\n");
                        p = doc.InsertParagraph();
                        p.Append(word.w + " : ").Bold();
                    }
                    else if (word.m == "punc")
                    {
                        p.Append(word.w);
                    }
                    else
                    {
                        p.Append(" " + word.w);
                    }
                }
                else if ((word.s - end) > splitTime)
                {
                    p.Append("\n");
                    p = doc.InsertParagraph();
                    p.Append(word.w);
                }
                else
                {
                    p.Append(" " + word.w);
                }
                end = word.e;
            }

            doc.Save();
            return true;
        }

        // GET: Projects/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            EventLoad.LogEvent(User.Identity.Name, null, "Project_Details", null, null, null, id);
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
                project.Password = formdata.Password;
                project.Email = formdata.Email;
                if (formdata.file != null)
                {
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(System.Configuration.ConfigurationManager.AppSettings["StorageConnectionString"]);

                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer blobContainer = blobClient.GetContainerReference("torquexmediaplayer");

                    string ext = Path.GetExtension(formdata.file.FileName);
                    string fn = Path.GetFileNameWithoutExtension(formdata.file.FileName);
                    string random = "_" + Path.GetRandomFileName().Replace(".", "").Substring(0, 8);
//                    string saveFile = Server.MapPath("~/Content/ProjectLogos") + "\\" + fn + random + ext;
                    project.PageLogo = fn + random + ext;

                    string blockName = StringUtils.blockName(project.PageLogo);

                    if (!string.IsNullOrEmpty(blockName))
                    {
                        CloudBlockBlob blob = blobContainer.GetBlockBlobReference(blockName);
                        //upload files 
                        blob.UploadFromStream(formdata.file.InputStream);
                    }


//                    formdata.file.SaveAs(saveFile);
                }
                db.Projects.Add(project);
                db.SaveChanges();
                EventLoad.LogEvent(User.Identity.Name, null, "Project_Details", null, null, null, project.ID);
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
            projectEdit.PageLogo = "https://torquexstorage01.blob.core.windows.net/torquexmediaplayer/" + project.PageLogo;
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
                project.Email = formdata.Email;
                project.Password = formdata.Password;

                if (formdata.file != null)
                {
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(System.Configuration.ConfigurationManager.AppSettings["StorageConnectionString"]);
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer blobContainer = blobClient.GetContainerReference("torquexmediaplayer");

                    string ext = Path.GetExtension(formdata.file.FileName);
                    string fn = Path.GetFileNameWithoutExtension(formdata.file.FileName);
                    string random = "_" + Path.GetRandomFileName().Replace(".", "").Substring(0, 8);
                    //                    string saveFile = Server.MapPath("~/Content/ProjectLogos") + "\\" + fn + random + ext;
                    project.PageLogo = fn + random + ext;

                    string blockName = StringUtils.blockName(project.PageLogo);

                    if (!string.IsNullOrEmpty(blockName))
                    {
                        CloudBlockBlob blob = blobContainer.GetBlockBlobReference(blockName);
                        //upload files 
                        blob.UploadFromStream(formdata.file.InputStream);
                    }
                }
                db.Entry(project).State = EntityState.Modified;
                db.SaveChanges();
                EventLoad.LogEvent(User.Identity.Name, null, "Project_Edit", null, null, null, project.ID);
                ViewBag.Message = "File has been uploaded successfully";
                ModelState.Clear();
                return RedirectToAction("Index");
            }
            

            return View(formdata);
        }

        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            } else { 

                var login = new ProjectLoginViewModel();
                login.id = System.Convert.ToInt32(id);
                Project project = db.Projects.Find(id);
                login.DeptName = project.DeptName;
                login.PageTitle = project.PageTitle;
                login.PageLogo = "https://torquexstorage01.blob.core.windows.net/torquexmediaplayer/" + project.PageLogo;
                return View(login);
            }
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(ProjectLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            Project project = db.Projects.Find(model.id);

            if (project.Password == model.Password)
            {
                Session["loggedIn"] = "True";
                Session["ProjectId"] = model.id;
                EventLoad.LogEvent(User.Identity.Name, null, "Project_Login", null, null, null, model.id);
                return RedirectToAction("Unit", new { id = model.id });
            } else
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                model.DeptName = project.DeptName;
                model.PageTitle = project.PageTitle;
                model.PageLogo = "https://torquexstorage01.blob.core.windows.net/torquexmediaplayer/" + project.PageLogo;
                return View(model);

            }

        }


        // GET: Projects/Unit/5
        [AllowAnonymous]
        public ActionResult Unit(int? id, string currentFilter, string searchString)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if ((Session["loggedIn"] == null) || (Session["loggedIn"].ToString() != "True") || (Session["ProjectId"].ToString() != id.ToString()) )
            {
                   return RedirectToAction("Login", new { id = id });
            }
            Project project = db.Projects.Find(id);

            if (searchString == null)
            { 
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            if (project == null)
            {
                return HttpNotFound();
            }

            var projectUnit = new PublicProject();
            projectUnit.DeptName = project.DeptName;
            projectUnit.PageFooter = project.PageFooter;
            projectUnit.PageLogo = "https://torquexstorage01.blob.core.windows.net/torquexmediaplayer/" + project.PageLogo;
            projectUnit.PageTitle = project.PageTitle;
            projectUnit.ProjectName = project.ProjectName;
            projectUnit.ID = project.ID;

            var transcripts = from s in db.Transcripts select s;
            transcripts = transcripts.Where(s => s.ProjectId == id && s.Active == true);

            if (!String.IsNullOrEmpty(searchString))
            {
                transcripts = transcripts.Where(s => s.Text_Plain.Contains(searchString));
            } 

            projectUnit.Transcripts = transcripts.ToList();
            EventLoad.LogEvent(User.Identity.Name, null, "Project_View", null, null, null, project.ID);

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
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (!transcript.Active)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if ((Session["loggedIn"] == null) || (Session["loggedIn"].ToString() != "True") || (Session["ProjectId"].ToString() != transcript.ProjectId.ToString()))
            {
                return RedirectToAction("Login", new { id = transcript.ProjectId });
            }


            if (transcript == null)
            {
                return HttpNotFound();
            }

            var PlayProject = new ProjectTranscript();

            Project project = db.Projects.Find(transcript.ProjectId);

            PlayProject.transcript = transcript;
            PlayProject.PageLogo = "https://torquexstorage01.blob.core.windows.net/torquexmediaplayer/" + project.PageLogo;

            string fn = Path.GetFileNameWithoutExtension(transcript.PlayFile);
            PlayProject.srtFile = fn + ".txt";
            string inFile = Server.MapPath("~/temp") + "\\" + PlayProject.srtFile;

            System.IO.File.WriteAllText(inFile, transcript.Text_Sort);

            // Update srt file to blob storage to get over CORs issues and get the latest version.
            /*            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(System.Configuration.ConfigurationManager.AppSettings["StorageConnectionString"]);
                        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                        CloudBlobContainer blobContainer = blobClient.GetContainerReference("torquexmediaplayer");

                        string blockName = StringUtils.blockName(PlayProject.srtFile);
                        CloudBlockBlob blob = blobContainer.GetBlockBlobReference(blockName);

                        using (var stream = new MemoryStream(Encoding.Default.GetBytes(transcript.Text_Sort), false))
                        {
                            blob.UploadFromStream(stream);
                        }

            */
            EventLoad.LogEvent(User.Identity.Name, transcript.Id, "Project_Load_Play", null, null, null, transcript.ProjectId);

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
            EventLoad.LogEvent(User.Identity.Name, null, "Project_Delete", null, null, null, id);
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
