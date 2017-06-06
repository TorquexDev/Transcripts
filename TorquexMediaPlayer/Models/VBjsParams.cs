using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorquexMediaPlayer.Models
{
    public class MediaResponse
    {
        public string mediaId { get; set; }
        public string status { get; set; }
        //       public string message { get; set; }
    }

    public class Media
    {
        //        public string media { get; set; }
        public MediaResponse media { get; set; }
    }
}