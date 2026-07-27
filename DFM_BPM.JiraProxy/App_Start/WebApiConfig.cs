using System.Web.Http;

namespace DFM_BPM.JiraProxy.App_Start
{
    /// <summary>
    /// Registers Web API routes and configuration.
    /// Using action-based routing so each controller method maps to /api/{controller}/{action}.
    /// </summary>
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Action-based route: /api/jiraproxy/fields, /api/jiraproxy/search, etc.
            config.Routes.MapHttpRoute(
                name: "ActionApi",
                routeTemplate: "api/{controller}/{action}",
                defaults: new { action = RouteParameter.Optional }
            );

            // Suppress the default XML formatter so responses are always JSON.
            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}
