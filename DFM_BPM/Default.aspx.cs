using System;
using System.Data;
using System.Text;
using System.Web.UI;
using DFM_BPM.App_Code.DAL;
using DFM_BPM.App_Code.Helpers;

namespace DFM_BPM
{
    public partial class DefaultPage : Page
    {
        protected bool IsAdmin { get { return AuthHelper.IsAdmin; } }

        private const int PageSize = 15;
        private int CurrentPage
        {
            get { int v; return int.TryParse(ViewState["pendingPage"] as string, out v) ? v : 1; }
            set { ViewState["pendingPage"] = value.ToString(); }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadJiraProjects();
                LoadPortfolioFilter();

                // Support redirect from Portfolio Hierarchy: ?resource=<id> pre-selects that Portfolio filter
                string qsResource = Request.QueryString["resource"];
                int qsRid;
                if (!string.IsNullOrEmpty(qsResource) && int.TryParse(qsResource, out qsRid) && qsRid > 0)
                {
                    if (ddlPortfolioFilter.Items.FindByValue(qsRid.ToString()) != null)
                        ddlPortfolioFilter.SelectedValue = qsRid.ToString();
                }

                LoadAll();
            }
        }

        private void LoadPortfolioFilter()
        {
            try
            {
                DataTable dt = PortfolioDAL.GetResourceDropdown();
                ddlPortfolioFilter.Items.Clear();
                ddlPortfolioFilter.Items.Add(new System.Web.UI.WebControls.ListItem("All Portfolios", "ALL"));
                foreach (DataRow r in dt.Rows)
                    ddlPortfolioFilter.Items.Add(new System.Web.UI.WebControls.ListItem(
                        r["DisplayName"].ToString(), r["ResourceID"].ToString()));
            }
            catch { /* no Portfolio data yet */ }
        }

        private void LoadJiraProjects()
        {
            try
            {
                DataTable dt = MastersDAL.GetJiraDropdown();
                ddlProject.Items.Clear();
                ddlProject.Items.Add(new System.Web.UI.WebControls.ListItem("All Projects", "ALL"));
                foreach (DataRow r in dt.Rows)
                    ddlProject.Items.Add(new System.Web.UI.WebControls.ListItem(
                        r["DisplayName"].ToString(), r["JiraID"].ToString()));
            }
            catch { /* no JIRA data yet */ }
        }

        private void LoadAll()
        {
            LoadKPIs();
            LoadRegisteredProjects();
            LoadPendingTree();
            LoadMyPet();
            LoadMyBudgetLines();
            LoadLastSync();
        }

        /// <summary>Simple "one row per registered Project" grid shown before Pending Approvals & Requests —
        /// shows every JIRA or Non-JIRA project registered via the Project Registration module, regardless of
        /// whether a Spend Request has ever been submitted/approved for it. Respects the Portfolio filter so
        /// dashboard/reporting can slice by Portfolio Hierarchy ownership.</summary>
        private void LoadRegisteredProjects()
        {
            try
            {
                int? resourceId = null;
                int rid;
                if (int.TryParse(ddlPortfolioFilter.SelectedValue, out rid) && rid > 0) resourceId = rid;

                string search = txtProjectSearch.Text.Trim();
                DataTable dt = ProjectDAL.GetProjects(string.IsNullOrEmpty(search) ? null : search, resourceId);
                gvRegisteredProjects.DataSource = dt;
                gvRegisteredProjects.DataBind();
                litRegisteredProjectsCount.Text = dt.Rows.Count.ToString();
            }
            catch { }
        }

        protected void btnProjectSearch_Click(object sender, EventArgs e)
        {
            gvRegisteredProjects.PageIndex = 0;
            LoadRegisteredProjects();
        }

        protected void btnProjectSearchReset_Click(object sender, EventArgs e)
        {
            txtProjectSearch.Text = "";
            gvRegisteredProjects.PageIndex = 0;
            LoadRegisteredProjects();
        }

        private void LoadLastSync()
        {
            try
            {
                var row = Db.QueryRow(
                    "SELECT TOP 1 CONVERT(VARCHAR,EndTime,120) AS T FROM dbo.SyncLog WHERE Status='Success' ORDER BY SyncID DESC");
                litLastSync.Text = row != null ? row["T"].ToString() : "Never";
            }
            catch { litLastSync.Text = "N/A"; }
        }

        private void LoadKPIs()
        {
            try
            {
                DataRow kpi = DashboardDAL.GetDashboardKPIs();
                if (kpi != null)
                {
                    litProjects.Text    = Fmt(kpi, "TotalProjects");
                    litPET.Text         = Fmt(kpi, "TotalPET");
                    litPending.Text     = Fmt(kpi, "TotalPending");
                    litApproved.Text    = Fmt(kpi, "TotalApproved");
                    litRejected.Text    = Fmt(kpi, "TotalRejected");
                    litCapexBudget.Text = FmtAmt(kpi, "TotalCapexBudget");
                    litOpexBudget.Text  = FmtAmt(kpi, "TotalOpexBudget");
                }
            }
            catch { /* data not yet available */ }
        }

        private void LoadPendingTree()
        {
            try
            {
                string jiraFilter = ddlProject.SelectedValue == "ALL" ? null : ddlProject.SelectedValue;
                string typeFilter = ddlType.SelectedValue == "ALL" ? null : ddlType.SelectedValue;
                string statFilter = ddlStatus.SelectedValue == "ALL" ? null : ddlStatus.SelectedValue;
                string viewFilter = ddlView.SelectedValue;
                string viewUser = AuthHelper.CurrentUserShort;
                DateTime? fromDate = null, toDate = null;
                DateTime dt;
                if (DateTime.TryParse(txtFromDate.Text, out dt)) fromDate = dt;
                if (DateTime.TryParse(txtToDate.Text, out dt)) toDate = dt;

                DataTable allForms = WorkflowDAL.GetPetFormsDashboard(
                    jiraFilter, typeFilter, statFilter, fromDate, toDate, viewFilter, viewUser);

                // ── Group by ProjectID ──────────────────────────────────────────
                var groupOrder = new System.Collections.Generic.List<string>();
                var groups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<DataRow>>();
                foreach (DataRow r in allForms.Rows)
                {
                    string proj = r["ProjectID"] == DBNull.Value ? "(No Project)" : r["ProjectID"].ToString();
                    if (!groups.ContainsKey(proj)) { groups[proj] = new System.Collections.Generic.List<DataRow>(); groupOrder.Add(proj); }
                    groups[proj].Add(r);
                }

                int totalGroups = groupOrder.Count;
                int pages = Math.Max(1, (int)Math.Ceiling(totalGroups / (double)PageSize));
                if (CurrentPage > pages) CurrentPage = pages;
                litPendingCount.Text = allForms.Rows.Count.ToString();
                litPageInfo.Text = string.Format(
                    "<span style='font-size:.85em;color:#64748b;padding:0 8px;'>Page {0} of {1} ({2} project(s), {3} PET(s))</span>",
                    CurrentPage, pages, totalGroups, allForms.Rows.Count);
                btnPrevPage.Enabled = CurrentPage > 1;
                btnNextPage.Enabled = CurrentPage < pages;

                int skip = (CurrentPage - 1) * PageSize;
                var sb = new StringBuilder();

                for (int gi = skip; gi < Math.Min(skip + PageSize, groupOrder.Count); gi++)
                {
                    string proj = groupOrder[gi];
                    var petRows = groups[proj];
                    // Safe CSS class (digits only to avoid selector issues)
                    string safeId = "pg" + Math.Abs(proj.GetHashCode() & 0x7FFFFFFF).ToString();
                    // Project display name (use ProjectName from first row if available)
                    string projName = "";
                    decimal projTotal = 0m;
                    if (petRows.Count > 0 && petRows[0].Table.Columns.Contains("ProjectName"))
                        projName = petRows[0]["ProjectName"] == DBNull.Value ? "" : petRows[0]["ProjectName"].ToString();

                    foreach (DataRow pr in petRows)
                        if (pr.Table.Columns.Contains("TotalRequestedAED") && pr["TotalRequestedAED"] != DBNull.Value)
                            projTotal += Convert.ToDecimal(pr["TotalRequestedAED"]);

                    string projDisplay = System.Web.HttpUtility.HtmlEncode(proj);
                    if (!string.IsNullOrEmpty(projName)) projDisplay += " &mdash; " + System.Web.HttpUtility.HtmlEncode(projName);

                    string projEsc = System.Web.HttpUtility.JavaScriptStringEncode(proj);

                    // Parent row
                    sb.AppendFormat(
                        "<tr style='background:#e8f0fe;'>" +
                        "<td colspan='10' style='border-bottom:1px solid #c7d2fe;'>" +
                        "<span class='tree-toggle' onclick='dfmTog(\"{0}\")' data-tog='{0}' style='cursor:pointer;color:#2563eb;font-size:1.1em;margin-right:6px;'>&#9658;</span>" +
                        "<i class='bi bi-folder2-open' style='color:#2563eb;margin-right:5px;'></i>{1}" +
                        "<span style='color:#64748b;font-weight:400;font-size:.82em;margin-left:10px;'>{2} PET(s) &mdash; Total AED: <strong style=\"color:#1a3c5e;\">{3}</strong></span>" +
                        "<span style='margin-left:14px;'>" +
                        "<button type='button' class='proj-action-btn btn-sr' onclick=\"dfmShowSR('{4}');\"><i class='bi bi-file-earmark-text'></i> Spend Request</button>" +
                        "<button type='button' class='proj-action-btn btn-bgt' onclick=\"dfmShowBgt('{4}');\"><i class='bi bi-cash-coin'></i> Budget</button>" +
                        "<button type='button' class='proj-action-btn btn-inv' onclick=\"dfmShowInv('{4}');\"><i class='bi bi-receipt'></i> Invoice</button>" +
                        "</span>" +
                        "</td></tr>",
                        safeId, projDisplay, petRows.Count, projTotal.ToString("N0"), projEsc);

                    // Child rows for each PET (ordered by PetFormID ASC = V1, V2, ...)
                    petRows.Sort(delegate (DataRow a, DataRow b) {
                        int ia = Convert.ToInt32(a["PetFormID"]);
                        int ib = Convert.ToInt32(b["PetFormID"]);
                        return ia.CompareTo(ib);
                    });

                    for (int vi = 0; vi < petRows.Count; vi++)
                    {
                        DataRow r = petRows[vi];
                        string status = r["Status"].ToString();
                        string badgeCss = status == "Draft" ? "st-draft"
                                        : status == "PendingReview" ? "st-review"
                                        : status == "PendingApproval" ? "st-pending"
                                        : status == "Approved" ? "st-approved"
                                        : status == "Rejected" ? "st-rejected"
                                        : "st-sent";
                        string petId = r["PetFormID"].ToString();
                        string refNo = r["PetRefNo"] == DBNull.Value ? "#" + petId : r["PetRefNo"].ToString();
                        string type = r["CapexOpexType"] == DBNull.Value ? "" : r["CapexOpexType"].ToString();
                        string src = r["BudgetSourceID"] == DBNull.Value ? "" : r["BudgetSourceID"].ToString();
                        string approver = r["ApproverUsername"] == DBNull.Value ? "" : r["ApproverUsername"].ToString();
                        string by = r["CreatedBy"] == DBNull.Value ? "" : r["CreatedBy"].ToString();
                        decimal reqAmt = r.Table.Columns.Contains("TotalRequestedAED") && r["TotalRequestedAED"] != DBNull.Value
                                          ? Convert.ToDecimal(r["TotalRequestedAED"]) : 0m;
                        string submitted = r["SubmittedDate"] == DBNull.Value ? ""
                                         : Convert.ToDateTime(r["SubmittedDate"]).ToString("dd-MMM-yy");
                        string typeColor = type == "CAPEX" ? "#2563eb" : (type == "OPEX" ? "#059669" : "#64748b");
                        // Delete is only offered for Draft / Pending Review / Pending Approval — never once Approved (or Rejected/SentBack).
                        string delBtn = WorkflowDAL.IsPetDeletable(status)
                            ? "<button type='button' class='btn btn-xs btn-danger' onclick=\"dfmPetDel('" + petId + "','" + System.Web.HttpUtility.JavaScriptStringEncode(refNo) + "');\"><i class='bi bi-trash'></i></button>"
                            : "";

                        sb.AppendFormat(
                            "<tr class='tree-row tree-hidden {0}'>" +
                            "<td style='padding-left:5px;'>" +
                            "<a href='Forms/PetWorkflow.aspx?id={1}' style='font-weight:700;color:#1a3c5e;'>v{2} &ndash; {3}</a></td><td>" +
                            "<span class='pet-status {4}' style='display:block;margin-top:2px;text-align:center;'>{5}</td></span>" +
                            "<td style='color:#374151;'>{6}</td>" +
                            "<td><span style='font-weight:700;color:{7};'>{8}</td></span>" +
                            "<td style='color:#475569;'>{9}</td>" +
                            "<td class='text-right' style='font-weight:700;color:#1a3c5e;'>{10}</td>" +
                            "<td>{11}</td>" +
                            "<td>{12}</td>" +
                            "<td style='color:#64748b;'>{13}</td>" +
                            "<td><div class='gv-acts'>" +
                            "<a href='Forms/PetWorkflow.aspx?id={1}' class='btn btn-xs btn-primary'><i class='bi bi-arrow-right-circle'></i></a>" +
                            "{14}</div></td>" +
                            "</tr>",
                            safeId,
                            petId,
                            vi + 1,
                            System.Web.HttpUtility.HtmlEncode(refNo),
                            badgeCss,
                            System.Web.HttpUtility.HtmlEncode(status),
                            System.Web.HttpUtility.HtmlEncode(proj),
                            typeColor,
                            System.Web.HttpUtility.HtmlEncode(type),
                            System.Web.HttpUtility.HtmlEncode(src),
                            reqAmt > 0 ? reqAmt.ToString("N0") : "",
                            System.Web.HttpUtility.HtmlEncode(approver),
                            System.Web.HttpUtility.HtmlEncode(by),
                            submitted,
                            delBtn);
                    }
                }

                if (sb.Length == 0)
                    sb.Append("<tr><td colspan='9' style='text-align:center;padding:18px;color:#94a3b8;'>No records found for the selected view/filters.</td></tr>");

                litPendingTree.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                litPendingTree.Text = "<tr><td colspan='9' style='padding:14px;color:#dc2626;'>Error loading data: " +
                    System.Web.HttpUtility.HtmlEncode(ex.Message) + "</td></tr>";
            }
        }

        private void LoadMyPet()
        {
            try
            {
                DataTable dt = WorkflowDAL.GetPetForms(AuthHelper.CurrentUserShort);
                gvMyPet.DataSource = dt;
                gvMyPet.DataBind();
            }
            catch { }
        }

        /// <summary>Renders the gvMyPet row's Delete button, but only for Draft / Pending Review / Pending
        /// Approval requests — hidden entirely once Approved (or Rejected/SentBack/Deleted), per the Delete
        /// Button Visibility business rule. Shared status rule lives in WorkflowDAL.IsPetDeletable.</summary>
        protected string DeleteButtonHtml(object petFormId, object petRefNo, object status)
        {
            string statusStr = status == null || status == DBNull.Value ? "" : status.ToString();
            if (!WorkflowDAL.IsPetDeletable(statusStr)) return "";

            string refNo = petRefNo == null || petRefNo == DBNull.Value ? "#" + petFormId : petRefNo.ToString();
            return "<button type='button' class='btn btn-xs btn-danger' onclick=\"dfmPetDel('" + petFormId + "','" +
                   System.Web.HttpUtility.JavaScriptStringEncode(refNo) + "');\"><i class='bi bi-trash'></i> Delete</button>";
        }

        /// <summary>Budget Line Items the current user has added across all of their (Approved) PET forms.</summary>
        private void LoadMyBudgetLines()
        {
            try
            {
                DataTable dt = WorkflowDAL.GetBudgetLinesByUser(AuthHelper.CurrentUserShort);
                gvMyBudgetLines.DataSource = dt;
                gvMyBudgetLines.DataBind();
            }
            catch { }
        }

        // ===== Events =====
        protected void Filter_Changed(object sender, EventArgs e)
        {
            CurrentPage = 1;
            LoadPendingTree();
            LoadKPIs();
        }

        protected void btnResetFilters_Click(object sender, EventArgs e)
        {
            ddlProject.SelectedValue = "ALL";
            ddlType.SelectedValue    = "ALL";
            ddlPortfolioFilter.SelectedValue = "ALL";
            ddlStatus.SelectedValue  = "ALL";
            ddlView.SelectedValue    = "MYAPPROVAL";
            txtFromDate.Text = txtToDate.Text = "";
            CurrentPage = 1;
            LoadAll();
        }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                string jiraFilter = ddlProject.SelectedValue == "ALL" ? null : ddlProject.SelectedValue;
                string typeFilter = ddlType.SelectedValue  == "ALL" ? null : ddlType.SelectedValue;
                string statFilter = ddlStatus.SelectedValue == "ALL" ? null : ddlStatus.SelectedValue;
                DateTime? fromDate = null, toDate = null;
                DateTime dt;
                if (DateTime.TryParse(txtFromDate.Text, out dt)) fromDate = dt;
                if (DateTime.TryParse(txtToDate.Text, out dt))   toDate   = dt;
                DataTable data = WorkflowDAL.GetPetFormsDashboard(jiraFilter, typeFilter, statFilter, fromDate, toDate);
                ExcelHelper.ExportDataTable(data, "PET_Dashboard", Response);
            }
            catch { }
        }

        protected void btnConfirmDeletePet_Click(object sender, EventArgs e)
        {
            int petId;
            if (int.TryParse(hfDeletePetId.Value, out petId) && petId > 0)
            {
                // Server-side guard mirrors the Delete button's visibility rule — re-fetch status fresh
                // from the DB rather than trusting anything client-side.
                DataRow f = WorkflowDAL.GetPetForm(petId);
                string status = f != null && f["Status"] != DBNull.Value ? f["Status"].ToString() : "";
                if (f != null && WorkflowDAL.IsPetDeletable(status))
                    WorkflowDAL.DeletePetForm(petId, AuthHelper.CurrentUserShort);
            }
            hfDeletePetId.Value = "0";
            CurrentPage = 1;
            LoadAll();
        }

        protected void btnExportMyBudgetLines_Click(object sender, EventArgs e)
        {
            ExcelHelper.ExportCsv(WorkflowDAL.GetBudgetLinesByUser(AuthHelper.CurrentUserShort), "My_Budget_Lines", Response);
        }

        protected void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (CurrentPage > 1) { CurrentPage--; LoadPendingTree(); }
        }

        protected void btnNextPage_Click(object sender, EventArgs e)
        {
            CurrentPage++;
            LoadPendingTree();
        }

        protected void gvRegisteredProjects_PageIndexChanging(object sender, System.Web.UI.WebControls.GridViewPageEventArgs e)
        {
            gvRegisteredProjects.PageIndex = e.NewPageIndex;
            LoadRegisteredProjects();
        }

        // ===== Project Action Modals (Spend Request / Budget / Invoice) =====

        protected void btnShowSpendRequests_Click(object sender, EventArgs e)
        {
            string projId = hfActionProjectId.Value;
            if (string.IsNullOrEmpty(projId)) return;

            litSRModalProject.Text = Server.HtmlEncode(projId);

            // Load Spend Requests for the project (including Draft)
            DataTable petForms = WorkflowDAL.GetPetFormsDashboard(projId, null, null, null, null);
            gvModalSpendRequests.DataSource = petForms;
            gvModalSpendRequests.DataBind();

            // Load ALL line items for the project (adjacent display)
            DataTable lines = WorkflowDAL.GetPetLinesByProject(projId);
            gvModalLineItems.DataSource = lines;
            gvModalLineItems.DataBind();

            ScriptManager.RegisterStartupScript(this, GetType(), "showSRModal",
                "$(function(){ $('#spendRequestModal').modal('show'); });", true);
        }

        protected void btnShowBudget_Click(object sender, EventArgs e)
        {
            string projId = hfActionProjectId.Value;
            if (string.IsNullOrEmpty(projId)) return;

            litBgtModalProject.Text = Server.HtmlEncode(projId);
            pnlBudgetInvoiceDetail.Visible = false;

            DataTable budget = WorkflowDAL.GetBudgetLinesByProject(projId);
            gvModalBudgetLines.DataSource = budget;
            gvModalBudgetLines.DataBind();

            ScriptManager.RegisterStartupScript(this, GetType(), "showBgtModal",
                "$(function(){ $('#budgetActionModal').modal('show'); });", true);
        }

        protected void btnShowInvoices_Click(object sender, EventArgs e)
        {
            string projId = hfActionProjectId.Value;
            if (string.IsNullOrEmpty(projId)) return;

            litInvModalProject.Text = Server.HtmlEncode(projId);

            DataTable invoices = WorkflowDAL.GetInvoicesByProject(projId);
            gvModalInvoices.DataSource = invoices;
            gvModalInvoices.DataBind();

            ScriptManager.RegisterStartupScript(this, GetType(), "showInvModal",
                "$(function(){ $('#invoiceActionModal').modal('show'); });", true);
        }

        protected void gvModalBudgetLines_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ShowInvoice")
            {
                int budgetLineId = Convert.ToInt32(e.CommandArgument);
                litBgtInvLineId.Text = budgetLineId.ToString();
                pnlBudgetInvoiceDetail.Visible = true;

                DataTable invoices = WorkflowDAL.GetBudgetInvoices(budgetLineId);
                gvModalBudgetInvoices.DataSource = invoices;
                gvModalBudgetInvoices.DataBind();

                // Re-bind the budget lines so the modal stays populated
                string projId = hfActionProjectId.Value;
                if (!string.IsNullOrEmpty(projId))
                {
                    litBgtModalProject.Text = Server.HtmlEncode(projId);
                    DataTable budget = WorkflowDAL.GetBudgetLinesByProject(projId);
                    gvModalBudgetLines.DataSource = budget;
                    gvModalBudgetLines.DataBind();
                }

                ScriptManager.RegisterStartupScript(this, GetType(), "showBgtModal",
                    "$(function(){ $('#budgetActionModal').modal('show'); });", true);
            }
        }

        private static string Fmt(DataRow r, string col)
        {
            if (r == null || !r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return "0";
            return Convert.ToInt32(r[col]).ToString("N0");
        }

        private static string FmtAmt(DataRow r, string col)
        {
            if (r == null || !r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return "AED 0";
            decimal v = Convert.ToDecimal(r[col]);
            if (v >= 1000000m) return "AED " + (v / 1000000m).ToString("N2") + "M";
            if (v >= 1000m)    return "AED " + (v / 1000m).ToString("N1") + "K";
            return "AED " + v.ToString("N0");
        }
    }
}
