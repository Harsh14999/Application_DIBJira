using System;
using System.Data;
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
            DataTable dt = ProjectDAL.GetProjects();
            gvProjectPortfolio.DataSource = dt;
            gvProjectPortfolio.DataBind();
            litProjectPortfolioCount.Text = dt.Rows.Count.ToString();
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

            ShowMsg("Sizing saved. Size: " + sizeResult + " (Score: " + weighted.ToString("F4") + ")");
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
            ScriptManager.RegisterStartupScript(this, GetType(), "showProjectRegistrationModal",
                "$(function(){ $('#projectRegistrationModal').modal('show'); });", true);
        }

        private void ShowMsg(string msg) { lblMsg.Text = msg; lblMsg.Visible = true; }
    }
}
