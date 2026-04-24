using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace SharePointCSOM_ListFiles_Console
{
    internal static class SpHelper
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        internal static string GetRequiredAppSetting(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Missing required appSetting: " + key);
            }

            return value;
        }

        internal sealed class ClientContext : Microsoft.SharePoint.Client.ClientContext
        {
            internal ClientContext(string siteUrl)
                : base(siteUrl)
            {
                var accessToken = AcquireSharePointAppTokenAsync(siteUrl).GetAwaiter().GetResult();
                ExecutingWebRequest += (sender, eventArgs) =>
                {
                    eventArgs.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken;
                };
            }
        }

        internal sealed class MsalClientContext : Microsoft.SharePoint.Client.ClientContext
        {
            internal MsalClientContext(string siteUrl)
                : base(siteUrl)
            {
                var accessToken = MsalTokenHelper.AcquireSharePointAppTokenAsync(siteUrl).GetAwaiter().GetResult();
                ExecutingWebRequest += (sender, eventArgs) =>
                {
                    eventArgs.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken;
                };
            }
        }

        internal static async Task<string> AcquireSharePointAppTokenAsync(string siteUrl)
        {
            var tenantId = GetRequiredAppSetting("ida:TenantId");
            var clientId = GetRequiredAppSetting("ida:ClientId");
            var clientSecret = ConfigurationManager.AppSettings["ida:ClientSecret"];
            var certThumbprint = ConfigurationManager.AppSettings["ida:CertThumbprint"];
            var siteUri = new Uri(siteUrl);
            var resourceScope = siteUri.GetLeftPart(UriPartial.Authority) + "/.default";
            var tokenEndpoint = "https://login.microsoftonline.com/" + tenantId + "/oauth2/v2.0/token";

            FormUrlEncodedContent form;

            if (!string.IsNullOrWhiteSpace(certThumbprint))
            {
                var certificate = FindCertificateByThumbprint(certThumbprint);
                if (certificate == null)
                {
                    throw new InvalidOperationException(
                        "Certificate not found for thumbprint: " + certThumbprint + ". Check CurrentUser/My or LocalMachine/My store.");
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
                if (string.IsNullOrWhiteSpace(clientSecret))
                {
                    throw new InvalidOperationException("Configure either ida:CertThumbprint or ida:ClientSecret.");
                }

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
            var certificate = FindCertificateInStore(StoreLocation.CurrentUser, normalizedThumbprint);
            if (certificate != null)
            {
                return certificate;
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