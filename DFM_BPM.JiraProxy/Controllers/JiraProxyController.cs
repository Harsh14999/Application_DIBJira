using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace DFM_BPM.JiraProxy.Controllers
{
    /// <summary>
    /// Server-side proxy that forwards requests to JIRA / Confluence using stored credentials.
    /// 
    /// Endpoints exposed to the browser dashboard:
    ///   GET  /api/jiraproxy/fields
    ///       → JIRA GET /rest/api/2/field
    ///
    ///   POST /api/jiraproxy/search
    ///       → JIRA POST /rest/api/2/search   (body forwarded as-is)
    ///
    ///   GET  /api/jiraproxy/issue?key=DMGT-123[&amp;fields=summary,status,...]
    ///       → JIRA GET /rest/api/2/issue/{key}[?fields=...]
    ///
    ///   GET  /api/jiraproxy/confluencecontent?pageId=178738414[&amp;expand=body.view]
    ///       → Confluence GET /rest/api/content/{pageId}[?expand=...]
    ///
    /// All calls add Basic-Auth from JiraUser / JiraPassword in Web.config.
    /// CORS is handled via IIS customHeaders in Web.config.
    /// </summary>
    public class JiraProxyController : ApiController
    {
        // Read configuration once per app-domain lifetime.
        private static readonly string JiraBaseUrl =
            (ConfigurationManager.AppSettings["JiraBaseUrl"] ?? string.Empty).TrimEnd('/');

        private static readonly string ConfluenceBaseUrl =
            string.IsNullOrEmpty(ConfigurationManager.AppSettings["ConfluenceBaseUrl"])
                ? JiraBaseUrl
                : ConfigurationManager.AppSettings["ConfluenceBaseUrl"].TrimEnd('/');

        private static readonly string JiraUser =
            ConfigurationManager.AppSettings["JiraUser"] ?? string.Empty;

        private static readonly string JiraPass =
            ConfigurationManager.AppSettings["JiraPassword"] ?? string.Empty;

        private static readonly int TimeoutMs =
            int.Parse(ConfigurationManager.AppSettings["HttpTimeoutSeconds"] ?? "120") * 1000;

        // ------------------------------------------------------------------ //
        //  GET /api/jiraproxy/fields                                          //
        //  Proxies: GET {JiraBaseUrl}/rest/api/2/field                        //
        // ------------------------------------------------------------------ //
        [HttpGet]
        public HttpResponseMessage Fields()
        {
            return Forward("GET", JiraBaseUrl + "/rest/api/2/field", null);
        }

        // ------------------------------------------------------------------ //
        //  POST /api/jiraproxy/search                                         //
        //  Proxies: POST {JiraBaseUrl}/rest/api/2/search                      //
        //  Body (JSON) is forwarded unchanged.                                //
        // ------------------------------------------------------------------ //
        [HttpPost]
        public HttpResponseMessage Search()
        {
            string body = Request.Content.ReadAsStringAsync().Result;
            return Forward("POST", JiraBaseUrl + "/rest/api/2/search", body);
        }

        // ------------------------------------------------------------------ //
        //  GET /api/jiraproxy/issue?key=DMGT-123[&fields=summary,status,...]  //
        //  Proxies: GET {JiraBaseUrl}/rest/api/2/issue/{key}[?fields=...]     //
        // ------------------------------------------------------------------ //
        [HttpGet]
        public HttpResponseMessage Issue(string key, string fields = null)
        {
            if (string.IsNullOrEmpty(key))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "'key' query parameter is required.");

            string url = JiraBaseUrl + "/rest/api/2/issue/" + Uri.EscapeDataString(key);
            if (!string.IsNullOrEmpty(fields))
                url += "?fields=" + Uri.EscapeDataString(fields);

            return Forward("GET", url, null);
        }

        // ------------------------------------------------------------------ //
        //  GET /api/jiraproxy/confluencecontent?pageId=...&expand=body.view   //
        //  Proxies: GET {ConfluenceBaseUrl}/rest/api/content/{pageId}?expand= //
        // ------------------------------------------------------------------ //
        [HttpGet]
        public HttpResponseMessage ConfluenceContent(string pageId, string expand = null)
        {
            if (string.IsNullOrEmpty(pageId))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "'pageId' query parameter is required.");

            string url = ConfluenceBaseUrl + "/rest/api/content/" + Uri.EscapeDataString(pageId);
            if (!string.IsNullOrEmpty(expand))
                url += "?expand=" + Uri.EscapeDataString(expand);

            return Forward("GET", url, null);
        }

        // ================================================================== //
        //  Core HTTP proxy helper                                             //
        // ================================================================== //
        private HttpResponseMessage Forward(string method, string url, string jsonBody)
        {
            // Enable TLS 1.2 / 1.1 for the outbound JIRA call.
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            HttpWebRequest req;
            try
            {
                req = (HttpWebRequest)WebRequest.Create(url);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadGateway,
                    "Invalid target URL '" + url + "': " + ex.Message);
            }

            req.Method = method;
            req.Timeout = TimeoutMs;
            req.ReadWriteTimeout = TimeoutMs;
            req.KeepAlive = true;
            req.Accept = "application/json";
            req.ServicePoint.Expect100Continue = false;

            // Attach Basic-Auth header using credentials from Web.config.
            if (!string.IsNullOrEmpty(JiraUser))
            {
                string token = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(JiraUser + ":" + JiraPass));
                req.Headers["Authorization"] = "Basic " + token;
            }

            // Write request body for POST.
            if (method == "POST" && jsonBody != null)
            {
                req.ContentType = "application/json";
                byte[] bytes = Encoding.UTF8.GetBytes(jsonBody);
                req.ContentLength = bytes.Length;
                using (Stream rs = req.GetRequestStream())
                    rs.Write(bytes, 0, bytes.Length);
            }

            // Execute and stream the response back to the caller.
            try
            {
                using (var resp = (HttpWebResponse)req.GetResponse())
                {
                    string responseBody;
                    using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                        responseBody = sr.ReadToEnd();

                    var msg = Request.CreateResponse(resp.StatusCode);
                    msg.Content = new StringContent(responseBody, Encoding.UTF8, "application/json");
                    return msg;
                }
            }
            catch (WebException wex)
            {
                // Return the JIRA error body intact so the browser can inspect it.
                if (wex.Response != null)
                {
                    var errResp = (HttpWebResponse)wex.Response;
                    string errBody;
                    using (var sr = new StreamReader(errResp.GetResponseStream(), Encoding.UTF8))
                        errBody = sr.ReadToEnd();

                    var msg = Request.CreateResponse(errResp.StatusCode);
                    msg.Content = new StringContent(errBody, Encoding.UTF8, "application/json");
                    return msg;
                }
                return Request.CreateErrorResponse(HttpStatusCode.BadGateway,
                    "JIRA proxy error: " + wex.Message);
            }
        }
    }
}
