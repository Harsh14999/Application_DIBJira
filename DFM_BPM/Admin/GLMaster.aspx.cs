using System;
using System.Data;
using System.Web.UI;
using DFM_BPM.App_Code.DAL;
using DFM_BPM.App_Code.Helpers;

namespace DFM_BPM.Admin
{
    public partial class GLMaster : Page
    {
        protected string FormChevClass { get; private set; }  // kept for legacy, unused
        protected string FormBodyStyle  { get; private set; }  // kept for legacy, unused
        protected string EditModalTitle { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            FormChevClass = ""; FormBodyStyle = "display:none;";
            EditModalTitle = "New GL Entry";
            if (!IsPostBack) Bind(null);
        }

        private void Bind(string s)
        {
            DataTable dt = MastersDAL.GetGLFull(s);
            gv.DataSource = dt; gv.DataBind();
            litCount.Text = dt.Rows.Count.ToString();
        }

        protected void btnSearch_Click(object sender, EventArgs e) { Bind(txtSearch.Text.Trim()); }

        protected void gv_PageIndexChanging(object sender, System.Web.UI.WebControls.GridViewPageEventArgs e)
        { gv.PageIndex = e.NewPageIndex; Bind(txtSearch.Text.Trim()); }
        protected void btnExport_Click(object sender, EventArgs e)
        {
            ExcelHelper.ExportDataTable(MastersDAL.GetGLFull(txtSearch.Text.Trim()), "GL_Master", Response);
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            string num = (txtGLNum.Text ?? "").Trim();
            if (string.IsNullOrEmpty(num)) { ShowMsg("GL Number is required."); return; }
            DateTime? opened = null;
            DateTime dt;
            if (DateTime.TryParse(txtOpenDate.Text, out dt)) opened = dt;
            try
            {
                MastersDAL.SaveGL(num, txtDesc.Text.Trim(), opened,
                    Dec(txtBudget.Text), Dec(txtBpmLocked.Text), Dec(txtAmsLocked.Text),
                    Dec(txtUtil.Text), Dec(txtBalance.Text), Dec(txtCapital.Text),
                    Dec(txtInvoice.Text), ddlActive.SelectedValue == "Yes", AuthHelper.CurrentUserShort);
                ShowMsg("GL entry saved."); ClearForm(); Bind(null);
            }
            catch (Exception ex)
            {
                ShowMsg("Error: " + ex.Message);
                ScriptManager.RegisterStartupScript(this, GetType(), "showEdit",
                    "$(function(){ $('#editModal').modal('show'); });", true);
            }
        }

        protected void btnReset_Click(object sender, EventArgs e) { ClearForm(); Bind(null); }

        protected void btnNewEntry_Click(object sender, EventArgs e)
        {
            ClearForm(); Bind(null);
            ScriptManager.RegisterStartupScript(this, GetType(), "showEdit",
                "$(function(){ $('#editModal').modal('show'); });", true);
        }

        private void ClearForm()
        {
            hfEditId.Value = ""; txtGLNum.Text = txtDesc.Text = txtOpenDate.Text = "";
            txtBudget.Text = txtBpmLocked.Text = txtAmsLocked.Text =
            txtUtil.Text = txtBalance.Text = txtCapital.Text = txtInvoice.Text = "0";
            ddlActive.SelectedValue = "Yes"; txtGLNum.ReadOnly = false;
            EditModalTitle = "New GL Entry";
        }

        protected void gv_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            string num = e.CommandArgument.ToString();
            if (e.CommandName == "EditRow")
            {
                DataRow r = Db.QueryRow("SELECT * FROM dbo.GLMaster WHERE GLNumber=@n", Db.P("@n", num));
                if (r == null) return;
                hfEditId.Value = num; txtGLNum.Text = num; txtGLNum.ReadOnly = true;
                txtDesc.Text      = r["GLDescription"] == DBNull.Value ? "" : r["GLDescription"].ToString();
                txtOpenDate.Text  = r["GLOpenedDate"] == DBNull.Value ? "" : Convert.ToDateTime(r["GLOpenedDate"]).ToString("yyyy-MM-dd");
                txtBudget.Text    = DecStr(r, "BudgetedAmount");
                txtBpmLocked.Text = DecStr(r, "BPMLockedAmount");
                txtAmsLocked.Text = DecStr(r, "AMSLockedAmount");
                txtUtil.Text      = DecStr(r, "UtilizedAmount");
                txtBalance.Text   = DecStr(r, "BalanceAmount");
                txtCapital.Text   = DecStr(r, "CapitalizedAmount");
                txtInvoice.Text   = DecStr(r, "InvoiceProcessedAmt");
                bool active = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]);
                ddlActive.SelectedValue = active ? "Yes" : "No";
                EditModalTitle = "Edit GL: " + num;
                ScriptManager.RegisterStartupScript(this, GetType(), "showEdit",
                    "$(function(){ $('#editModal').modal('show'); });", true);
            }
            else if (e.CommandName == "DeleteRow")
            {
                MastersDAL.DeleteGL(num, AuthHelper.CurrentUserShort);
                ShowMsg("GL entry deleted."); Bind(null);
            }
            else if (e.CommandName == "ViewHist")
            {
                litHistId.Text = System.Web.HttpUtility.HtmlEncode(num);
                gvHistory.DataSource = MastersDAL.GetGLHistory(num);
                gvHistory.DataBind();
                ScriptManager.RegisterStartupScript(this, GetType(), "showHist",
                    "$(function(){ $('#histModal').modal('show'); });", true);
            }
        }

        private static decimal Dec(string s) { decimal v; return decimal.TryParse((s??"").Replace(",",""), out v) ? v : 0m; }
        private static string DecStr(DataRow r, string col) { return r[col] == DBNull.Value ? "0" : Convert.ToDecimal(r[col]).ToString("N2"); }
        private void ShowMsg(string msg) { lblMsg.Text = msg; lblMsg.Visible = true; }
    }
}
