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

        private List<Words> wordlist = new List<Words>();
        private Media VBJson;
        private Media JSONout;
        private Words word;
        private Words wordout;


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
                            trans.Duration = VBresponse.media.metadata.length.milliseconds;
                            trans.WordCount = VBresponse.media.transcripts.latest.words.Count();
                            if (trans.Diarization) JSON = diarize(JSON);
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


        private string diarize(string JSON)
        {

            int min;
            int sec;
            Diarization speaker;
            Words nextword;
            string rJSON = JSON;
            //            txbJSON.Text = rJSON;
            try
            {
                var json_serializer1 = new JavaScriptSerializer();
                var json_serializer2 = new JavaScriptSerializer();
                VBJson = json_serializer1.Deserialize<Media>(rJSON);
                JSONout = json_serializer2.Deserialize<Media>(rJSON);
                JSONout.media.transcripts.latest.words = wordlist.ToArray();
                int diarycounter = 0;
                int wordcounter = 0;
                int outcounter = 0;
                int numwords = VBJson.media.transcripts.latest.words.Count();
                word = VBJson.media.transcripts.latest.words[0];
                nextword = VBJson.media.transcripts.latest.words[1];
                speaker = VBJson.media.transcripts.latest.diarization[diarycounter];
                string lastspeaker = speaker.speakerlabel;

                while (wordcounter < numwords)
                {
                    word = VBJson.media.transcripts.latest.words[wordcounter];


                    // check if first word add in turn.
                    if (word.p == 0 && speaker.start <= word.s)
                    {
                        wordout = (Words)word.Clone();
                        wordout.p = outcounter;
                        wordout.e = wordout.s + 1000;
                        wordout.w = speaker.gender + "-" + speaker.speakerlabel + " : ";
                        wordout.m = "turn";
                        wordlist.Add(wordout);
                        outcounter++;
                    }

                    if (numwords > word.p + 1)
                    {
                        nextword = VBJson.media.transcripts.latest.words[word.p + 1];
                    }

                    while (word.s > speaker.start)
                    {
                        diarycounter++;
                        speaker = VBJson.media.transcripts.latest.diarization[diarycounter];
                    }


                    if (word.s <= speaker.start && nextword.s >= speaker.start)
                    {
                        diarycounter++;
                        if (word.m != null && word.m == "punc")
                        {
                            wordcounter++;
                            wordout = (Words)word.Clone(); ;
                            wordout.p = outcounter;
                            wordlist.Add(wordout);
                            outcounter++;
                        }
                        else if (nextword.m != null && nextword.m == "punc")
                        {
                            wordout = (Words)word.Clone();
                            wordout.p = outcounter;
                            wordlist.Add(wordout);
                            outcounter++;
                            wordout = (Words)nextword.Clone(); ;
                            wordout.p = outcounter;
                            wordlist.Add(wordout);
                            outcounter++;
                            wordcounter++;
                            wordcounter++;
                        }
                        word = VBJson.media.transcripts.latest.words[wordcounter];
                        min = (int)Decimal.Truncate(word.s / 1000 / 60);
                        sec = (int)((word.s / 1000) - (min * 60));
                        wordout = (Words)word.Clone();
                        wordout.p = outcounter;
                        wordout.e = wordout.s + 1000;
                        wordout.w = speaker.gender + "-" + speaker.speakerlabel + " : ";
                        wordout.m = "turn";
                        wordlist.Add(wordout);
                        outcounter++;
                        wordout = (Words)word.Clone();
                        wordout.p = outcounter;
                        wordlist.Add(wordout);
                        outcounter++;

                        wordcounter++;
                        speaker = VBJson.media.transcripts.latest.diarization[diarycounter];
                    }
                    else
                    {

                        wordcounter++;
                        wordout = (Words)word.Clone();
                        wordout.p = outcounter;
                        wordlist.Add(wordout);
                        outcounter++;

                    }

                }
            }
            catch (Exception)
            {
                //                            throw;
            }


            JSONout.media.transcripts.latest.words = wordlist.ToArray();
            var sJSONout = new JavaScriptSerializer().Serialize(JSONout);
            return sJSONout;
        }


    }
}