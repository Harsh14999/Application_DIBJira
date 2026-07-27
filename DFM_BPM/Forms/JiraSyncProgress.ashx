<%@ WebHandler Language="C#" Class="DFM_BPM.Forms.JiraSyncProgress" %>

using System;
using System.Web;

namespace DFM_BPM.Forms
{
    public class JiraSyncProgress : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Cache.SetNoStore();

            string key = context.Request.QueryString["key"];
            if (string.IsNullOrEmpty(key))
            {
                context.Response.Write("{\"error\":\"missing key\",\"done\":true,\"percent\":0,\"status\":\"\",\"pulled\":0,\"inserted\":0,\"updated\":0,\"failed\":0}");
                return;
            }

            string cacheKey = "SyncProg_" + key;
            string json = HttpRuntime.Cache[cacheKey] as string;

            if (string.IsNullOrEmpty(json))
                json = "{\"percent\":0,\"status\":\"Waiting...\",\"done\":false,\"pulled\":0,\"inserted\":0,\"updated\":0,\"failed\":0,\"error\":\"\"}";

            context.Response.Write(json);
        }

        public bool IsReusable { get { return false; } }
    }
}
