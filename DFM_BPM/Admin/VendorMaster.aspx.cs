using System;
using System.Data;
using System.Web.UI;
using DFM_BPM.App_Code.DAL;
using DFM_BPM.App_Code.Helpers;

namespace DFM_BPM.Admin
{
    public partial class VendorMaster : Page
    {
        protected string FormChevClass { get; private set; }
        protected string FormBodyStyle { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            FormChevClass = ""; FormBodyStyle = "display:none;";
            if (!IsPostBack) Bind(null);
        }

        private void Bind(string s)
        {
            DataTable dt = MastersDAL.GetVendorsFull(s);
            gv.DataSource = dt; gv.DataBind();
            litCount.Text = dt.Rows.Count.ToString();
        }

        protected void btnSearch_Click(object sender, EventArgs e) { Bind(txtSearch.Text.Trim()); }

        protected void gv_PageIndexChanging(object sender, System.Web.UI.WebControls.GridViewPageEventArgs e)
        { gv.PageIndex = e.NewPageIndex; Bind(txtSearch.Text.Trim()); }
        protected void btnExport_Click(object sender, EventArgs e)
        {
            ExcelHelper.ExportDataTable(MastersDAL.GetVendorsFull(txtSearch.Text.Trim()), "Vendor_Master", Response);
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            string code = (txtCode.Text ?? "").Trim();
            string name = (txtName.Text ?? "").Trim();
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            { ShowMsg("Vendor Code and Name are required."); return; }
            try
            {
                MastersDAL.SaveVendor(code, name, txtEmail.Text.Trim(), txtPhone.Text.Trim(),
                    ddlActive.SelectedValue == "Yes", AuthHelper.CurrentUserShort);
                ShowMsg("Vendor saved."); ClearForm(); Bind(null);
            }
            catch (Exception ex) { ShowMsg("Error: " + ex.Message); }
        }

        protected void btnReset_Click(object sender, EventArgs e) { ClearForm(); Bind(null); }

        private void ClearForm()
        {
            hfEditId.Value = ""; txtCode.Text = txtName.Text = txtEmail.Text = txtPhone.Text = "";
            ddlActive.SelectedValue = "Yes"; txtCode.ReadOnly = false;
            FormBodyStyle = "display:none;"; FormChevClass = "";
        }

        protected void gv_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            string code = e.CommandArgument.ToString();
            if (e.CommandName == "EditRow")
            {
                DataRow r = Db.QueryRow("SELECT * FROM dbo.VendorMaster WHERE VendorCode=@c", Db.P("@c", code));
                if (r == null) return;
                hfEditId.Value = code; txtCode.Text = code; txtCode.ReadOnly = true;
                txtName.Text  = r["VendorName"] == DBNull.Value ? "" : r["VendorName"].ToString();
                txtEmail.Text = r.Table.Columns.Contains("ContactEmail") && r["ContactEmail"] != DBNull.Value ? r["ContactEmail"].ToString() : "";
                txtPhone.Text = r.Table.Columns.Contains("ContactPhone") && r["ContactPhone"] != DBNull.Value ? r["ContactPhone"].ToString() : "";
                bool active = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]);
                ddlActive.SelectedValue = active ? "Yes" : "No";
                FormBodyStyle = "display:block;"; FormChevClass = "open";
            }
            else if (e.CommandName == "DeleteRow")
            {
                MastersDAL.DeleteVendor(code, AuthHelper.CurrentUserShort);
                ShowMsg("Deleted."); Bind(null);
            }
            else if (e.CommandName == "ViewHist")
            {
                pnlHistory.Visible = true;
                litHistId.Text = System.Web.HttpUtility.HtmlEncode(code);
                gvHistory.DataSource = MastersDAL.GetVendorHistory(code);
                gvHistory.DataBind();
            }
        }

        protected void btnCloseHist_Click(object sender, EventArgs e) { pnlHistory.Visible = false; }
        private void ShowMsg(string msg) { lblMsg.Text = msg; lblMsg.Visible = true; }
    }
}
