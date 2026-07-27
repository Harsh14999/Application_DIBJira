using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using DFM_BPM.App_Code.DAL;
using DFM_BPM.App_Code.Helpers;

namespace DFM_BPM.Forms
{
    public partial class JiraIntegration : Page
    {
        // -- Instance log (used for non-async operations) -----------------
        private readonly StringBuilder _logBuf = new StringBuilder();

        // -- Lifecycle ----------------------------------------------------
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!AuthHelper.IsAdmin) { Response.Redirect("~/Default.aspx"); return; }
            if (!IsPostBack)
            {
                LoadConfigFromWebConfig();
                BindHistory();
            }
        }

        private void LoadConfigFromWebConfig()
        {
            var cfg = ConfigurationManager.AppSettings;
            txtBaseUrl.Text     = cfg["JiraBaseUrl"]        ?? "";
            txtJiraUser.Text    = cfg["JiraEmail"]          ?? cfg["JiraUser"] ?? "";
            txtProjects.Text    = cfg["JiraProjectKey"]     ?? "DMGT";
            txtBatchSize.Text   = cfg["BatchSize"]          ?? "1000";
            txtMinBatch.Text    = cfg["MinBatchSize"]       ?? "100";
            txtTimeout.Text     = cfg["HttpTimeoutSeconds"] ?? "600";
            txtMaxAttempts.Text = cfg["MaxFetchAttempts"]   ?? "3";
            txtFields.Text      = cfg["JiraFields"]         ?? "";
        }

        // -- Save config --------------------------------------------------
        protected void btnSaveConfig_Click(object sender, EventArgs e)
        {
            try
            {
                var cfg = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~");
                SetCfg(cfg, "JiraBaseUrl",        txtBaseUrl.Text.Trim());
                SetCfg(cfg, "JiraEmail",          txtJiraUser.Text.Trim());
                SetCfg(cfg, "JiraProjectKey",     txtProjects.Text.Trim());
                SetCfg(cfg, "BatchSize",          txtBatchSize.Text.Trim());
                SetCfg(cfg, "MinBatchSize",       txtMinBatch.Text.Trim());
                SetCfg(cfg, "HttpTimeoutSeconds", txtTimeout.Text.Trim());
                SetCfg(cfg, "MaxFetchAttempts",   txtMaxAttempts.Text.Trim());
                SetCfg(cfg, "JiraFields",         txtFields.Text.Trim());
                if (!string.IsNullOrEmpty(txtJiraPass.Text))
                    SetCfg(cfg, "JiraApiToken", txtJiraPass.Text.Trim());
                cfg.Save(System.Configuration.ConfigurationSaveMode.Minimal);
                ShowMsg("Configuration saved to Web.config.");
            }
            catch (Exception ex) { ShowMsg("Save failed: " + ex.Message); }
        }

        private static void SetCfg(System.Configuration.Configuration cfg, string key, string value)
        {
            if (cfg.AppSettings.Settings[key] != null)
                cfg.AppSettings.Settings[key].Value = value;
            else
                cfg.AppSettings.Settings.Add(key, value);
        }

        // -- Run full sync (background) -----------------------------------
        protected void btnRunSync_Click(object sender, EventArgs e)
        {
            string baseUrl     = txtBaseUrl.Text.Trim().TrimEnd('/');
            string jiraUser    = txtJiraUser.Text.Trim();
            string jiraToken   = txtJiraPass.Text;
            string projects    = txtProjects.Text.Trim();
            int    batchSize   = ParseInt(txtBatchSize.Text, 1000);
            int    minBatch    = ParseInt(txtMinBatch.Text,  100);
            int    timeoutSec  = ParseInt(txtTimeout.Text,   600);
            int    maxAttempts = ParseInt(txtMaxAttempts.Text, 3);
            string fieldsOvr   = (txtFields.Text ?? "").Trim();
            string triggeredBy = AuthHelper.CurrentUserShort;

            string syncKey = Guid.NewGuid().ToString("N");
            hfSyncKey.Value = syncKey;
            SetProgress(syncKey, 0, "Starting...", false, 0, 0, 0, 0, "");
            pnlSyncProgress.Visible = true;
            btnRunSync.Enabled = false;

            Task.Run(delegate
            {
                RunSyncBackground(syncKey, baseUrl, jiraUser, jiraToken, projects,
                    batchSize, minBatch, timeoutSec, maxAttempts, fieldsOvr, triggeredBy);
            });

            string js = "setTimeout(function(){ beginSyncPoll('" + syncKey + "'); }, 500);";
            ScriptManager.RegisterStartupScript(this, GetType(), "syncPoll", js, true);
        }

        // -- Background sync (static, no HttpContext) ---------------------
        private static void RunSyncBackground(string syncKey, string baseUrl,
            string jiraUser, string jiraToken, string projects,
            int batchSize, int minBatch, int timeoutSec, int maxAttempts,
            string fieldsOvr, string triggeredBy)
        {
            int pulled = 0, inserted = 0, updated = 0, failed = 0;
            DateTime start = DateTime.Now;
            int syncId = 0;
            try
            {
                syncId = StartSyncLogStatic(start, triggeredBy);
                ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                string[] projectList = (projects ?? "").Split(
                    new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                int projCount = Math.Max(projectList.Length, 1);
                int projDone  = 0;

                foreach (string p in projectList)
                {
                    string proj = p.Trim();
                    if (proj.Length == 0) continue;
                    int basePercent = projDone * 80 / projCount;
                    int nextPercent = (projDone + 1) * 80 / projCount;
                    SetProgress(syncKey, basePercent, "Syncing " + proj + "...",
                        false, pulled, inserted, updated, failed, "");

                    SyncProjectStatic(syncKey, proj, baseUrl, jiraUser, jiraToken,
                        batchSize, minBatch, timeoutSec * 1000, maxAttempts, fieldsOvr,
                        ref pulled, ref inserted, ref updated, ref failed,
                        basePercent, nextPercent);
                    projDone++;
                    SetProgress(syncKey, nextPercent, "Completed " + proj,
                        false, pulled, inserted, updated, failed, "");
                }

                SetProgress(syncKey, 85, "Applying hierarchy...", false, pulled, inserted, updated, failed, "");
                TryExecSPStatic("sp_ApplyHierarchyInheritance");

                SetProgress(syncKey, 92, "Refreshing dashboard...", false, pulled, inserted, updated, failed, "");
                TryExecSPStatic("sp_RefreshDashboardSummary");
                TryExecSPStatic("sp_RefreshDashboardReport");

                DateTime end = DateTime.Now;
                FinishSyncLogStatic(syncId, end, "Success", null, pulled, inserted, updated, failed);
                SetProgress(syncKey, 100, "Sync complete!", true, pulled, inserted, updated, failed, "");
            }
            catch (Exception ex)
            {
                if (syncId > 0)
                    FinishSyncLogStatic(syncId, DateTime.Now, "Failed", ex.ToString(), pulled, inserted, updated, failed);
                SetProgress(syncKey, 100, "Sync failed: " + ex.Message, true, pulled, inserted, updated, failed, ex.Message);
            }
        }

        // -- Progress cache -----------------------------------------------
        private static void SetProgress(string syncKey, int percent, string status, bool done,
            int pulled, int inserted, int updated, int failed, string error)
        {
            string s = (status ?? "").Replace("\\","\\\\").Replace("\"","\\\"").Replace("\n"," ").Replace("\r","");
            string er = (error  ?? "").Replace("\\","\\\\").Replace("\"","\\\"").Replace("\n"," ").Replace("\r","");
            string json = string.Format(
                "{{\"percent\":{0},\"status\":\"{1}\",\"done\":{2}," +
                "\"pulled\":{3},\"inserted\":{4},\"updated\":{5},\"failed\":{6},\"error\":\"{7}\"}}",
                percent, s, done ? "true" : "false", pulled, inserted, updated, failed, er);
            HttpRuntime.Cache.Insert("SyncProg_" + syncKey, json, null,
                DateTime.Now.AddMinutes(5), System.Web.Caching.Cache.NoSlidingExpiration);
        }

        // -- Sync project -------------------------------------------------
        private static void SyncProjectStatic(string syncKey, string projectKey,
            string baseUrl, string jiraUser, string jiraToken,
            int batchSize, int minBatch, int timeoutMs, int maxAttempts, string fieldsOvr,
            ref int pulled, ref int inserted, ref int updated, ref int failed,
            int basePercent, int nextPercent)
        {
            // fieldsOvr == null  -> use heavy default list (same as Program.cs)
            // fieldsOvr == ""    -> omit fields param (let JIRA pick defaults)
            // fieldsOvr != ""    -> use operator config
            string defaultFields =
                "summary,status,issuetype,priority,project,created,updated,duedate,assignee,reporter,issuelinks," +
                "customfield_12609,customfield_12604,customfield_11610,customfield_13380," +
                "customfield_13419,customfield_13511,customfield_13510,customfield_13505,customfield_13509," +
                "customfield_13317,customfield_13358,customfield_13357,customfield_14001," +
                "customfield_12603,customfield_13339,customfield_13379,customfield_13376," +
                "customfield_13306,customfield_13359,customfield_10964," +
                "customfield_13375,customfield_13374,customfield_13362," +
                "customfield_13307,customfield_13308," +
                "customfield_10043,customfield_10044," +
                "customfield_10911,customfield_10916,customfield_13310,customfield_10909,customfield_13101,customfield_13600," +
                "customfield_13304,customfield_13314,customfield_10605,customfield_10606," +
                "customfield_14125,customfield_14131,customfield_14132,customfield_14130,customfield_14128,customfield_14129,customfield_14126," +
                "customfield_10966,customfield_10102,customfield_13818,customfield_11627," +
                "customfield_13405,customfield_13406,customfield_11208,customfield_13513,customfield_13007,customfield_13006";

            // null means "not set" -> use default. Empty string means "let JIRA pick".
            string fields = fieldsOvr == null ? defaultFields : fieldsOvr;

            int startAt = 0, total = int.MaxValue, currentBatch = batchSize;

            while (startAt < total)
            {
                string jql = "project=" + projectKey + " ORDER BY updated DESC";
                string body = null;
                int attempt = 0;

                while (attempt < maxAttempts && body == null)
                {
                    attempt++;
                    try
                    {
                        body = JiraSearchStatic(baseUrl, jiraUser, jiraToken,
                            jql, startAt, currentBatch, fields, timeoutMs);
                    }
                    catch (WebException wex)
                    {
                        bool isTimeout = wex.Status == WebExceptionStatus.Timeout
                                      || wex.Status == WebExceptionStatus.ReceiveFailure
                                      || wex.Status == WebExceptionStatus.ConnectFailure
                                      || wex.Status == WebExceptionStatus.KeepAliveFailure;
                        if (attempt >= maxAttempts) break;
                        if (isTimeout && currentBatch > minBatch)
                            currentBatch = Math.Max(minBatch, currentBatch / 2);
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }

                if (body == null) { startAt += currentBatch; continue; }

                var ser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                Dictionary<string, object> obj;
                try { obj = ser.Deserialize<Dictionary<string, object>>(body); }
                catch (Exception) { break; }
                if (obj == null || !obj.ContainsKey("issues")) break;

                var issues = obj["issues"] as ArrayList;
                if (issues == null || issues.Count == 0) break;

                total = obj.ContainsKey("total") ? Convert.ToInt32(obj["total"]) : issues.Count;

                if (total > 0 && nextPercent > basePercent)
                {
                    int pct = basePercent + (startAt * (nextPercent - basePercent) / total);
                    SetProgress(syncKey, pct,
                        string.Format("Syncing {0}: {1}/{2}", projectKey, startAt, total),
                        false, pulled, inserted, updated, failed, "");
                }

                string connStr = Db.ConnectionString;
                using (var con = new SqlConnection(connStr))
                {
                    con.Open();
                    using (var cmd = BuildUpsertCommand(con))
                    {
                        foreach (Dictionary<string, object> issue in issues)
                        {
                            try
                            {
                                int r = UpsertIssue(cmd, issue, projectKey);
                                pulled++;
                                if (r == 1) inserted++; else updated++;
                            }
                            catch (Exception)
                            {
                                failed++;
                            }
                        }
                    }
                }

                int fetched = issues.Count;
                issues.Clear(); obj = null; body = null;
                startAt += fetched;
                if (startAt >= total || fetched < currentBatch) break;

                // Gradually recover batch size after successful batch
                if (currentBatch < batchSize)
                    currentBatch = Math.Min(batchSize, currentBatch * 2);
            }
        }

        // -- JIRA API: POST to /rest/api/2/search (matches Program.cs strategy) --
        private static string JiraSearchStatic(string baseUrl, string user, string pass,
            string jql, int startAt, int maxResults, string fieldsCsv, int timeoutMs)
        {
            string url = baseUrl.TrimEnd('/') + "/rest/api/2/search";
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method          = "POST";
            req.Timeout         = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            req.KeepAlive       = true;
            req.ServicePoint.Expect100Continue = false;
            req.Accept          = "application/json";
            req.ContentType     = "application/json";
            if (!string.IsNullOrEmpty(user))
            {
                string token = Convert.ToBase64String(Encoding.UTF8.GetBytes(user + ":" + pass));
                req.Headers["Authorization"] = "Basic " + token;
            }

            var sb = new StringBuilder();
            sb.Append("{\"jql\":").Append(JsonString(jql))
              .Append(",\"startAt\":").Append(startAt)
              .Append(",\"maxResults\":").Append(maxResults);
            if (!string.IsNullOrEmpty(fieldsCsv))
            {
                sb.Append(",\"fields\":[");
                string[] parts = fieldsCsv.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(JsonString(parts[i].Trim()));
                }
                sb.Append("]");
            }
            sb.Append("}");

            byte[] payload = Encoding.UTF8.GetBytes(sb.ToString());
            req.ContentLength = payload.Length;
            using (var rs = req.GetRequestStream()) { rs.Write(payload, 0, payload.Length); }

            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var sr   = new System.IO.StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return sr.ReadToEnd();
        }

        private static string JsonString(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char c in s)
            {
                if      (c == '"')  sb.Append("\\\"");
                else if (c == '\\') sb.Append("\\\\");
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else                sb.Append(c);
            }
            sb.Append('"');
            return sb.ToString();
        }

        // -- Upsert command (all fields aligned with Program.cs) ----------
        private static SqlCommand BuildUpsertCommand(SqlConnection con)
        {
            var cmd = con.CreateCommand();
            cmd.CommandTimeout = 150;
            cmd.CommandText = @"
SET NOCOUNT ON;
IF EXISTS (SELECT 1 FROM dbo.JiraIssues WHERE JiraKey=@k)
BEGIN
    UPDATE dbo.JiraIssues SET
        JiraID=@jid, Summary=@summary, Status=@status, OverallStatus=@status,
        Priority=@priority, IssueType=@itype, ProjectType=@itype,
        ProjectKey=@proj, ProjectName=@projName, ParentJiraID=@parent,
        Platform=@plat, PlatformVertical=@pv, PlatformName=@pn, SecondaryPlatform=@sp,
        ActivityRagStatus=@arag, ScheduleRag=@srag, BudgetRag=@brag, RaidRag=@raidr,
        OverallProjectRag=@orag, ProjectRAG=ISNULL(@computedRag, ProjectRAG),
        ChiefNameMapping=@chief, Manager=@mgr, TechLead=@tl,
        AccountableExecLead=@ael, SmeLead=@sme, AccountableExec=@ae,
        Sponsor=@ae, Stakeholder=@ael,
        Assignee=@assignee, Reporter=@reporter,
        AssignedProjectManager=@apm, IdhPortfolioHead=@iph,
        DemandOwner=@downer, DemandType=@dtype,
        TargetCompletionDate=@tcd, Target_Completion_Date=@tcd,
        ProposedDemandPickupDate=@pdp, Proposed_Demand_Pick_up_Date=@pdp,
        Actual_Go_Live_Date=@agld,
        Proposed_Baseline_0_End_Date=@pb0e, Proposed_Baseline_0_Start_Date=@pb0s,
        Proposed_Baseline_0_submission_Date=@pb0sb,
        Primary_Classification=@pcl, Classification=@cl, Department=@dep,
        JiraCreated=ISNULL(JiraCreated,@cre), JiraUpdated=@upd,
        CreatedDate=ISNULL(CreatedDate,@cre), UpdatedDate=@upd,
        EmployeeEmail=ISNULL(@empEmail,EmployeeEmail), EmployeeName=ISNULL(@empName,EmployeeName),
        ProjectPerformingDept=@ppd, ProjectSponsorDept=@psd,
        DemandDepartment=@ddept, RequesterDept=@rdept, ProjectDept=@pdept,
        DemandSegment=@dseg, DemandTitle=@dtitle, RegulatoryObservation=@regobs,
        BaselineStartDate=@blsd, BaselineEndDate=@bled,
        Baseline1ActualStart=@bl1as, Baseline0PlannedStart=@bl0ps,
        Baseline0PlannedEnd=@bl0pe, Baseline0ActualEnd=@bl0ae,
        Baseline1ActualGoLive=@bl1agl, Baseline0ActualStart=@bl0as, Baseline1ActualEnd=@bl1ae,
        RolloutStatus=@rost, EpicStatus=@epst, BrdStatus=@brdst,
        ScriptStatus=@scst, StatusGrey=@stgr, StatusReason=@streason,
        InitiativeStatus=@inst, ProjectOverallStatus=@posts, CbtpBrdStatus=@cbtp, FsdStatus=@fsdst
    WHERE JiraKey=@k;
    SELECT 0;
END
ELSE
BEGIN
    INSERT INTO dbo.JiraIssues(
        JiraID,JiraKey,Summary,Status,OverallStatus,Priority,IssueType,ProjectType,
        ProjectKey,ProjectName,ParentJiraID,
        Platform,PlatformVertical,PlatformName,SecondaryPlatform,
        ActivityRagStatus,ScheduleRag,BudgetRag,RaidRag,OverallProjectRag,ProjectRAG,
        ChiefNameMapping,Manager,TechLead,AccountableExecLead,SmeLead,
        AccountableExec,Sponsor,Stakeholder,Assignee,Reporter,
        AssignedProjectManager,IdhPortfolioHead,DemandOwner,DemandType,
        TargetCompletionDate,Target_Completion_Date,
        ProposedDemandPickupDate,Proposed_Demand_Pick_up_Date,
        Actual_Go_Live_Date,Proposed_Baseline_0_End_Date,Proposed_Baseline_0_Start_Date,
        Proposed_Baseline_0_submission_Date,Primary_Classification,Classification,Department,
        JiraCreated,JiraUpdated,CreatedDate,UpdatedDate,EmployeeEmail,EmployeeName,
        ProjectPerformingDept,ProjectSponsorDept,DemandDepartment,RequesterDept,ProjectDept,
        DemandSegment,DemandTitle,RegulatoryObservation,
        BaselineStartDate,BaselineEndDate,Baseline1ActualStart,Baseline0PlannedStart,
        Baseline0PlannedEnd,Baseline0ActualEnd,Baseline1ActualGoLive,Baseline0ActualStart,
        Baseline1ActualEnd,RolloutStatus,EpicStatus,BrdStatus,ScriptStatus,
        StatusGrey,StatusReason,InitiativeStatus,ProjectOverallStatus,CbtpBrdStatus,FsdStatus)
    VALUES(
        @jid,@k,@summary,@status,@status,@priority,@itype,@itype,
        @proj,@projName,@parent,
        @plat,@pv,@pn,@sp,
        @arag,@srag,@brag,@raidr,@orag,@computedRag,
        @chief,@mgr,@tl,@ael,@sme,@ae,@ae,@ael,@assignee,@reporter,
        @apm,@iph,@downer,@dtype,
        @tcd,@tcd,@pdp,@pdp,
        @agld,@pb0e,@pb0s,@pb0sb,@pcl,@cl,@dep,
        @cre,@upd,@cre,@upd,@empEmail,@empName,
        @ppd,@psd,@ddept,@rdept,@pdept,
        @dseg,@dtitle,@regobs,
        @blsd,@bled,@bl1as,@bl0ps,@bl0pe,@bl0ae,@bl1agl,@bl0as,@bl1ae,
        @rost,@epst,@brdst,@scst,@stgr,@streason,@inst,@posts,@cbtp,@fsdst);
    SELECT 1;
END";
            foreach (string p in new[] {
                "@jid","@k","@summary","@status","@priority","@itype","@proj","@projName","@parent",
                "@plat","@pv","@pn","@sp","@arag","@srag","@brag","@raidr","@orag","@computedRag",
                "@chief","@mgr","@tl","@ael","@sme","@ae","@assignee","@reporter",
                "@apm","@iph","@downer","@dtype","@pcl","@cl","@dep","@empEmail","@empName",
                "@ppd","@psd","@ddept","@rdept","@pdept","@dseg","@dtitle","@regobs",
                "@rost","@epst","@brdst","@scst","@stgr","@streason","@inst","@posts","@cbtp","@fsdst" })
                cmd.Parameters.Add(p, SqlDbType.NVarChar, 500);
            foreach (string p in new[] {
                "@tcd","@pdp","@agld","@pb0e","@pb0s","@pb0sb","@cre","@upd",
                "@blsd","@bled","@bl1as","@bl0ps","@bl0pe","@bl0ae","@bl1agl","@bl0as","@bl1ae" })
                cmd.Parameters.Add(p, SqlDbType.DateTime);
            return cmd;
        }

        private static int UpsertIssue(SqlCommand cmd, Dictionary<string, object> issue, string projectKey)
        {
            string key = GetStr(issue, "key");
            if (string.IsNullOrEmpty(key)) throw new InvalidOperationException("Issue missing key");
            var flds = issue.ContainsKey("fields") ? issue["fields"] as Dictionary<string, object> : null;
            if (flds == null) throw new InvalidOperationException("Issue " + key + " missing fields");

            string summary    = GetStr(flds, "summary");
            string status     = GetNested(flds, "status",    "name");
            string priority   = GetNested(flds, "priority",  "name");
            string issuetype  = GetNested(flds, "issuetype", "name");
            string projName   = GetNested(flds, "project",   "name");
            string parentJid  = ExtractParentKey(flds);

            // Platform fields
            string platform         = GetNestedAny(flds, "customfield_12609", new[] { "value","name" });
            string platformVertical = GetNestedAny(flds, "customfield_12604", new[] { "value","name" });
            string platformName     = GetNestedAny(flds, "customfield_11610", new[] { "value","name" });
            string secPlatform      = GetNestedAny(flds, "customfield_13380", new[] { "value","name" });
            if (string.IsNullOrEmpty(platform)) platform = GetNestedAny(flds, "customfield_10043", new[] { "value","name" });
            if (string.IsNullOrEmpty(platform)) platform = GetNestedAny(flds, "customfield_10044", new[] { "value","name" });
            if (string.IsNullOrEmpty(platform)) platform = ScanForPlatform(flds);

            // RAG
            string activityRag = GetNestedAny(flds, "customfield_13419", new[] { "value","name" });
            string scheduleRag = GetNestedAny(flds, "customfield_13511", new[] { "value","name" });
            string budgetRag   = GetNestedAny(flds, "customfield_13510", new[] { "value","name" });
            string raidRag     = GetNestedAny(flds, "customfield_13505", new[] { "value","name" });
            string overallRag  = GetNestedAny(flds, "customfield_13509", new[] { "value","name" });

            // People
            string accExecLead      = GetNested(flds, "customfield_13357", "displayName");
            string smeLead          = GetNested(flds, "customfield_13358", "displayName");
            string accExec          = GetNested(flds, "customfield_13379", "displayName");
            string idhPortfolioHead = GetNested(flds, "customfield_13376", "displayName");
            string assignedPm       = GetNested(flds, "customfield_12603", "displayName");
            string demandOwner      = GetNested(flds, "customfield_13317", "displayName");
            string chief            = GetNested(flds, "customfield_14001", "displayName");
            string assignee         = GetNested(flds, "assignee",          "displayName");
            string assigneeEmail    = GetNested(flds, "assignee",          "emailAddress");
            string reporter         = GetNested(flds, "reporter",          "displayName");

            // Demand / classification
            string demandType            = GetNestedAny(flds, "customfield_13339", new[] { "value","name" });
            string primaryClassification = GetNestedAny(flds, "customfield_13307", new[] { "value","name" });
            string department            = GetNestedAny(flds, "customfield_13308", new[] { "value","name" });

            // Date strings
            string targetD   = GetStr(flds, "customfield_13306");
            string proposedD = GetStr(flds, "customfield_13359");
            string actualGoD = GetStr(flds, "customfield_10964");
            string pb0End    = GetStr(flds, "customfield_13375");
            string pb0Start  = GetStr(flds, "customfield_13374");
            string pb0Submit = GetStr(flds, "customfield_13362");

            // New department fields
            string projPerformingDept = GetNestedAny(flds, "customfield_10911", new[] { "value","name" });
            string projSponsorDept    = GetNestedAny(flds, "customfield_10916", new[] { "value","name" });
            string demandDept         = GetNestedAny(flds, "customfield_13310", new[] { "value","name" });
            string requesterDept      = GetNestedAny(flds, "customfield_10909", new[] { "value","name" });
            string projectDept        = GetNestedAny(flds, "customfield_13101", new[] { "value","name" });
            string demandSegment      = GetNestedAny(flds, "customfield_13600", new[] { "value","name" });
            string demandTitle        = GetStr(flds,  "customfield_13304");
            string regulatoryObs      = GetStr(flds,  "customfield_13314");

            // Baseline dates
            string baselineStart  = GetStr(flds, "customfield_10605");
            string baselineEnd    = GetStr(flds, "customfield_10606");
            string bl1ActStart    = GetStr(flds, "customfield_14125");
            string bl0PlannedSt   = GetStr(flds, "customfield_14131");
            string bl0PlannedEnd  = GetStr(flds, "customfield_14132");
            string bl0ActEnd      = GetStr(flds, "customfield_14130");
            string bl1ActGoLive   = GetStr(flds, "customfield_14128");
            string bl0ActStart    = GetStr(flds, "customfield_14129");
            string bl1ActEnd      = GetStr(flds, "customfield_14126");

            // Status fields
            string rolloutStatus  = GetNestedAny(flds, "customfield_10966", new[] { "value","name" });
            string epicStatus     = GetNestedAny(flds, "customfield_10102", new[] { "value","name" });
            string brdStatus      = GetNestedAny(flds, "customfield_13818", new[] { "value","name" });
            string scriptStatus   = GetNestedAny(flds, "customfield_11627", new[] { "value","name" });
            string statusGrey     = GetNestedAny(flds, "customfield_13405", new[] { "value","name" });
            string statusReason   = GetNestedAny(flds, "customfield_13406", new[] { "value","name" });
            string initStatus     = GetNestedAny(flds, "customfield_11208", new[] { "value","name" });
            string projOverStatus = GetNestedAny(flds, "customfield_13513", new[] { "value","name" });
            string cbtpBrd        = GetNestedAny(flds, "customfield_13007", new[] { "value","name" });
            string fsdStatus      = GetNestedAny(flds, "customfield_13006", new[] { "value","name" });

            DateTime? tcdDt   = ParseDate(targetD);
            string computedRag = ComputeRag(tcdDt);

            cmd.Parameters["@jid"].Value      = (object)key       ?? DBNull.Value;
            cmd.Parameters["@k"].Value        = key;
            cmd.Parameters["@summary"].Value  = (object)Clip(summary, 500) ?? DBNull.Value;
            cmd.Parameters["@status"].Value   = (object)status    ?? DBNull.Value;
            cmd.Parameters["@priority"].Value = (object)priority  ?? DBNull.Value;
            cmd.Parameters["@itype"].Value    = (object)issuetype ?? DBNull.Value;
            cmd.Parameters["@proj"].Value     = projectKey;
            cmd.Parameters["@projName"].Value = (object)Clip(string.IsNullOrEmpty(projName) ? summary : projName, 300) ?? DBNull.Value;
            cmd.Parameters["@parent"].Value   = (object)parentJid ?? DBNull.Value;
            cmd.Parameters["@plat"].Value     = (object)platform  ?? DBNull.Value;
            cmd.Parameters["@pv"].Value       = (object)platformVertical ?? DBNull.Value;
            cmd.Parameters["@pn"].Value       = (object)platformName     ?? DBNull.Value;
            cmd.Parameters["@sp"].Value       = (object)secPlatform      ?? DBNull.Value;
            cmd.Parameters["@arag"].Value     = (object)activityRag      ?? DBNull.Value;
            cmd.Parameters["@srag"].Value     = (object)scheduleRag      ?? DBNull.Value;
            cmd.Parameters["@brag"].Value     = (object)budgetRag        ?? DBNull.Value;
            cmd.Parameters["@raidr"].Value    = (object)raidRag          ?? DBNull.Value;
            cmd.Parameters["@orag"].Value     = (object)overallRag       ?? DBNull.Value;
            cmd.Parameters["@computedRag"].Value = (object)computedRag   ?? DBNull.Value;
            cmd.Parameters["@chief"].Value    = (object)chief      ?? DBNull.Value;
            cmd.Parameters["@mgr"].Value      = (object)assignedPm ?? DBNull.Value;
            cmd.Parameters["@tl"].Value       = (object)smeLead    ?? DBNull.Value;
            cmd.Parameters["@ael"].Value      = (object)accExecLead     ?? DBNull.Value;
            cmd.Parameters["@sme"].Value      = (object)smeLead         ?? DBNull.Value;
            cmd.Parameters["@ae"].Value       = (object)accExec         ?? DBNull.Value;
            cmd.Parameters["@assignee"].Value = (object)assignee        ?? DBNull.Value;
            cmd.Parameters["@reporter"].Value = (object)reporter        ?? DBNull.Value;
            cmd.Parameters["@empEmail"].Value = (object)assigneeEmail   ?? DBNull.Value;
            cmd.Parameters["@empName"].Value  = (object)assignee        ?? DBNull.Value;
            cmd.Parameters["@apm"].Value      = (object)assignedPm      ?? DBNull.Value;
            cmd.Parameters["@iph"].Value      = (object)idhPortfolioHead ?? DBNull.Value;
            cmd.Parameters["@downer"].Value   = (object)demandOwner     ?? DBNull.Value;
            cmd.Parameters["@dtype"].Value    = (object)demandType      ?? DBNull.Value;
            cmd.Parameters["@tcd"].Value      = DtVal(tcdDt);
            cmd.Parameters["@pdp"].Value      = DtVal(ParseDate(proposedD));
            cmd.Parameters["@agld"].Value     = DtVal(ParseDate(actualGoD));
            cmd.Parameters["@pb0e"].Value     = DtVal(ParseDate(pb0End));
            cmd.Parameters["@pb0s"].Value     = DtVal(ParseDate(pb0Start));
            cmd.Parameters["@pb0sb"].Value    = DtVal(ParseDate(pb0Submit));
            cmd.Parameters["@pcl"].Value      = (object)primaryClassification ?? DBNull.Value;
            cmd.Parameters["@cl"].Value       = (object)primaryClassification ?? DBNull.Value;
            cmd.Parameters["@dep"].Value      = (object)department      ?? DBNull.Value;
            cmd.Parameters["@cre"].Value      = DtVal(ParseDate(GetStr(flds, "created")));
            cmd.Parameters["@upd"].Value      = DtVal(ParseDate(GetStr(flds, "updated")));
            cmd.Parameters["@ppd"].Value      = (object)projPerformingDept ?? DBNull.Value;
            cmd.Parameters["@psd"].Value      = (object)projSponsorDept    ?? DBNull.Value;
            cmd.Parameters["@ddept"].Value    = (object)demandDept         ?? DBNull.Value;
            cmd.Parameters["@rdept"].Value    = (object)requesterDept      ?? DBNull.Value;
            cmd.Parameters["@pdept"].Value    = (object)projectDept        ?? DBNull.Value;
            cmd.Parameters["@dseg"].Value     = (object)demandSegment      ?? DBNull.Value;
            cmd.Parameters["@dtitle"].Value   = (object)Clip(demandTitle, 500)  ?? DBNull.Value;
            cmd.Parameters["@regobs"].Value   = (object)regulatoryObs      ?? DBNull.Value;
            cmd.Parameters["@blsd"].Value     = DtVal(ParseDate(baselineStart));
            cmd.Parameters["@bled"].Value     = DtVal(ParseDate(baselineEnd));
            cmd.Parameters["@bl1as"].Value    = DtVal(ParseDate(bl1ActStart));
            cmd.Parameters["@bl0ps"].Value    = DtVal(ParseDate(bl0PlannedSt));
            cmd.Parameters["@bl0pe"].Value    = DtVal(ParseDate(bl0PlannedEnd));
            cmd.Parameters["@bl0ae"].Value    = DtVal(ParseDate(bl0ActEnd));
            cmd.Parameters["@bl1agl"].Value   = DtVal(ParseDate(bl1ActGoLive));
            cmd.Parameters["@bl0as"].Value    = DtVal(ParseDate(bl0ActStart));
            cmd.Parameters["@bl1ae"].Value    = DtVal(ParseDate(bl1ActEnd));
            cmd.Parameters["@rost"].Value     = (object)rolloutStatus  ?? DBNull.Value;
            cmd.Parameters["@epst"].Value     = (object)epicStatus     ?? DBNull.Value;
            cmd.Parameters["@brdst"].Value    = (object)brdStatus      ?? DBNull.Value;
            cmd.Parameters["@scst"].Value     = (object)scriptStatus   ?? DBNull.Value;
            cmd.Parameters["@stgr"].Value     = (object)statusGrey     ?? DBNull.Value;
            cmd.Parameters["@streason"].Value = (object)statusReason   ?? DBNull.Value;
            cmd.Parameters["@inst"].Value     = (object)initStatus     ?? DBNull.Value;
            cmd.Parameters["@posts"].Value    = (object)projOverStatus ?? DBNull.Value;
            cmd.Parameters["@cbtp"].Value     = (object)cbtpBrd        ?? DBNull.Value;
            cmd.Parameters["@fsdst"].Value    = (object)fsdStatus      ?? DBNull.Value;

            object r = cmd.ExecuteScalar();
            return r == null || r == DBNull.Value ? 0 : Convert.ToInt32(r);
        }

        // -- Field helpers -----------------------------------------------
        private static string GetStr(IDictionary<string, object> d, string k)
        {
            return d != null && d.ContainsKey(k) && d[k] != null ? d[k].ToString() : null;
        }

        private static string GetNested(IDictionary<string, object> d, string outer, string inner)
        {
            if (d == null || !d.ContainsKey(outer) || d[outer] == null) return null;
            var inn = d[outer] as Dictionary<string, object>;
            return GetStr(inn, inner);
        }

        private static string GetNestedAny(IDictionary<string, object> d, string outer, string[] innerKeys)
        {
            if (d == null || !d.ContainsKey(outer) || d[outer] == null) return null;
            var inn = d[outer] as Dictionary<string, object>;
            if (inn == null) return d[outer].ToString();
            foreach (string k in innerKeys)
            {
                string v = GetStr(inn, k);
                if (!string.IsNullOrEmpty(v)) return v;
            }
            return null;
        }

        private static string ScanForPlatform(IDictionary<string, object> fields)
        {
            if (fields == null) return null;
            foreach (var kv in fields)
            {
                if (kv.Value == null) continue;
                string k = kv.Key.ToLowerInvariant();
                if (!k.Contains("platform")) continue;
                var d = kv.Value as Dictionary<string, object>;
                if (d != null)
                {
                    if (d.ContainsKey("value") && d["value"] != null) return d["value"].ToString();
                    if (d.ContainsKey("name")  && d["name"]  != null) return d["name"].ToString();
                }
                string sv = kv.Value as string;
                if (!string.IsNullOrEmpty(sv)) return sv;
            }
            return null;
        }

        private static string ExtractParentKey(IDictionary<string, object> fields)
        {
            if (fields == null || !fields.ContainsKey("issuelinks") || fields["issuelinks"] == null) return null;
            var links = fields["issuelinks"] as ArrayList;
            if (links == null) return null;
            foreach (Dictionary<string, object> link in links)
            {
                if (link == null || !link.ContainsKey("type")) continue;
                var type = link["type"] as Dictionary<string, object>;
                string typeName = GetStr(type, "name")   ?? "";
                string inward   = GetStr(type, "inward") ?? "";
                if (typeName.IndexOf("Parent", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    inward.IndexOf("child of", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (link.ContainsKey("inwardIssue"))
                    {
                        var iss = link["inwardIssue"] as Dictionary<string, object>;
                        if (iss != null && iss.ContainsKey("key")) return iss["key"].ToString();
                    }
                }
            }
            return null;
        }

        private static string ComputeRag(DateTime? targetDate)
        {
            if (!targetDate.HasValue) return "Green";
            int days = (int)Math.Floor((targetDate.Value.Date - DateTime.Today).TotalDays);
            if (days > 30) return "Red";
            if (days > 3)  return "Amber";
            return "Green";
        }

        private static DateTime? ParseDate(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            DateTime dt;
            if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal |
                System.Globalization.DateTimeStyles.AdjustToUniversal, out dt))
                return dt;
            if (DateTime.TryParseExact(s, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out dt))
                return dt;
            return null;
        }

        private static object DtVal(DateTime? d)
        {
            return d.HasValue ? (object)d.Value : DBNull.Value;
        }

        private static string Clip(string s, int max)
        {
            if (s == null) return null;
            return s.Length <= max ? s : s.Substring(0, max);
        }

        // -- Sync log (static) -------------------------------------------
        private static int StartSyncLogStatic(DateTime start, string triggeredBy)
        {
            try
            {
                return Convert.ToInt32(Db.Scalar(
                    "INSERT INTO dbo.SyncLog(StartTime,Status,TriggeredBy) " +
                    "OUTPUT INSERTED.SyncID VALUES(@s,'Running',@by)",
                    Db.P("@s", start), Db.P("@by", triggeredBy)));
            }
            catch { return 0; }
        }

        private static void FinishSyncLogStatic(int syncId, DateTime end, string status,
            string errDetail, int pulled, int inserted, int updated, int failed)
        {
            if (syncId == 0) return;
            try
            {
                Db.Exec(
                    "UPDATE dbo.SyncLog SET EndTime=@e,Status=@s,ErrorDetail=@err," +
                    "PulledCount=@p,InsertedCount=@i,UpdatedCount=@u,FailedCount=@f " +
                    "WHERE SyncID=@id",
                    Db.P("@e", end), Db.P("@s", status),
                    Db.P("@err", errDetail != null ? (object)errDetail : DBNull.Value),
                    Db.P("@p", pulled), Db.P("@i", inserted),
                    Db.P("@u", updated), Db.P("@f", failed),
                    Db.P("@id", syncId));
            }
            catch { }
        }

        private static void TryExecSPStatic(string spName)
        {
            try { Db.ExecSP(spName); }
            catch { }
        }

        // -- Instance helpers for non-async operations -------------------
        protected void btnApplyHierarchy_Click(object sender, EventArgs e)
        {
            Log("Applying hierarchy inheritance...");
            TryExecSP("sp_ApplyHierarchyInheritance");
            litLog.Text = FormatLog();
            ShowMsg("Hierarchy inheritance applied.");
        }

        protected void btnRefreshDash_Click(object sender, EventArgs e)
        {
            Log("Refreshing DashboardSummary...");
            TryExecSP("sp_RefreshDashboardSummary");
            Log("Refreshing DashboardReport...");
            TryExecSP("sp_RefreshDashboardReport");
            litLog.Text = FormatLog();
            ShowMsg("Dashboard refreshed.");
        }

        private void BindHistory()
        {
            try
            {
                gvSyncHistory.DataSource = Db.Query(
                    "SELECT TOP 20 SyncID,StartTime,EndTime,Status," +
                    "PulledCount,InsertedCount,UpdatedCount,FailedCount,TriggeredBy " +
                    "FROM dbo.SyncLog ORDER BY SyncID DESC");
                gvSyncHistory.DataBind();
            }
            catch
            {
                gvSyncHistory.DataSource = null;
                gvSyncHistory.DataBind();
            }
        }

        private void Log(string msg)
        {
            string ts  = DateTime.Now.ToString("HH:mm:ss");
            string css = msg.StartsWith("FATAL") ? "log-err"
                       : msg.StartsWith("=====") || msg.StartsWith("---") ? "log-ok" : "";
            _logBuf.AppendFormat("<span class='{0}'>[{1}] {2}</span>\n",
                css, ts, HttpUtility.HtmlEncode(msg));
        }

        private string FormatLog() { return _logBuf.ToString(); }

        private void TryExecSP(string spName)
        {
            try   { Db.ExecSP(spName); Log(spName + " OK."); }
            catch (Exception ex) { Log(spName + " failed: " + ex.Message); }
        }

        private static int ParseInt(string s, int def)
        {
            int v;
            return int.TryParse(s ?? "", out v) && v > 0 ? v : def;
        }

        private void ShowMsg(string msg)
        {
            lblMsg.Text    = msg;
            lblMsg.Visible = true;
        }
    }
}