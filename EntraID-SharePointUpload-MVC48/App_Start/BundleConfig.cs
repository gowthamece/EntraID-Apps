using System.Web.Optimization;

namespace EntraID.SharePointUpload.Mvc48
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new StyleBundle("~/Content/css").Include("~/Content/Site.css"));
            BundleTable.EnableOptimizations = false;
        }
    }
}
