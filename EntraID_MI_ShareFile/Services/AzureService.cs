using System.IO.Compression;
using System.Text.RegularExpressions;
using Azure.Identity;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace EntraID_MI_ShareFile.Services
{
    public class AzureService
    {
        private readonly ILogger<AzureService> _logger;

        public AzureService(ILogger<AzureService> logger)
        {
            _logger = logger;
        }

        public async Task<Stream> DownloadZipFileAsync(string storageAcount, string fileShareName, string? directoryPath, string fileNameRegex)
        {
            try
            {
                _logger.LogInformation("[APP_INFO]: Starting Azure File Share download.", storageAcount, fileShareName, directoryPath, fileNameRegex);
                directoryPath = directoryPath?.Trim().TrimStart('/').TrimStart('\\');

                Regex regex;

                try
                {
                    regex = new Regex(fileNameRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }
                catch
                {
                    _logger.LogError("[APP_ERROR]: Ivalid filename regex provided: {Regex}", fileNameRegex);
                    throw;
                }

                var credential = new DefaultAzureCredential();
                //var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                //{
                //    TenantId = "6e01b1f9-b1e5-4073-ac97-778069a0ad64",
                //    ExcludeEnvironmentCredential = true,  // Exclude client secret from env vars
                //    ExcludeWorkloadIdentityCredential = true,
                //    ExcludeManagedIdentityCredential = true,
                //    ExcludeVisualStudioCredential = false,
                //    ExcludeVisualStudioCodeCredential = true,
                //    ExcludeAzureCliCredential = true,
                //    ExcludeAzurePowerShellCredential = true,
                //    ExcludeInteractiveBrowserCredential = true
                //});
                var serviceUri = new Uri($"https://{storageAcount}.file.core.windows.net");

                _logger.LogInformation("Connecting to Azure File Share {Uri}", serviceUri);

                var options = new ShareClientOptions
                {
                    ShareTokenIntent = ShareTokenIntent.Backup
                };

                var serviceClient = new ShareServiceClient(serviceUri, credential, options);
                var shareClient = serviceClient.GetShareClient(fileShareName);

                if (!await shareClient.ExistsAsync())
                {
                    _logger.LogWarning("[APP_INOF]: File Share does not exists: {Share}", fileShareName);
                    return null;
                }

                ShareDirectoryClient directoryClient = string.IsNullOrWhiteSpace(directoryPath) ? shareClient.GetRootDirectoryClient() : shareClient.GetDirectoryClient(directoryPath);

                if (!await directoryClient.ExistsAsync())
                {
                    _logger.LogWarning("[APP_INFO]: Directory does not exists: {Directory}", directoryPath);
                    return null;
                }

                string? matchedFileName = null;

                await foreach (ShareFileItem item in directoryClient.GetFilesAndDirectoriesAsync())
                {
                    if (item.IsDirectory)
                    {
                        continue;
                    }
                    if (regex.IsMatch(item.Name))
                    {
                        matchedFileName = item.Name;
                        _logger.LogInformation("[APP_INFO]: Matched file found: {FileName}", matchedFileName);
                        break;
                    }
                }
                if (matchedFileName != null)
                    if (matchedFileName == null)
                    {
                        _logger.LogInformation("[APP_INFO]: No files matched regex: {Regex}", fileNameRegex);
                        return null;
                    }

                var fileClient = directoryClient.GetFileClient(matchedFileName);
                var downloadInfo = await fileClient.DownloadAsync();

                var zipStream = new MemoryStream();
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var zipEntry = archive.CreateEntry(matchedFileName);

                    using var entryStream = zipEntry.Open();
                    await downloadInfo.Value.Content.CopyToAsync(entryStream);
                }

                zipStream.Position = 0;

                _logger.LogInformation("[APP_INFO]: ZIP created successfully for file {Filename}", matchedFileName);

                return zipStream;
            }

            catch (Exception ex)
            {
                _logger.LogError("[APP_ERROR]: [APP_ERROR]: ***************** Error downloading files as ZIP: {Message} *****************", ex.Message);
                throw new ApplicationException("Error downloading files as ZIP: " + ex.Message, ex);
                //throw;
            }
        }
    }
}
