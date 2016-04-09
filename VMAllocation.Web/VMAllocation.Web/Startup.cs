using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(VMAllocation.Web.Startup))]
namespace VMAllocation.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
