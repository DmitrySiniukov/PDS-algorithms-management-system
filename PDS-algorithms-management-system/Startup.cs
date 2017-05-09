using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Enterprise.Startup))]
namespace Enterprise
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
