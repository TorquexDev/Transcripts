﻿using System;
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
using MediaToolkit.Options;

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
            transcripts = transcripts.Where(s => s.createby.Equals(User.Identity.Name) && s.Active == true);// override object.Equals
 
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
            EventLoad.LogEvent(User.Identity.Name, null, "List_Transcripts", null, null, null, null);
            return View(transcripts.ToPagedList(pageNumber, pageSize));


        }

        [AllowAnonymous]
        public FileResult downloadTranscript(string mediaid, string format)
        {
            Transcript transcript = db.Transcripts.FirstOrDefault(s => s.mediaId == mediaid);

            string filepath = Server.MapPath("~/temp/");
            string fname = mediaid;
            string filename = filepath + fname;

            EventLoad.LogEvent(User.Identity.Name, transcript.Id, "Download", format, null, null, null);


            switch (format)
            {
                case "rtf":
                    fname = mediaid + ".docx";
                    filename = filepath + fname;
                    CreateFormatWordDoc(filename, transcript.JSON);
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
                    } else if (word.m == "punc")
                    {
                        p.Append(word.w);
                    } else
                    {
                        p.Append(" " + word.w);
                    }
                } else if ((word.s - end) > splitTime)
                {
                    p.Append("\n");
                    p = doc.InsertParagraph();
                    p.Append(word.w);
                } else
                {
                    p.Append(" " + word.w);
                }
                end = word.e;
            }

            doc.Save();
            return true;
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
            EventLoad.LogEvent(User.Identity.Name, transcript.Id, "Transcript_Details", null, null, null, null);

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

            if (!transcript.Active)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }


            string fn = Path.GetFileNameWithoutExtension(transcript.PlayFile);
            ViewBag.srtFile = fn + ".txt";
            string inFile = Server.MapPath("~/temp") + "\\" + ViewBag.srtFile;

            System.IO.File.WriteAllText(inFile, transcript.Text_Sort);

            EventLoad.LogEvent(User.Identity.Name, transcript.Id, "Load_Play", null, null, null, null);


            return View(transcript);
        }


        // GET: Transcripts/Create
        public ActionResult Create()
        {
            var projects = db.Projects.Where(s => s.CreateBy.Equals(User.Identity.Name))
                                        .Select(s => new { s.ID, s.ProjectName })
                                        .OrderBy(s => s.ProjectName)
                                        .ToList();
 //           var formdata = new FileUpload();
            ViewBag.projectList = new SelectList(projects, "ID", "ProjectName"); 


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
            string blockName = "";


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
                    //transcript.ProjectId = formdata.projectId;
                    transcript.Vocabs = formdata.custom_vocab;
                    transcript.Channels = formdata.channel;
                    if (formdata.channel == "mono") transcript.Diarization = formdata.diarization;

                    HttpPostedFileBase file = httpFiles[i];
                    transcript.Filename = Path.GetFileName(file.FileName);


                    // Split up files so we can rename and make unique

                    string ext = Path.GetExtension(file.FileName);
                    string fn = Path.GetFileName(file.FileName);
                    fn = Path.GetFileNameWithoutExtension(fn);
                    string random = "_" + Path.GetRandomFileName().Replace(".", "").Substring(0, 8);

                    transcript.PlayFile = fn + random + ext;



                    // reset filestream to start
                    if (file.InputStream.CanSeek)
                    {
                        file.InputStream.Seek(0, SeekOrigin.Begin);
                    }
                    // Save file to blob
                    //Generates  a blobName 
                    blockName = StringUtils.blockName(transcript.PlayFile);

                    if (!string.IsNullOrEmpty(blockName))
                    {
                        CloudBlockBlob blob = blobContainer.GetBlockBlobReference(blockName);
                        //upload files 
                        blob.UploadFromStream(file.InputStream);
//                        file.InputStream.Dispose();
                    }

                    if (file.InputStream.CanSeek)
                    {
                        file.InputStream.Seek(0, SeekOrigin.Begin);
                    }


                    // Send to Voicebase
                    try
                    {
                        transcript.mediaId = GetMedia(file, transcript, formdata.channel, formdata.stereo_channel1, formdata.stereo_channel2);
                        transcript.VBstatus = "submitted";
                    }
                    catch
                    {
                        transcript.VBstatus = "failed";
                    }


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
//                            file.InputStream.Dispose();

                        var inputFile = new MediaFile { Filename = @inFile };
                        var outputFile = new MediaFile { Filename = @outFile };
                   
                        using (var engine = new Engine(Server.MapPath("~/Content/ffmpeg.exe")))
                        {
                            engine.Convert(inputFile, outputFile);
 //                           file.InputStream.Dispose();
                        }
                        blockName = StringUtils.blockName(outFile);
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
                    // Check if file is avi and convert to mp4
                    else if (ext.ToLower() == ".avi")
                    {
                        // reset filestream to start
                        if (file.InputStream.CanSeek)
                        {
                            file.InputStream.Seek(0, SeekOrigin.Begin);
                        }

                        // write to local disk

                        string inFile = Server.MapPath("~/temp") + "\\" + fn + random + ext;
                        string outFile = Server.MapPath("~/temp") + "\\" + fn + random + ".mp4";
                        file.SaveAs(inFile);
                        // Free up memory as we don't need this one any more
                        //                            file.InputStream.Dispose();

                        var inputFile = new MediaFile { Filename = @inFile };
                        var outputFile = new MediaFile { Filename = @outFile };


                        using (var engine = new Engine(Server.MapPath("~/Content/ffmpeg.exe")))
                        {
                            engine.Convert(inputFile, outputFile);
                            //                           file.InputStream.Dispose();
                        }
                        blockName = StringUtils.blockName(outFile);
                        if (!string.IsNullOrEmpty(blockName))
                        {
                            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(blockName);
                            //upload files 
                            blob.UploadFromFile(outFile);
                        }
                        transcript.PlayFile = fn + random + ".mp4";

                        // Clean up files on disk

                        System.IO.File.Delete(@inFile);
                        System.IO.File.Delete(@outFile);


                    }




                    if (formdata.projectList != null)
                    {
                        if (formdata.projectList > 0)
                        {
                            transcript.ProjectId = formdata.projectList;
                            var project = db.Projects.Where(s => s.ID == formdata.projectList)
                                        .Select(s => new { s.ProjectName })
                                        .ToList();
                            if (project.Count == 1) transcript.Project = project[0].ProjectName;

                        }
                    } else if (formdata.project != null)
                    {
                        var project = db.Projects.Where(s => s.ProjectName.Equals(formdata.project) && s.CreateBy.Equals(User.Identity.Name))
                                    .Select(s => new { s.ID })
                                    .ToList();
                        if (project.Count == 1)
                        {
                            transcript.ProjectId = project[0].ID;
                        } else
                        {
                            var projectNew = new Project();
                            projectNew.CreateDate = DateTime.Now;
                            projectNew.ProjectName = formdata.project;
                            projectNew.CreateBy = User.Identity.Name;
                            db.Projects.Add(projectNew);
                            db.SaveChanges();
                            transcript.ProjectId = projectNew.ID;
                        }

                    }





                    // Save to database
                    transcript.createby = User.Identity.Name;
                    transcript.CreateTime = DateTime.Now;
                    transcript.Active = true;
                    


                    db.Transcripts.Add(transcript);
                    db.SaveChanges();

                }
                return RedirectToAction("Index");
            } else
            {
                return RedirectToAction("Create");
            }

        }

        [HttpPost]
        public ContentResult UploadFiles()
        {
            var r = new List<UploadFilesResult>();

            foreach (string file in Request.Files)
            {
                HttpPostedFileBase hpf = Request.Files[file] as HttpPostedFileBase;
                if (hpf.ContentLength == 0)
                    continue;

                string savedFileName = Path.Combine(Server.MapPath("~/App_Data"), Path.GetFileName(hpf.FileName));
                hpf.SaveAs(savedFileName); // Save the file

                r.Add(new UploadFilesResult()
                {
                    Name = hpf.FileName,
                    Length = hpf.ContentLength,
                    Type = hpf.ContentType
                });
            }
            // Returns json
            return Content("{\"name\":\"" + r[0].Name + "\",\"type\":\"" + r[0].Type + "\",\"size\":\"" + string.Format("{0} bytes", r[0].Length) + "\"}", "application/json");
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
 //           request.RequestFormat = DataFormat.Json;
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddFile("media", file.InputStream.CopyTo, file.FileName, file.ContentType);
            request.Files[0].ContentLength = file.ContentLength;
            request.Timeout = 600000;
     

            configuration sconfig = new configuration();

            sconfig.executor = "v2";
            sconfig.language = transcript.Language;
 //           sconfig.mediaURL = HttpUtility.UrlEncode(System.Configuration.ConfigurationManager.AppSettings["StorageBaseURL"] + transcript.PlayFile);
            sconfig.transcripts = new cfgtranscripts();
            sconfig.transcripts.engine = "premium";
            sconfig.detections = new List<detections>();
            detections model = new detections();
            model.model = "PCI";
            sconfig.detections.Add(model);
            

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



 //           request.AddBody("mediaUrl="+System.Configuration.ConfigurationManager.AppSettings["StorageBaseURL"] + HttpUtility.UrlEncode(transcript.PlayFile));
            request.AddParameter("configuration", jsonconfig, ParameterType.RequestBody);
            //            request.AddParameter("mediasUrl", System.Configuration.ConfigurationManager.AppSettings["StorageBaseURL"] + HttpUtility.UrlEncode(transcript.PlayFile), ParameterType.RequestBody);
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
            var projects = db.Projects.Where(s => s.CreateBy.Equals(User.Identity.Name))
                                        .Select(s => new { s.ID, s.ProjectName })
                                        .OrderBy(s => s.ProjectName)
                                        .ToList();
            //           var formdata = new FileUpload();
            ViewBag.projectList = new SelectList(projects, "ID", "ProjectName");
//            ViewBag.projectList.SelectedValue = transcript.ProjectId;



            return View(transcript);
        }

        // POST: Transcripts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit( Transcript transcript)
        {
            if (ModelState.IsValid)
            {

                Transcript updatedtrans = (from s in db.Transcripts
                                          where s.Id == transcript.Id
                                          select s).FirstOrDefault();
                updatedtrans.Filename = transcript.Filename;
                var project = db.Projects.Where(s => s.ID == transcript.ProjectId)
                            .Select(s => new { s.ProjectName })
                            .ToList();
                if (project.Count == 1) updatedtrans.Project = project[0].ProjectName;
                updatedtrans.ProjectId = transcript.ProjectId;
                db.Entry(updatedtrans).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");

            }
            EventLoad.LogEvent(User.Identity.Name, transcript.Id, "Edit_Transcript", null, null, null, null);

            return View(transcript);
        }

        [HttpPost]
        public ActionResult Update(PUpload updata)
        {
            List<Words> wordlist = new List<Words>();
            Media VB;
            Media JSONout;
            Words word;
            Words wordout;
            Words oldWord;
            PWords newWord;
            var data = new ResponseData();
            data.requestStatus = "FAILED";
            Transcript transcript = (from s in db.Transcripts
                                     where s.mediaId == updata.mediaId
                                     select s).FirstOrDefault();
            string JSON = transcript.JSON;
            var json_serializer1 = new JavaScriptSerializer();
            var json_serializer2 = new JavaScriptSerializer();
            VB = json_serializer1.Deserialize<Media>(JSON);
            JSONout = json_serializer2.Deserialize<Media>(JSON);
            JSONout.media.transcripts.latest.words = wordlist.ToArray();

            // Get Txt Srt file into memory.
            List<SrtFile> srtList = new List<SrtFile>();
            SrtFile srtIntance = new SrtFile();
            SrtFile srtObj = (SrtFile)srtIntance.Clone();
            int sPos = 0;
            

            using (StringReader reader = new StringReader(transcript.Text_Sort))
            {
                string line = string.Empty;
                do
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        if (line.Length == 0)
                        {
                            srtList.Add(srtObj);
                            srtObj = (SrtFile)srtIntance.Clone();
                            sPos = 0;
                            srtObj.content = "";
                        }
                        else
                        {
                            switch (sPos)
                            {
                                case 0:
                                    srtObj.pos = line;
                                    sPos++;
                                    break;
                                case 1:
                                    sPos++;
                                    string[] separators = { " --> " };
                                    string[] times = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                                    srtObj.s = StringUtils.timeToMilliseconds(times[0]);
                                    srtObj.e = StringUtils.timeToMilliseconds(times[1]);
                                    srtObj.time = line;
                                    break;
                                default:
                                    srtObj.content += line + "\n";
                                    break;
                            }
                        }

                    }
                } while (line != null);
                srtList.Add(srtObj);
            }

            int numwords = updata.content.Count();
            int i = 0;
            int outcounter = 0;

            while (outcounter < numwords)
            {
                word = VB.media.transcripts.latest.words[i];
                newWord = updata.content[outcounter];

                // Check if speaker insertion
                if ((newWord.m != null) && ((word.m == null) || (word.m == "punc")))
                {
                    wordout = (Words)word.Clone();
                    wordout.p = outcounter;
                    wordout.e = wordout.s + 1000;
                    wordout.w = newWord.w.Trim();
                    wordout.m = "turn";
                    wordlist.Add(wordout);
                    outcounter++;
                    newWord = updata.content[outcounter];
                }

                // Check if word deleted
                if (word.s < newWord.s)
                {
                    i++;
                }
                else
                {
                    wordout = (Words)word.Clone();
                    wordout.p = outcounter;
                    wordout.w = newWord.w.Trim();
                    wordlist.Add(wordout);
                    if (word.w != newWord.w.Trim())  // write a record to the change log table.
                    {
                        var wChange = new WordChange();
                        wChange.changeBy = User.Identity.Name;
                        wChange.changeDate = DateTime.Now;
                        wChange.p = i;
                        wChange.TranscriptId = transcript.Id;
                        wChange.m = wordout.m;
                        wChange.oldWord = word.w;
                        wChange.newWord = newWord.w.Trim();
                        wChange.s = word.s;
                        db.WordChanges.Add(wChange);
                        db.SaveChanges();

                        // Update Srt
                        // Find relevant object.
                        int j = 0;
                        do
                        {
                            if ((srtList[j].s <= word.s) && (srtList[j].e >= word.s))
                            {
                                srtList[j].content = srtList[j].content.Replace(word.w, newWord.w.Trim());
                            }
                            j++;
                        } while (j < srtList.Count);

                        EventLoad.LogEvent(User.Identity.Name, transcript.Id, "Word_Change", null, wChange.oldWord, wChange.newWord, null);

                    }
                    outcounter++;
                    i++;
                }
            }

            JSONout.media.transcripts.latest.words = wordlist.ToArray();
            var sJSONout = new JavaScriptSerializer().Serialize(JSONout);

            //updated text srt file
            string srtString = "";
            int cnt = 0;
            do
            {
                srtString += srtList[cnt].pos + "\n";
                srtString += srtList[cnt].time + "\n";
                srtString += srtList[cnt].content + "\n";
                cnt++;
            } while (cnt < srtList.Count);


            transcript.JSON = sJSONout;
            transcript.Text_Sort = srtString;
            db.Entry(transcript).State = EntityState.Modified;
            db.SaveChanges();

            data.requestStatus = "SUCCESS";
            data.id = transcript.Id;

            //return RedirectToAction("Play", new { id = transcript.Id });

            return Json(data);
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
            EventLoad.LogEvent(User.Identity.Name, transcript.Id, "Delete_Transcript", null, null, null, null);
            transcript.Active = false;
            db.Entry(transcript).State = EntityState.Modified;
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
