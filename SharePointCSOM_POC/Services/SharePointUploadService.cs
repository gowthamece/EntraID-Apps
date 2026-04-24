using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Web.Script.Serialization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SharePoint.Client;

namespace SharePointCSOM_POC.Services
{
    public class SharePointUploadService
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public class TokenDiagnostics
        {
            public string Audience { get; set; }

            public string Roles { get; set; }

            public string Scope { get; set; }

            public string TenantId { get; set; }

            public string ClientAppId { get; set; }

            public string AppIdAcr { get; set; }

            public string ExpiresUtc { get; set; }
        }

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
            var certThumbprint = ConfigurationManager.AppSettings["ida:CertThumbprint"];

            var accessToken = await AcquireSharePointAppTokenAsync(siteUrl, tenantId, clientId, clientSecret, certThumbprint).ConfigureAwait(false);

          //  var probe = await ProbeSharePointAccessAsync(siteUrl, accessToken).ConfigureAwait(false);
            //if (!probe.Success)
            //{
            //    throw new InvalidOperationException("SharePoint preflight failed. " + probe.Message);
            //}

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
                    try
                    {
                        context.ExecuteQuery();
                    }
                    catch (ServerUnauthorizedAccessException ex)
                    {
                        throw new InvalidOperationException("SharePoint CSOM unauthorized (ServerUnauthorizedAccessException). " + ex.Message, ex);
                    }
                    catch (ClientRequestException ex)
                    {
                        throw new InvalidOperationException("SharePoint CSOM client request failed. " + ex.Message, ex);
                    }
                    catch (ServerException ex)
                    {
                        throw new InvalidOperationException("SharePoint CSOM server exception. Code=" + ex.ServerErrorCode + ", Type=" + ex.ServerErrorTypeName + ", Message=" + ex.ServerErrorValue + ", CorrelationId=" + ex.ServerErrorTraceCorrelationId, ex);
                    }

                    return uploadedFile.ServerRelativeUrl;
                }
            }
        }

                public async Task<TokenDiagnostics> GetTokenDiagnosticsAsync()
                {
                    var siteUrl = ConfigurationManager.AppSettings["SharePoint:SiteUrl"];
                    var tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
                    var clientId = ConfigurationManager.AppSettings["ida:ClientId"];
                    var clientSecret = ConfigurationManager.AppSettings["ida:ClientSecret"];
                    var certThumbprint = ConfigurationManager.AppSettings["ida:CertThumbprint"];

                    var accessToken = await AcquireSharePointAppTokenAsync(siteUrl, tenantId, clientId, clientSecret, certThumbprint).ConfigureAwait(false);
                    return BuildDiagnostics(accessToken);
                }

                private static TokenDiagnostics BuildDiagnostics(string jwt)
                {
                    var handler = new JwtSecurityTokenHandler();
                    var token = handler.ReadJwtToken(jwt);

                    var roles = token.Claims
                        .Where(c => c.Type == "roles" || c.Type == "role")
                        .Select(c => c.Value)
                        .Distinct()
                        .ToArray();

                    var scope = token.Claims.FirstOrDefault(c => c.Type == "scp")?.Value;
                    var exp = token.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
                    string expiresUtc = null;

                    if (!string.IsNullOrWhiteSpace(exp) && long.TryParse(exp, out var expUnix))
                    {
                        expiresUtc = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime.ToString("u");
                    }

                    return new TokenDiagnostics
                    {
                        Audience = token.Audiences.FirstOrDefault(),
                        Roles = roles.Length == 0 ? "(none)" : string.Join(", ", roles),
                        Scope = string.IsNullOrWhiteSpace(scope) ? "(none)" : scope,
                        TenantId = token.Claims.FirstOrDefault(c => c.Type == "tid")?.Value,
                        ClientAppId = token.Claims.FirstOrDefault(c => c.Type == "appid")?.Value,
                        AppIdAcr = token.Claims.FirstOrDefault(c => c.Type == "appidacr")?.Value,
                        ExpiresUtc = string.IsNullOrWhiteSpace(expiresUtc) ? "(unknown)" : expiresUtc
                    };
                }

                private static async Task<PreflightResult> ProbeSharePointAccessAsync(string siteUrl, string accessToken)
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, siteUrl.TrimEnd('/') + "/_api/web?$select=Title");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        return PreflightResult.Ok();
                    }

                    var sb = new StringBuilder();
                    sb.Append("Status=").Append((int)response.StatusCode).Append(" ").Append(response.ReasonPhrase);

                    if (response.Headers.TryGetValues("SPRequestGuid", out var spRequestGuidValues))
                    {
                        sb.Append(", SPRequestGuid=").Append(string.Join(",", spRequestGuidValues));
                    }

                    if (response.Headers.TryGetValues("WWW-Authenticate", out var authenticateValues))
                    {
                        sb.Append(", WWW-Authenticate=").Append(string.Join(" | ", authenticateValues));
                    }

                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        var trimmed = body.Length > 800 ? body.Substring(0, 800) + "..." : body;
                        sb.Append(", Body=").Append(trimmed.Replace("\r", " ").Replace("\n", " "));

                        if (trimmed.IndexOf("Unsupported app only token", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            sb.Append(", Hint=SharePoint Online rejected secret-based app-only token. Use certificate-based app-only authentication for SharePoint (client assertion / certificate credential) instead of client secret.");
                        }
                    }

                    return PreflightResult.Fail(sb.ToString());
                }

                private class PreflightResult
                {
                    public bool Success { get; private set; }

                    public string Message { get; private set; }

                    public static PreflightResult Ok()
                    {
                        return new PreflightResult { Success = true, Message = string.Empty };
                    }

                    public static PreflightResult Fail(string message)
                    {
                        return new PreflightResult { Success = false, Message = message };
                    }
                }

        private static async Task<string> AcquireSharePointAppTokenAsync(string siteUrl, string tenantId, string clientId, string clientSecret, string certThumbprint)
        {
            var siteUri = new Uri(siteUrl);
            var resourceScope = siteUri.GetLeftPart(UriPartial.Authority) + "/.default";
            var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

            FormUrlEncodedContent form;

            if (!string.IsNullOrWhiteSpace(certThumbprint))
            {
                var certificate = FindCertificateByThumbprint(certThumbprint);
                if (certificate == null)
                {
                    throw new InvalidOperationException("Certificate not found for thumbprint: " + certThumbprint + ". Check CurrentUser/My or LocalMachine/My store.");
                }

                var clientAssertion = CreateClientAssertion(tokenEndpoint, clientId, certificate);
                form = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("scope", resourceScope),
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
                    new KeyValuePair<string, string>("client_assertion", clientAssertion)
                });
            }
            else
            {
                form = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("scope", resourceScope),
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });
            }

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

        private static X509Certificate2 FindCertificateByThumbprint(string thumbprint)
        {
            var normalizedThumbprint = thumbprint.Replace(" ", string.Empty).ToUpperInvariant();
            var cert = FindCertificateInStore(StoreLocation.CurrentUser, normalizedThumbprint);
            if (cert != null)
            {
                return cert;
            }

            return FindCertificateInStore(StoreLocation.LocalMachine, normalizedThumbprint);
        }

        private static X509Certificate2 FindCertificateInStore(StoreLocation location, string normalizedThumbprint)
        {
            using (var store = new X509Store(StoreName.My, location))
            {
                store.Open(OpenFlags.ReadOnly);
                return store.Certificates
                    .Find(X509FindType.FindByThumbprint, normalizedThumbprint, validOnly: false)
                    .OfType<X509Certificate2>()
                    .FirstOrDefault();
            }
        }

        private static string CreateClientAssertion(string tokenEndpoint, string clientId, X509Certificate2 certificate)
        {
            var now = DateTime.UtcNow;
            var payload = new JwtPayload
            {
                { "aud", tokenEndpoint },
                { "iss", clientId },
                { "sub", clientId },
                { "jti", Guid.NewGuid().ToString() },
                { "nbf", new DateTimeOffset(now).ToUnixTimeSeconds() },
                { "iat", new DateTimeOffset(now).ToUnixTimeSeconds() },
                { "exp", new DateTimeOffset(now.AddMinutes(10)).ToUnixTimeSeconds() }
            };

            var signingCredentials = new X509SigningCredentials(certificate, SecurityAlgorithms.RsaSha256);
            var header = new JwtHeader(signingCredentials);
            if (!header.ContainsKey("x5t"))
            {
                header["x5t"] = Base64UrlEncoder.Encode(certificate.GetCertHash());
            }

            var assertionToken = new JwtSecurityToken(header, payload);
            return new JwtSecurityTokenHandler().WriteToken(assertionToken);
        }

        private class TokenResponse
        {
            public string access_token { get; set; }
        }
    }
}