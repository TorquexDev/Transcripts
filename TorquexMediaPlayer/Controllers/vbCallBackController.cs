using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TorquexMediaPlayer.Models;
using Novacode;
using RestSharp;
using System.Web.Script.Serialization;
using System.Data.Entity;


namespace TorquexMediaPlayer.Controllers
{

    public class vbCallBackController : Controller
    {
        private TranscriptDBContext db = new TranscriptDBContext();


        // POST: vbCallBack
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult CheckStatus()
        {
            var token = System.Configuration.ConfigurationManager.AppSettings["VoicebaseToken"];
            var transcripts = from s in db.Transcripts
                              where s.VBstatus == "running" || s.VBstatus == "submitted" 
                              select s;

            foreach (Transcript trans in transcripts )
            {
                if (!string.IsNullOrEmpty(trans.mediaId))
                {
                    string JSON = GetTranscript(trans.mediaId, token);
                    string PlainText = "";
                    string PlainSrt = "";

                    string status = "";
                    try
                    {
                        var json_serializer = new JavaScriptSerializer();
                        Media VBresponse = json_serializer.Deserialize<Media>(JSON);
                        status = VBresponse.media.status;
                        if (status == "finished")
                        {
                            PlainText = GetTranscriptPlain(trans.mediaId, token);
                            PlainSrt = GetTranscriptPlainSrt(trans.mediaId, token);
                        }
                        else
                        {
                            PlainText = "";
                            PlainSrt = "";
                        }

                        trans.VBstatus = status;
                        trans.Text_Plain = PlainText;
                        trans.Text_Sort = PlainSrt;
                        trans.JSON = JSON;
                        db.Entry(trans).State = EntityState.Modified;


                    }
                    catch (Exception)
                    {
                        //                            throw;
                    }
                    
                }
 
            }
            if (ModelState.IsValid)
            {
                db.SaveChanges();
            }

            return RedirectToAction("Index", "Transcripts");
        }

        static string GetTranscript(string mediaId, string token)
        {
             var client = new RestClient();
            client.BaseUrl = new Uri("https://apis.voicebase.com/v2-beta");
            var request = new RestRequest();
            request.Resource = "/media/" + mediaId;
            request.AddHeader("Authorization", "Bearer " + token);
            IRestResponse response = client.Execute(request);
            return response.Content;
        }

        static string GetTranscriptPlain(string mediaId, string token)
        {
            var client = new RestClient();
            client.BaseUrl = new Uri("https://apis.voicebase.com/v2-beta");
            var request = new RestRequest();
            request.Resource = "/media/" + mediaId + "/transcripts/latest";
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddHeader("Accept", "text/plain");
            IRestResponse response = client.Execute(request);
            return response.Content;
        }

        static string GetTranscriptPlainSrt(string mediaId, string token)
        {
            var client = new RestClient();
            client.BaseUrl = new Uri("https://apis.voicebase.com/v2-beta");
            var request = new RestRequest();
            request.Resource = "/media/" + mediaId + "/transcripts/latest";
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddHeader("Accept", "text/srt");
            IRestResponse response = client.Execute(request);
            return response.Content;
        }



    }
}