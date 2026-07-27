using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using DFM_BPM.App_Code.DAL;
using DFM_BPM.App_Code.Helpers;

namespace DFM_BPM.Admin
{
    public partial class EngineerMaster : Page
    {
        protected string FormChevClass { get; private set; }
        protected string FormBodyStyle { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            FormChevClass = ""; FormBodyStyle = "display:none;";
            if (!IsPostBack) { BindParentDropdown(); Bind(null); }
        }

        private void Bind(string search)
        {
            DataTable dt = Db.Query(@"SELECT r.ResourceID, r.ResourceName, r.IsActive, r.ModifiedBy, r.ModifiedDate,
                                            pr.ResourceName AS ParentName
                                     FROM dbo.PortfolioResource r
                                     LEFT JOIN dbo.PortfolioResource pr ON pr.ResourceID = r.ParentResourceID
                                     WHERE r.Title = 'Engineer'" +
                (string.IsNullOrWhiteSpace(search) ? "" : " AND r.ResourceName LIKE @s") +
                " ORDER BY r.ResourceName",
                string.IsNullOrWhiteSpace(search) ? new System.Data.SqlClient.SqlParameter[0] : new[] { Db.P("@s", "%" + search.Trim() + "%") });
            gv.DataSource = dt; gv.DataBind();
            litCount.Text = dt.Rows.Count.ToString();
        }

        private void BindParentDropdown()
        {
            DataTable dt = PortfolioDAL.GetResourceDropdown();
            ddlParent.DataSource     = dt;
            ddlParent.DataTextField  = "DisplayName";
            ddlParent.DataValueField = "ResourceID";
            ddlParent.DataBind();
            ddlParent.Items.Insert(0, new ListItem("-- Select Reporting Manager --", ""));
        }

        protected void btnSearch_Click(object sender, EventArgs e) { Bind(txtSearch.Text.Trim()); }

        protected void gv_PageIndexChanging(object sender, GridViewPageEventArgs e)
        { gv.PageIndex = e.NewPageIndex; Bind(txtSearch.Text.Trim()); }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            string name = (txtName.Text ?? "").Trim();
            if (string.IsNullOrEmpty(name)) { ShowMsg("Engineer Name is required."); return; }

            int editId;
            int.TryParse(hfEditId.Value, out editId);

            int? parentId = null;
            int pid;
            if (int.TryParse(ddlParent.SelectedValue, out pid) && pid > 0) parentId = pid;

            try
            {
                PortfolioDAL.SaveResource(editId, name, "Engineer", parentId,
                    ddlActive.SelectedValue == "Yes", AuthHelper.CurrentUserShort);
                ShowMsg("Engineer saved."); ClearForm(); Bind(null);
            }
            catch (Exception ex) { ShowMsg("Error: " + ex.Message); }
        }

        protected void btnReset_Click(object sender, EventArgs e) { ClearForm(); Bind(null); }

        private void ClearForm()
        {
            hfEditId.Value = "0"; txtName.Text = "";
            ddlActive.SelectedValue = "Yes";
            if (ddlParent.Items.Count > 0) ddlParent.SelectedIndex = 0;
            FormBodyStyle = "display:none;"; FormChevClass = "";
        }

        protected void gv_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id = Convert.ToInt32(e.CommandArgument);
            if (e.CommandName == "EditRow")
            {
                DataRow r = PortfolioDAL.GetResourceById(id);
                if (r == null) return;
                hfEditId.Value = id.ToString();
                txtName.Text = r["ResourceName"] == DBNull.Value ? "" : r["ResourceName"].ToString();
                ddlActive.SelectedValue = (r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"])) ? "Yes" : "No";
                if (r["ParentResourceID"] != DBNull.Value)
                {
                    string pid2 = r["ParentResourceID"].ToString();
                    if (ddlParent.Items.FindByValue(pid2) != null) ddlParent.SelectedValue = pid2;
                }
                FormBodyStyle = "display:block;"; FormChevClass = "open";
            }
            else if (e.CommandName == "DeleteRow")
            {
                if (PortfolioDAL.HasChildrenOrProjects(id))
                    ShowMsg("Cannot delete: engineer has assigned projects. Reassign them first.");
                else
                {
                    PortfolioDAL.DeleteResource(id);
                    ShowMsg("Deleted."); Bind(null);
                }
            }
        }

        private void ShowMsg(string msg) { lblMsg.Text = msg; lblMsg.Visible = true; }
    }
}
