using System;
using System.Data;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using DFM_BPM.App_Code.DAL;
using DFM_BPM.App_Code.Helpers;

namespace DFM_BPM.Admin
{
    public partial class UserManagement : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!AuthHelper.IsAdmin)
            {
                Response.Redirect("~/Default.aspx");
                return;
            }
            if (!IsPostBack)
            {
                BindAll();
            }
        }

        private void BindAll()
        {
            gvUsers.DataSource = UserDAL.GetUsers();
            gvUsers.DataBind();

            DataTable roles = UserDAL.GetRoles();
            ddlRole.DataSource     = roles;
            ddlRole.DataTextField  = "RoleName";
            ddlRole.DataValueField = "RoleID";
            ddlRole.DataBind();

            gvRoles.DataSource = roles;
            gvRoles.DataBind();
        }

        /// <summary>Renders colour-coded role badges for the user grid.</summary>
        protected string GetRoleBadges(string username)
        {
            var sb = new StringBuilder();
            try
            {
                DataTable dt = UserDAL.GetUserRoleAssignments(username);
                foreach (DataRow r in dt.Rows)
                {
                    string role = r["RoleType"].ToString();
                    string css = role == "Admin"     ? "role-admin" :
                                 role == "Approver"  ? "role-approver" :
                                 role == "Reviewer"  ? "role-reviewer" : "role-requestor";
                    sb.AppendFormat("<span class='role-badge {0}'>{1}</span>", css, role);
                }
            }
            catch { /* ignore */ }
            return sb.ToString();
        }

        // ===== Save / Update Windows user =====
        protected void btnSaveUser_Click(object sender, EventArgs e)
        {
            string user = (txtUsername.Text ?? "").Trim();
            if (string.IsNullOrEmpty(user)) { ShowMsg("Windows username is required."); return; }

            // Strip domain prefix for storage
            int bs = user.LastIndexOf('\\');
            if (bs >= 0) user = user.Substring(bs + 1);

            int userId = Convert.ToInt32(hfEditUserId.Value);
            int roleId = Convert.ToInt32(ddlRole.SelectedValue);

            if (userId == 0)
            {
                // Insert new user (no password for Windows auth)
                int exists = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM dbo.AppUsers WHERE Username=@u", Db.P("@u", user)));
                if (exists > 0)
                {
                    // Update instead
                    Db.Exec("UPDATE dbo.AppUsers SET FullName=@n, Email=@e, Department=@d, RoleID=@r WHERE Username=@u",
                        Db.P("@n", txtFullName.Text.Trim()), Db.P("@e", txtEmail.Text.Trim()),
                        Db.P("@d", txtDept.Text.Trim()), Db.P("@r", roleId), Db.P("@u", user));
                    ShowMsg("User updated.");
                }
                else
                {
                    Db.Exec("INSERT INTO dbo.AppUsers(Username,FullName,Email,Department,RoleID,IsEnabled,CreatedBy) VALUES(@u,@n,@e,@d,@r,1,@cb)",
                        Db.P("@u", user), Db.P("@n", txtFullName.Text.Trim()),
                        Db.P("@e", txtEmail.Text.Trim()), Db.P("@d", txtDept.Text.Trim()),
                        Db.P("@r", roleId), Db.P("@cb", AuthHelper.CurrentUserShort));
                    // Default role assignment
                    Db.Exec("IF NOT EXISTS(SELECT 1 FROM dbo.UserRoleAssignments WHERE Username=@u AND RoleType='Requestor') " +
                            "INSERT INTO dbo.UserRoleAssignments(Username,RoleType,CreatedBy) VALUES(@u,'Requestor',@cb)",
                        Db.P("@u", user), Db.P("@cb", AuthHelper.CurrentUserShort));
                    ShowMsg("User '" + user + "' registered. They will be provisioned on first login.");
                }
            }
            else
            {
                Db.Exec("UPDATE dbo.AppUsers SET FullName=@n, Email=@e, Department=@d, RoleID=@r WHERE UserID=@id",
                    Db.P("@n", txtFullName.Text.Trim()), Db.P("@e", txtEmail.Text.Trim()),
                    Db.P("@d", txtDept.Text.Trim()), Db.P("@r", roleId), Db.P("@id", userId));
                ShowMsg("User updated.");
            }

            ClearForm();
            BindAll();
        }

        protected void btnCancelEdit_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            hfEditUserId.Value = "0";
            txtUsername.Text = txtFullName.Text = txtEmail.Text = txtDept.Text = "";
            txtUsername.ReadOnly = false;
        }

        // ===== User Grid Commands =====
        protected void gvUsers_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditUser")
            {
                int uid = Convert.ToInt32(e.CommandArgument);
                DataRow row = Db.QueryRow(
                    "SELECT u.*, r.RoleName FROM dbo.AppUsers u INNER JOIN dbo.UserRoles r ON r.RoleID=u.RoleID WHERE u.UserID=@id",
                    Db.P("@id", uid));
                if (row == null) return;
                hfEditUserId.Value = uid.ToString();
                txtUsername.Text   = row["Username"].ToString();
                txtUsername.ReadOnly = true;
                txtFullName.Text   = row["FullName"].ToString();
                txtEmail.Text      = row["Email"].ToString();
                txtDept.Text       = row["Department"].ToString();
                int roleId = Convert.ToInt32(row["RoleID"]);
                if (ddlRole.Items.FindByValue(roleId.ToString()) != null)
                    ddlRole.SelectedValue = roleId.ToString();
            }
            else if (e.CommandName == "ToggleEnable")
            {
                UserDAL.ToggleEnabled(Convert.ToInt32(e.CommandArgument));
                BindAll();
            }
            else if (e.CommandName == "DeleteUser")
            {
                UserDAL.DeleteUser(Convert.ToInt32(e.CommandArgument));
                ShowMsg("User removed.");
                BindAll();
            }
            else if (e.CommandName == "Assign")
            {
                string username = e.CommandArgument.ToString();
                hfAssignUsername.Value = username;
                litAssignUser.Text     = Server.HtmlEncode(username);
                pnlAssign.Visible      = true;

                DataTable dt = UserDAL.GetUserRoleAssignments(username);
                var roles = new System.Collections.Generic.HashSet<string>();
                foreach (DataRow r in dt.Rows) roles.Add(r["RoleType"].ToString());

                chkRequestor.Checked = roles.Contains("Requestor");
                chkReviewer.Checked  = roles.Contains("Reviewer");
                chkApprover.Checked  = roles.Contains("Approver");
                chkAdmin.Checked     = roles.Contains("Admin");
            }
        }

        // ===== Export =====
        protected void btnExportUsers_Click(object sender, EventArgs e)
        {
            App_Code.Helpers.ExcelHelper.ExportGridView(gvUsers, "Users", Response);
        }

        // ===== Save role assignments =====
        protected void btnSaveAssign_Click(object sender, EventArgs e)
        {
            string u = hfAssignUsername.Value;
            if (string.IsNullOrEmpty(u)) { ShowMsg("No user selected."); return; }

            Db.Exec("DELETE FROM dbo.UserRoleAssignments WHERE Username=@u", Db.P("@u", u));
            string by = AuthHelper.CurrentUserShort;
            if (chkRequestor.Checked) InsertRoleAssign(u, "Requestor", by);
            if (chkReviewer.Checked)  InsertRoleAssign(u, "Reviewer",  by);
            if (chkApprover.Checked)  InsertRoleAssign(u, "Approver",  by);
            if (chkAdmin.Checked)     InsertRoleAssign(u, "Admin",     by);

            // Also update the primary role if Admin was selected
            if (chkAdmin.Checked)
            {
                int adminRoleId = Convert.ToInt32(Db.Scalar(
                    "SELECT TOP 1 RoleID FROM dbo.UserRoles WHERE RoleName='Admin'"));
                if (adminRoleId > 0)
                    Db.Exec("UPDATE dbo.AppUsers SET RoleID=@r WHERE Username=@u",
                        Db.P("@r", adminRoleId), Db.P("@u", u));
            }

            ShowMsg("Role assignments saved for " + u + ".");
            pnlAssign.Visible = false;
            BindAll();
        }

        private static void InsertRoleAssign(string username, string roleType, string createdBy)
        {
            Db.Exec(
                "IF NOT EXISTS(SELECT 1 FROM dbo.UserRoleAssignments WHERE Username=@u AND RoleType=@r) " +
                "INSERT INTO dbo.UserRoleAssignments(Username,RoleType,CreatedBy) VALUES(@u,@r,@cb)",
                Db.P("@u", username), Db.P("@r", roleType), Db.P("@cb", createdBy));
        }

        protected void btnCancelAssign_Click(object sender, EventArgs e)
        {
            pnlAssign.Visible = false;
        }

        // ===== Add custom role =====
        protected void btnSaveRole_Click(object sender, EventArgs e)
        {
            string name = (txtNewRole.Text ?? "").Trim();
            if (string.IsNullOrEmpty(name)) { ShowMsg("Role name is required."); return; }
            UserDAL.CreateRole(name, txtRoleDesc.Text.Trim());
            txtNewRole.Text = txtRoleDesc.Text = "";
            ShowMsg("Role '" + name + "' created.");
            BindAll();
        }

        private void ShowMsg(string msg)
        {
            lblMsg.Text    = msg;
            lblMsg.Visible = true;
        }
    }
}
