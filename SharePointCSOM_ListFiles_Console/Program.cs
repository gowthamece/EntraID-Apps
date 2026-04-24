using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;

namespace SharePointCSOM_ListFiles_Console
{
    internal static class Program
    {
        private const string SiteUrlKey = "SharePoint:SiteUrl";
        private const string FolderKey = "SharePoint:DefaultFolderServerRelativeUrl";

        private static int Main(string[] args)
        {
            try
            {
                RunAsync(args).GetAwaiter().GetResult();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("SharePoint file listing failed.");
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static async Task RunAsync(string[] args)
        {
            var siteUrl = SpHelper.GetRequiredAppSetting(SiteUrlKey);
            var folderServerRelativeUrl = args.Length > 0
                ? args[0]
                : SpHelper.GetRequiredAppSetting(FolderKey);

            var fileNames = await GetFileNamesAsync(siteUrl, folderServerRelativeUrl).ConfigureAwait(false);

            if (fileNames.Count == 0)
            {
                Console.WriteLine("No files found.");
                return;
            }

            foreach (var fileName in fileNames)
            {
                Console.WriteLine(fileName);
            }
        }

        private static Task<IReadOnlyList<string>> GetFileNamesAsync(string siteUrl, string folderServerRelativeUrl)
        {
            using (var context = new MsalTokenHelper.ClientContext(siteUrl))
            {
                var folder = context.Web.GetFolderByServerRelativeUrl(folderServerRelativeUrl);
                var files = folder.Files;

                context.Load(files, collection => collection.Include(file => file.Name));
                context.ExecuteQuery();

                IReadOnlyList<string> fileNames = files
                    .Select(file => file.Name)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return Task.FromResult(fileNames);
            }
        }
    }
}