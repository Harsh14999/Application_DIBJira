using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using DFM_BPM.App_Code.DAL;
using DFM_BPM.App_Code.Helpers;

namespace DFM_BPM.Admin
{
    public partial class EmailConfig : Page
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
                LoadGrid();
            }
        }

        private void LoadGrid()
        {
            gvConfig.DataSource = EmailDAL.GetAllConfig();
            gvConfig.DataBind();
        }

        protected void gvConfig_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditRow")
            {
                string key = e.CommandArgument.ToString();
                DataRow r = Db.QueryRow("SELECT ConfigKey, ConfigValue, IsEncrypted FROM dbo.EmailConfig WHERE ConfigKey=@k", Db.P("@k", key));
                if (r == null) return;
                pnlEdit.Visible  = true;
                txtEditKey.Text  = r["ConfigKey"].ToString();
                txtEditValue.Text = "";     // never pre-fill sensitive values
                chkEditEncrypt.Checked = r["IsEncrypted"] != DBNull.Value && Convert.ToBoolean(r["IsEncrypted"]);
            }
        }

        protected void btnSaveConfig_Click(object sender, EventArgs e)
        {
            string key   = txtEditKey.Text.Trim();
            string value = txtEditValue.Text.Trim();
            bool encrypt = chkEditEncrypt.Checked;

            if (string.IsNullOrEmpty(key)) return;

            if (encrypt && !string.IsNullOrEmpty(value))
                value = EmailHelper.Encrypt(value);

            EmailDAL.SetConfigValue(key, value, encrypt, AuthHelper.CurrentUserShort);
            pnlEdit.Visible = false;
            txtEditKey.Text = txtEditValue.Text = "";

            ShowAlert("Setting saved successfully.", "success");
            LoadGrid();
        }

        protected void btnCancelEdit_Click(object sender, EventArgs e)
        {
            pnlEdit.Visible = false;
        }

        protected void btnEncrypt_Click(object sender, EventArgs e)
        {
            string input = txtToolInput.Text;
            litToolResult.Text  = System.Web.HttpUtility.HtmlEncode(EmailHelper.Encrypt(input));
            pnlToolResult.Visible = true;
        }

        protected void btnDecrypt_Click(object sender, EventArgs e)
        {
            string input = txtToolInput.Text;
            litToolResult.Text  = System.Web.HttpUtility.HtmlEncode(EmailHelper.Decrypt(input));
            pnlToolResult.Visible = true;
        }

        protected void btnTestSmtp_Click(object sender, EventArgs e)
        {
            string to = txtTestTo.Text.Trim();
            if (string.IsNullOrEmpty(to)) return;

            string body = "<p>This is a test email from <strong>DFM BPM</strong> Email Configuration.</p><p>If you see this, SMTP is configured correctly.</p>";
            int logId = EmailHelper.SendPetEmail(
                "SMTP Test",
                0,
                to,
                "",
                "DFM BPM — SMTP Test",
                body,
                AuthHelper.CurrentUserShort);

            DataRow log = EmailDAL.GetEmailLogDetail(logId);
            bool ok = log != null && log["Status"].ToString() == "Sent";
            pnlTestResult.Visible = true;
            string css = ok ? "alert-success" : "alert-danger";
            string msg = ok ? "Test email sent successfully!" : ("Send failed: " + (log != null ? log["ErrorMessage"].ToString() : "unknown error"));
            litTestResult.Text = string.Format("<strong>{0}</strong>", System.Web.HttpUtility.HtmlEncode(msg));
            // set CSS class via code — simpler than UpdatePanel
            ScriptManager.RegisterStartupScript(this, GetType(), "testcss",
                string.Format("document.querySelector('#pnlTestResult div').className='alert {0}';", css), true);
        }

        private void ShowAlert(string msg, string type)
        {
            pnlAlert.Visible   = true;
            litAlert.Text      = System.Web.HttpUtility.HtmlEncode(msg);
            ScriptManager.RegisterStartupScript(this, GetType(), "alrtcss",
                string.Format("document.querySelector('#pnlAlert .alert').className='alert alert-{0}';", type), true);
        }
    }
}
