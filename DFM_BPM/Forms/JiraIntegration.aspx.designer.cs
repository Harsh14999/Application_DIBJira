// Auto-generated designer file for JiraIntegration.aspx
namespace DFM_BPM.Forms
{
    public partial class JiraIntegration
    {
        protected global::System.Web.UI.WebControls.Label        lblMsg;
        protected global::System.Web.UI.WebControls.TextBox      txtBaseUrl;
        protected global::System.Web.UI.WebControls.TextBox      txtJiraUser;
        protected global::System.Web.UI.WebControls.TextBox      txtJiraPass;
        protected global::System.Web.UI.WebControls.TextBox      txtProjects;
        protected global::System.Web.UI.WebControls.TextBox      txtBatchSize;
        protected global::System.Web.UI.WebControls.TextBox      txtMinBatch;
        protected global::System.Web.UI.WebControls.TextBox      txtTimeout;
        protected global::System.Web.UI.WebControls.TextBox      txtMaxAttempts;
        protected global::System.Web.UI.WebControls.TextBox      txtFields;
        protected global::System.Web.UI.WebControls.Button       btnSaveConfig;
        protected global::System.Web.UI.WebControls.Button       btnRunSync;
        protected global::System.Web.UI.WebControls.Button       btnApplyHierarchy;
        protected global::System.Web.UI.WebControls.Button       btnRefreshDash;
        protected global::System.Web.UI.WebControls.Button       btnClearLog;
        protected global::System.Web.UI.HtmlControls.HtmlGenericControl syncLog;
        protected global::System.Web.UI.WebControls.HiddenField  hfSyncKey;
        protected global::System.Web.UI.WebControls.Panel        pnlSyncProgress;
        protected global::System.Web.UI.WebControls.Label        lblSyncStatus;
        protected global::System.Web.UI.WebControls.Literal      litPulled;
        protected global::System.Web.UI.WebControls.Literal      litInserted;
        protected global::System.Web.UI.WebControls.Literal      litUpdated;
        protected global::System.Web.UI.WebControls.Literal      litFailed;
        protected global::System.Web.UI.WebControls.Literal      litDuration;
        protected global::System.Web.UI.WebControls.Literal      litLastStatus;
        protected global::System.Web.UI.WebControls.Literal      litLog;
        protected global::System.Web.UI.WebControls.GridView     gvSyncHistory;
    }
}
