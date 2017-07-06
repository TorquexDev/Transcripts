using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TorquexMediaPlayer.Models;
using Novacode;
using RestSharp;
using System.Web.Script.Serialization;
using PagedList;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.IO;
using MediaToolkit.Model;
using MediaToolkit;
using TorquexUtilities;

namespace TorquexMediaPlayer.Controllers
{
    public class TranscriptsController : Controller
    {



        private TranscriptDBContext db = new TranscriptDBContext();

        // GET: Transcripts
        public ActionResult Index(string sortOrder, string currentFilter, string searchString, int? page)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.CreateSortParm = String.IsNullOrEmpty(sortOrder) ? "CreateTime_desc" : "";
            ViewBag.FileNameSortParm = sortOrder == "Filename" ? "Filename_desc" : "Filename";
            ViewBag.ProjectSortParm = sortOrder == "Project" ? "Project_desc" : "Project";
            var transcripts = from s in db.Transcripts select s;
            transcripts = transcripts.Where(s => s.createby.Equals(User.Identity.Name));

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;


            if (!String.IsNullOrEmpty(searchString))
            {
                transcripts = transcripts.Where(s => s.Text_Plain.Contains(searchString));
            }

            switch(sortOrder)
            {
                case "Filename_desc":
                    transcripts = transcripts.OrderByDescending(s => s.Filename);
                    break;
                case "Filename":
                    transcripts = transcripts.OrderBy(s => s.Filename);
                    break;
                case "CreateTime_desc":
                    transcripts = transcripts.OrderByDescending(s => s.CreateTime);
                    break;
                case "CreateTime":
                    transcripts = transcripts.OrderBy(s => s.CreateTime);
                    break;
                case "Project_desc":
                    transcripts = transcripts.OrderByDescending(s => s.Project);
                    break;
                case "Project":
                    transcripts = transcripts.OrderBy(s => s.Project);
                    break;
                default:
                    transcripts = transcripts.OrderByDescending(s => s.Id);
                    break;
            }
            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(transcripts.ToPagedList(pageNumber, pageSize));


        }

        [AllowAnonymous]
        public FileResult downloadTranscript(string mediaid, string format)
        {
            Transcript transcript = db.Transcripts.FirstOrDefault(s => s.mediaId == mediaid);

            string filepath = Server.MapPath("~/temp/");
            string fname = mediaid;
            string filename = filepath + fname;


            switch (format)
            {
                case "rtf":
                    fname = mediaid + ".docx";
                    filename = filepath + fname;
                    var doc = DocX.Create(filename);
                    doc.InsertParagraph(transcript.Text_Plain);
                    doc.Save();
                    break;
                case "srt":
                    fname = mediaid + ".srt";
                    filename = filepath + fname;
                    System.IO.File.WriteAllText(@filename, transcript.Text_Sort);
                    break;
                case "docd":
                    fname = mediaid + ".docx";
                    filename = filepath + fname;
                    CreateTimeStampWordDoc(filename, transcript.JSON);
                    break;
                default:
                    fname = mediaid + ".txt";
                    filename = filepath + fname;
                    System.IO.File.WriteAllText(@filename, transcript.Text_Plain);
                    break;
            }
            return File(filename, System.Net.Mime.MediaTypeNames.Application.Octet,fname);


        }

        private Boolean CreateTimeStampWordDoc(string filename, string jsonTranscript)
        {
            var json_serializer = new JavaScriptSerializer();
            int splitTime = 180000;
            TimeSpan ts;

            Media VBJson = json_serializer.Deserialize<Media>(jsonTranscript);

            DocX doc = DocX.Create(filename);
            Paragraph p;
            int i = 0;
            Boolean bDiary = false;

            p = doc.InsertParagraph();

            // Check the first 50 words to see if diarization is on

            foreach (Words word in VBJson.media.transcripts.latest.words)
            {
                i++;
                if (word.m != null && word.m == "turn")
                {
                    bDiary = true;
                    break;
                }
                if (i > 50)
                {
                    p.Append("[00:00:00]\n").FontSize(9);
                    break;
                }
            }

            Boolean bSplit = false;
            Boolean bDone = true;

            foreach (Words word in VBJson.media.transcripts.latest.words)
            {
                if ((!bSplit) && (!bDone) && ((word.s % splitTime) < 10000))
                {
                    bSplit = true;
                }

                if (bDone && ((word.s % splitTime) > 10000))
                {
                    bDone = false;
                    bSplit = false;
                }

                if (word.m != null && word.m == "punc")
                {
                    p.Append(word.w);
                    if (bSplit && !bDone && !bDiary)
                    {
                        p.Append("\n");
                        p = doc.InsertParagraph();
                        ts = TimeSpan.FromMilliseconds((double)word.s);
                        p.AppendLine("[" + ts.ToString(@"hh\:mm\:ss") + "]\n").FontSize(9);
                        bDone = true;
                    }
                }
                else if (word.m != null && word.m == "turn")
                {
                    p.Append("\n");
                    p = doc.InsertParagraph();
                    ts = TimeSpan.FromMilliseconds((double)word.s);
                    p.Append("[" + ts.ToString(@"hh\:mm\:ss") + "] " + word.w + "\n").FontSize(9);
                }
                else
                {
                    p.Append(" " + word.w);
                }
            }

            doc.Save();
            return true;
        }



        // GET: Transcripts/Details/5
        public ActionResult Details(int? id)
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
            return View(transcript);
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

            return View(transcript);
        }


        // GET: Transcripts/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Transcripts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Upload(FileUpload formdata)
        {
           CloudStorageAccount storageAccount = CloudStorageAccount.Parse(System.Configuration.ConfigurationManager.AppSettings["StorageConnectionString"]);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference("torquexmediaplayer");
 
            if (Request.Files.Count > 0)
            {
                var stereo1 = formdata.stereo_channel1;
                var stereo2 = formdata.stereo_channel2;


                HttpFileCollectionBase httpFiles = Request.Files;
                for (int i = 0; i < httpFiles.Count; i++)
                {

                    var transcript = new Transcript();

                    transcript.Language = formdata.language;
                    transcript.Project = formdata.project;
                    transcript.Vocabs = formdata.custom_vocab;
                    transcript.Channels = formdata.channel;
                    if (formdata.channel == "mono") transcript.Diarization = formdata.diarization;

                    HttpPostedFileBase file = httpFiles[i];
                    transcript.Filename = file.FileName;

                    // Send to Voicebase
                    transcript.mediaId = GetMedia(file, transcript, formdata.channel, formdata.stereo_channel1, formdata.stereo_channel2);

                    // Split up files so we can rename and make unique

                    string ext = Path.GetExtension(file.FileName);
                    string fn = Path.GetFileNameWithoutExtension(file.FileName);
                    string random = "_" + Path.GetRandomFileName().Replace(".", "").Substring(0, 8);

                     // Check if file is wav and convert to mp3
                    if (ext.ToLower() == ".wav")
                    {
                        // reset filestream to start
                        if (file.InputStream.CanSeek)
                        {
                            file.InputStream.Seek(0, SeekOrigin.Begin);
                        }

                        // write to local disk

                        string inFile = Server.MapPath("~/temp") + "\\" + fn + random + ext;
                        string outFile = Server.MapPath("~/temp") + "\\" + fn + random + ".mp3";
                            file.SaveAs(inFile);
                            // Free up memory as we don't need this one any more
                            file.InputStream.Dispose();

                        var inputFile = new MediaFile { Filename = @inFile };
                        var outputFile = new MediaFile { Filename = @outFile };
                   
                        using (var engine = new Engine(Server.MapPath("~/Content/ffmpeg.exe")))
                        {
                            engine.Convert(inputFile, outputFile);
                            file.InputStream.Dispose();
                        }
                        string blockName = StringUtils.blockName(outFile);
                        if (!string.IsNullOrEmpty(blockName))
                        {
                            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(blockName);
                            //upload files 
                            blob.UploadFromFile(outFile);
                        }
                        transcript.PlayFile = fn + random + ".mp3";

                        // Clean up files on disk

                        System.IO.File.Delete(@inFile);
                        System.IO.File.Delete(@outFile);


                    }
                    else
                    {
                        transcript.PlayFile = fn + random + ext;
                        // reset filestream to start
                        if (file.InputStream.CanSeek)
                        {
                            file.InputStream.Seek(0, SeekOrigin.Begin);
                        }
                        // Save file to blob
                        //Generates  a blobName 
                        string blockName = StringUtils.blockName(transcript.PlayFile);

                        if (!string.IsNullOrEmpty(blockName))
                        {
                            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(blockName);
                            //upload files 
                            blob.UploadFromStream(file.InputStream);
                         }


                    }

 
                    // Save to database
                    transcript.createby = User.Identity.Name;
                    transcript.CreateTime = DateTime.Now;
                    transcript.VBstatus = "submitted";


                    db.Transcripts.Add(transcript);
                    db.SaveChanges();

                }




                // Upload to voicebase


                return RedirectToAction("Index");
            } else
            {
                return RedirectToAction("Create");
            }

        }

        public ActionResult StatusUpdate()
        {
            return RedirectToAction("CheckStatus", "vbCallBack");
        }

        static string GetMedia(HttpPostedFileBase file, Transcript transcript, string channel, string l_channel, string r_channel)
        {
            var client = new RestClient();
            var token = System.Configuration.ConfigurationManager.AppSettings["VoicebaseToken"];
            client.BaseUrl = new Uri("https://apis.voicebase.com/v2-beta");
            var request = new RestRequest(Method.POST);
            request.Resource = "/media";
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddFile("media", file.InputStream.CopyTo, file.FileName, file.ContentType);
            request.Files[0].ContentLength = file.ContentLength;
            request.Timeout = 300000;

            configuration sconfig = new configuration();

            sconfig.executor = "v2";
            sconfig.language = transcript.Language;
            sconfig.transcripts = new cfgtranscripts();
            sconfig.transcripts.engine = "premium";
            

            // If custom vocabs set clean up and pass in
            if (!string.IsNullOrEmpty(transcript.Vocabs))
            { 
                if (transcript.Vocabs.Trim().Length > 0) {
                    string[] terms = transcript.Vocabs.Trim().Split(';');
                    cfgvocabs vcab = new cfgvocabs();
                    vcab.terms = new List<string>();
                    for (int x = 0; x < terms.Length; x = x + 1)
                    {
                        terms[x] = terms[x].Trim();
                        if (terms[x].Length > 0)
                        {
                            vcab.terms.Add(terms[x]);
                        }
                    }

                    sconfig.transcripts.vocabularies = new List<cfgvocabs>();

                    sconfig.transcripts.vocabularies.Add(vcab);

                }
            }

            if (channel == "stereo")
            {
                sconfig.ingest = new cfgingest();
                sconfig.ingest.channels = new cfgchannels();
                sconfig.ingest.channels.left = new cfgleft();
                sconfig.ingest.channels.right = new cfgright();
                sconfig.ingest.channels.left.speaker = l_channel;
                sconfig.ingest.channels.right.speaker = r_channel;
             } else if (transcript.Diarization)
            {
                sconfig.ingest = new cfgingest();
                sconfig.ingest.diarization = true;
            }

            var config_all = new configwrapper();
            config_all.configuration = sconfig;

            var jsonconfig = new JavaScriptSerializer().Serialize(config_all);


 
            request.AddParameter("configuration", jsonconfig, ParameterType.RequestBody);
            IRestResponse<MediaResponse> response = client.Execute<MediaResponse>(request);
            return response.Data.mediaId;
        }


        // GET: Transcripts/Edit/5
        public ActionResult Edit(int? id)
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
            return View(transcript);
        }

        // POST: Transcripts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Filename,mediaId,Processtime,CreateTime,DirDate,VBstatus,JSON,Text_Plain,Text_Sort,Project,Link,createby,Language,Channels,Vocab")] Transcript transcript)
        {
            if (ModelState.IsValid)
            {
                db.Entry(transcript).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(transcript);
        }

        // GET: Transcripts/Delete/5
        public ActionResult Delete(int? id)
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
            return View(transcript);
        }

        // POST: Transcripts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Transcript transcript = db.Transcripts.Find(id);
            db.Transcripts.Remove(transcript);
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
