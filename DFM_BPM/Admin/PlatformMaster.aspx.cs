using System;
using System.Data;
using System.Web.UI;
using DFM_BPM.App_Code.DAL;
using DFM_BPM.App_Code.Helpers;

namespace DFM_BPM.Admin
{
    public partial class PlatformMaster : Page
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
            DataTable dt = MastersDAL.GetPlatforms(s);
            gv.DataSource = dt; gv.DataBind();
            litCount.Text = dt.Rows.Count.ToString();
        }

        protected void btnSearch_Click(object sender, EventArgs e) { Bind(txtSearch.Text.Trim()); }

        protected void gv_PageIndexChanging(object sender, System.Web.UI.WebControls.GridViewPageEventArgs e)
        { gv.PageIndex = e.NewPageIndex; Bind(txtSearch.Text.Trim()); }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            string name = (txtName.Text ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            { ShowMsg("Platform Name is required."); return; }
            int editId;
            int.TryParse(hfEditId.Value, out editId);
            try
            {
                MastersDAL.SavePlatform(editId, name, ddlActive.SelectedValue == "Yes", AuthHelper.CurrentUserShort);
                ShowMsg("Platform saved."); ClearForm(); Bind(null);
            }
            catch (Exception ex) { ShowMsg("Error: " + ex.Message); }
        }

        protected void btnReset_Click(object sender, EventArgs e) { ClearForm(); Bind(null); }

        private void ClearForm()
        {
            hfEditId.Value = "0"; txtName.Text = "";
            ddlActive.SelectedValue = "Yes";
            FormBodyStyle = "display:none;"; FormChevClass = "";
        }

        protected void gv_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            int id = Convert.ToInt32(e.CommandArgument);
            if (e.CommandName == "EditRow")
            {
                DataRow r = MastersDAL.GetPlatformById(id);
                if (r == null) return;
                hfEditId.Value = id.ToString();
                txtName.Text  = r["PlatformName"] == DBNull.Value ? "" : r["PlatformName"].ToString();
                bool active = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]);
                ddlActive.SelectedValue = active ? "Yes" : "No";
                FormBodyStyle = "display:block;"; FormChevClass = "open";
            }
            else if (e.CommandName == "DeleteRow")
            {
                MastersDAL.DeletePlatform(id);
                ShowMsg("Deleted."); Bind(null);
            }
        }

        private void ShowMsg(string msg) { lblMsg.Text = msg; lblMsg.Visible = true; }
    }
}
