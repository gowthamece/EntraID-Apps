using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(SharePointCSOM_POC.Startup))]

namespace SharePointCSOM_POC
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // App-only flow: no delegated user authentication middleware.
        }
    }
}