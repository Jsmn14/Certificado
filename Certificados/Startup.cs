using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Certificados.Startup))]
namespace Certificados
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
        }
    }
}
