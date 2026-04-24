using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Web.Mvc;
using SharePointCSOM_POC.Models;
using SharePointCSOM_POC.Services;

namespace SharePointCSOM_POC.Controllers
{
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

            PopulateTokenDiagnostics(model).GetAwaiter().GetResult();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(SharePointUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateTokenDiagnostics(model).ConfigureAwait(false);
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

            await PopulateTokenDiagnostics(model).ConfigureAwait(false);

            return View(model);
        }

        private async Task PopulateTokenDiagnostics(SharePointUploadViewModel model)
        {
            try
            {
                var diagnostics = await _sharePointUploadService.GetTokenDiagnosticsAsync().ConfigureAwait(false);
                model.TokenAudience = diagnostics.Audience;
                model.TokenRoles = diagnostics.Roles;
                model.TokenScope = diagnostics.Scope;
                model.TokenTenantId = diagnostics.TenantId;
                model.TokenClientAppId = diagnostics.ClientAppId;
                model.TokenAppIdAcr = diagnostics.AppIdAcr;
                model.TokenExpiresUtc = diagnostics.ExpiresUtc;
                model.TokenDiagnosticsError = null;
            }
            catch (Exception ex)
            {
                model.TokenDiagnosticsError = ex.Message;
            }
        }
    }
}