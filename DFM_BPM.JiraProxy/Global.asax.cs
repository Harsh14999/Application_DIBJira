using System.Web.Http;
using DFM_BPM.JiraProxy.App_Start;

namespace DFM_BPM.JiraProxy
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Register routes and formatters.
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
