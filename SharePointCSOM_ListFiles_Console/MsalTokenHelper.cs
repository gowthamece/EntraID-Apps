using System;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace SharePointCSOM_ListFiles_Console
{
    internal static class MsalTokenHelper
    {
        private const string TenantIdKey = "ida:TenantId";
        private const string ClientIdKey = "ida:ClientId";
        private const string CertThumbprintKey = "ida:CertThumbprint";

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

        internal static Microsoft.SharePoint.Client.ClientContext GetClientContext(string siteUrl)
        {
            return new ClientContext(siteUrl);
        }

        internal static async Task<string> AcquireSharePointAppTokenAsync(string siteUrl)
        {
            var tenantId = SpHelper.GetRequiredAppSetting(TenantIdKey);
            var clientId = SpHelper.GetRequiredAppSetting(ClientIdKey);
            var certThumbprint = SpHelper.GetRequiredAppSetting(CertThumbprintKey);
            var resourceScope = BuildSharePointScope(siteUrl);
            var certificate = FindCertificateByThumbprint(certThumbprint);

            if (certificate == null)
            {
                throw new InvalidOperationException(
                    "Certificate not found for thumbprint: " + certThumbprint + ". Check CurrentUser/My or LocalMachine/My store.");
            }

            var app = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                .WithCertificate(certificate)
                .Build();

            var authenticationResult = await app
                .AcquireTokenForClient(new[] { resourceScope })
                .ExecuteAsync()
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(authenticationResult.AccessToken))
            {
                throw new InvalidOperationException("Unable to acquire access token for SharePoint.");
            }

            return authenticationResult.AccessToken;
        }

        private static string BuildSharePointScope(string siteUrl)
        {
            var siteUri = new Uri(siteUrl);
            return siteUri.GetLeftPart(UriPartial.Authority) + "/.default";
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
    }
}