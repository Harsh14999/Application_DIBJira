using System;
using System.Data;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
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

                LoadAll();
            }
        }

        private void LoadJiraProjects()
        {
            try
            {
                DataTable dt = ProjectDAL.GetProjects();
                ddlProject.Items.Clear();
                ddlProject.Items.Add(new System.Web.UI.WebControls.ListItem("All Projects", "ALL"));
                foreach (DataRow r in dt.Rows)
                {
                    string projectId = Val(r, "ProjectID");
                    string projectName = Val(r, "ProjectName");
                    ddlProject.Items.Add(new System.Web.UI.WebControls.ListItem(
                        string.IsNullOrEmpty(projectName) ? projectId : projectId + " - " + projectName, projectId));
                }
            }
            catch { /* no registered projects yet */ }
        }

        private void LoadAll()
        {
            LoadKPIs();
            LoadBulkApproval();
            LoadProjectWiseKpi();
            LoadRegisteredProjects();
            LoadLastSync();
        }

        /// <summary>One row per registered Project, with matching Spend Requests expanded client-side below it.</summary>
        private void LoadRegisteredProjects()
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

                DataTable projects = ProjectDAL.GetProjects();
                DataTable allForms = WorkflowDAL.GetPetFormsDashboard(
                    jiraFilter, typeFilter, statFilter, fromDate, toDate, viewFilter, viewUser);

                var requestsByProject = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<DataRow>>(StringComparer.OrdinalIgnoreCase);
                foreach (DataRow request in allForms.Rows)
                {
                    string projectId = Val(request, "ProjectID");
                    if (string.IsNullOrEmpty(projectId)) projectId = "(No Project)";
                    if (!requestsByProject.ContainsKey(projectId))
                        requestsByProject[projectId] = new System.Collections.Generic.List<DataRow>();
                    requestsByProject[projectId].Add(request);
                }

                bool restrictToMatchingRequests = !string.IsNullOrEmpty(typeFilter)
                    || !string.IsNullOrEmpty(statFilter)
                    || fromDate.HasValue
                    || toDate.HasValue
                    || !string.Equals(viewFilter, "ALL", StringComparison.OrdinalIgnoreCase);

                var projectRows = new System.Collections.Generic.List<DataRow>();
                foreach (DataRow project in projects.Rows)
                {
                    string projectId = Val(project, "ProjectID");
                    if (!string.IsNullOrEmpty(jiraFilter) && !string.Equals(projectId, jiraFilter, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (restrictToMatchingRequests && !requestsByProject.ContainsKey(projectId))
                        continue;
                    projectRows.Add(project);
                }

                int totalProjects = projectRows.Count;
                int totalRequests = allForms.Rows.Count;
                int pages = Math.Max(1, (int)Math.Ceiling(totalProjects / (double)PageSize));
                if (CurrentPage > pages) CurrentPage = pages;
                if (CurrentPage < 1) CurrentPage = 1;

                litRegisteredProjectsCount.Text = totalProjects.ToString();
                litPageInfo.Text = string.Format(
                    "<span style='font-size:.85em;color:#64748b;padding:0 8px;'>Page {0} of {1} ({2} project(s), {3} request(s))</span>",
                    CurrentPage, pages, totalProjects, totalRequests);
                btnPrevPage.Enabled = CurrentPage > 1;
                btnNextPage.Enabled = CurrentPage < pages;

                int skip = (CurrentPage - 1) * PageSize;
                var sb = new StringBuilder();
                for (int i = skip; i < Math.Min(skip + PageSize, projectRows.Count); i++)
                {
                    DataRow project = projectRows[i];
                    string projectId = Val(project, "ProjectID");
                    string safeId = "pr" + Math.Abs(projectId.GetHashCode() & 0x7FFFFFFF).ToString();
                    System.Collections.Generic.List<DataRow> requests;
                    if (!requestsByProject.TryGetValue(projectId, out requests))
                        requests = new System.Collections.Generic.List<DataRow>();

                    string toggle = requests.Count > 0
                        ? "<span class='project-toggle' data-project-tog='" + safeId + "' onclick=\"event.cancelBubble=true; return dfmProjectTog('" + safeId + "');\">&#9658;</span>"
                        : "<span style='display:inline-block;width:18px;'></span>";
                    string statusHtml = GetBool(project, "IsActive")
                        ? "<span class='badge-success'>Active</span>"
                        : "<span class='badge-danger'>Inactive</span>";
                    decimal requestedTotal = 0m;
                    foreach (DataRow request in requests)
                        requestedTotal += GetDecimal(request, "TotalRequestedAED");

                    sb.AppendFormat(
                        "<tr class='project-parent-row{12}'{13}>" +
                        "<td class='col-project-id'>{0}<strong class='project-id-cell'>{1}</strong></td>" +
                        "<td class='col-project-name'>{2}</td><td class='col-project-type'>{3}</td><td class='col-lead'>{4}</td><td class='col-lead'>{5}</td><td class='col-manager'>{6}</td><td class='col-requestor'>{7}</td><td class='col-status'>{8}</td><td class='col-date'>{9}</td>" +
                        "<td class='col-count text-right'><strong>{10}</strong></td><td class='col-amount text-right'><strong>{11}</strong></td>" +
                        "<td class='col-action'><div class='gv-acts'>" +
                        "<button type='button' class='btn btn-xs btn-primary' data-project-id='{14}' onclick=\"return dfmOpenProjectFrom(this, event);\"><i class='bi bi-pencil'></i> Edit</button>" +
                        "<button type='button' class='btn btn-xs btn-success' data-project-id='{14}' onclick=\"return dfmOpenSpendRequestForProject(this, event);\"><i class='bi bi-plus-circle'></i> New SR</button>" +
                        "</div></td></tr>",
                        toggle,
                        FormatProjectId(projectId),
                        Html(Val(project, "ProjectName")),
                        GetBool(project, "IsNonJiraProject") ? "Non-JIRA" : "JIRA",
                        Html(Val(project, "AccountableExecLead")),
                        Html(Val(project, "SmeLead")),
                        Html(Val(project, "ProjectManager")),
                        Html(Val(project, "CreatedBy")),
                        statusHtml,
                        FormatDate(project, "CreatedDate"),
                        requests.Count.ToString("N0"),
                        requestedTotal > 0 ? requestedTotal.ToString("N0") : "",
                        requests.Count > 0 ? " has-requests" : "",
                        requests.Count > 0 ? " onclick=\"return dfmProjectTog('" + safeId + "');\"" : "",
                        Attr(projectId));

                    if (requests.Count > 0)
                        AppendRequestChildRows(sb, safeId, requests);
                }

                if (sb.Length == 0)
                    sb.Append("<tr><td colspan='12' style='text-align:center;padding:18px;color:#94a3b8;'>No registered projects found for the selected filters.</td></tr>");

                litRegisteredProjectRows.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                litRegisteredProjectRows.Text = "<tr><td colspan='12' style='padding:14px;color:#dc2626;'>Error loading projects: " +
                    System.Web.HttpUtility.HtmlEncode(ex.Message) + "</td></tr>";
            }
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
            catch { }
        }

        private void LoadBulkApproval()
        {
            try
            {
                DataTable dt = WorkflowDAL.GetPetFormsForApprover(AuthHelper.CurrentUserShort);
                gvBulkApproval.DataSource = dt;
                gvBulkApproval.DataBind();
                litBulkApprovalCount.Text = dt.Rows.Count.ToString();
            }
            catch
            {
                gvBulkApproval.DataSource = null;
                gvBulkApproval.DataBind();
                litBulkApprovalCount.Text = "0";
            }
        }

        private void LoadProjectWiseKpi()
        {
            try
            {
                string projectId = ddlProject.SelectedValue == "ALL" ? null : ddlProject.SelectedValue;
                gvProjectWiseKpi.DataSource = WorkflowDAL.GetProjectWiseKpiSummary(projectId);
                gvProjectWiseKpi.DataBind();
            }
            catch
            {
                gvProjectWiseKpi.DataSource = null;
                gvProjectWiseKpi.DataBind();
            }
        }

        /// <summary>Renders the Delete button only for Draft / Pending Review / Pending Approval requests.</summary>
        protected string DeleteButtonHtml(object petFormId, object petRefNo, object status)
        {
            string statusStr = status == null || status == DBNull.Value ? "" : status.ToString();
            if (!WorkflowDAL.IsPetDeletable(statusStr)) return "";

            string refNo = petRefNo == null || petRefNo == DBNull.Value ? "#" + petFormId : petRefNo.ToString();
            return "<button type='button' class='btn btn-xs btn-danger' onclick=\"dfmPetDel('" + petFormId + "','" +
                   System.Web.HttpUtility.JavaScriptStringEncode(refNo) + "');\"><i class='bi bi-trash'></i> Delete</button>";
        }

        private void AppendRequestChildRows(StringBuilder sb, string safeId, System.Collections.Generic.List<DataRow> requests)
        {
            requests.Sort(delegate (DataRow a, DataRow b) {
                int ia = Convert.ToInt32(a["PetFormID"]);
                int ib = Convert.ToInt32(b["PetFormID"]);
                return ia.CompareTo(ib);
            });

            sb.Append("<tr class='project-child-row tree-hidden " + safeId + "'><td colspan='12'>");
            sb.Append("<div class='project-child-box'><div class='project-child-title'>Spend Requests under this Project</div>");
            sb.Append("<table class='dfm-table' style='width:100%;'><thead><tr>");
            sb.Append("<th>Code</th><th>Status</th><th>Project</th><th>Type</th><th>Budget Source</th><th class='text-right'>Requested (AED)</th><th>Approver</th><th>Requestor</th><th>Submitted</th><th>Action</th>");
            sb.Append("</tr></thead><tbody>");

            for (int i = 0; i < requests.Count; i++)
            {
                DataRow r = requests[i];
                string status = Val(r, "Status");
                string badgeCss = status == "Draft" ? "st-draft"
                                : status == "PendingReview" ? "st-review"
                                : status == "PendingApproval" ? "st-pending"
                                : status == "Approved" ? "st-approved"
                                : status == "Rejected" ? "st-rejected"
                                : "st-sent";
                string petId = Val(r, "PetFormID");
                string refNo = string.IsNullOrEmpty(Val(r, "PetRefNo")) ? "#" + petId : Val(r, "PetRefNo");
                string type = Val(r, "CapexOpexType");
                string typeColor = type == "CAPEX" ? "#2563eb" : (type == "OPEX" ? "#059669" : "#64748b");
                decimal requested = GetDecimal(r, "TotalRequestedAED");

                sb.AppendFormat(
                    "<tr>" +
                    "<td><a href='javascript:void(0);' onclick=\"return dfmOpenSpendRequest('{0}', null);\" style='font-weight:700;color:#1a3c5e;'>v{1} &ndash; {2}</a></td>" +
                    "<td><span class='pet-status {3}'>{4}</span></td>" +
                    "<td>{5}</td>" +
                    "<td><span style='font-weight:700;color:{6};'>{7}</span></td>" +
                    "<td>{8}</td>" +
                    "<td class='text-right' style='font-weight:700;color:#1a3c5e;'>{9}</td>" +
                    "<td>{10}</td><td>{11}</td><td>{12}</td>" +
                    "<td><div class='gv-acts'><button type='button' class='btn btn-xs btn-primary' onclick=\"return dfmOpenSpendRequest('{0}', null);\"><i class='bi bi-arrow-right-circle'></i></button>{13}</div></td>" +
                    "</tr>",
                    petId,
                    i + 1,
                    Html(refNo),
                    badgeCss,
                    Html(status),
                    Html(Val(r, "ProjectID")),
                    typeColor,
                    Html(type),
                    Html(Val(r, "BudgetSourceID")),
                    requested > 0 ? requested.ToString("N0") : "",
                    Html(Val(r, "ApproverUsername")),
                    Html(Val(r, "CreatedBy")),
                    FormatDate(r, "SubmittedDate"),
                    DeleteButtonHtml(petId, refNo, status));
            }

            sb.Append("</tbody></table></div></td></tr>");
        }

        // ===== Events =====
        protected void Filter_Changed(object sender, EventArgs e)
        {
            CurrentPage = 1;
            LoadRegisteredProjects();
            LoadKPIs();
            LoadBulkApproval();
            LoadProjectWiseKpi();
        }

        protected void btnResetFilters_Click(object sender, EventArgs e)
        {
            ddlProject.SelectedValue = "ALL";
            ddlType.SelectedValue    = "ALL";
            ddlStatus.SelectedValue  = "ALL";
            ddlView.SelectedValue    = "MYAPPROVAL";
            txtFromDate.Text = txtToDate.Text = "";
            CurrentPage = 1;
            LoadAll();
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

        protected void btnBulkApproveSelected_Click(object sender, EventArgs e)
        {
            BulkDecision(true);
        }

        protected void btnBulkSendBackSelected_Click(object sender, EventArgs e)
        {
            BulkDecision(false);
        }

        private void BulkDecision(bool approve)
        {
            int done = 0;
            string user = AuthHelper.CurrentUserShort;
            string comments = txtBulkApprovalComments.Text.Trim();

            for (int i = 0; i < gvBulkApproval.Rows.Count; i++)
            {
                GridViewRow row = gvBulkApproval.Rows[i];
                CheckBox chk = row.FindControl("chkBulkApproval") as CheckBox;
                if (chk == null || !chk.Checked) continue;

                int petId = Convert.ToInt32(gvBulkApproval.DataKeys[i].Values["PetFormID"]);
                DataRow form = WorkflowDAL.GetPetForm(petId);
                if (form == null) continue;

                string status = Val(form, "Status");
                if (status == "PendingReview" && string.Equals(Val(form, "ReviewerUsername"), user, StringComparison.OrdinalIgnoreCase))
                {
                    WorkflowDAL.ReviewPet(petId, user, approve ? "Approve" : "SentBack", comments);
                    done++;
                }
                else if (status == "PendingApproval" && string.Equals(Val(form, "ApproverUsername"), user, StringComparison.OrdinalIgnoreCase))
                {
                    WorkflowDAL.ApprovePet(petId, user, approve ? "Approved" : "SentBack", comments);
                    done++;
                }
            }

            lblBulkApprovalMsg.Text = done == 0 ? "No selected pending item was processed." : done + " item(s) processed.";
            txtBulkApprovalComments.Text = "";
            CurrentPage = 1;
            LoadAll();
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

        protected void btnShowProjectTracker_Click(object sender, EventArgs e)
        {
            string projId = hfActionProjectId.Value;
            if (string.IsNullOrEmpty(projId)) return;

            DataRow project = ProjectDAL.GetProjectById(projId);
            DataRow summary = WorkflowDAL.GetProjectFinancialSummary(projId);
            DataTable forms = WorkflowDAL.GetPetFormsDashboard(projId, null, null, null, null);
            DataTable tracker = WorkflowDAL.GetProjectBudgetTracker(projId);

            string projectName = project != null ? Val(project, "ProjectName") : "";
            if (string.IsNullOrEmpty(projectName)) projectName = projId;
            string capexId = FirstValue(forms, "BudgetSourceID");
            decimal approvedBudget = GetDecimal(summary, "ApprovedSpendRequestTotal");
            decimal committed = GetDecimal(summary, "BudgetTotal");
            decimal invoiced = GetDecimal(summary, "InvoiceTotal");
            decimal remaining = approvedBudget - committed;
            decimal utilization = approvedBudget > 0 ? Math.Round((committed / approvedBudget) * 100m, 0) : 0m;

            int lpoIssued = 0;
            int pendingCamLpo = 0;
            int paidInvoices = 0;
            var lpoLines = new System.Collections.Generic.HashSet<int>();
            var pendingLines = new System.Collections.Generic.HashSet<int>();
            foreach (DataRow row in tracker.Rows)
            {
                int budgetLineId = Convert.ToInt32(row["BudgetLineID"]);
                string lpoRequest = Val(row, "LpoRequest");
                string camStatus = Val(row, "CamStatus");
                string lpoStatus = Val(row, "LpoStatus");
                string invoiceStatus = Val(row, "InvoiceStatus");

                if (!string.IsNullOrEmpty(lpoRequest)) lpoLines.Add(budgetLineId);
                if (!StatusComplete(camStatus) || !StatusComplete(lpoStatus)) pendingLines.Add(budgetLineId);
                if (IsPaidInvoice(invoiceStatus)) paidInvoices++;
            }
            lpoIssued = lpoLines.Count;
            pendingCamLpo = pendingLines.Count;

            litTrackerModalProject.Text = Server.HtmlEncode(projId);
            litTrackerProjectTitle.Text = Server.HtmlEncode(projectName);
            litTrackerProjectName.Text = Server.HtmlEncode(projectName);
            litTrackerDemandId.Text = Server.HtmlEncode(projId);
            litTrackerCapexId.Text = Server.HtmlEncode(string.IsNullOrEmpty(capexId) ? "-" : capexId);
            litTrackerApprovedBudget.Text = FormatMoneyAed(approvedBudget);
            litTrackerApprovedBudgetTile.Text = FormatMoneyAed(approvedBudget);
            litTrackerCommitted.Text = FormatMoneyAed(committed);
            litTrackerInvoiced.Text = FormatMoneyAed(invoiced);
            litTrackerRemaining.Text = FormatMoneyAed(remaining);
            litTrackerUtilization.Text = utilization.ToString("N0") + "%";
            litTrackerLpoIssued.Text = lpoIssued.ToString("N0");
            litTrackerPendingCamLpo.Text = pendingCamLpo.ToString("N0");
            litTrackerPaidInvoices.Text = paidInvoices.ToString("N0");

            gvProjectBudgetTracker.DataSource = tracker;
            gvProjectBudgetTracker.DataBind();

            ScriptManager.RegisterStartupScript(this, GetType(), "showProjectTrackerModal",
                "$(function(){ $('#projectTrackerModal').modal('show'); });", true);
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

        private static string FormatCurrency(decimal value)
        {
            if (value >= 1000000m) return "AED " + (value / 1000000m).ToString("N2") + "M";
            if (value >= 1000m) return "AED " + (value / 1000m).ToString("N1") + "K";
            return "AED " + value.ToString("N0");
        }

        private static string FormatMoneyAed(decimal value)
        {
            return value.ToString("N2") + " AED";
        }

        private static string FirstValue(DataTable table, string column)
        {
            if (table == null || !table.Columns.Contains(column)) return "";
            foreach (DataRow row in table.Rows)
            {
                string value = Val(row, column);
                if (!string.IsNullOrEmpty(value)) return value;
            }
            return "";
        }

        private static bool StatusComplete(string status)
        {
            if (string.IsNullOrEmpty(status)) return false;
            return status.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                || status.Equals("Issued", StringComparison.OrdinalIgnoreCase)
                || status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                || status.Equals("Closed", StringComparison.OrdinalIgnoreCase)
                || status.Equals("Paid", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPaidInvoice(string status)
        {
            if (string.IsNullOrEmpty(status)) return false;
            return status.Equals("Paid", StringComparison.OrdinalIgnoreCase)
                || status.Equals("Processed / Archived", StringComparison.OrdinalIgnoreCase)
                || status.Equals("Received", StringComparison.OrdinalIgnoreCase);
        }

        private static string Val(DataRow row, string col)
        {
            if (row == null || !row.Table.Columns.Contains(col) || row[col] == DBNull.Value) return "";
            return row[col].ToString();
        }

        private static string Html(string value)
        {
            return System.Web.HttpUtility.HtmlEncode(value ?? "");
        }

        private static string Attr(string value)
        {
            return System.Web.HttpUtility.HtmlAttributeEncode(value ?? "");
        }

        private static bool GetBool(DataRow row, string col)
        {
            if (row == null || !row.Table.Columns.Contains(col) || row[col] == DBNull.Value) return false;
            return Convert.ToBoolean(row[col]);
        }

        private static decimal GetDecimal(DataRow row, string col)
        {
            if (row == null || !row.Table.Columns.Contains(col) || row[col] == DBNull.Value) return 0m;
            return Convert.ToDecimal(row[col]);
        }

        private static string FormatDate(DataRow row, string col)
        {
            if (row == null || !row.Table.Columns.Contains(col) || row[col] == DBNull.Value) return "";
            return Convert.ToDateTime(row[col]).ToString("dd-MMM-yyyy");
        }

        private static string FormatProjectId(string projectId)
        {
            string value = projectId ?? "";
            string js = System.Web.HttpUtility.JavaScriptStringEncode(value);
            return "<a href='javascript:void(0);' class='project-id-link' title='Open Project Budget Tracker' onclick=\"event.cancelBubble=true; return dfmShowTracker('" + js + "');\">" + Html(value) + "</a>";
        }
    }
}
