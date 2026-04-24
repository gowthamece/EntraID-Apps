using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Microsoft.SharePoint.Client;

namespace EntraID.SharePointUpload.Mvc48.Services
{
    public class SharePointUploadService
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public async Task<string> UploadFileAsync(string fileName, byte[] fileContent, string folderServerRelativeUrl)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name is required.", nameof(fileName));
            }

            if (fileContent == null || fileContent.Length == 0)
            {
                throw new ArgumentException("File content is empty.", nameof(fileContent));
            }

            var siteUrl = ConfigurationManager.AppSettings["SharePoint:SiteUrl"];
            var tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
            var clientId = ConfigurationManager.AppSettings["ida:ClientId"];
            var clientSecret = ConfigurationManager.AppSettings["ida:ClientSecret"];

            var accessToken = await AcquireSharePointAppTokenAsync(siteUrl, tenantId, clientId, clientSecret).ConfigureAwait(false);

            using (var context = new ClientContext(siteUrl))
            {
                context.ExecutingWebRequest += (sender, args) =>
                {
                    args.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken;
                };

                var folder = context.Web.GetFolderByServerRelativeUrl(folderServerRelativeUrl);
                context.Load(folder);
                context.ExecuteQuery();

                using (var stream = new MemoryStream(fileContent))
                {
                    var fileInfo = new FileCreationInformation
                    {
                        ContentStream = stream,
                        Url = fileName,
                        Overwrite = true
                    };

                    var uploadedFile = folder.Files.Add(fileInfo);
                    context.Load(uploadedFile, f => f.ServerRelativeUrl);
                    context.ExecuteQuery();

                    return uploadedFile.ServerRelativeUrl;
                }
            }
        }

        private static async Task<string> AcquireSharePointAppTokenAsync(string siteUrl, string tenantId, string clientId, string clientSecret)
        {
            var siteUri = new Uri(siteUrl);
            var resourceScope = siteUri.GetLeftPart(UriPartial.Authority) + "/.default";
            var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("scope", resourceScope),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await HttpClient.PostAsync(tokenEndpoint, form).ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var serializer = new JavaScriptSerializer();
            var tokenResponse = serializer.Deserialize<TokenResponse>(payload);
            if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.access_token))
            {
                throw new InvalidOperationException("Unable to acquire access token for SharePoint.");
            }

            return tokenResponse.access_token;
        }

        private class TokenResponse
        {
            public string access_token { get; set; }
        }
    }
}
