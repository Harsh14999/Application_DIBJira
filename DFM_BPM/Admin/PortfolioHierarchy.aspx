<%@ Page Title="Portfolio Hierarchy" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="PortfolioHierarchy.aspx.cs" Inherits="DFM_BPM.Admin.PortfolioHierarchy" %>

<asp:Content ID="HeadCt" ContentPlaceHolderID="HeadContent" runat="server">
<style>
.card-panel { border-radius:8px; overflow:hidden; border:1px solid #e2e8f0; margin-bottom:14px; }
.card-panel-hdr { padding:12px 14px; font-weight:700; font-size:.95em; display:flex; align-items:center; gap:8px; }
.card-panel-body { padding:14px; }
.dfm-table th { background:#1e3a5f !important; color:#fff !important; font-size:.8em; padding:6px 8px; white-space:nowrap; }
.dfm-table td { font-size:.82em; padding:5px 8px; vertical-align:middle; }
.dfm-table tr:nth-child(odd)  td { background:#fafbff; }
.dfm-table tr:nth-child(even) td { background:#ffffff; }

/* ---- Org Chart (BALKAN-style top-down tree with connector lines) ---- */
.org-chart { overflow-x:auto; padding:20px 0; }
.org-chart ul { display:flex; justify-content:center; padding-top:20px; position:relative; transition:all .3s; list-style:none; margin:0; padding-left:0; }
.org-chart ul ul { padding-top:20px; }
.org-chart li { display:flex; flex-direction:column; align-items:center; position:relative; padding:20px 8px 0; transition:all .3s; }

/* Vertical connector from parent down */
.org-chart li::before, .org-chart li::after { content:''; position:absolute; top:0; border-top:2px solid #93c5fd; width:50%; }
.org-chart li::before { right:50%; border-right:2px solid #93c5fd; }
.org-chart li::after  { left:50%;  border-left:2px solid #93c5fd; }
.org-chart li:only-child::before, .org-chart li:only-child::after { display:none; }
.org-chart li:only-child { padding-top:0; }
.org-chart li:first-child::before, .org-chart li:last-child::after { border:0 none; }
.org-chart li:last-child::before  { border-right:2px solid #93c5fd; border-radius:0 5px 0 0; }
.org-chart li:first-child::after  { border-left:2px solid #93c5fd;  border-radius:5px 0 0 0; }

/* Vertical line down from node to children */
.org-chart ul > li:not(:only-child)::before { border-top:2px solid #93c5fd; }
.org-chart > ul > li { padding-top:0; }
.org-chart > ul > li::before, .org-chart > ul > li::after { display:none; }

/* Node Card */
.oc-node { position:relative; display:inline-block; border:2px solid #93c5fd; border-radius:10px; padding:10px 16px;
           background:#fff; box-shadow:0 3px 12px rgba(37,99,235,.08); text-align:center; min-width:140px;
           cursor:pointer; transition:all .2s ease; margin-bottom:2px; }
.oc-node:hover { transform:translateY(-2px); box-shadow:0 6px 20px rgba(37,99,235,.15); border-color:#2563eb; }
.oc-node.oc-level-0 { background:linear-gradient(135deg,#1e3a5f,#2563eb); border-color:#1e3a5f; }
.oc-node.oc-level-0 .oc-name, .oc-node.oc-level-0 .oc-title { color:#fff; }
.oc-node.oc-level-0 .oc-badge { background:rgba(255,255,255,.2); color:#fff; }
.oc-node.oc-level-1 { background:linear-gradient(135deg,#dbeafe,#eff6ff); border-color:#60a5fa; }
.oc-node.oc-level-2 { background:linear-gradient(135deg,#d1fae5,#ecfdf5); border-color:#34d399; }
.oc-node.oc-inactive { opacity:.5; }
.oc-name { font-weight:800; font-size:.88em; color:#1e3a5f; margin-bottom:2px; }
.oc-title { font-size:.72em; color:#64748b; }
.oc-badge { display:inline-block; font-size:.65em; background:#e0f2fe; color:#0369a1; padding:1px 6px; border-radius:8px; margin-top:3px; }
.oc-acts { position:absolute; top:3px; right:5px; display:none; }
.oc-node:hover .oc-acts { display:block; }
.oc-acts a { color:#94a3b8; font-size:.8em; margin-left:2px; }
.oc-acts a:hover { color:#dc2626; }

/* Vertical line down from node to children -- MUST be position:absolute (not display:block), because the
   parent <ul> is display:flex and a non-absolute pseudo-element would be promoted to a flex item and laid
   out inline alongside the <li> children instead of rendering as a centered vertical connector above them. */
.org-chart li > ul { position:relative; }
.org-chart li > ul::before { content:''; position:absolute; top:0; left:50%; margin-left:-1px; width:2px; height:20px; background:#93c5fd; z-index:1; }

/* ---- Grouping: dashed box + "{Name}'s Team" label around the set of siblings reporting to the same node ---- */
.org-chart ul.oc-group { border:1px dashed #93c5fd; border-radius:12px; padding:26px 14px 8px; margin:6px 6px 0; background:rgba(239,246,255,.35); }
.org-chart ul.oc-group::after {
    content: attr(data-label); position:absolute; top:-10px; left:50%; transform:translateX(-50%);
    background:#eff6ff; color:#2563eb; font-size:.68em; font-weight:700; padding:2px 10px;
    border-radius:10px; border:1px solid #93c5fd; white-space:nowrap; z-index:2;
}

/* ---- Avatar (uploaded photo or initials fallback) ---- */
.oc-avatar { width:38px; height:38px; border-radius:50%; object-fit:cover; display:block; margin:0 auto 6px;
             border:2px solid #fff; box-shadow:0 0 0 1px #93c5fd; }
.oc-avatar-fallback { display:flex; align-items:center; justify-content:center; background:#dbeafe; color:#1e3a5f;
                      font-weight:800; font-size:.78em; }
.oc-node.oc-level-0 .oc-avatar-fallback { background:rgba(255,255,255,.25); color:#fff; box-shadow:0 0 0 1px rgba(255,255,255,.6); }

/* Collapsed children */
.oc-collapsed > ul { display:none !important; }
.oc-expand-btn { display:inline-block; margin-top:4px; font-size:.7em; color:#2563eb; cursor:pointer; user-select:none; }
.oc-expand-btn:hover { text-decoration:underline; }
</style>
</asp:Content>

<asp:Content ID="MainCt" ContentPlaceHolderID="MainContent" runat="server">
<h1 class="page-title"><i class="bi bi-diagram-3"></i> Portfolio Hierarchy
    <small style="font-size:.55em;color:#64748b;font-weight:400;">Reporting hierarchy used to assign Project ownership</small>
</h1>
<asp:Label ID="lblMsg" runat="server" CssClass="alert-info" Visible="false" />

<asp:HiddenField ID="hfAction" runat="server" Value="" />
<asp:HiddenField ID="hfActionId" runat="server" Value="0" />
<asp:HiddenField ID="hfSelectedResourceId" runat="server" Value="0" />
<asp:Button ID="btnDoAction" runat="server" style="display:none;" OnClick="btnDoAction_Click" CausesValidation="false" Text="_do" />

<% if (IsAdmin) { %>
<div class="card-panel">
    <div class="card-panel-hdr"><i class="bi bi-pencil-square"></i> Add / Edit Resource</div>
    <div class="card-panel-body">
        <asp:HiddenField ID="hfEditResourceId" runat="server" Value="0" />
        <div class="form-grid-4">
            <div class="form-group"><label>Resource Name *</label><asp:TextBox ID="txtResourceName" runat="server" CssClass="form-control" /></div>
            <div class="form-group"><label>Title / Role</label><asp:TextBox ID="txtTitle" runat="server" CssClass="form-control" /></div>
            <div class="form-group"><label>Reports Under</label>
                <asp:DropDownList ID="ddlParent" runat="server" CssClass="form-control select2-enable" /></div>
            <div class="form-group"><label>Active</label>
                <asp:DropDownList ID="ddlResourceActive" runat="server" CssClass="form-control">
                    <asp:ListItem Value="Yes">Yes</asp:ListItem><asp:ListItem Value="No">No</asp:ListItem></asp:DropDownList></div>
        </div>
        <div class="form-grid-4">
            <div class="form-group">
                <label>Photo</label>
                <asp:FileUpload ID="fuPhoto" runat="server" CssClass="form-control" />
                <small style="color:#64748b;">JPG/PNG, shown as the avatar on the org chart card.</small>
            </div>
            <asp:Panel ID="pnlCurrentPhoto" runat="server" Visible="false" CssClass="form-group">
                <label>Current Photo</label><br />
                <asp:Image ID="imgCurrentPhoto" runat="server" style="width:50px;height:50px;border-radius:50%;object-fit:cover;border:1px solid #cbd5e1;" />
            </asp:Panel>
        </div>
        <asp:Button ID="btnSaveResource" runat="server" CssClass="btn btn-primary" Text="Save Resource" OnClick="btnSaveResource_Click" />
        <asp:Button ID="btnCancelResource" runat="server" CssClass="btn btn-default" Text="Cancel" CausesValidation="false" OnClick="btnCancelResource_Click" />
    </div>
</div>
<% } %>

<div class="card-panel">
    <div class="card-panel-hdr"><i class="bi bi-diagram-3"></i> Organisation Chart
        <small style="font-weight:400;color:#64748b;margin-left:8px;">Click a card to view their projects on the Dashboard</small>
    </div>
    <div class="card-panel-body">
        <div class="org-chart">
            <asp:Literal ID="litTree" runat="server" />
        </div>
    </div>
</div>

<asp:Panel ID="pnlSelectedProjects" runat="server" Visible="false">
<div class="card-panel">
    <div class="card-panel-hdr"><i class="bi bi-folder2-open"></i> Projects assigned to
        <asp:Literal ID="litSelectedResourceName" runat="server" />
    </div>
    <div class="card-panel-body" style="padding:0;overflow-x:auto;">
        <asp:GridView ID="gvSelectedProjects" runat="server" AutoGenerateColumns="false"
            CssClass="dfm-table" GridLines="None" EmptyDataText="No projects assigned to this resource yet.">
            <Columns>
                <asp:BoundField DataField="ProjectID"   HeaderText="Project" />
                <asp:BoundField DataField="ProjectName" HeaderText="Project Name" />
                <asp:TemplateField HeaderText="Type">
                    <ItemTemplate><%# Convert.ToBoolean(Eval("IsNonJiraProject")) ? "Non-JIRA" : "JIRA" %></ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="ProjectManager" HeaderText="Project Manager" />
                <asp:TemplateField HeaderText="Action">
                    <ItemTemplate>
                        <a href='<%# ResolveUrl("~/Forms/ProjectRegistration.aspx") %>?pid=<%# Server.UrlEncode(Eval("ProjectID").ToString()) %>' class="btn btn-xs btn-primary"><i class="bi bi-arrow-right-circle"></i> Open</a>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>
</div>
</asp:Panel>

<script>
function pfAction(action, id) {
    document.getElementById('<%= hfAction.ClientID %>').value = action;
    document.getElementById('<%= hfActionId.ClientID %>').value = id;
    document.getElementById('<%= btnDoAction.ClientID %>').click();
}
// Expand/collapse child nodes
function ocToggle(el, id) {
    var li = el.closest('li');
    if (!li) return;
    li.classList.toggle('oc-collapsed');
    var isCollapsed = li.classList.contains('oc-collapsed');
    el.textContent = isCollapsed ? '[ + Expand ]' : '[ - Collapse ]';
}
</script>

</asp:Content>
