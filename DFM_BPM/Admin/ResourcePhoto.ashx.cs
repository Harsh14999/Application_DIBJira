using System;
using System.Web;
using DFM_BPM.App_Code.DAL;

namespace DFM_BPM.Admin
{
    /// <summary>Streams a Portfolio Resource's uploaded avatar photo (org chart node image),
    /// e.g. Admin/ResourcePhoto.ashx?id=7. Returns 404 when the resource has no photo.
    /// IMPORTANT: this handler MUST use a CodeBehind file (not inline &lt;script&gt; code in the .ashx)
    /// so it is compiled once as part of the normal project build into DFM_BPM.dll. If the code lived
    /// inline in the .ashx instead, ASP.NET would dynamically JIT-compile it at first request into a
    /// separate temporary assembly that ALSO needs to resolve DFM_BPM.App_Code.DAL.PortfolioDAL — and
    /// since the literal "App_Code" folder name is special-cased by the ASP.NET runtime (it gets its
    /// own dynamically-compiled assembly regardless of project type), that type would then exist in
    /// BOTH the App_Code dynamic assembly and the precompiled DFM_BPM.dll, causing
    /// CS0433 "type exists in both ... .dll and ... DFM_BPM.DLL".</summary>
    public class ResourcePhoto : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            int id;
            int.TryParse(context.Request.QueryString["id"], out id);

            string contentType = null;
            byte[] photo = id > 0 ? PortfolioDAL.GetResourcePhoto(id, out contentType) : null;

            if (photo == null || photo.Length == 0)
            {
                context.Response.StatusCode = 404;
                return;
            }

            context.Response.ContentType = string.IsNullOrEmpty(contentType) ? "image/jpeg" : contentType;
            context.Response.Cache.SetCacheability(HttpCacheability.Private);
            context.Response.Cache.SetMaxAge(TimeSpan.FromMinutes(10));
            context.Response.BinaryWrite(photo);
        }

        public bool IsReusable { get { return false; } }
    }
}
