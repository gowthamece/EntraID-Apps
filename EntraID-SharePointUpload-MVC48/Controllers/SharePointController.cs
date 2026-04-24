using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Web.Mvc;
using EntraID.SharePointUpload.Mvc48.Models;
using EntraID.SharePointUpload.Mvc48.Services;

namespace EntraID.SharePointUpload.Mvc48.Controllers
{
    [Authorize]
    public class SharePointController : Controller
    {
        private readonly SharePointUploadService _sharePointUploadService;

        public SharePointController()
        {
            _sharePointUploadService = new SharePointUploadService();
        }

        [HttpGet]
        public ActionResult Index()
        {
            var model = new SharePointUploadViewModel
            {
                TargetFolderServerRelativeUrl = ConfigurationManager.AppSettings["SharePoint:DefaultFolderServerRelativeUrl"]
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(SharePointUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                byte[] bytes;
                using (var memoryStream = new MemoryStream())
                {
                    model.UploadFile.InputStream.CopyTo(memoryStream);
                    bytes = memoryStream.ToArray();
                }

                var serverRelativeUrl = await _sharePointUploadService.UploadFileAsync(
                    Path.GetFileName(model.UploadFile.FileName),
                    bytes,
                    model.TargetFolderServerRelativeUrl).ConfigureAwait(false);

                model.Success = true;
                model.LastUploadedFileName = model.UploadFile.FileName;
                model.ResultMessage = "Uploaded successfully to " + serverRelativeUrl;

                ModelState.Clear();
                model.UploadFile = null;
            }
            catch (Exception ex)
            {
                model.Success = false;
                model.ResultMessage = "Upload failed: " + ex.Message;
            }

            return View(model);
        }
    }
}
