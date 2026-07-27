using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.UI;
using DFM_BPM.App_Code.DAL;
using DFM_BPM.App_Code.Helpers;

namespace DFM_BPM.Admin
{
    public partial class OpexMaster : Page
    {
        protected string FormChevClass { get; private set; }  // kept for legacy, unused
        protected string FormBodyStyle  { get; private set; }  // kept for legacy, unused
        protected string EditModalTitle { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            FormChevClass = ""; FormBodyStyle = "display:none;";
            EditModalTitle = "New OPEX Entry";
            if (!IsPostBack) Bind(null);
        }

        private void Bind(string search)
        {
            DataTable dt = MastersDAL.GetOpexFull(search);
            gv.DataSource = dt; gv.DataBind();
            litCount.Text = dt.Rows.Count.ToString();
        }

        protected void btnSearch_Click(object sender, EventArgs e) { Bind(txtSearch.Text.Trim()); }

        protected void gv_PageIndexChanging(object sender, System.Web.UI.WebControls.GridViewPageEventArgs e)
        { gv.PageIndex = e.NewPageIndex; Bind(txtSearch.Text.Trim()); }
        protected void btnExport_Click(object sender, EventArgs e)
        {
            ExcelHelper.ExportDataTable(MastersDAL.GetOpexFull(txtSearch.Text.Trim()), "OPEX_Master", Response);
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            string id = (txtId.Text ?? "").Trim();
            if (string.IsNullOrEmpty(id)) { ShowMsg("OPEX ID is required."); return; }
            try
            {
                MastersDAL.SaveOpex(id, txtDesc.Text.Trim(),
                    ParseDec(txtBudget.Text), ParseDec(txtUtil.Text), ParseDec(txtAvail.Text),
                    ParseDec(txtLocked.Text), ParseDec(txtAfterLock.Text),
                    ParseDec(txtClaim.Text), ParseDec(txtNet.Text),
                    ddlActive.SelectedValue == "Yes", AuthHelper.CurrentUserShort);
                ShowMsg("OPEX entry saved."); ClearForm(); Bind(null);
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
            hfEditId.Value = ""; txtId.Text = txtDesc.Text = "";
            txtBudget.Text = txtUtil.Text = txtAvail.Text =
            txtLocked.Text = txtAfterLock.Text = txtClaim.Text = txtNet.Text = "0";
            ddlActive.SelectedValue = "Yes"; txtId.ReadOnly = false;
            EditModalTitle = "New OPEX Entry";
        }

        protected void gv_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            string id = e.CommandArgument.ToString();
            if (e.CommandName == "EditRow")
            {
                DataRow r = Db.QueryRow("SELECT * FROM dbo.OpexMaster WHERE OpexID=@id", Db.P("@id", id));
                if (r == null) return;
                hfEditId.Value = id; txtId.Text = id; txtId.ReadOnly = true;
                txtDesc.Text      = r["Description"] == DBNull.Value ? "" : r["Description"].ToString();
                txtBudget.Text    = DecStr(r, "BudgetedAmount");
                txtUtil.Text      = DecStr(r, "UtilizedAmount");
                txtAvail.Text     = DecStr(r, "AvailableAmount");
                txtLocked.Text    = DecStr(r, "LockedAmount");
                txtAfterLock.Text = DecStr(r, "BudgetAfterLockedAmount");
                txtClaim.Text     = DecStr(r, "ClaimAmount");
                txtNet.Text       = DecStr(r, "NetBalance");
                bool active = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]);
                ddlActive.SelectedValue = active ? "Yes" : "No";
                EditModalTitle = "Edit OPEX: " + id;
                ScriptManager.RegisterStartupScript(this, GetType(), "showEdit",
                    "$(function(){ $('#editModal').modal('show'); });", true);
            }
            else if (e.CommandName == "DeleteRow")
            {
                MastersDAL.DeleteOpex(id, AuthHelper.CurrentUserShort);
                ShowMsg("Deleted."); Bind(null);
            }
            else if (e.CommandName == "ViewHist")
            {
                litHistId.Text = System.Web.HttpUtility.HtmlEncode(id);
                gvHistory.DataSource = MastersDAL.GetOpexHistory(id);
                gvHistory.DataBind();
                ScriptManager.RegisterStartupScript(this, GetType(), "showHist",
                    "$(function(){ $('#histModal').modal('show'); });", true);
            }
        }

        protected void btnImport_Click(object sender, EventArgs e)
        {
            if (!fuOpex.HasFile) { ShowMsg("Please select a CSV file."); return; }
            int inserted = 0, updated = 0, skipped = 0;
            string user = AuthHelper.CurrentUserShort;
            try
            {
                using (var sr = new StreamReader(fuOpex.FileContent, Encoding.UTF8))
                {
                    string header = sr.ReadLine();
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string[] cols = SplitCsvLine(line);
                        if (cols.Length < 10) { skipped++; continue; }
                        string type  = cols[0].Trim().Trim('"');
                        string opexId = cols[1].Trim().Trim('"');
                        if (string.IsNullOrEmpty(opexId)) { skipped++; continue; }
                        if (!type.Equals("Opex", StringComparison.OrdinalIgnoreCase) &&
                            !type.Equals("OPEX", StringComparison.OrdinalIgnoreCase))
                        { skipped++; continue; }
                        string desc = cols[2].Trim().Trim('"');
                        bool isNew = Convert.ToInt32(Db.Scalar(
                            "SELECT COUNT(*) FROM dbo.OpexMaster WHERE OpexID=@id",
                            Db.P("@id", opexId))) == 0;
                        MastersDAL.SaveOpex(opexId, desc,
                            ParseDecCsv(cols[3]), ParseDecCsv(cols[4]), ParseDecCsv(cols[5]),
                            ParseDecCsv(cols[6]), ParseDecCsv(cols[7]), ParseDecCsv(cols[8]),
                            ParseDecCsv(cols[9]), true, user);
                        if (isNew) inserted++; else updated++;
                    }
                }
                lblImportResult.Text = string.Format("Import complete: {0} inserted, {1} updated, {2} skipped.", inserted, updated, skipped);
                lblImportResult.Visible = true; Bind(null);
            }
            catch (Exception ex) { lblImportResult.Text = "Import failed: " + ex.Message; lblImportResult.Visible = true; }
        }

        private static decimal ParseDec(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0m;
            decimal v; return decimal.TryParse(s.Replace(",", ""), out v) ? v : 0m;
        }
        private static decimal ParseDecCsv(string s)
        {
            s = (s ?? "").Trim().Trim('"').Replace(",", "");
            decimal v; return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : 0m;
        }
        private static string DecStr(DataRow r, string col)
        {
            if (r[col] == DBNull.Value) return "0";
            return Convert.ToDecimal(r[col]).ToString("N2");
        }
        private static string[] SplitCsvLine(string line)
        {
            var result = new System.Collections.Generic.List<string>();
            bool inQ = false; var cur = new StringBuilder();
            foreach (char c in line)
            {
                if (c == '"') inQ = !inQ;
                else if (c == ',' && !inQ) { result.Add(cur.ToString()); cur.Clear(); }
                else cur.Append(c);
            }
            result.Add(cur.ToString()); return result.ToArray();
        }
        private void ShowMsg(string msg) { lblMsg.Text = msg; lblMsg.Visible = true; }
    }
}
