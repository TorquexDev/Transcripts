using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorquexMediaPlayer.Models
{
    public class FileUpload
    {
        public HttpPostedFileBase Files { get; set; }
        public string language { get; set; }
        public string custom_vocab { get; set; }
        public string channel { get; set; }
        public string stereo_channel1 { get; set; }
        public string stereo_channel2 { get; set; }
        public string project { get; set; }
        public bool diarization { get; set; }
        public int? projectId { get; set; }
        public int? projectList {get;set;} 

    }

    public class ProjectList
    {
        public int ID { get; set; }
        public string ProjectName{ get; set; }
    }

    public class UploadFilesResult
    {
        public string Name { get; set; }
        public int Length { get; set; }
        public string Type { get; set; }
    }
}