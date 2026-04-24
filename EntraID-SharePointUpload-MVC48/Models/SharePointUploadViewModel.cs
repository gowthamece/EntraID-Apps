using System.ComponentModel.DataAnnotations;
using System.Web;

namespace EntraID.SharePointUpload.Mvc48.Models
{
    public class SharePointUploadViewModel
    {
        [Required]
        [Display(Name = "File")]
        public HttpPostedFileBase UploadFile { get; set; }

        [Required]
        [Display(Name = "Target folder server-relative URL")]
        public string TargetFolderServerRelativeUrl { get; set; }

        public string LastUploadedFileName { get; set; }
        public string ResultMessage { get; set; }
        public bool Success { get; set; }
    }
}
