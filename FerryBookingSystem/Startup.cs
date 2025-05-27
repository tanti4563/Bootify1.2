using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(FerryBookingSystem.Startup))]
namespace FerryBookingSystem
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}