using System;
using System.Data;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using DFM_BPM.App_Code.DAL;
using DFM_BPM.App_Code.Helpers;

namespace DFM_BPM.Admin
{
    public partial class PortfolioHierarchy : Page
    {
        protected bool IsAdmin { get { return AuthHelper.IsAdmin; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindParentDropdown();
                ClearForm();
            }
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            BindTree();

            int selId;
            if (int.TryParse(hfSelectedResourceId.Value, out selId) && selId > 0)
                LoadSelectedProjects(selId);
            else
                pnlSelectedProjects.Visible = false;
        }

        private void BindParentDropdown()
        {
            string current = ddlParent.SelectedValue;
            DataTable dt = PortfolioDAL.GetResourceDropdown();
            ddlParent.DataSource     = dt;
            ddlParent.DataTextField  = "DisplayName";
            ddlParent.DataValueField = "ResourceID";
            ddlParent.DataBind();
            ddlParent.Items.Insert(0, new ListItem("-- Top Level (Root) --", ""));

            // A resource can't be its own parent.
            int editId;
            if (int.TryParse(hfEditResourceId.Value, out editId) && editId > 0)
            {
                var self = ddlParent.Items.FindByValue(editId.ToString());
                if (self != null) ddlParent.Items.Remove(self);
            }

            if (!string.IsNullOrEmpty(current) && ddlParent.Items.FindByValue(current) != null)
                ddlParent.SelectedValue = current;
        }

        private void BindTree()
        {
            DataTable dt = PortfolioDAL.GetResourceTree();
            litTree.Text = dt.Rows.Count == 0
                ? "<div class='alert alert-info'>No resources configured yet." +
                  (IsAdmin ? " Add the first one above." : "") + "</div>"
                : RenderTree(dt);
        }

        /// <summary>Builds a nested &lt;ul&gt;/&lt;li&gt; org-chart tree (BALKAN OrgChartJS style --
        /// top-down card nodes with CSS connector lines). First 3 levels are expanded on page load;
        /// deeper levels are collapsed with a toggle. Clicking a card name redirects to Dashboard
        /// filtered by that resource's projects.</summary>
        private string RenderTree(DataTable dt)
        {
            // Build parent -> children lookup
            var childMap = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<DataRow>>();
            var roots = new System.Collections.Generic.List<DataRow>();
            foreach (DataRow r in dt.Rows)
            {
                int id = Convert.ToInt32(r["ResourceID"]);
                if (!childMap.ContainsKey(id))
                    childMap[id] = new System.Collections.Generic.List<DataRow>();
            }
            foreach (DataRow r in dt.Rows)
            {
                if (r["ParentResourceID"] == DBNull.Value)
                    roots.Add(r);
                else
                {
                    int pid = Convert.ToInt32(r["ParentResourceID"]);
                    if (!childMap.ContainsKey(pid))
                        childMap[pid] = new System.Collections.Generic.List<DataRow>();
                    childMap[pid].Add(r);
                }
            }

            var sb = new StringBuilder();
            sb.Append("<ul>");
            foreach (DataRow root in roots)
                RenderNode(sb, root, childMap, 0);
            sb.Append("</ul>");
            return sb.ToString();
        }

        private void RenderNode(StringBuilder sb, DataRow r,
            System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<DataRow>> childMap, int level)
        {
            int id         = Convert.ToInt32(r["ResourceID"]);
            string rawName = Convert.ToString(r["ResourceName"]);
            string name    = Server.HtmlEncode(rawName);
            string title   = r["Title"] == DBNull.Value ? "" : Server.HtmlEncode(r["Title"].ToString());
            bool active    = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]);
            int projCount  = Convert.ToInt32(r["ProjectCount"]);
            bool hasPhoto  = r.Table.Columns.Contains("HasPhoto") && r["HasPhoto"] != DBNull.Value && Convert.ToBoolean(r["HasPhoto"]);
            var children   = childMap.ContainsKey(id) ? childMap[id] : new System.Collections.Generic.List<DataRow>();
            bool hasKids   = children.Count > 0;
            bool collapsed = level >= 3 && hasKids; // 3 levels shown by default

            string liClass = collapsed ? " class='oc-collapsed'" : "";
            sb.Append("<li").Append(liClass).Append(">");

            // Node card
            string levelCss = level <= 2 ? " oc-level-" + level : "";
            sb.Append("<div class='oc-node").Append(levelCss).Append(active ? "" : " oc-inactive")
              .Append("' onclick=\"pfAction('select',").Append(id).Append(")\">");

            // Avatar: uploaded photo if present, otherwise a fallback initials circle
            if (hasPhoto)
                sb.Append("<img class='oc-avatar' src='").Append(ResolveUrl("~/Admin/ResourcePhoto.ashx?id=" + id)).Append("' alt='' />");
            else
                sb.Append("<div class='oc-avatar oc-avatar-fallback'>").Append(Initials(rawName)).Append("</div>");

            sb.Append("<div class='oc-name'>").Append(name).Append("</div>");
            if (!string.IsNullOrEmpty(title))
                sb.Append("<div class='oc-title'>").Append(title).Append("</div>");
            if (projCount > 0)
                sb.Append("<span class='oc-badge'>").Append(projCount).Append(" project(s)</span>");
            if (!active)
                sb.Append("<span class='oc-badge' style='background:#fee2e2;color:#991b1b;'>Inactive</span>");

            if (IsAdmin)
            {
                sb.Append("<span class='oc-acts'>")
                  .Append("<a href='javascript:void(0)' onclick=\"event.stopPropagation();pfAction('edit',").Append(id).Append(")\" title='Edit'><i class='bi bi-pencil'></i></a>")
                  .Append("<a href='javascript:void(0)' onclick=\"event.stopPropagation();if(confirm('Delete?'))pfAction('delete',").Append(id).Append(")\" title='Delete'><i class='bi bi-trash'></i></a>")
                  .Append("</span>");
            }
            sb.Append("</div>"); // end .oc-node

            // Expand/collapse toggle for deep levels
            if (hasKids && level >= 3)
                sb.Append("<span class='oc-expand-btn' onclick=\"ocToggle(this,").Append(id).Append(")\">[ + Expand ]</span>");
            else if (hasKids && level >= 2)
                sb.Append("<span class='oc-expand-btn' onclick=\"ocToggle(this,").Append(id).Append(")\">[ - Collapse ]</span>");

            // Recurse into children -- wrapped in a "grouping" box (dashed border + "{Name}'s Team" label)
            // so siblings reporting to this same node read visually as one team, similar to the balkan.app demo.
            if (hasKids)
            {
                sb.Append("<ul class='oc-group' data-label=\"").Append(name).Append("&#39;s Team\">");
                foreach (DataRow child in children)
                    RenderNode(sb, child, childMap, level + 1);
                sb.Append("</ul>");
            }

            sb.Append("</li>");
        }

        /// <summary>1-2 letter initials fallback avatar (e.g. "Naveed Khan" -&gt; "NK") for resources without an
        /// uploaded photo.</summary>
        private static string Initials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            string[] parts = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
            return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpper();
        }

        private void LoadSelectedProjects(int resourceId)
        {
            DataRow r = PortfolioDAL.GetResourceById(resourceId);
            if (r == null) { pnlSelectedProjects.Visible = false; return; }

            litSelectedResourceName.Text = " " + Server.HtmlEncode(Convert.ToString(r["ResourceName"]));
            gvSelectedProjects.DataSource = PortfolioDAL.GetProjectsByResource(resourceId);
            gvSelectedProjects.DataBind();
            pnlSelectedProjects.Visible = true;
        }

        protected void btnDoAction_Click(object sender, EventArgs e)
        {
            int id;
            int.TryParse(hfActionId.Value, out id);

            switch (hfAction.Value)
            {
                case "select":
                    // Redirect to Default.aspx with the resource filter pre-applied so the project
                    // grid shows only this team member's projects (per requirement).
                    Response.Redirect("~/Default.aspx?resource=" + id);
                    break;

                case "edit":
                    if (IsAdmin) LoadResourceForEdit(id);
                    break;

                case "delete":
                    if (IsAdmin)
                    {
                        if (PortfolioDAL.HasChildrenOrProjects(id))
                            ShowMsg("Cannot delete: this resource still has child resources or assigned projects. Reassign those first.");
                        else
                        {
                            PortfolioDAL.DeleteResource(id);
                            ShowMsg("Resource deleted.");
                        }
                    }
                    break;
            }
        }

        private void LoadResourceForEdit(int resourceId)
        {
            DataRow r = PortfolioDAL.GetResourceById(resourceId);
            if (r == null) return;

            hfEditResourceId.Value = resourceId.ToString();
            txtResourceName.Text = r["ResourceName"] == DBNull.Value ? "" : r["ResourceName"].ToString();
            txtTitle.Text        = r["Title"]        == DBNull.Value ? "" : r["Title"].ToString();
            bool active = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]);
            ddlResourceActive.SelectedValue = active ? "Yes" : "No";

            BindParentDropdown();
            if (r["ParentResourceID"] != DBNull.Value)
            {
                string pid = r["ParentResourceID"].ToString();
                if (ddlParent.Items.FindByValue(pid) != null) ddlParent.SelectedValue = pid;
            }

            string photoContentType;
            byte[] photo = PortfolioDAL.GetResourcePhoto(resourceId, out photoContentType);
            if (photo != null && photo.Length > 0)
            {
                imgCurrentPhoto.ImageUrl = ResolveUrl("~/Admin/ResourcePhoto.ashx?id=" + resourceId);
                pnlCurrentPhoto.Visible = true;
            }
            else
            {
                pnlCurrentPhoto.Visible = false;
            }
        }

        protected void btnSaveResource_Click(object sender, EventArgs e)
        {
            if (!IsAdmin) return;
            string name = txtResourceName.Text.Trim();
            if (string.IsNullOrEmpty(name)) { ShowMsg("Resource Name is required."); return; }

            int? parentId = null;
            int pid;
            if (int.TryParse(ddlParent.SelectedValue, out pid) && pid > 0) parentId = pid;

            int editId;
            int.TryParse(hfEditResourceId.Value, out editId);

            try
            {
                int resourceId = PortfolioDAL.SaveResource(editId, name, txtTitle.Text.Trim(), parentId,
                    ddlResourceActive.SelectedValue == "Yes", AuthHelper.CurrentUserShort);

                // Photo is optional on every save — only touch it when a new file was actually chosen,
                // so re-saving other fields never wipes out a previously uploaded photo.
                if (fuPhoto.HasFile)
                {
                    string contentType = string.IsNullOrEmpty(fuPhoto.PostedFile.ContentType)
                        ? "image/jpeg" : fuPhoto.PostedFile.ContentType;
                    PortfolioDAL.SaveResourcePhoto(resourceId, fuPhoto.FileBytes, contentType);
                }

                ShowMsg("Resource saved.");
                ClearForm();
                BindParentDropdown();
            }
            catch (Exception ex) { ShowMsg("Error: " + ex.Message); }
        }

        protected void btnCancelResource_Click(object sender, EventArgs e)
        {
            ClearForm();
            BindParentDropdown();
        }

        private void ClearForm()
        {
            hfEditResourceId.Value = "0";
            txtResourceName.Text = "";
            txtTitle.Text = "";
            ddlResourceActive.SelectedValue = "Yes";
            ddlParent.SelectedValue = "";
            pnlCurrentPhoto.Visible = false;
        }

        private void ShowMsg(string msg) { lblMsg.Text = msg; lblMsg.Visible = true; }
    }
}
