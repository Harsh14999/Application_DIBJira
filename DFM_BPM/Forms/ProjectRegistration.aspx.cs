using System;
using System.Data;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using DFM_BPM.App_Code.DAL;
using DFM_BPM.App_Code.Helpers;

namespace DFM_BPM.Forms
{
    public partial class ProjectRegistration : Page
    {
        protected string CurrentProjectId
        {
            get { return hfProjectId.Value; }
            set { hfProjectId.Value = value ?? ""; }
        }

        protected bool IsExistingProject { get { return !string.IsNullOrEmpty(CurrentProjectId); } }

        private bool InlineProjectModal { get { return Request.QueryString["inline"] == "1"; } }

        /// <summary>Delete is only offered for an already-registered project, when no active Spend Request
        /// references it yet (ProjectDAL.HasPetForms is the hard-delete safety net), and only to Admins or
        /// the user who originally registered it.</summary>
        protected bool CanDeleteProject
        {
            get
            {
                if (!IsExistingProject) return false;
                if (ProjectDAL.HasPetForms(CurrentProjectId)) return false;
                if (AuthHelper.IsAdmin) return true;
                DataRow p = ProjectDAL.GetProjectById(CurrentProjectId);
                return p != null && string.Equals(Convert.ToString(p["CreatedBy"]), AuthHelper.CurrentUserShort, StringComparison.OrdinalIgnoreCase);
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadDropdowns();
                BindProjectPortfolio();

                string pid = Request.QueryString["pid"];
                if (!string.IsNullOrEmpty(pid))
                {
                    CurrentProjectId = pid;
                    LoadProject(pid);
                    ShowProjectModal();
                }
                else if (Request.QueryString["new"] == "1")
                {
                    ClearProjectForm();
                    ApplyProjectModePanels();
                    pnlProjectDetails.Visible = false;
                    pnlNoProject.Visible = true;
                    ShowProjectModal();
                }
                else
                {
                    ApplyProjectModePanels();
                    pnlProjectDetails.Visible = false;
                    pnlNoProject.Visible = true;
                }

                if (Session["ProjectNextStep"] != null)
                {
                    ShowNextStep(Session["ProjectNextStep"].ToString());
                    Session.Remove("ProjectNextStep");
                }
            }
            else
            {
                ApplyProjectModePanels();
                if (IsExistingProject) txtNonJiraProjectId.ReadOnly = true;
            }
        }

        private void LoadDropdowns()
        {
            BindJiraDropdown();
        }

        private void BindProjectPortfolio()
        {
            DataTable projects = ProjectDAL.GetProjects();
            DataTable allForms = WorkflowDAL.GetPetFormsDashboard(null, null, null, null, null);
            litProjectPortfolioCount.Text = projects.Rows.Count.ToString();

            var requestsByProject = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<DataRow>>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow request in allForms.Rows)
            {
                string projectId = Val(request, "ProjectID");
                if (string.IsNullOrEmpty(projectId)) projectId = "(No Project)";
                if (!requestsByProject.ContainsKey(projectId))
                    requestsByProject[projectId] = new System.Collections.Generic.List<DataRow>();
                requestsByProject[projectId].Add(request);
            }

            StringBuilder sb = new StringBuilder();
            foreach (DataRow project in projects.Rows)
            {
                string projectId = Val(project, "ProjectID");
                string safeId = "pf" + Math.Abs(projectId.GetHashCode() & 0x7FFFFFFF).ToString();
                System.Collections.Generic.List<DataRow> requests;
                if (!requestsByProject.TryGetValue(projectId, out requests))
                    requests = new System.Collections.Generic.List<DataRow>();

                decimal requestedTotal = 0m;
                foreach (DataRow request in requests)
                    requestedTotal += GetDecimal(request, "TotalRequestedAED");

                string toggle = requests.Count > 0
                    ? "<span class='project-toggle' data-project-tog='" + safeId + "' onclick=\"event.cancelBubble=true; return prProjectTog('" + safeId + "');\">&#9658;</span>"
                    : "<span style='display:inline-block;width:18px;'></span>";
                string statusHtml = GetBool(project, "IsActive")
                    ? "<span class='badge-success'>Active</span>"
                    : "<span class='badge-danger'>Inactive</span>";
                string jsProjectId = System.Web.HttpUtility.JavaScriptStringEncode(projectId);

                sb.AppendFormat(
                    "<tr class='project-parent-row{12}'{13}>" +
                    "<td class='col-project-id'>{0}<strong class='project-id-cell'>{1}</strong></td>" +
                    "<td class='col-project-name'>{2}</td><td class='col-project-type'>{3}</td><td class='col-lead'>{4}</td><td class='col-lead'>{5}</td><td class='col-manager'>{6}</td><td class='col-requestor'>{7}</td><td class='col-status'>{8}</td><td class='col-date'>{9}</td>" +
                    "<td class='col-count text-right'><strong>{10}</strong></td><td class='col-amount text-right'><strong>{11}</strong></td>" +
                    "<td class='col-action'><div class='gv-acts'>" +
                    "<button type='button' class='btn btn-xs btn-primary' onclick=\"event.cancelBubble=true; return prOpenProject('{14}');\"><i class='bi bi-pencil'></i> Edit</button>" +
                    "<button type='button' class='btn btn-xs btn-success' onclick=\"event.cancelBubble=true; return prOpenSpendRequest(null, '{14}');\"><i class='bi bi-plus-circle'></i> New SR</button>" +
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
                    requests.Count > 0 ? " onclick=\"return prProjectTog('" + safeId + "');\"" : "",
                    jsProjectId);

                if (requests.Count > 0)
                    AppendPortfolioRequestChildRows(sb, safeId, requests);
            }

            if (sb.Length == 0)
                sb.Append("<tr><td colspan='12' style='text-align:center;padding:18px;color:#94a3b8;'>No projects registered yet.</td></tr>");

            litProjectPortfolioRows.Text = sb.ToString();
        }

        private void AppendPortfolioRequestChildRows(StringBuilder sb, string safeId, System.Collections.Generic.List<DataRow> requests)
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
                string jsPetId = System.Web.HttpUtility.JavaScriptStringEncode(petId);

                sb.AppendFormat(
                    "<tr>" +
                    "<td><strong>v{0} - {1}</strong></td>" +
                    "<td><span class='pet-status {2}'>{3}</span></td>" +
                    "<td>{4}</td>" +
                    "<td><span style='font-weight:700;color:{5};'>{6}</span></td>" +
                    "<td>{7}</td>" +
                    "<td class='text-right' style='font-weight:700;color:#1a3c5e;'>{8}</td>" +
                    "<td>{9}</td><td>{10}</td><td>{11}</td>" +
                    "<td><button type='button' class='btn btn-xs btn-primary' onclick=\"return prOpenSpendRequest('{12}', null);\"><i class='bi bi-arrow-right-circle'></i></button></td>" +
                    "</tr>",
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
                    jsPetId);
            }

            sb.Append("</tbody></table></div></td></tr>");
        }

        private void BindJiraDropdown()
        {
            string currentSelection = ddlProject.SelectedValue;
            DataTable dtP = MastersDAL.GetJiraDropdown();
            ddlProject.DataSource     = dtP;
            ddlProject.DataTextField  = "DisplayName";
            ddlProject.DataValueField = "JiraID";
            ddlProject.DataBind();
            ddlProject.Items.Insert(0, new ListItem("-- Select JIRA Project --", ""));
            if (!string.IsNullOrEmpty(currentSelection) && ddlProject.Items.FindByValue(currentSelection) != null)
                ddlProject.SelectedValue = currentSelection;
        }

        /// <summary>Ensures an already-registered JIRA project still appears as a dropdown option even if it no
        /// longer satisfies the current Platform-filter selection.</summary>
        private void EnsureJiraOptionPresent(string jiraId)
        {
            if (string.IsNullOrEmpty(jiraId) || ddlProject.Items.FindByValue(jiraId) != null) return;
            DataRow j = MastersDAL.GetJiraById(jiraId);
            if (j != null)
                ddlProject.Items.Add(new ListItem(jiraId + " - " + Convert.ToString(j["Summary"]), jiraId));
        }

        private void ApplyProjectModePanels()
        {
            bool nonJira = rblProjectMode.SelectedValue == "NONJIRA";
            phJiraSelect.Visible        = !nonJira;
            phNonJiraSelect.Visible     = nonJira;
            txtProjectName.ReadOnly     = !nonJira;
            txtProjectManager.ReadOnly  = !nonJira;
        }

        protected void rblProjectMode_Changed(object sender, EventArgs e)
        {
            ApplyProjectModePanels();
            if (rblProjectMode.SelectedValue == "NONJIRA")
            {
                txtProjectName.Text = "";
                txtProjectManager.Text = "";
                LoadProjectDetails(null);
            }
            else
            {
                txtNonJiraProjectId.Text = "";
                LoadFromJira(ddlProject.SelectedValue);
            }
            ShowProjectModal();
        }

        protected void ddlProject_Changed(object sender, EventArgs e)
        {
            LoadFromJira(ddlProject.SelectedValue);
            ShowProjectModal();
        }

        /// <summary>Populates Project Name / Project Manager from the selected JIRA row.</summary>
        private void LoadFromJira(string jiraId)
        {
            if (string.IsNullOrEmpty(jiraId))
            {
                txtProjectName.Text = "";
                txtProjectManager.Text = "";
                LoadProjectDetails(null);
                return;
            }

            DataRow j = MastersDAL.GetJiraById(jiraId);
            if (j == null) { LoadProjectDetails(jiraId); return; }

            txtProjectName.Text    = Convert.ToString(j["Summary"]);
            txtProjectManager.Text = Convert.ToString(j["AssignedProjectManager"]);

            LoadProjectDetails(jiraId);
        }

        private void LoadProjectDetails(string jiraId)
        {
            if (string.IsNullOrEmpty(jiraId))
            {
                pnlProjectDetails.Visible = false;
                pnlNoProject.Visible = true;
                litNoProjectMsg.Text = "Select a JIRA project (or enter a Non-JIRA Project ID) above to see its details here.";
                return;
            }

            DataRow j = MastersDAL.GetJiraById(jiraId);
            if (j == null)
            {
                pnlProjectDetails.Visible = false;
                pnlNoProject.Visible = true;
                litNoProjectMsg.Text = "This is a Non-JIRA project &mdash; no additional JIRA metadata is available.";
                return;
            }

            pnlNoProject.Visible      = false;
            pnlProjectDetails.Visible = true;

            litJProjectType.Text   = Server.HtmlEncode(Convert.ToString(j["ProjectType"]));
            litJStage.Text         = Server.HtmlEncode(Convert.ToString(j["ProjectStage"]));
            litJRag.Text           = Server.HtmlEncode(Convert.ToString(j["ProjectRAG"]));
            litJDept.Text          = Server.HtmlEncode(Convert.ToString(j["Department"]));
            litJPlatform.Text      = Server.HtmlEncode(Convert.ToString(j["Platform"]));
            litJTech.Text          = Server.HtmlEncode(Convert.ToString(j["TechLead"]));
            litJAccExec.Text       = Server.HtmlEncode(Convert.ToString(j["AccountableExec"]));
            litJAccExecLead.Text   = Server.HtmlEncode(Convert.ToString(j["AccountableExecLead"]));
            litJSmeLead.Text       = Server.HtmlEncode(Convert.ToString(j["SmeLead"]));
            litJOverallStatus.Text = Server.HtmlEncode(Convert.ToString(j["ProjectOverallStatus"]));
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            bool isNonJira = rblProjectMode.SelectedValue == "NONJIRA";
            string projectId, projectName;

            if (isNonJira)
            {
                projectId   = txtNonJiraProjectId.Text.Trim();
                projectName = txtProjectName.Text.Trim();
                if (string.IsNullOrEmpty(projectId))   { ShowMsg("Project ID is required."); ShowProjectModal(); return; }
                if (string.IsNullOrEmpty(projectName)) { ShowMsg("Project Name is required."); ShowProjectModal(); return; }
            }
            else
            {
                projectId = ddlProject.SelectedValue;
                if (string.IsNullOrEmpty(projectId)) { ShowMsg("Select a JIRA project."); ShowProjectModal(); return; }
                projectName = txtProjectName.Text.Trim();
            }

            if (!IsExistingProject && ProjectDAL.ProjectExists(projectId))
            {
                ShowMsg("A project with this ID is already registered."); ShowProjectModal(); return;
            }

            // Resolve JIRA hierarchy fields to store denormalized for grid display
            string accExecLead = null, smeLead = null;
            if (!isNonJira)
            {
                DataRow jRow = MastersDAL.GetJiraById(projectId);
                if (jRow != null)
                {
                    accExecLead = Convert.ToString(jRow["AccountableExecLead"]);
                    smeLead     = Convert.ToString(jRow["SmeLead"]);
                }
            }

            ProjectDAL.SaveProject(projectId, projectName, isNonJira,
                txtProjectManager.Text.Trim(), null, ddlActive.SelectedValue == "Yes", AuthHelper.CurrentUserShort,
                accExecLead, smeLead);

            Session["ProjectNextStep"] = "Project saved. Next: create a Spend Request for this project, then add line items and submit it for approval.";
            Response.Redirect("~/Forms/ProjectRegistration.aspx?pid=" + Server.UrlEncode(projectId));
        }

        protected void btnNewProject_Click(object sender, EventArgs e)
        {
            ClearProjectForm();
            BindProjectPortfolio();
            ShowProjectModal();
        }

        protected void gvProjectPortfolio_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "EditProject") return;
            string projectId = Convert.ToString(e.CommandArgument);
            CurrentProjectId = projectId;
            LoadProject(projectId);
            ShowProjectModal();
        }

        protected void btnDeleteProject_Click(object sender, EventArgs e)
        {
            if (!CanDeleteProject)
            {
                ShowMsg("This project cannot be deleted (an active Spend Request already references it, or you don't have permission).");
                return;
            }
            ProjectDAL.DeleteProject(CurrentProjectId);
            Response.Redirect("~/Default.aspx");
        }

        private void LoadProject(string projectId)
        {
            DataRow p = ProjectDAL.GetProjectById(projectId);
            if (p == null) { Response.Redirect("~/Forms/ProjectRegistration.aspx"); return; }

            bool isNonJira = p["IsNonJiraProject"] != DBNull.Value && Convert.ToBoolean(p["IsNonJiraProject"]);
            rblProjectMode.SelectedValue = isNonJira ? "NONJIRA" : "JIRA";
            ApplyProjectModePanels();

            // Project identity (Type + JIRA ID / Project ID) is the primary key — lock it once registered.
            // Only the descriptive fields below (Name refresh, Manager, Portfolio, Active) may be edited.
            rblProjectMode.Enabled = false;

            if (isNonJira)
            {
                txtNonJiraProjectId.Text = projectId;
                txtNonJiraProjectId.ReadOnly = true; // Project ID is the primary key — not editable after creation
            }
            else
            {
                EnsureJiraOptionPresent(projectId);
                var item = ddlProject.Items.FindByValue(projectId);
                if (item != null) ddlProject.SelectedValue = projectId;
                ddlProject.Enabled = false;
            }

            txtProjectName.Text    = p["ProjectName"]    == DBNull.Value ? "" : p["ProjectName"].ToString();
            txtProjectManager.Text = p["ProjectManager"] == DBNull.Value ? "" : p["ProjectManager"].ToString();
            ddlActive.SelectedValue = (p["IsActive"] != DBNull.Value && Convert.ToBoolean(p["IsActive"])) ? "Yes" : "No";

            litCreatedInfo.Text = string.Format("Registered by {0} on {1}",
                Server.HtmlEncode(Convert.ToString(p["CreatedBy"])),
                p["CreatedDate"] == DBNull.Value ? "" : Convert.ToDateTime(p["CreatedDate"]).ToString("dd-MMM-yyyy HH:mm"));
            pnlCreatedInfo.Visible = true;

            LoadProjectDetails(isNonJira ? null : projectId);

            gvProjectPets.DataSource = WorkflowDAL.GetPetFormsDashboard(projectId, null, null, null, null);
            gvProjectPets.DataBind();
            pnlProjectPets.Visible = true;

            // Project Sizing (1 per project, editable)
            LoadProjectSizing(projectId);
        }

        private void LoadProjectSizing(string projectId)
        {
            pnlSizing.Visible = true;
            DataRow sz = ProjectDAL.GetProjectSizing(projectId);
            if (sz != null)
            {
                hfSzQ1.Value = sz["Q1Score"] == DBNull.Value ? "" : Convert.ToDecimal(sz["Q1Score"]).ToString("0");
                hfSzQ2.Value = sz["Q2Score"] == DBNull.Value ? "" : Convert.ToDecimal(sz["Q2Score"]).ToString("0");
                hfSzQ3.Value = sz["Q3Score"] == DBNull.Value ? "" : Convert.ToDecimal(sz["Q3Score"]).ToString("0");
                hfSzQ4.Value = sz["Q4Score"] == DBNull.Value ? "" : Convert.ToDecimal(sz["Q4Score"]).ToString("0");
                hfSzQ5.Value = sz["Q5Score"] == DBNull.Value ? "" : Convert.ToDecimal(sz["Q5Score"]).ToString("0");
                hfSzQ6.Value = sz["Q6Score"] == DBNull.Value ? "" : Convert.ToDecimal(sz["Q6Score"]).ToString("0");
                hfSzQ7.Value = sz["Q7Score"] == DBNull.Value ? "" : Convert.ToDecimal(sz["Q7Score"]).ToString("0");
                string sr = sz["SizeResult"] == DBNull.Value ? "" : sz["SizeResult"].ToString();
                litSizingBadge.Text = string.IsNullOrEmpty(sr) ? ""
                    : " <span class='ps-size-badge size-" + sr.ToLower() + "'>" + Server.HtmlEncode(sr) + "</span>";
                litSizingSavedInfo.Text = sz["ModifiedBy"] == DBNull.Value ? ""
                    : "<div style='font-size:.8em;color:#64748b;margin-bottom:10px;'>Last saved by " +
                      Server.HtmlEncode(Convert.ToString(sz["ModifiedBy"])) +
                      (sz["ModifiedDate"] == DBNull.Value ? "" : " on " + Convert.ToDateTime(sz["ModifiedDate"]).ToString("dd-MMM-yyyy HH:mm")) +
                      "</div>";
            }
        }

        protected void btnSizingSave_Click(object sender, EventArgs e)
        {
            if (!IsExistingProject) { ShowMsg("Save the project first."); return; }

            decimal q1, q2, q3, q4, q5, q6, q7;
            if (!decimal.TryParse(hfSzQ1.Value, out q1) || !decimal.TryParse(hfSzQ2.Value, out q2) ||
                !decimal.TryParse(hfSzQ3.Value, out q3) || !decimal.TryParse(hfSzQ4.Value, out q4) ||
                !decimal.TryParse(hfSzQ5.Value, out q5) || !decimal.TryParse(hfSzQ6.Value, out q6) ||
                !decimal.TryParse(hfSzQ7.Value, out q7))
            {
                ShowMsg("Please complete all 7 criteria."); return;
            }

            decimal weighted = q1 * 0.20m + q2 * 0.20m + q3 * 0.15m + q4 * 0.15m
                             + q5 * 0.15m + q6 * 0.10m + q7 * 0.05m;
            string sizeResult;
            if      (weighted <= 1.5m) sizeResult = "XS";
            else if (weighted <= 2.3m) sizeResult = "S";
            else if (weighted <= 3.2m) sizeResult = "M";
            else if (weighted <= 4.1m) sizeResult = "L";
            else                       sizeResult = "XL";

            string capacityMap;
            switch (sizeResult)
            {
                case "XS": capacityMap = "< 100 hrs";        break;
                case "S":  capacityMap = "100 - 500 hrs";    break;
                case "M":  capacityMap = "500 - 2,000 hrs";  break;
                case "L":  capacityMap = "2,000 - 5,000 hrs"; break;
                default:   capacityMap = "> 5,000 hrs";       break;
            }

            ProjectDAL.SaveProjectSizing(CurrentProjectId, q1, q2, q3, q4, q5, q6, q7,
                weighted, sizeResult, capacityMap, AuthHelper.CurrentUserShort);

            ShowNextStep("Sizing saved. Size: " + sizeResult + " (Score: " + weighted.ToString("F4") + "). Next: create or continue the Spend Request for this project.");
            LoadProjectSizing(CurrentProjectId);
        }

        private void ClearProjectForm()
        {
            CurrentProjectId = "";
            rblProjectMode.Enabled = true;
            rblProjectMode.SelectedValue = "JIRA";
            ddlProject.Enabled = true;
            if (ddlProject.Items.Count > 0) ddlProject.SelectedIndex = 0;
            txtNonJiraProjectId.Text = "";
            txtNonJiraProjectId.ReadOnly = false;
            txtProjectName.Text = "";
            txtProjectManager.Text = "";
            ddlActive.SelectedValue = "Yes";
            pnlCreatedInfo.Visible = false;
            pnlProjectDetails.Visible = false;
            pnlProjectPets.Visible = false;
            pnlSizing.Visible = false;
            pnlNoProject.Visible = true;
            litNoProjectMsg.Text = "Select a JIRA project (or enter a Non-JIRA Project ID) above to see its details here.";
            ApplyProjectModePanels();
        }

        private void ShowProjectModal()
        {
            if (InlineProjectModal)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "showProjectRegistrationInline",
                    "$(function(){ var m=$('#projectRegistrationModal'); m.removeClass('modal fade').addClass('project-modal-inline').css({display:'block',position:'static'}); m.find('.modal-dialog').css({width:'100%',maxWidth:'none',margin:'0'}); m.find('.modal-content').css({border:'0',boxShadow:'none'}); m.find('[data-dismiss=\"modal\"]').hide(); $('.modal-backdrop').remove(); $('body').removeClass('modal-open').css('padding-right',''); });", true);
            }
            else
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "showProjectRegistrationModal",
                    "$(function(){ $('#projectRegistrationModal').modal('show'); });", true);
            }
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
            return Html(projectId);
        }

        private void ShowMsg(string msg) { lblMsg.Text = msg; lblMsg.CssClass = "alert alert-info"; lblMsg.Visible = true; }

        private void ShowNextStep(string msg)
        {
            lblMsg.Text = msg;
            lblMsg.CssClass = "next-step-message";
            lblMsg.Visible = true;
            string safe = System.Web.HttpUtility.JavaScriptStringEncode(msg);
            string script = "(function(){var t=document.createElement('div');t.className='next-step-toast';t.innerHTML='" + safe + "';document.body.appendChild(t);setTimeout(function(){if(t&&t.parentNode)t.parentNode.removeChild(t);},7000);})();";
            ScriptManager.RegisterStartupScript(this, GetType(), "projectNextStepToast" + DateTime.Now.Ticks.ToString(), script, true);
        }
    }
}
