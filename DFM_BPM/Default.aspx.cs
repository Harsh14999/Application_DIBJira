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
                LoadLeadFilters();

                LoadAll();
            }
        }

        private void LoadLeadFilters()
        {
            LoadLeadFilter(ddlAccountableExecLeadFilter, ProjectDAL.GetDistinctAccountableExecLeads(), "All Accountable Exec Leads");
            LoadLeadFilter(ddlSmeLeadFilter, ProjectDAL.GetDistinctSmeLeads(), "All SME Leads");
        }

        private static void LoadLeadFilter(System.Web.UI.WebControls.DropDownList ddl, DataTable dt, string allText)
        {
            ddl.Items.Clear();
            ddl.Items.Add(new System.Web.UI.WebControls.ListItem(allText, "ALL"));
            foreach (DataRow r in dt.Rows)
            {
                string leadName = r["LeadName"] == DBNull.Value ? "" : r["LeadName"].ToString();
                if (!string.IsNullOrWhiteSpace(leadName))
                    ddl.Items.Add(new System.Web.UI.WebControls.ListItem(leadName, leadName));
            }
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
            LoadMyPet();
            LoadMyBudgetLines();
            LoadLastSync();
        }

        /// <summary>Merged Registered Projects tree with expandable matching Spend Request rows —
        /// shows every JIRA or Non-JIRA project registered via the Project Registration module, regardless of
        /// whether a Spend Request has ever been submitted/approved for it. Respects the dashboard's lead
        /// filters so reporting can slice by Accountable Exec Lead and SME Lead ownership.</summary>
        private void LoadRegisteredProjects()
        {
            try
            {
                string search = txtProjectSearch.Text.Trim();
                string jiraFilter = ddlProject.SelectedValue == "ALL" ? null : ddlProject.SelectedValue;
                string typeFilter = ddlType.SelectedValue == "ALL" ? null : ddlType.SelectedValue;
                string statFilter = ddlStatus.SelectedValue == "ALL" ? null : ddlStatus.SelectedValue;
                string accountableExecLead = ddlAccountableExecLeadFilter.SelectedValue == "ALL" ? null : ddlAccountableExecLeadFilter.SelectedValue;
                string smeLead = ddlSmeLeadFilter.SelectedValue == "ALL" ? null : ddlSmeLeadFilter.SelectedValue;
                string viewFilter = ddlView.SelectedValue;
                string viewUser = AuthHelper.CurrentUserShort;
                DateTime? fromDate = null, toDate = null;
                DateTime dt;
                if (DateTime.TryParse(txtFromDate.Text, out dt)) fromDate = dt;
                if (DateTime.TryParse(txtToDate.Text, out dt)) toDate = dt;

                DataTable projects = ProjectDAL.GetProjects(
                    string.IsNullOrEmpty(search) ? null : search,
                    null, accountableExecLead, smeLead, jiraFilter);
                DataTable allForms = WorkflowDAL.GetPetFormsDashboard(
                    jiraFilter, typeFilter, statFilter, fromDate, toDate, viewFilter, viewUser,
                    accountableExecLead, smeLead);

                var groups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<DataRow>>(StringComparer.OrdinalIgnoreCase);
                foreach (DataRow r in allForms.Rows)
                {
                    string projectId = r["ProjectID"] == DBNull.Value ? "" : r["ProjectID"].ToString();
                    if (!groups.ContainsKey(projectId)) groups[projectId] = new System.Collections.Generic.List<DataRow>();
                    groups[projectId].Add(r);
                }

                int totalProjects = projects.Rows.Count;
                int totalRequests = allForms.Rows.Count;
                int pages = Math.Max(1, (int)Math.Ceiling(totalProjects / (double)PageSize));
                if (CurrentPage > pages) CurrentPage = pages;
                litRegisteredProjectsCount.Text = totalProjects.ToString();
                litPageInfo.Text = string.Format(
                    "<span style='font-size:.85em;color:#64748b;padding:0 8px;'>Page {0} of {1} ({2} registered project(s), {3} matching request(s))</span>",
                    CurrentPage, pages, totalProjects, totalRequests);
                btnPrevPage.Enabled = CurrentPage > 1;
                btnNextPage.Enabled = CurrentPage < pages;

                int skip = (CurrentPage - 1) * PageSize;
                var sb = new StringBuilder();
                for (int pi = skip; pi < Math.Min(skip + PageSize, projects.Rows.Count); pi++)
                {
                    DataRow p = projects.Rows[pi];
                    string projectId = Val(p, "ProjectID");
                    string projectName = Val(p, "ProjectName");
                    string projectManager = Val(p, "ProjectManager");
                    string requestor = Val(p, "CreatedBy");
                    string accLead = Val(p, "AccountableExecLead");
                    string sme = Val(p, "SmeLead");
                    string projectSize = Val(p, "ProjectSize");
                    string createdDate = p["CreatedDate"] == DBNull.Value ? "" : Convert.ToDateTime(p["CreatedDate"]).ToString("dd-MMM-yyyy");
                    bool isNonJira = p["IsNonJiraProject"] != DBNull.Value && Convert.ToBoolean(p["IsNonJiraProject"]);
                    bool isActive = p["IsActive"] == DBNull.Value || Convert.ToBoolean(p["IsActive"]);

                    System.Collections.Generic.List<DataRow> petRows;
                    if (!groups.TryGetValue(projectId, out petRows)) petRows = new System.Collections.Generic.List<DataRow>();
                    petRows.Sort(delegate (DataRow a, DataRow b) {
                        int ia = Convert.ToInt32(a["PetFormID"]);
                        int ib = Convert.ToInt32(b["PetFormID"]);
                        return ia.CompareTo(ib);
                    });

                    decimal projectTotal = 0m;
                    foreach (DataRow pr in petRows)
                        if (pr.Table.Columns.Contains("TotalRequestedAED") && pr["TotalRequestedAED"] != DBNull.Value)
                            projectTotal += Convert.ToDecimal(pr["TotalRequestedAED"]);

                    string safeId = "pg" + Math.Abs(projectId.GetHashCode() & 0x7FFFFFFF).ToString();
                    string projectEsc = System.Web.HttpUtility.JavaScriptStringEncode(projectId);
                    string openProjectUrl = ResolveUrl("~/Forms/ProjectRegistration.aspx") + "?pid=" + Server.UrlEncode(projectId);
                    string toggle = petRows.Count > 0
                        ? "<span class='tree-toggle' onclick='dfmTog(\"" + safeId + "\")' data-tog='" + safeId + "' style='cursor:pointer;color:#2563eb;font-size:1.1em;margin-right:6px;'>&#9658;</span>"
                        : "<span style='color:#cbd5e1;font-size:1.1em;margin-right:6px;'>&#9675;</span>";
                    string statusBadge = isActive ? "<span class='badge-success'>Active</span>" : "<span class='badge-danger'>Inactive</span>";
                    string sizeBadge = ProjectSizeBadge(projectSize);

                    sb.AppendFormat(
                        "<tr style='background:#e8f0fe;'>" +
                        "<td colspan='12' style='border-bottom:1px solid #c7d2fe;'>" +
                        "{0}<i class='bi bi-folder2-open' style='color:#2563eb;margin-right:5px;'></i>" +
                        "<strong>{1}</strong> <span style='color:#64748b;font-size:.82em;'>-- {2}</span>" +
                        "<span style='color:#64748b;font-weight:400;font-size:.82em;margin-left:10px;'>{3} | {4} | {5} PET(s) | Total AED: <strong style=\"color:#1a3c5e;\">{6}</strong></span>" +
                        "<span style='color:#64748b;font-weight:400;font-size:.82em;margin-left:10px;'>Manager: {7} | Requestor: {8} | Created: {9}</span>" +
                        "<span style='display:block;margin-top:6px;color:#64748b;font-size:.82em;'>Accountable Exec Lead: <strong>{10}</strong> | SME Lead: <strong>{11}</strong></span>" +
                        "<span style='display:block;margin-top:7px;'>" +
                        "<a href='{12}' class='btn btn-xs btn-primary'><i class='bi bi-arrow-right-circle'></i> Open Project</a> " +
                        "<button type='button' class='proj-action-btn btn-sr' onclick=\"dfmShowSR('{13}');\"><i class='bi bi-file-earmark-text'></i> Spend Request</button>" +
                        "<button type='button' class='proj-action-btn btn-bgt' onclick=\"dfmShowBgt('{13}');\"><i class='bi bi-cash-coin'></i> Budget</button>" +
                        "<button type='button' class='proj-action-btn btn-inv' onclick=\"dfmShowInv('{13}');\"><i class='bi bi-receipt'></i> Invoice</button>" +
                        "</span></td></tr>",
                        toggle,
                        Html(projectName),
                        Html(projectId),
                        isNonJira ? "Non-JIRA" : "JIRA",
                        statusBadge,
                        sizeBadge,
                        projectTotal.ToString("N0"),
                        Html(projectManager),
                        Html(requestor),
                        Html(createdDate),
                        Html(accLead),
                        Html(sme),
                        openProjectUrl,
                        projectEsc);

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
                        string delBtn = WorkflowDAL.IsPetDeletable(status)
                            ? "<button type='button' class='btn btn-xs btn-danger' onclick=\"dfmPetDel('" + petId + "','" + System.Web.HttpUtility.JavaScriptStringEncode(refNo) + "');\"><i class='bi bi-trash'></i></button>"
                            : "";

                        sb.AppendFormat(
                            "<tr class='tree-row tree-hidden {0}'>" +
                            "<td style='padding-left:26px;color:#64748b;'>v{1}</td>" +
                            "<td><a href='Forms/PetWorkflow.aspx?id={2}' style='font-weight:700;color:#1a3c5e;'>{3}</a></td>" +
                            "<td><span class='pet-status {4}' style='display:block;margin-top:2px;text-align:center;'>{5}</span></td>" +
                            "<td><span style='font-weight:700;color:{6};'>{7}</span></td>" +
                            "<td style='color:#475569;'>{8}</td>" +
                            "<td class='text-right' style='font-weight:700;color:#1a3c5e;'>{9}</td>" +
                            "<td>{10}</td>" +
                            "<td>{11}</td>" +
                            "<td style='color:#64748b;'>{12}</td>" +
                            "<td></td><td></td>" +
                            "<td><div class='gv-acts'>" +
                            "<a href='Forms/PetWorkflow.aspx?id={2}' class='btn btn-xs btn-primary'><i class='bi bi-arrow-right-circle'></i></a>" +
                            "{13}</div></td>" +
                            "</tr>",
                            safeId,
                            vi + 1,
                            petId,
                            Html(refNo),
                            badgeCss,
                            Html(status),
                            typeColor,
                            Html(type),
                            Html(src),
                            reqAmt > 0 ? reqAmt.ToString("N0") : "",
                            Html(approver),
                            Html(by),
                            submitted,
                            delBtn);
                    }
                }

                if (sb.Length == 0)
                    sb.Append("<tr><td colspan='12' style='text-align:center;padding:18px;color:#94a3b8;'>No registered projects found for the selected filters.</td></tr>");

                litRegisteredProjectTree.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                litRegisteredProjectTree.Text = "<tr><td colspan='12' style='padding:14px;color:#dc2626;'>Error loading data: " +
                    System.Web.HttpUtility.HtmlEncode(ex.Message) + "</td></tr>";
            }
        }

        private static string Val(DataRow r, string col)
        {
            return r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? r[col].ToString() : "";
        }

        private static string Html(string value)
        {
            return System.Web.HttpUtility.HtmlEncode(value ?? "");
        }

        private static string ProjectSizeBadge(string size)
        {
            if (string.IsNullOrEmpty(size)) return "<span style='color:#94a3b8;'>--</span>";
            return "<span class='ps-size-badge size-" + Html(size.ToLower()) + "'>" + Html(size) + "</span>";
        }

        protected void btnProjectSearch_Click(object sender, EventArgs e)
        {
            CurrentPage = 1;
            LoadRegisteredProjects();
        }

        protected void btnProjectSearchReset_Click(object sender, EventArgs e)
        {
            txtProjectSearch.Text = "";
            CurrentPage = 1;
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
            LoadRegisteredProjects();
            LoadKPIs();
        }

        protected void btnResetFilters_Click(object sender, EventArgs e)
        {
            ddlProject.SelectedValue = "ALL";
            ddlType.SelectedValue    = "ALL";
            ddlAccountableExecLeadFilter.SelectedValue = "ALL";
            ddlSmeLeadFilter.SelectedValue = "ALL";
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
                string accountableExecLead = ddlAccountableExecLeadFilter.SelectedValue == "ALL" ? null : ddlAccountableExecLeadFilter.SelectedValue;
                string smeLead = ddlSmeLeadFilter.SelectedValue == "ALL" ? null : ddlSmeLeadFilter.SelectedValue;
                DateTime? fromDate = null, toDate = null;
                DateTime dt;
                if (DateTime.TryParse(txtFromDate.Text, out dt)) fromDate = dt;
                if (DateTime.TryParse(txtToDate.Text, out dt))   toDate   = dt;
                DataTable data = WorkflowDAL.GetPetFormsDashboard(jiraFilter, typeFilter, statFilter, fromDate, toDate,
                    null, null, accountableExecLead, smeLead);
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
            if (CurrentPage > 1) { CurrentPage--; LoadRegisteredProjects(); }
        }

        protected void btnNextPage_Click(object sender, EventArgs e)
        {
            CurrentPage++;
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
