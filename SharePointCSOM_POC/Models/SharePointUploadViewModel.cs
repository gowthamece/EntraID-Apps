using System.ComponentModel.DataAnnotations;
using System.Web;

namespace SharePointCSOM_POC.Models
{
    public class SharePointUploadViewModel
    {
        [Required]
        [Display(Name = "Target folder (server relative URL)")]
        public string TargetFolderServerRelativeUrl { get; set; }

        [Required]
        [Display(Name = "File")]
        public HttpPostedFileBase UploadFile { get; set; }

        public string ResultMessage { get; set; }

        public string LastUploadedFileName { get; set; }

        public bool Success { get; set; }

        public string TokenAudience { get; set; }

        public string TokenRoles { get; set; }

        public string TokenScope { get; set; }

        public string TokenTenantId { get; set; }

        public string TokenClientAppId { get; set; }

        public string TokenAppIdAcr { get; set; }

        public string TokenExpiresUtc { get; set; }

        public string TokenDiagnosticsError { get; set; }
    }
}