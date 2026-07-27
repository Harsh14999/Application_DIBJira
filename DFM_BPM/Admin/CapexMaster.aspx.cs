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
    public partial class CapexMaster : Page
    {
        protected string FormChevClass { get; private set; }  // kept for legacy, unused
        protected string FormBodyStyle  { get; private set; }  // kept for legacy, unused
        protected string EditModalTitle { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            FormChevClass = ""; FormBodyStyle = "display:none;";
            EditModalTitle = "New CAPEX Entry";
            if (!IsPostBack) Bind(null);
        }

        private void Bind(string search)
        {
            DataTable dt = MastersDAL.GetCapexFull(search);
            gv.DataSource = dt;
            gv.DataBind();
            litCount.Text = dt.Rows.Count.ToString();
        }

        protected void btnSearch_Click(object sender, EventArgs e) { Bind(txtSearch.Text.Trim()); }

        protected void gv_PageIndexChanging(object sender, System.Web.UI.WebControls.GridViewPageEventArgs e)
        { gv.PageIndex = e.NewPageIndex; Bind(txtSearch.Text.Trim()); }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            ExcelHelper.ExportDataTable(MastersDAL.GetCapexFull(txtSearch.Text.Trim()), "CAPEX_Master", Response);
        }

        // â”€â”€ Add / Edit â”€â”€
        protected void btnSave_Click(object sender, EventArgs e)
        {
            string id = (txtId.Text ?? "").Trim();
            if (string.IsNullOrEmpty(id)) { ShowMsg("CAPEX ID is required."); return; }
            try
            {
                MastersDAL.SaveCapex(id, txtDesc.Text.Trim(),
                    ParseDec(txtBudget.Text), ParseDec(txtUtil.Text), ParseDec(txtAvail.Text),
                    ParseDec(txtLocked.Text), ParseDec(txtAfterLock.Text),
                    ParseDec(txtClaim.Text), ParseDec(txtNet.Text),
                    ddlActive.SelectedValue == "Yes", AuthHelper.CurrentUserShort);
                ShowMsg("CAPEX entry saved.");
                ClearForm(); Bind(null);
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
            EditModalTitle = "New CAPEX Entry";
        }

        // â”€â”€ Grid commands â”€â”€
        protected void gv_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            string id = e.CommandArgument.ToString();
            if (e.CommandName == "EditRow")
            {
                DataRow r = Db.QueryRow("SELECT * FROM dbo.CapexMaster WHERE CapexID=@id", Db.P("@id", id));
                if (r == null) return;
                hfEditId.Value = id;
                txtId.Text     = id; txtId.ReadOnly = true;
                txtDesc.Text   = r["Description"] == DBNull.Value ? "" : r["Description"].ToString();
                txtBudget.Text    = DecStr(r, "BudgetedAmount");
                txtUtil.Text      = DecStr(r, "UtilizedAmount");
                txtAvail.Text     = DecStr(r, "AvailableAmount");
                txtLocked.Text    = DecStr(r, "LockedAmount");
                txtAfterLock.Text = DecStr(r, "BudgetAfterLockedAmount");
                txtClaim.Text     = DecStr(r, "ClaimAmount");
                txtNet.Text       = DecStr(r, "NetBalance");
                bool active = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]);
                ddlActive.SelectedValue = active ? "Yes" : "No";
                EditModalTitle = "Edit CAPEX: " + id;
                ScriptManager.RegisterStartupScript(this, GetType(), "showEdit",
                    "$(function(){ $('#editModal').modal('show'); });", true);
            }
            else if (e.CommandName == "DeleteRow")
            {
                MastersDAL.DeleteCapex(id, AuthHelper.CurrentUserShort);
                ShowMsg("Deleted."); Bind(null);
            }
            else if (e.CommandName == "ViewHist")
            {
                litHistId.Text = System.Web.HttpUtility.HtmlEncode(id);
                gvHistory.DataSource = MastersDAL.GetCapexHistory(id);
                gvHistory.DataBind();
                ScriptManager.RegisterStartupScript(this, GetType(), "showHist",
                    "$(function(){ $('#histModal').modal('show'); });", true);
            }
        }

        // â”€â”€ CSV Import â”€â”€
        protected void btnImport_Click(object sender, EventArgs e)
        {
            if (!fuCapex.HasFile) { ShowMsg("Please select a CSV file."); return; }
            int inserted = 0, updated = 0, skipped = 0;
            string user = AuthHelper.CurrentUserShort;
            try
            {
                using (var sr = new StreamReader(fuCapex.FileContent, Encoding.UTF8))
                {
                    string header = sr.ReadLine(); // skip header
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string[] cols = SplitCsvLine(line);
                        if (cols.Length < 10) { skipped++; continue; }
                        string type  = cols[0].Trim().TrimStart('"').TrimEnd('"');
                        string capexId = cols[1].Trim().TrimStart('"').TrimEnd('"');
                        if (string.IsNullOrEmpty(capexId)) { skipped++; continue; }
                        if (!type.Equals("Capex", StringComparison.OrdinalIgnoreCase) &&
                            !type.Equals("CAPEX", StringComparison.OrdinalIgnoreCase))
                        { skipped++; continue; }
                        string desc        = cols[2].Trim().TrimStart('"').TrimEnd('"');
                        decimal budget     = ParseDecCsv(cols[3]);
                        decimal util       = ParseDecCsv(cols[4]);
                        decimal avail      = ParseDecCsv(cols[5]);
                        decimal locked     = ParseDecCsv(cols[6]);
                        decimal afterLock  = ParseDecCsv(cols[7]);
                        decimal claim      = ParseDecCsv(cols[8]);
                        decimal net        = ParseDecCsv(cols[9]);

                        bool isNew = Convert.ToInt32(Db.Scalar(
                            "SELECT COUNT(*) FROM dbo.CapexMaster WHERE CapexID=@id",
                            Db.P("@id", capexId))) == 0;

                        MastersDAL.SaveCapex(capexId, desc, budget, util, avail,
                            locked, afterLock, claim, net, true, user);
                        if (isNew) inserted++; else updated++;
                    }
                }
                lblImportResult.Text = string.Format(
                    "Import complete: {0} inserted, {1} updated, {2} skipped.", inserted, updated, skipped);
                lblImportResult.Visible = true;
                Bind(null);
            }
            catch (Exception ex)
            {
                lblImportResult.Text = "Import failed: " + ex.Message;
                lblImportResult.Visible = true;
            }
        }

        // â”€â”€ Helpers â”€â”€
        private static decimal ParseDec(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0m;
            decimal v; return decimal.TryParse(s.Replace(",", ""), out v) ? v : 0m;
        }

        private static decimal ParseDecCsv(string s)
        {
            s = (s ?? "").Trim().TrimStart('"').TrimEnd('"').Replace(",", "");
            decimal v; return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : 0m;
        }

        private static string DecStr(DataRow r, string col)
        {
            if (r[col] == DBNull.Value) return "0";
            return Convert.ToDecimal(r[col]).ToString("N2");
        }

        private static string[] SplitCsvLine(string line)
        {
            // Handles quoted fields with commas
            var result = new System.Collections.Generic.List<string>();
            bool inQuote = false;
            var cur = new StringBuilder();
            foreach (char c in line)
            {
                if (c == '"') { inQuote = !inQuote; }
                else if (c == ',' && !inQuote) { result.Add(cur.ToString()); cur.Clear(); }
                else { cur.Append(c); }
            }
            result.Add(cur.ToString());
            return result.ToArray();
        }

        private void ShowMsg(string msg) { lblMsg.Text = msg; lblMsg.Visible = true; }
    }
}

