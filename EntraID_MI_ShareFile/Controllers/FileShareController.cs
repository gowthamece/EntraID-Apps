using EntraID_MI_ShareFile.Models;
using EntraID_MI_ShareFile.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EntraID_MI_ShareFile.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileShareController : ControllerBase
    {
        private readonly AzureService _mainSAService;
        private readonly ILogger<FileShareController> _logger;

        public FileShareController(AzureService mainSAService, ILogger<FileShareController> logger)
        {
            _mainSAService = mainSAService;
            _logger = logger;
            _logger.LogInformation("SourceContext test: {@SourceContext}", _logger.GetType().Name);
        }

        [HttpGet("DownloadFile")]
        public async Task<IActionResult> DownloadZipFile([FromQuery] FileShareDownloadRequest request)
        {
            try
            {
                Request.Headers.Add("x-ms-file-request-intent", "Backup");

                _logger.LogInformation("[APP_INFO]: Download request received.", request.StorageAcount, request.FileShare, request.Directory, request.FileNameRegex);

                var zipStream = await _mainSAService.DownloadZipFileAsync(request.StorageAcount, request.FileShare, request.Directory, request.FileNameRegex);

                if (zipStream == null)
                {
                    _logger.LogInformation("[APP_INFO]: No matching file found. Returning 204 No content");
                    return NoContent();
                }
                _logger.LogInformation("[APP_INFO]: File Downloaded Successfully");
                return File(zipStream, "application/zip", "download.zip");
            }
            catch (Exception ex)
            {
                _logger.LogError("[APP_ERROR]: ***************** Exception occured while downloading file from Azure File Share: {Message} *****************", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("API is up and running");
        }
    }
}
