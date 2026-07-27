using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using DFM_BPM.App_Code.DAL;
using DFM_BPM.App_Code.Helpers;

namespace DFM_BPM
{
    public partial class SiteMaster : MasterPage
    {
        // Notification controls (no Site.Master.designer.cs exists in this project — declared directly here).
        protected Repeater rptNotifications;
        protected Panel pnlNoNotif;

        public string CurrentUser     { get { return AuthHelper.CurrentUserShort; } }
        public string CurrentFullName { get { return AuthHelper.CurrentFullName; } }
        public bool   IsAdminUser     { get { return AuthHelper.IsAdmin; } }
        public bool   IsDev           { get { return AuthHelper.IsDev; } }
        public int    UnreadNotifCount { get; private set; }
        public string LastSyncLabel   { get; private set; }

        public string UserInitials
        {
            get
            {
                string n = CurrentFullName ?? CurrentUser;
                if (string.IsNullOrEmpty(n)) return "?";
                var parts = n.Split(' ');
                return parts.Length >= 2
                    ? (parts[0][0].ToString() + parts[1][0]).ToUpper()
                    : n.Substring(0, Math.Min(2, n.Length)).ToUpper();
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Sign-out handling
            if (Request.QueryString["signout"] == "1")
            {
                AuthHelper.SignOut();
                Response.Redirect("~/Default.aspx");
            }

            try
            {
                UnreadNotifCount = UserDAL.GetUnreadCount(CurrentUser);
                DataTable notifDt = UserDAL.GetNotifications(CurrentUser);
                rptNotifications.DataSource = notifDt;
                rptNotifications.DataBind();
                pnlNoNotif.Visible = notifDt.Rows.Count == 0;
            }
            catch { UnreadNotifCount = 0; }

            try
            {
                var row = Db.QueryRow("SELECT TOP 1 CONVERT(VARCHAR,EndTime,120) AS T FROM dbo.SyncLog WHERE Status='Success' ORDER BY SyncID DESC");
                LastSyncLabel = row != null ? row["T"].ToString() : "Never";
            }
            catch { LastSyncLabel = "N/A"; }

            if (IsDev && !IsPostBack)
            {
                LoadDevToolbar();
            }
        }

        private void LoadDevToolbar()
        {
            try
            {
                var ctrl = FindControl("ddlDevUser") as DropDownList;
                if (ctrl != null && ctrl.Items.Count == 0)
                {
                    var rows = Db.Query(
                        "SELECT Username FROM dbo.AppUsers WHERE IsEnabled=1 ORDER BY Username");
                    ctrl.Items.Clear();
                    foreach (System.Data.DataRow r in rows.Rows)
                        ctrl.Items.Add(r["Username"].ToString());

                    string curUser = AuthHelper.CurrentUserShort;
                    var item = ctrl.Items.FindByValue(curUser);
                    if (item != null) item.Selected = true;
                }

                var roleOverride = Session["DevRoleOverride"] as string;
                var ddlRole = FindControl("ddlDevRole") as DropDownList;
                if (ddlRole != null && !string.IsNullOrEmpty(roleOverride))
                {
                    var ri = ddlRole.Items.FindByValue(roleOverride);
                    if (ri != null) ri.Selected = true;
                }

                var lbl = FindControl("lblDevInfo") as Label;
                if (lbl != null)
                    lbl.Text = string.Format("User: {0} | Role: {1}",
                        AuthHelper.CurrentUserShort, AuthHelper.CurrentRole);
            }
            catch { /* dev toolbar load errors should not break the page */ }
        }

        protected void ddlDevUser_Changed(object sender, EventArgs e)
        {
            var ddl = sender as DropDownList;
            if (ddl != null && !string.IsNullOrEmpty(ddl.SelectedValue))
                AuthHelper.SwitchDevUser(ddl.SelectedValue);
            Response.Redirect(Request.RawUrl);
        }

        protected void ddlDevRole_Changed(object sender, EventArgs e)
        {
            var ddl = sender as DropDownList;
            if (ddl != null)
                AuthHelper.SwitchDevRole(ddl.SelectedValue);
            Response.Redirect(Request.RawUrl);
        }

        protected string IsActive(string page)
        {
            string path = Request.AppRelativeCurrentExecutionFilePath ?? "";
            return path.IndexOf(page, StringComparison.OrdinalIgnoreCase) >= 0 ? "active" : "";
        }

        // ===== Notifications =====
        protected void rptNotifications_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0) return;

            if (e.CommandName == "OpenNotif")
            {
                UserDAL.MarkRead(id);
                DataRow r = Db.QueryRow("SELECT LinkUrl FROM dbo.Notifications WHERE NotificationID=@id", Db.P("@id", id));
                string url = r != null && r["LinkUrl"] != DBNull.Value ? r["LinkUrl"].ToString() : "";
                Response.Redirect(ResolveUrl(string.IsNullOrEmpty(url) ? "~/Default.aspx" : url));
            }
            else if (e.CommandName == "DelNotif")
            {
                UserDAL.DeleteNotification(id);
                Response.Redirect(Request.RawUrl);
            }
        }

        protected void btnClearAllNotif_Click(object sender, EventArgs e)
        {
            UserDAL.DeleteAllNotifications(CurrentUser);
            Response.Redirect(Request.RawUrl);
        }
    }
}
