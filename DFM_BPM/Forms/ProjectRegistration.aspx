<%@ Page Title="Project Portfolio" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="ProjectRegistration.aspx.cs" Inherits="DFM_BPM.Forms.ProjectRegistration" %>

<asp:Content ID="HeadCt" ContentPlaceHolderID="HeadContent" runat="server">
<link href="<%= ResolveUrl("~/Content/bootstrap-icons.css") %>" rel="stylesheet" />
<link href="<%= ResolveUrl("~/Content/select2.min.css") %>" rel="stylesheet" />
<style>
.jira-detail-tbl td { border:1px solid #e5e7eb; padding:7px 12px; vertical-align:middle; font-size:.87em; }
.jira-detail-tbl td.lbl { background:#f8f9fa; font-weight:700; color:#374151; width:18%; white-space:nowrap; }
.jira-detail-tbl td.lbl i.bi { color:#2563eb; margin-right:5px; }
.jira-detail-tbl td.val { background:#fff; width:32%; }
.card-panel { border-radius:8px; overflow:hidden; border:1px solid #e2e8f0; margin-bottom:14px; }
.card-panel-hdr { padding:12px 14px; font-weight:700; font-size:.95em; display:flex; align-items:center; gap:8px; }
.card-panel-body { padding:14px; }
.card-panel.panel-spend-request { border-color:#B4C7E7; background:#F5F9FF; }
.card-panel.panel-spend-request .card-panel-hdr { background:#F5F9FF; color:#2F5597; border-bottom:2px solid #2F5597; }
.card-panel.panel-spend-request .dfm-table th { background:#2F5597 !important; color:#fff; }
.dfm-table th { background:#1e3a5f !important; color:#fff !important; font-size:.8em; padding:6px 8px; white-space:nowrap; }
.dfm-table td { font-size:.82em; padding:5px 8px; vertical-align:middle; }
.dfm-table tr:nth-child(odd)  td { background:#fafbff; }
.dfm-table tr:nth-child(even) td { background:#ffffff; }
.dfm-table tr:hover td { background:#eff6ff; }
.dash-section { margin-bottom:14px; border:1px solid #e2e8f0; border-radius:10px; overflow:hidden; background:#fff; }
.dash-sec-hdr { padding:10px 16px; cursor:pointer; display:flex; justify-content:space-between; align-items:center; font-weight:700; font-size:.9em; user-select:none; border-bottom:1px solid #e2e8f0; }
.dash-sec-body { padding:0; }
.dash-section.collapsed .dash-sec-body { display:none; }
.dash-section.collapsed .dash-sec-toggle { transform:rotate(-90deg); }
#sec-project-portfolio-filters .dash-sec-hdr { background:#f8fafc; color:#475569; }
#sec-project-portfolio .dash-sec-hdr { background:#F3F9FD; color:#1F4E78; }
.horizontal-filter-panel { padding:14px 16px; }
.filter-grid { display:flex; gap:10px; flex-wrap:wrap; align-items:flex-end; }
.filter-grid .form-group { flex:1; min-width:140px; margin:0; }
.filter-actions { display:flex; gap:8px; margin-top:10px; flex-wrap:wrap; }
.tree-hidden { display:none !important; }
.pet-status { display:inline-block; padding:2px 8px; border-radius:10px; font-size:.75em; font-weight:700; white-space:nowrap; }
.st-draft { background:#f1f5f9; color:#475569; }
.st-pending { background:#fef3c7; color:#92400e; }
.st-review { background:#dbeafe; color:#1d4ed8; }
.st-approved { background:#d1fae5; color:#065f46; }
.st-rejected { background:#fee2e2; color:#991b1b; }
.st-sent { background:#ede9fe; color:#5b21b6; }
.page-nav { display:flex; justify-content:center; gap:4px; padding:10px; }
.proj-action-btn { font-size:.75em; padding:2px 6px; margin-right:2px; border-radius:4px; font-weight:600; cursor:pointer; border:none; }
.proj-action-btn.btn-sr { background:#E8F0FE; color:#2F5597; border:1px solid #B4C7E7; }
.proj-action-btn.btn-bgt { background:#F0F9EC; color:#548235; border:1px solid #C6E0B4; }
.proj-action-btn.btn-inv { background:#FFF3E8; color:#C55A11; border:1px solid #F4B183; }
.card-panel.panel-budget-line-items { border-color:#C6E0B4; background:#fff; }
.card-panel.panel-budget-line-items .card-panel-hdr { background:#F6FBF4; color:#548235; border-bottom:2px solid #C6E0B4; }
.card-panel.panel-budget-invoice { border-color:#F4B183; background:#fff; }
.card-panel.panel-budget-invoice .card-panel-hdr { background:#FFF8F2; color:#C55A11; border-bottom:2px solid #F4B183; }
.action-modal-grid th { font-size:.8em; padding:6px 8px; white-space:nowrap; }
.action-modal-grid td { font-size:.82em; padding:5px 8px; vertical-align:middle; color:#1e293b; }
.del-pet-name { font-size:1em; font-weight:800; color:#dc2626; word-break:break-all; }
</style>
</asp:Content>

<asp:Content ID="MainCt" ContentPlaceHolderID="MainContent" runat="server">
<h1 class="page-title"><i class="bi bi-folder-plus"></i> Project Portfolio
    <% if (IsExistingProject) { %>
        &nbsp;<span style="font-size:.6em;color:#2563eb;"><%= Server.HtmlEncode(CurrentProjectId) %></span>
    <% } %>
    <% if (CanDeleteProject) { %>
    <span style="float:right;margin-top:4px;">
        <asp:Button ID="btnDeleteProject" runat="server" CssClass="btn btn-sm btn-danger"
            Text="Delete This Project" OnClientClick="$('#projectDelModal').modal('show');return false;"
            CausesValidation="false" />
    </span>
    <% } %>
</h1>

<asp:Label ID="lblMsg" runat="server" CssClass="alert alert-info" Visible="false" />
<asp:HiddenField ID="hfProjectId" runat="server" Value="" />

<!-- ── PROJECT PORTFOLIO FILTERS ── -->
<div class="dash-section" id="sec-project-portfolio-filters">
    <div class="dash-sec-hdr" onclick="ppSecTog('sec-project-portfolio-filters')">
        <span><i class="bi bi-funnel"></i> Portfolio Filters</span>
        <i class="bi bi-chevron-down dash-sec-toggle"></i>
    </div>
    <div class="dash-sec-body">
        <div class="horizontal-filter-panel">
            <div class="filter-grid">
                <div class="form-group">
                    <label>Project</label>
                    <asp:DropDownList ID="ddlPortfolioProjectFilter" runat="server" CssClass="form-control select2-enable"
                        AutoPostBack="true" OnSelectedIndexChanged="PortfolioFilter_Changed">
                        <asp:ListItem Text="All Projects" Value="ALL" />
                    </asp:DropDownList>
                </div>
                <div class="form-group">
                    <label>Type</label>
                    <asp:DropDownList ID="ddlPortfolioTypeFilter" runat="server" CssClass="form-control select2-enable"
                        AutoPostBack="true" OnSelectedIndexChanged="PortfolioFilter_Changed">
                        <asp:ListItem Text="All" Value="ALL" />
                        <asp:ListItem Text="CAPEX" Value="CAPEX" />
                        <asp:ListItem Text="OPEX" Value="OPEX" />
                    </asp:DropDownList>
                </div>
                <div class="form-group">
                    <label>Accountable Exec Lead</label>
                    <asp:DropDownList ID="ddlPortfolioAccountableExecLeadFilter" runat="server" CssClass="form-control select2-enable"
                        AutoPostBack="true" OnSelectedIndexChanged="PortfolioFilter_Changed">
                        <asp:ListItem Text="All Accountable Exec Leads" Value="ALL" />
                    </asp:DropDownList>
                </div>
                <div class="form-group">
                    <label>SME Lead</label>
                    <asp:DropDownList ID="ddlPortfolioSmeLeadFilter" runat="server" CssClass="form-control select2-enable"
                        AutoPostBack="true" OnSelectedIndexChanged="PortfolioFilter_Changed">
                        <asp:ListItem Text="All SME Leads" Value="ALL" />
                    </asp:DropDownList>
                </div>
                <div class="form-group">
                    <label>View</label>
                    <asp:DropDownList ID="ddlPortfolioViewFilter" runat="server" CssClass="form-control select2-enable"
                        AutoPostBack="true" OnSelectedIndexChanged="PortfolioFilter_Changed">
                        <asp:ListItem Text="Pending My Action" Value="MYAPPROVAL" />
                        <asp:ListItem Text="My Requests" Value="MYREQUESTS" />
                        <asp:ListItem Text="All Items" Value="ALL" />
                    </asp:DropDownList>
                </div>
                <div class="form-group">
                    <label>Status</label>
                    <asp:DropDownList ID="ddlPortfolioStatusFilter" runat="server" CssClass="form-control select2-enable"
                        AutoPostBack="true" OnSelectedIndexChanged="PortfolioFilter_Changed">
                        <asp:ListItem Text="All" Value="ALL" />
                        <asp:ListItem Text="Draft" Value="Draft" />
                        <asp:ListItem Text="Pending Review" Value="PendingReview" />
                        <asp:ListItem Text="Pending Approval" Value="PendingApproval" />
                        <asp:ListItem Text="Approved" Value="Approved" />
                        <asp:ListItem Text="Rejected" Value="Rejected" />
                        <asp:ListItem Text="Sent Back" Value="SentBack" />
                        <asp:ListItem Text="Deleted" Value="Deleted" />
                    </asp:DropDownList>
                </div>
                <div class="form-group">
                    <label>From Date</label>
                    <asp:TextBox ID="txtPortfolioFromDate" runat="server" CssClass="form-control" TextMode="Date" />
                </div>
                <div class="form-group">
                    <label>To Date</label>
                    <asp:TextBox ID="txtPortfolioToDate" runat="server" CssClass="form-control" TextMode="Date" />
                </div>
            </div>
            <div class="filter-actions">
                <asp:Button ID="btnPortfolioApply" runat="server" Text="Apply Filters" CssClass="btn btn-primary"
                    OnClick="PortfolioFilter_Changed" />
                <asp:Button ID="btnPortfolioReset" runat="server" Text="Reset" CssClass="btn btn-default"
                    OnClick="btnPortfolioReset_Click" CausesValidation="false" />
                <asp:Button ID="btnPortfolioExport" runat="server" Text="Export Excel" CssClass="btn btn-success"
                    OnClick="btnPortfolioExport_Click" />
            </div>
        </div>
    </div>
</div>

<!-- ── PROJECT PORTFOLIO LIST ── -->
<div class="dash-section" id="sec-project-portfolio">
    <div class="dash-sec-hdr" onclick="ppSecTog('sec-project-portfolio')">
        <span><i class="bi bi-folder2-open"></i> Registered Projects
            <span style="background:#93c5fd;color:#1e3a5f;border-radius:10px;padding:1px 8px;font-size:.8em;margin-left:6px;">
                <asp:Literal ID="litPortfolioProjectsCount" runat="server" Text="0" />
            </span>
        </span>
        <i class="bi bi-chevron-down dash-sec-toggle"></i>
    </div>
    <div class="dash-sec-body">
        <div style="padding:10px 14px;display:flex;gap:8px;flex-wrap:wrap;align-items:flex-end;">
            <div class="form-group" style="flex:2;min-width:200px;margin:0;">
                <label style="font-size:.78em;">Search (Name, ID, Lead, Manager, Requestor)</label>
                <asp:TextBox ID="txtPortfolioProjectSearch" runat="server" CssClass="form-control" placeholder="Type to filter..." />
            </div>
            <asp:Button ID="btnPortfolioProjectSearch" runat="server" CssClass="btn btn-primary btn-sm" Text="Filter"
                OnClick="btnPortfolioProjectSearch_Click" CausesValidation="false" />
            <asp:Button ID="btnPortfolioProjectSearchReset" runat="server" CssClass="btn btn-default btn-sm" Text="Reset"
                OnClick="btnPortfolioProjectSearchReset_Click" CausesValidation="false" />
        </div>
        <div class="card-panel" style="border-top:none;border-radius:0 0 8px 8px;margin:0;padding:0;overflow-x:auto;">
            <table class="dfm-table" style="width:100%;">
                <thead>
                    <tr>
                        <th style="width:210px;">Project / Request</th>
                        <th style="width:150px;">Code</th>
                        <th style="width:100px;">Status</th>
                        <th>Type</th>
                        <th>Budget Source</th>
                        <th class="text-right">Requested (AED)</th>
                        <th>Approver</th>
                        <th>Requestor</th>
                        <th>Submitted</th>
                        <th>Size</th>
                        <th>Lead</th>
                        <th>Action</th>
                    </tr>
                </thead>
                <tbody>
                    <asp:Literal ID="litPortfolioProjectTree" runat="server" />
                </tbody>
            </table>
            <div class="page-nav">
                <asp:LinkButton ID="btnPortfolioPrevPage" runat="server" CssClass="btn btn-default btn-sm"
                    Text="&#8249; Prev" OnClick="btnPortfolioPrevPage_Click" CausesValidation="false" />
                <asp:Literal ID="litPortfolioPageInfo" runat="server" />
                <asp:LinkButton ID="btnPortfolioNextPage" runat="server" CssClass="btn btn-default btn-sm"
                    Text="Next &#8250;" OnClick="btnPortfolioNextPage_Click" CausesValidation="false" />
            </div>
        </div>
    </div>
</div>

<div class="card-panel panel-spend-request">
    <div class="card-panel-hdr"><i class="bi bi-pencil-square"></i> Register / Edit Project</div>
    <div class="card-panel-body">
        <asp:Panel ID="pnlCreatedInfo" runat="server" Visible="false" style="margin-bottom:10px;font-size:.85em;color:#64748b;">
            <asp:Literal ID="litCreatedInfo" runat="server" />
        </asp:Panel>

        <table class="table jira-detail-tbl">
            <tbody>
                <tr>
                    <td class="lbl"><i class="bi bi-diagram-2"></i>Project Type</td>
                    <td class="val" colspan="3">
                        <asp:RadioButtonList ID="rblProjectMode" runat="server" RepeatDirection="Horizontal"
                            AutoPostBack="true" OnSelectedIndexChanged="rblProjectMode_Changed">
                            <asp:ListItem Text="JIRA Project" Value="JIRA" Selected="True" />
                            <asp:ListItem Text="Non-JIRA Project" Value="NONJIRA" />
                        </asp:RadioButtonList>
                        <small style="color:#64748b;display:block;">For Non-JIRA projects, Project Name/ID and the Portfolio assignment are entered manually.</small>
                    </td>
                </tr>
                <tr>
                    <td class="lbl"><i class="bi bi-key"></i>JIRA ID / Project ID <span style="color:#dc2626;">*</span></td>
                    <td class="val">
                        <asp:PlaceHolder ID="phJiraSelect" runat="server">
                            <asp:DropDownList ID="ddlProject" runat="server" CssClass="form-control select2-enable"
                                AutoPostBack="true" OnSelectedIndexChanged="ddlProject_Changed" />
                        </asp:PlaceHolder>
                        <asp:PlaceHolder ID="phNonJiraSelect" runat="server" Visible="false">
                            <asp:TextBox ID="txtNonJiraProjectId" runat="server" CssClass="form-control"
                                placeholder="Enter a free-text Project ID" />
                        </asp:PlaceHolder>
                    </td>
                    <td class="lbl"><i class="bi bi-file-earmark-text"></i>Project Name <span style="color:#dc2626;">*</span></td>
                    <td class="val">
                        <asp:TextBox ID="txtProjectName" runat="server" CssClass="form-control" ReadOnly="true" />
                    </td>
                </tr>
                <tr>
                    <td class="lbl"><i class="bi bi-person-vcard"></i>Project Manager</td>
                    <td class="val" colspan="3">
                        <asp:TextBox ID="txtProjectManager" runat="server" CssClass="form-control" ReadOnly="true" />
                    </td>
                </tr>
                <tr>
                    <td class="lbl"><i class="bi bi-diagram-3"></i>Assigned To (Hierarchy)</td>
                    <td class="val" colspan="3">
                        <div style="display:grid;grid-template-columns:repeat(4,1fr);gap:8px;">
                            <div>
                                <label style="font-size:.72em;color:#64748b;display:block;margin-bottom:2px;">Accountable Exec</label>
                                <asp:DropDownList ID="ddlHierExec" runat="server" CssClass="form-control select2-enable"
                                    AutoPostBack="true" OnSelectedIndexChanged="ddlHierExec_Changed" />
                            </div>
                            <div>
                                <label style="font-size:.72em;color:#64748b;display:block;margin-bottom:2px;">Accountable Exec Lead</label>
                                <asp:DropDownList ID="ddlHierExecLead" runat="server" CssClass="form-control select2-enable"
                                    AutoPostBack="true" OnSelectedIndexChanged="ddlHierExecLead_Changed" />
                            </div>
                            <div>
                                <label style="font-size:.72em;color:#64748b;display:block;margin-bottom:2px;">SME Lead</label>
                                <asp:DropDownList ID="ddlHierSmeLead" runat="server" CssClass="form-control select2-enable"
                                    AutoPostBack="true" OnSelectedIndexChanged="ddlHierSmeLead_Changed" />
                            </div>
                            <div>
                                <label style="font-size:.72em;color:#64748b;display:block;margin-bottom:2px;">Engineer <small>(optional, multiple allowed)</small></label>
                                <asp:ListBox ID="ddlHierEngineer" runat="server" CssClass="form-control select2-enable" SelectionMode="Multiple" />
                            </div>
                        </div>
                        <small style="color:#64748b;">Auto-filled from JIRA when available (still overridable). Manage entries via
                            <a href="<%= ResolveUrl("~/Admin/PortfolioHierarchy.aspx") %>" target="_blank">Portfolio Hierarchy</a> or
                            <a href="<%= ResolveUrl("~/Admin/EngineerMaster.aspx") %>" target="_blank">Engineer Master</a>.</small>
                    </td>
                </tr>
                <tr>
                    <td class="lbl"><i class="bi bi-toggle-on"></i>Status</td>
                    <td class="val">
                        <asp:DropDownList ID="ddlActive" runat="server" CssClass="form-control">
                            <asp:ListItem Text="Active" Value="Yes" />
                            <asp:ListItem Text="Inactive" Value="No" />
                        </asp:DropDownList>
                    </td>
                    <td class="lbl" colspan="2"></td>
                </tr>
            </tbody>
        </table>

        <asp:Button ID="btnSave" runat="server" CssClass="btn btn-primary" Text="Save Project Portfolio" OnClick="btnSave_Click" />
        <% if (IsExistingProject) { %>
        <a href='<%= ResolveUrl("~/Forms/PetWorkflow.aspx") %>?project=<%= Server.UrlEncode(CurrentProjectId) %>' class="btn btn-success">
            <i class="bi bi-plus-circle"></i> Create Spend Request for this Project
        </a>
        <% } %>
    </div>
</div>

<asp:Panel ID="pnlProjectDetails" runat="server" Visible="false">
<div class="card-panel panel-spend-request">
    <div class="card-panel-hdr"><i class="bi bi-folder2-open"></i> JIRA Project Snapshot</div>
    <div class="card-panel-body">
        <table class="table jira-detail-tbl mb-0">
            <tbody>
                <tr>
                    <td class="lbl"><i class="bi bi-lightning"></i>Project Type</td>
                    <td class="val"><asp:Literal ID="litJProjectType" runat="server" /></td>
                    <td class="lbl"><i class="bi bi-graph-up"></i>Stage</td>
                    <td class="val"><asp:Literal ID="litJStage" runat="server" /></td>
                </tr>
                <tr>
                    <td class="lbl"><i class="bi bi-circle-fill"></i>RAG Status</td>
                    <td class="val"><asp:Literal ID="litJRag" runat="server" /></td>
                    <td class="lbl"><i class="bi bi-building"></i>Department</td>
                    <td class="val"><asp:Literal ID="litJDept" runat="server" /></td>
                </tr>
                <tr>
                    <td class="lbl"><i class="bi bi-cpu"></i>Platform</td>
                    <td class="val"><asp:Literal ID="litJPlatform" runat="server" /></td>
                    <td class="lbl"><i class="bi bi-tools"></i>Tech Lead</td>
                    <td class="val"><asp:Literal ID="litJTech" runat="server" /></td>
                </tr>
                <tr>
                    <td class="lbl"><i class="bi bi-person-gear"></i>Accountable Exec</td>
                    <td class="val"><asp:Literal ID="litJAccExec" runat="server" /></td>
                    <td class="lbl"><i class="bi bi-person-check"></i>Accountable Exec Lead</td>
                    <td class="val"><asp:Literal ID="litJAccExecLead" runat="server" /></td>
                </tr>
                <tr>
                    <td class="lbl"><i class="bi bi-person-workspace"></i>SME Lead</td>
                    <td class="val"><asp:Literal ID="litJSmeLead" runat="server" /></td>
                    <td class="lbl"><i class="bi bi-check2-all"></i>Project Overall Status</td>
                    <td class="val"><asp:Literal ID="litJOverallStatus" runat="server" /></td>
                </tr>
            </tbody>
        </table>
    </div>
</div>
</asp:Panel>
<asp:Panel ID="pnlNoProject" runat="server">
    <div class="alert alert-info"><asp:Literal ID="litNoProjectMsg" runat="server" Text="Select a JIRA project (or enter a Non-JIRA Project ID) above to see its details here." /></div>
</asp:Panel>

<asp:Panel ID="pnlProjectPets" runat="server" Visible="false">
<div class="card-panel panel-spend-request">
    <div class="card-panel-hdr"><i class="bi bi-file-earmark-text"></i> Spend Requests for this Project</div>
    <div class="card-panel-body" style="padding:0;overflow-x:auto;">
        <asp:GridView ID="gvProjectPets" runat="server" AutoGenerateColumns="false"
            CssClass="dfm-table" GridLines="None" EmptyDataText="No Spend Requests raised yet for this project.">
            <Columns>
                <asp:BoundField DataField="PetRefNo"      HeaderText="Ref No" />
                <asp:BoundField DataField="CapexOpexType" HeaderText="Type" />
                <asp:BoundField DataField="Title"         HeaderText="Title" />
                <asp:BoundField DataField="Status"        HeaderText="Status" />
                <asp:BoundField DataField="CreatedBy"     HeaderText="Requestor" />
                <asp:BoundField DataField="CreatedDate"   HeaderText="Created" DataFormatString="{0:dd-MMM-yyyy}" />
                <asp:TemplateField HeaderText="Action">
                    <ItemTemplate>
                        <a href='<%# ResolveUrl("~/Forms/PetWorkflow.aspx") %>?id=<%# Eval("PetFormID") %>' class="btn btn-xs btn-primary"><i class="bi bi-arrow-right-circle"></i> Open</a>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>
</div>
</asp:Panel>

<!-- ── PROJECT SIZING (1 per project, editable any time) ── -->
<asp:Panel ID="pnlSizing" runat="server" Visible="false">
<div class="card-panel panel-spend-request">
    <div class="card-panel-hdr"><i class="bi bi-rulers"></i> Project Sizing
        <asp:Literal ID="litSizingBadge" runat="server" />
    </div>
    <div class="card-panel-body">
        <asp:Literal ID="litSizingSavedInfo" runat="server" />
        <div style="background:#f0f9ff;border:1px solid #bae6fd;border-radius:8px;padding:12px 16px;margin-bottom:14px;font-size:.84em;color:#0c4a6e;">
            <strong>Scoring Guide:</strong> Select Low (1), Medium (3) or High (5) for each criterion.
            Weighted total &rarr; Size: <strong>XS</strong> (&le;1.5) | <strong>S</strong> (1.5&ndash;2.3) | <strong>M</strong> (2.3&ndash;3.2) | <strong>L</strong> (3.2&ndash;4.1) | <strong>XL</strong> (&gt;4.1)
        </div>

        <div class="ps-card">
            <div class="ps-card-title">1. Technical / Service Complexity <span class="label label-default" style="font-size:.7em;">Weight: 20%</span></div>
            <div class="ps-radio-group">
                <div class="ps-radio-btn"><input type="radio" name="prsz_q1" id="prsz_q1_1" value="1" onchange="prszScore()" /><label for="prsz_q1_1" class="low">&#10003; Low (1)<br/><small>Existing platforms, proven tech</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q1" id="prsz_q1_3" value="3" onchange="prszScore()" /><label for="prsz_q1_3" class="medium">&#9888; Medium (3)<br/><small>Some custom design, limited novelty</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q1" id="prsz_q1_5" value="5" onchange="prszScore()" /><label for="prsz_q1_5" class="high">&#9940; High (5)<br/><small>New/unproven tech, complex integrations</small></label></div>
            </div>
        </div>
        <div class="ps-card">
            <div class="ps-card-title">2. Regulatory / Compliance / Security <span class="label label-default" style="font-size:.7em;">Weight: 20%</span></div>
            <div class="ps-radio-group">
                <div class="ps-radio-btn"><input type="radio" name="prsz_q2" id="prsz_q2_1" value="1" onchange="prszScore()" /><label for="prsz_q2_1" class="low">&#10003; Low (1)<br/><small>No regulated data, standard security</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q2" id="prsz_q2_3" value="3" onchange="prszScore()" /><label for="prsz_q2_3" class="medium">&#9888; Medium (3)<br/><small>Compliance / privacy requirements</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q2" id="prsz_q2_5" value="5" onchange="prszScore()" /><label for="prsz_q2_5" class="high">&#9940; High (5)<br/><small>High regulatory/audit exposure</small></label></div>
            </div>
        </div>
        <div class="ps-card">
            <div class="ps-card-title">3. Stakeholder Complexity <span class="label label-default" style="font-size:.7em;">Weight: 15%</span></div>
            <div class="ps-radio-group">
                <div class="ps-radio-btn"><input type="radio" name="prsz_q3" id="prsz_q3_1" value="1" onchange="prszScore()" /><label for="prsz_q3_1" class="low">&#10003; Low (1)<br/><small>Single business owner, aligned</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q3" id="prsz_q3_3" value="3" onchange="prszScore()" /><label for="prsz_q3_3" class="medium">&#9888; Medium (3)<br/><small>Multiple BUs, competing priorities</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q3" id="prsz_q3_5" value="5" onchange="prszScore()" /><label for="prsz_q3_5" class="high">&#9940; High (5)<br/><small>Many stakeholders, divergent interests</small></label></div>
            </div>
        </div>
        <div class="ps-card">
            <div class="ps-card-title">4. Resource / Capability Dependency <span class="label label-default" style="font-size:.7em;">Weight: 15%</span></div>
            <div class="ps-radio-group">
                <div class="ps-radio-btn"><input type="radio" name="prsz_q4" id="prsz_q4_1" value="1" onchange="prszScore()" /><label for="prsz_q4_1" class="low">&#10003; Low (1)<br/><small>Skills available internally</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q4" id="prsz_q4_3" value="3" onchange="prszScore()" /><label for="prsz_q4_3" class="medium">&#9888; Medium (3)<br/><small>Some external specialists</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q4" id="prsz_q4_5" value="5" onchange="prszScore()" /><label for="prsz_q4_5" class="high">&#9940; High (5)<br/><small>Critical niche skills, major hiring</small></label></div>
            </div>
        </div>
        <div class="ps-card">
            <div class="ps-card-title">5. Scale / Reliability / Performance <span class="label label-default" style="font-size:.7em;">Weight: 15%</span></div>
            <div class="ps-radio-group">
                <div class="ps-radio-btn"><input type="radio" name="prsz_q5" id="prsz_q5_1" value="1" onchange="prszScore()" /><label for="prsz_q5_1" class="low">&#10003; Low (1)<br/><small>Non-critical, degradation acceptable</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q5" id="prsz_q5_3" value="3" onchange="prszScore()" /><label for="prsz_q5_3" class="medium">&#9888; Medium (3)<br/><small>Normal production SLAs</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q5" id="prsz_q5_5" value="5" onchange="prszScore()" /><label for="prsz_q5_5" class="high">&#9940; High (5)<br/><small>Mission-critical, strict HA/SLA</small></label></div>
            </div>
        </div>
        <div class="ps-card">
            <div class="ps-card-title">6. Interdependencies / Portfolio <span class="label label-default" style="font-size:.7em;">Weight: 10%</span></div>
            <div class="ps-radio-group">
                <div class="ps-radio-btn"><input type="radio" name="prsz_q6" id="prsz_q6_1" value="1" onchange="prszScore()" /><label for="prsz_q6_1" class="low">&#10003; Low (1)<br/><small>Standalone, few dependencies</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q6" id="prsz_q6_3" value="3" onchange="prszScore()" /><label for="prsz_q6_3" class="medium">&#9888; Medium (3)<br/><small>Some upstream/downstream deps</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q6" id="prsz_q6_5" value="5" onchange="prszScore()" /><label for="prsz_q6_5" class="high">&#9940; High (5)<br/><small>Foundational, impacts many initiatives</small></label></div>
            </div>
        </div>
        <div class="ps-card">
            <div class="ps-card-title">7. Budget / Contract Complexity <span class="label label-default" style="font-size:.7em;">Weight: 5%</span></div>
            <div class="ps-radio-group">
                <div class="ps-radio-btn"><input type="radio" name="prsz_q7" id="prsz_q7_1" value="1" onchange="prszScore()" /><label for="prsz_q7_1" class="low">&#10003; Low (1)<br/><small>Small budget, simple procurement</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q7" id="prsz_q7_3" value="3" onchange="prszScore()" /><label for="prsz_q7_3" class="medium">&#9888; Medium (3)<br/><small>Multi-phase funding, complex terms</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q7" id="prsz_q7_5" value="5" onchange="prszScore()" /><label for="prsz_q7_5" class="high">&#9940; High (5)<br/><small>Large capital, strategic supplier</small></label></div>
            </div>
        </div>

        <div id="prszLiveResult" class="ps-result-panel">
            <div class="ps-result-badge" id="prszLiveBadge">--</div>
            <div id="prszLiveLabel" style="font-size:1em;font-weight:600;margin-bottom:6px;"></div>
            <div class="ps-score-bar"><div class="ps-score-fill" id="prszScoreFill" style="width:0%;background:#facc15;"></div></div>
            <div style="font-size:.8em;opacity:.8;" id="prszScoreText">Select all 7 criteria to see score</div>
        </div>

        <asp:HiddenField ID="hfSzQ1" runat="server" /><asp:HiddenField ID="hfSzQ2" runat="server" />
        <asp:HiddenField ID="hfSzQ3" runat="server" /><asp:HiddenField ID="hfSzQ4" runat="server" />
        <asp:HiddenField ID="hfSzQ5" runat="server" /><asp:HiddenField ID="hfSzQ6" runat="server" />
        <asp:HiddenField ID="hfSzQ7" runat="server" />

        <div style="margin-top:16px;display:flex;gap:10px;flex-wrap:wrap;align-items:center;">
            <asp:Button ID="btnSizingSave" runat="server" Text="Save Sizing" CssClass="btn btn-primary"
                OnClientClick="return prSzPreSave();" OnClick="btnSizingSave_Click" CausesValidation="false" />
            <button type="button" class="btn btn-default" onclick="prszClear(); return false;">
                <i class="bi bi-arrow-clockwise"></i> Reset
            </button>
        </div>
    </div>
</div>
</asp:Panel>

<script>
window.addEventListener('load', function() {
    // Pre-select from hidden fields (edit mode) so the previously-saved answers show when re-opening.
    for (var q = 1; q <= 7; q++) {
        var hf = document.getElementById('<%= hfSzQ1.ClientID %>'.replace('hfSzQ1', 'hfSzQ' + q));
        if (!hf || !hf.value) continue;
        var radios = document.getElementsByName('prsz_q' + q);
        for (var i = 0; i < radios.length; i++) { if (radios[i].value === hf.value) { radios[i].checked = true; break; } }
    }
    if (typeof prszScore === 'function') prszScore();

    // select2 for all dropdowns marked (JIRA picker + hierarchy cascading pickers)
    if (typeof jQuery !== 'undefined' && jQuery.fn && jQuery.fn.select2) {
        jQuery('.select2-enable').select2({ width: '100%' });
    }
});

var prszWeights = [0.20, 0.20, 0.15, 0.15, 0.15, 0.10, 0.05];
function prszScore() {
    var total = 0;
    for (var q = 1; q <= 7; q++) {
        var radios = document.getElementsByName('prsz_q' + q);
        var val = 0;
        for (var i = 0; i < radios.length; i++) { if (radios[i].checked) { val = parseFloat(radios[i].value); break; } }
        total += val * prszWeights[q - 1];
    }
    var res = document.getElementById('prszLiveResult');
    var badge = document.getElementById('prszLiveBadge');
    var lbl   = document.getElementById('prszLiveLabel');
    var fill  = document.getElementById('prszScoreFill');
    var txt   = document.getElementById('prszScoreText');
    if (!res || total === 0) return;
    res.className = 'ps-result-panel visible';
    txt.textContent = 'Total Weighted Score: ' + total.toFixed(2);
    fill.style.width = Math.min(100, Math.max(0, ((total - 1) / 4) * 100)) + '%';
    var size, cls, color;
    if (total <= 1.5)      { size='XS'; cls='ps-result-xs'; color='#22c55e'; }
    else if (total <= 2.3) { size='S';  cls='ps-result-s';  color='#4ade80'; }
    else if (total <= 3.2) { size='M';  cls='ps-result-m';  color='#facc15'; }
    else if (total <= 4.1) { size='L';  cls='ps-result-l';  color='#fb923c'; }
    else                   { size='XL'; cls='ps-result-xl'; color='#f87171'; }
    badge.textContent = size;
    lbl.textContent   = 'Project Size: ' + size;
    fill.style.background = color;
    res.className = 'ps-result-panel visible ' + cls;
}
function prszClear() {
    for (var q = 1; q <= 7; q++) {
        var radios = document.getElementsByName('prsz_q' + q);
        for (var i = 0; i < radios.length; i++) radios[i].checked = false;
    }
    var res = document.getElementById('prszLiveResult');
    if (res) res.className = 'ps-result-panel';
}
function prSzPreSave() {
    for (var q = 1; q <= 7; q++) {
        var radios = document.getElementsByName('prsz_q' + q); var val = '';
        for (var i = 0; i < radios.length; i++) { if (radios[i].checked) { val = radios[i].value; break; } }
        if (!val) { alert('Please select a rating (Low / Medium / High) for all 7 criteria before saving.'); return false; }
        var hf = document.getElementById('<%= hfSzQ1.ClientID %>'.replace('hfSzQ1', 'hfSzQ' + q));
        if (hf) hf.value = val;
    }
    return true;
}
</script>

<!-- Project Portfolio Action Hidden Fields + Buttons -->
<asp:HiddenField ID="hfPortfolioActionProjectId" runat="server" Value="" />
<asp:Button ID="btnPortfolioShowSpendRequests" runat="server" Text="_sr" style="display:none;"
    OnClick="btnPortfolioShowSpendRequests_Click" CausesValidation="false" />
<asp:Button ID="btnPortfolioShowBudget" runat="server" Text="_bgt" style="display:none;"
    OnClick="btnPortfolioShowBudget_Click" CausesValidation="false" />
<asp:Button ID="btnPortfolioShowInvoices" runat="server" Text="_inv" style="display:none;"
    OnClick="btnPortfolioShowInvoices_Click" CausesValidation="false" />

<div class="modal fade" id="portfolioSpendRequestModal" tabindex="-1" role="dialog">
    <div class="modal-dialog modal-lg" role="document" style="max-width:95%;width:95%;">
        <div class="modal-content">
            <div class="modal-header" style="background:#2F5597;color:#fff;">
                <button type="button" class="close" data-dismiss="modal" style="color:#fff;opacity:.8;">&times;</button>
                <h4 class="modal-title"><i class="bi bi-file-earmark-text"></i> Spend Requests &mdash; <asp:Literal ID="litPortfolioSRModalProject" runat="server" /></h4>
            </div>
            <div class="modal-body" style="padding:16px;max-height:75vh;overflow-y:auto;">
                <div class="card-panel panel-spend-request" style="margin-bottom:14px;">
                    <div class="card-panel-hdr"><i class="bi bi-file-earmark-text"></i> Spend Requests (including Draft)</div>
                    <div class="card-panel-body" style="padding:0;overflow-x:auto;">
                        <asp:GridView ID="gvPortfolioModalSpendRequests" runat="server" AutoGenerateColumns="false"
                            CssClass="dfm-table action-modal-grid" GridLines="None" EmptyDataText="No Spend Requests for this project.">
                            <Columns>
                                <asp:BoundField DataField="PetRefNo" HeaderText="Ref No" />
                                <asp:BoundField DataField="CapexOpexType" HeaderText="Type" />
                                <asp:BoundField DataField="Title" HeaderText="Title" />
                                <asp:BoundField DataField="Status" HeaderText="Status" />
                                <asp:BoundField DataField="CreatedBy" HeaderText="Requestor" />
                                <asp:BoundField DataField="CreatedDate" HeaderText="Created" DataFormatString="{0:dd-MMM-yyyy}" />
                                <asp:BoundField DataField="TotalRequestedAED" HeaderText="Requested (AED)" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                                <asp:TemplateField HeaderText="Action">
                                    <ItemTemplate>
                                        <a href='<%# ResolveUrl("~/Forms/PetWorkflow.aspx") + "?id=" + Eval("PetFormID") %>' class="btn btn-xs btn-primary"><i class="bi bi-arrow-right-circle"></i> Open</a>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
                <div class="card-panel panel-spend-request">
                    <div class="card-panel-hdr"><i class="bi bi-list-ul"></i> Spend Request Line Items (All)</div>
                    <div class="card-panel-body" style="padding:0;overflow-x:auto;">
                        <asp:GridView ID="gvPortfolioModalLineItems" runat="server" AutoGenerateColumns="false"
                            CssClass="dfm-table action-modal-grid" GridLines="None" EmptyDataText="No line items.">
                            <Columns>
                                <asp:BoundField DataField="PetRefNo" HeaderText="Request" />
                                <asp:BoundField DataField="SerialNo" HeaderText="#" ItemStyle-Width="30px" />
                                <asp:BoundField DataField="ExpHead" HeaderText="Head" />
                                <asp:BoundField DataField="Topic" HeaderText="Topic" />
                                <asp:BoundField DataField="VendorName" HeaderText="Vendor" />
                                <asp:BoundField DataField="CostType" HeaderText="Cost Type" />
                                <asp:BoundField DataField="Units" HeaderText="Units" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                                <asp:BoundField DataField="UnitPrice" HeaderText="Unit Price" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                                <asp:BoundField DataField="BaseCurrency" HeaderText="CCY" />
                                <asp:BoundField DataField="AmtFCY" HeaderText="FCY Amt" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                                <asp:BoundField DataField="FinalAmtLCY" HeaderText="Final AED" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
            </div>
            <div class="modal-footer"><button type="button" class="btn btn-default" data-dismiss="modal"><i class="bi bi-x-lg"></i> Close</button></div>
        </div>
    </div>
</div>

<div class="modal fade" id="portfolioBudgetActionModal" tabindex="-1" role="dialog">
    <div class="modal-dialog modal-lg" role="document" style="max-width:95%;width:95%;">
        <div class="modal-content">
            <div class="modal-header" style="background:#548235;color:#fff;">
                <button type="button" class="close" data-dismiss="modal" style="color:#fff;opacity:.8;">&times;</button>
                <h4 class="modal-title"><i class="bi bi-cash-coin"></i> Budget &mdash; <asp:Literal ID="litPortfolioBgtModalProject" runat="server" /></h4>
            </div>
            <div class="modal-body" style="padding:16px;max-height:75vh;overflow-y:auto;">
                <div class="card-panel panel-budget-line-items" style="margin-bottom:14px;">
                    <div class="card-panel-hdr"><i class="bi bi-cash-coin"></i> Budget Line Items</div>
                    <div class="card-panel-body" style="padding:0;overflow-x:auto;">
                        <asp:GridView ID="gvPortfolioModalBudgetLines" runat="server" AutoGenerateColumns="false"
                            CssClass="dfm-table action-modal-grid" GridLines="None" DataKeyNames="BudgetLineID"
                            OnRowCommand="gvPortfolioModalBudgetLines_RowCommand"
                            EmptyDataText="No budget lines for this project.">
                            <Columns>
                                <asp:BoundField DataField="PetRefNo" HeaderText="Request Ref" />
                                <asp:BoundField DataField="VendorName" HeaderText="Vendor" />
                                <asp:BoundField DataField="Justification" HeaderText="Justification" />
                                <asp:BoundField DataField="Cost" HeaderText="Cost" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                                <asp:BoundField DataField="Currency" HeaderText="CCY" />
                                <asp:BoundField DataField="CamStatus" HeaderText="CAM Status" />
                                <asp:BoundField DataField="LpoStatus" HeaderText="LPO Status" />
                                <asp:BoundField DataField="InvoiceTotal" HeaderText="Invoiced" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                                <asp:TemplateField HeaderText="Invoices">
                                    <ItemTemplate>
                                        <asp:LinkButton runat="server" CssClass="btn btn-xs btn-info" CommandName="ShowInvoice" CommandArgument='<%# Eval("BudgetLineID") %>'><i class="bi bi-receipt"></i> Invoice</asp:LinkButton>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
                <asp:Panel ID="pnlPortfolioBudgetInvoiceDetail" runat="server" Visible="false">
                <div class="card-panel panel-budget-invoice">
                    <div class="card-panel-hdr"><i class="bi bi-receipt"></i> Invoices for Budget Line #<asp:Literal ID="litPortfolioBgtInvLineId" runat="server" /></div>
                    <div class="card-panel-body" style="padding:0;overflow-x:auto;">
                        <asp:GridView ID="gvPortfolioModalBudgetInvoices" runat="server" AutoGenerateColumns="false"
                            CssClass="dfm-table action-modal-grid" GridLines="None" EmptyDataText="No invoices for this budget line.">
                            <Columns>
                                <asp:BoundField DataField="InvoiceID" HeaderText="ID" />
                                <asp:BoundField DataField="InvoiceNo" HeaderText="Invoice No" />
                                <asp:BoundField DataField="InvoiceAmount" HeaderText="Amount" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                                <asp:BoundField DataField="InvoiceStatus" HeaderText="Status" />
                                <asp:BoundField DataField="PaymentDate" HeaderText="Payment Date" DataFormatString="{0:dd-MMM-yyyy}" />
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
                </asp:Panel>
            </div>
            <div class="modal-footer"><button type="button" class="btn btn-default" data-dismiss="modal"><i class="bi bi-x-lg"></i> Close</button></div>
        </div>
    </div>
</div>

<div class="modal fade" id="portfolioInvoiceActionModal" tabindex="-1" role="dialog">
    <div class="modal-dialog modal-lg" role="document" style="max-width:90%;width:90%;">
        <div class="modal-content">
            <div class="modal-header" style="background:#C55A11;color:#fff;">
                <button type="button" class="close" data-dismiss="modal" style="color:#fff;opacity:.8;">&times;</button>
                <h4 class="modal-title"><i class="bi bi-receipt"></i> Invoices &mdash; <asp:Literal ID="litPortfolioInvModalProject" runat="server" /></h4>
            </div>
            <div class="modal-body" style="padding:16px;max-height:75vh;overflow-y:auto;">
                <div class="card-panel panel-budget-invoice">
                    <div class="card-panel-hdr"><i class="bi bi-receipt"></i> All Invoices for this Project</div>
                    <div class="card-panel-body" style="padding:0;overflow-x:auto;">
                        <asp:GridView ID="gvPortfolioModalInvoices" runat="server" AutoGenerateColumns="false"
                            CssClass="dfm-table action-modal-grid" GridLines="None" EmptyDataText="No invoices for this project.">
                            <Columns>
                                <asp:BoundField DataField="PetRefNo" HeaderText="Request Ref" />
                                <asp:BoundField DataField="VendorName" HeaderText="Vendor" />
                                <asp:BoundField DataField="InvoiceNo" HeaderText="Invoice No" />
                                <asp:BoundField DataField="InvoiceAmount" HeaderText="Amount" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                                <asp:BoundField DataField="InvoiceStatus" HeaderText="Status" />
                                <asp:BoundField DataField="PaymentDate" HeaderText="Payment Date" DataFormatString="{0:dd-MMM-yyyy}" />
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
            </div>
            <div class="modal-footer"><button type="button" class="btn btn-default" data-dismiss="modal"><i class="bi bi-x-lg"></i> Close</button></div>
        </div>
    </div>
</div>

<asp:HiddenField ID="hfPortfolioDeletePetId" runat="server" Value="0" />
<asp:Button ID="btnPortfolioConfirmDeletePet" runat="server" Text="_del" style="display:none;"
    OnClick="btnPortfolioConfirmDeletePet_Click" CausesValidation="false" />

<div class="modal fade" id="portfolioPetDelModal" tabindex="-1" role="dialog">
    <div class="modal-dialog" role="document" style="max-width:460px;">
        <div class="modal-content">
            <div class="modal-header" style="background:#b91c1c;color:#fff;">
                <button type="button" class="close" data-dismiss="modal" style="color:#fff;opacity:.8;">&times;</button>
                <h4 class="modal-title"><i class="bi bi-exclamation-triangle-fill"></i> Confirm Delete Spend Request</h4>
            </div>
            <div class="modal-body" style="padding:24px;text-align:center;">
                <div style="font-size:2.8em;color:#dc2626;margin-bottom:10px;"><i class="bi bi-trash3-fill"></i></div>
                <p style="font-size:.94em;font-weight:600;color:#1e293b;margin-bottom:4px;">Are you sure you want to delete Spend Request</p>
                <p id="portfolioPetDelRefNo" class="del-pet-name"></p>
                <p style="font-size:.82em;color:#64748b;margin-top:8px;">The Spend Request will be marked as <strong>Deleted</strong> and removed from all views. Workflow history is retained.</p>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-default" data-dismiss="modal"><i class="bi bi-x-lg"></i> Cancel</button>
                <button type="button" class="btn btn-danger" onclick="ppPetDelConfirm();"><i class="bi bi-trash"></i> Yes, Delete</button>
            </div>
        </div>
    </div>
</div>

<script>
function ppSecTog(id) {
    var el = document.getElementById(id);
    if (el) el.classList.toggle('collapsed');
}
function ppTog(cls) {
    var rows = document.querySelectorAll('.tree-row.' + cls);
    var hidden = rows.length > 0 && rows[0].classList.contains('tree-hidden');
    for (var i = 0; i < rows.length; i++) {
        if (hidden) rows[i].classList.remove('tree-hidden');
        else rows[i].classList.add('tree-hidden');
    }
    var btns = document.querySelectorAll('[data-tog="' + cls + '"]');
    for (var j = 0; j < btns.length; j++) btns[j].innerHTML = hidden ? '&#9660;' : '&#9658;';
}
function ppPetDel(id, refNo) {
    document.getElementById('<%= hfPortfolioDeletePetId.ClientID %>').value = id;
    document.getElementById('portfolioPetDelRefNo').textContent = refNo;
    jQuery('#portfolioPetDelModal').modal('show');
}
function ppPetDelConfirm() {
    jQuery('#portfolioPetDelModal').modal('hide');
    document.getElementById('<%= btnPortfolioConfirmDeletePet.ClientID %>').click();
}
function ppShowSR(projId) {
    document.getElementById('<%= hfPortfolioActionProjectId.ClientID %>').value = projId;
    document.getElementById('<%= btnPortfolioShowSpendRequests.ClientID %>').click();
}
function ppShowBgt(projId) {
    document.getElementById('<%= hfPortfolioActionProjectId.ClientID %>').value = projId;
    document.getElementById('<%= btnPortfolioShowBudget.ClientID %>').click();
}
function ppShowInv(projId) {
    document.getElementById('<%= hfPortfolioActionProjectId.ClientID %>').value = projId;
    document.getElementById('<%= btnPortfolioShowInvoices.ClientID %>').click();
}
</script>

<!-- Project Delete Confirmation Modal -->
<div class="modal fade" id="projectDelModal" tabindex="-1" role="dialog">
    <div class="modal-dialog" role="document" style="max-width:460px;">
        <div class="modal-content">
            <div class="modal-header" style="background:#b91c1c;color:#fff;">
                <button type="button" class="close" data-dismiss="modal" style="color:#fff;opacity:.8;">&times;</button>
                <h4 class="modal-title"><i class="bi bi-exclamation-triangle-fill"></i> Confirm Delete Project</h4>
            </div>
            <div class="modal-body" style="padding:24px;text-align:center;">
                <div style="font-size:2.8em;color:#dc2626;margin-bottom:10px;"><i class="bi bi-trash3-fill"></i></div>
                <p style="font-size:.94em;font-weight:600;color:#1e293b;margin-bottom:4px;">Are you sure you want to delete</p>
                <p style="font-size:1.1em;font-weight:800;color:#dc2626;"><%= Server.HtmlEncode(CurrentProjectId) %></p>
                <p style="font-size:.82em;color:#64748b;margin-top:8px;">This permanently removes the project portfolio record, its Sizing record and Engineer assignments. This cannot be undone.</p>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-default" data-dismiss="modal"><i class="bi bi-x-lg"></i> Cancel</button>
                <asp:Button ID="btnConfirmDeleteProject" runat="server" CssClass="btn btn-danger"
                    Text="Yes, Delete" OnClick="btnDeleteProject_Click" CausesValidation="false" />
            </div>
        </div>
    </div>
</div>

</asp:Content>
