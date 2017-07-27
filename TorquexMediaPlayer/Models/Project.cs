using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TorquexMediaPlayer.Models
{
    public class Project
    {
        public int ID { get; set; }
        public string ProjectName { get; set; }
        public string DeptName { get; set; }
        public string PageTitle { get; set; }
        public string PageLogo { get; set; }
        public string PageFooter { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateDate { get; set; }

    }

    public class ProjectUpload
    {
        [Display(Name = "Upload File")]
        [ValidateFile]
        public HttpPostedFileBase file { get; set; }

        public string PageLogo { get; set; }
        public int ID { get; set; }

        [Required(ErrorMessage = "Please Enter a Short Name for this Unit")]
        [Display(Name = "Unit Name")]
        public string ProjectName { get; set; }
        [Display(Name = "Department Name")]
        public string DeptName { get; set; }

        [Required(ErrorMessage = "Please Enter the full name of this Unit")]
        [Display(Name = "Full Unit Name")]
        public string PageTitle { get; set; }

        [Display(Name = "Footer Message")]
        public string PageFooter { get; set; }

    }

    public class PublicProject
    {
        public string PageLogo { get; set; }
        public int ID { get; set; }

        public string ProjectName { get; set; }
        public string DeptName { get; set; }

        public string PageTitle { get; set; }

        public string PageFooter { get; set; }

        public List<Transcript> Transcripts { get; set; }
    }

    public class ProjectTranscript
    {
        public Transcript transcript { get; set; }
        public string PageLogo { get; set; }
    }


    public class ValidateFileAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            int MaxContentLength = 1024 * 1024 * 1; //1 MB
            string[] AllowedFileExtensions = new string[] { ".jpg", ".gif", ".png" };

            var file = value as HttpPostedFileBase;
            string filename = file.FileName.ToLower();

            if (file == null)
                return true;
            else if (!AllowedFileExtensions.Contains(filename.Substring(filename.LastIndexOf('.'))))
            {
                ErrorMessage = "Please upload Your Logo Image of type: " + string.Join(", ", AllowedFileExtensions);
                return false;
            }
            else if (file.ContentLength > MaxContentLength)
            {
                ErrorMessage = "Your image is too large, maximum allowed size is : " + (MaxContentLength / 1024).ToString() + "MB";
                return false;
            }
            else
                return true;
        }
    }

}