using System;
using System.Web;

namespace DFM_BPM
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            bool shouldProvision = App_Code.Helpers.AuthHelper.IsDev;
            if (!shouldProvision && HttpContext.Current != null && HttpContext.Current.User != null &&
                HttpContext.Current.User.Identity != null && HttpContext.Current.User.Identity.IsAuthenticated)
                shouldProvision = true;

            if (shouldProvision)
                try { App_Code.Helpers.AuthHelper.EnsureWindowsUser(); }
                catch { /* allow page to load even if DB is unavailable */ }
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            if (ex is HttpException && ((HttpException)ex).GetHttpCode() == 404) return;
        }

        protected void Session_End(object sender, EventArgs e)
        {
        }

        protected void Application_End(object sender, EventArgs e)
        {
        }
    }
}
