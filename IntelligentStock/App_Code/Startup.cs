using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(IntelligentStock.Startup))]
namespace IntelligentStock
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
