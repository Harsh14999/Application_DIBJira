using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using DFM_BPM.App_Code.DAL;
using DFM_BPM.App_Code.Helpers;

namespace DFM_BPM.Admin
{
    public partial class EmailLog : Page
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
            int topN = 100;
            int.TryParse(ddlTopN.SelectedValue, out topN);
            int? petId = null;
            int tmp;
            if (!string.IsNullOrEmpty(txtFilterPetId.Text.Trim()) && int.TryParse(txtFilterPetId.Text.Trim(), out tmp))
                petId = tmp;
            gvLog.DataSource = EmailDAL.GetEmailLog(petId, topN);
            gvLog.DataBind();
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            LoadGrid();
        }

        protected void gvLog_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ViewDetail")
            {
                int logId;
                if (!int.TryParse(e.CommandArgument.ToString(), out logId)) return;
                DataRow r = EmailDAL.GetEmailLogDetail(logId);
                if (r == null) return;

                litDLogID.Text   = logId.ToString();
                litDDate.Text    = System.Web.HttpUtility.HtmlEncode(r["SentDate"] == DBNull.Value ? "" : Convert.ToDateTime(r["SentDate"]).ToString("dd-MMM-yyyy HH:mm:ss"));
                litDEvent.Text   = System.Web.HttpUtility.HtmlEncode(r["TriggerEvent"].ToString());
                litDPetID.Text   = r["PetFormID"] == DBNull.Value ? "" : r["PetFormID"].ToString();
                litDTo.Text      = System.Web.HttpUtility.HtmlEncode(r["ToAddress"].ToString());
                litDCc.Text      = System.Web.HttpUtility.HtmlEncode(r["CcAddress"].ToString());
                litDSubject.Text = System.Web.HttpUtility.HtmlEncode(r["Subject"].ToString());
                litDStatus.Text  = System.Web.HttpUtility.HtmlEncode(r["Status"].ToString());
                litDError.Text   = System.Web.HttpUtility.HtmlEncode(r["ErrorMessage"].ToString());
                litDSentBy.Text  = System.Web.HttpUtility.HtmlEncode(r["SentBy"].ToString());
                // Render body as raw HTML (it's already HTML we generated)
                litDBody.Text    = r["Body"].ToString();

                ScriptManager.RegisterStartupScript(this, GetType(), "showlog",
                    "jQuery('#logDetailModal').modal('show');", true);
            }
        }
    }
}
