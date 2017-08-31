using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace TorquexMediaPlayer.Models
{
    public class SupportRequest
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Details { get; set; }
        public string Attachment { get; set; }
        public string CreateBy { get; set; }
        public Nullable<System.DateTime> CreateDate { get; set; }
        public string Email { get; set; }
    }

    public class SupportView
    {
        public int Id { get; set; }
        [Required]
        public string Subject { get; set; }
        [Required]
        public string Details { get; set; }
        [Display(Name = "Upload File")]
        [ValidateFile]
        public HttpPostedFileBase file { get; set; }
        public string Attachment { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateDate { get; set; }
        public string Email { get; set; }
    }


}