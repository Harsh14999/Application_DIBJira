<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="DFM_BPM.DefaultPage" %>

<asp:Content ID="HeadCt" ContentPlaceHolderID="HeadContent" runat="server">
<style>
/* ── sec toggle ── */
.dash-section { margin-bottom:14px; border:1px solid #e2e8f0; border-radius:10px; overflow:hidden; background:#fff; }
.dash-sec-hdr  { padding:10px 16px;
                 cursor:pointer; display:flex; justify-content:space-between; align-items:center;
                 font-weight:700; font-size:.9em; user-select:none; border-bottom:1px solid #e2e8f0; }
.dash-sec-toggle { transition:transform .2s; }
.dash-sec-body { padding:0; }
.dash-section.collapsed .dash-sec-body { display:none; }
.dash-section.collapsed .dash-sec-toggle { transform:rotate(-90deg); }
/* Section header colors — light bg, dark colorful text */
#sec-filters .dash-sec-hdr  { background:#f8fafc; color:#475569; }
#sec-projects .dash-sec-hdr { background:#F3F9FD; color:#1F4E78; }
#sec-pending .dash-sec-hdr  { background:#F5F9FF; color:#2F5597; }
#sec-myforms .dash-sec-hdr  { background:#F5F9FF; color:#2F5597; }
#sec-budget .dash-sec-hdr   { background:#F6FBF4; color:#548235; }
/* ── filter ── */
.horizontal-filter-panel { padding:14px 16px; }
.filter-grid { display:flex; gap:10px; flex-wrap:wrap; align-items:flex-end; }
.filter-grid .form-group { flex:1; min-width:140px; margin:0; }
.filter-actions { display:flex; gap:8px; margin-top:10px; flex-wrap:wrap; }
/* ── KPI ── */
.kpi-row { display:flex; gap:10px; flex-wrap:wrap; padding:14px 0 4px; }
.kpi-card { flex:1; min-width:130px; background:#fff; border:1px solid #e2e8f0; border-radius:10px;
            padding:14px; display:flex; align-items:center; gap:12px; }
.kpi-card .kpi-icon { font-size:1.8em; }
.kpi-label { font-size:.72em; font-weight:700; text-transform:uppercase; color:#64748b; }
.kpi-val   { font-size:1.2em; font-weight:900; color:#1a3c5e; }
.kpi-blue .kpi-icon   { color:#2563eb; } .kpi-green .kpi-icon { color:#059669; }
.kpi-orange .kpi-icon { color:#ea580c; } .kpi-red .kpi-icon   { color:#dc2626; }
.kpi-teal .kpi-icon   { color:#0891b2; } .kpi-slate .kpi-icon { color:#475569; }
/* ── pending tree ── */
.tree-item { padding:7px 12px; border-bottom:1px solid #f1f5f9; display:flex; gap:8px; align-items:center; font-size:.86em; }
.tree-item:last-child { border-bottom:none; }
.tree-item:hover { background:#f8fafc; }
.project-parent-row { background:#fff; }
.project-parent-row:hover { background:#f8fafc; }
.project-toggle { cursor:pointer; color:#2563eb; font-size:1.05em; margin-right:6px; }
.project-child-row td { background:#f8fafc; border-top:0; }
.project-child-box { margin:4px 0 8px 24px; border:1px solid #dbeafe; border-radius:8px; overflow:hidden; background:#fff; }
.project-child-title { padding:8px 10px; background:#eff6ff; color:#1d4ed8; font-weight:700; font-size:.84em; }
.project-child-empty { padding:10px 12px; color:#94a3b8; font-size:.86em; }
.pet-status { display:inline-block; padding:2px 8px; border-radius:10px; font-size:.75em; font-weight:700; white-space:nowrap; }
.st-draft    { background:#f1f5f9; color:#475569; }
.st-pending  { background:#fef3c7; color:#92400e; }
.st-review   { background:#dbeafe; color:#1d4ed8; }
.st-approved { background:#d1fae5; color:#065f46; }
.st-rejected { background:#fee2e2; color:#991b1b; }
.st-sent     { background:#ede9fe; color:#5b21b6; }
/* ── pagination ── */
.page-nav { display:flex; justify-content:center; gap:4px; padding:10px; }
.page-nav .btn { min-width:36px; }
/* ── tree view ── */
.tree-row { }
.tree-hidden { display:none !important; }
/* ── delete modal ── */
.del-pet-name { font-size:1em; font-weight:800; color:#dc2626; word-break:break-all; }
/* ── card-panel colors — light backgrounds, dark colorful text ── */
.card-panel { border-radius:8px; overflow:hidden; border:1px solid #e2e8f0; background:#fff; }
.card-panel-hdr { padding:12px 14px; font-weight:700; font-size:.95em; display:flex; align-items:center; gap:8px; }
.card-panel-body { padding:0; }
.card-panel.panel-spend-request { border-color:#B4C7E7; background:#fff; }
.card-panel.panel-spend-request .card-panel-hdr { background:#F5F9FF; color:#2F5597; border-bottom:2px solid #B4C7E7; }
.card-panel.panel-spend-request .dfm-table th { background:#E8F0FE !important; color:#1e3a5f; border-bottom:1px solid #B4C7E7; }
.card-panel.panel-budget-line-items { border-color:#C6E0B4; background:#fff; }
.card-panel.panel-budget-line-items .card-panel-hdr { background:#F6FBF4; color:#548235; border-bottom:2px solid #C6E0B4; }
.card-panel.panel-budget-line-items .dfm-table th { background:#EDF7E8 !important; color:#2d5016; border-bottom:1px solid #C6E0B4; }
.card-panel.panel-budget-invoice { border-color:#F4B183; background:#fff; }
.card-panel.panel-budget-invoice .card-panel-hdr { background:#FFF8F2; color:#C55A11; border-bottom:2px solid #F4B183; }
.card-panel.panel-budget-invoice .dfm-table th { background:#FFF0E5 !important; color:#7c3006; border-bottom:1px solid #F4B183; }
/* ── CAPEX/OPEX summary ── */
.budget-summary { display:grid; grid-template-columns:repeat(auto-fit,minmax(220px,1fr)); gap:10px; padding:12px 14px; }
.budget-summary-card { border:1px solid #e2e8f0; border-radius:8px; padding:12px; background:#fff; }
.budget-summary-title { font-weight:800; color:#1e293b; margin-bottom:6px; }
.budget-summary-value { font-size:1.25em; font-weight:900; color:#1a3c5e; margin-bottom:8px; }
.budget-bar { height:10px; border-radius:999px; background:#e2e8f0; overflow:hidden; }
.budget-bar span { display:block; height:100%; border-radius:999px; }
.budget-capex { background:#2563eb; }
.budget-opex { background:#059669; }
/* ── action buttons ── */
.proj-action-btn { font-size:.75em; padding:2px 6px; margin-right:2px; border-radius:4px; font-weight:600; cursor:pointer; border:none; }
.proj-action-btn.btn-sr { background:#E8F0FE; color:#2F5597; border:1px solid #B4C7E7; }
.proj-action-btn.btn-sr:hover { background:#D4E4FA; }
.proj-action-btn.btn-bgt { background:#F0F9EC; color:#548235; border:1px solid #C6E0B4; }
.proj-action-btn.btn-bgt:hover { background:#E0F3D8; }
.proj-action-btn.btn-inv { background:#FFF3E8; color:#C55A11; border:1px solid #F4B183; }
.proj-action-btn.btn-inv:hover { background:#FFE8D4; }
/* ── modal action panel grids ── */
.action-modal-grid th { font-size:.8em; padding:6px 8px; white-space:nowrap; }
.action-modal-grid td { font-size:.82em; padding:5px 8px; vertical-align:middle; color:#1e293b; }
.action-modal-grid tr:nth-child(odd) td { background:#fafbff; }
.action-modal-grid tr:nth-child(even) td { background:#fff; }
.action-modal-grid tr:hover td { background:#eff6ff; }
.project-frame-modal .modal-dialog { width:95%; max-width:1180px; }
.project-frame-modal .modal-body { padding:0; height:78vh; overflow:hidden; }
.project-frame-modal iframe { width:100%; height:100%; border:0; display:block; background:#fff; }
</style>
</asp:Content>

<asp:Content ID="MainCt" ContentPlaceHolderID="MainContent" runat="server">
<asp:ScriptManager ID="smDashboard" runat="server" />
<h1 class="page-title">
    <i class="bi bi-house"></i> Dashboard
    <span style="font-size:.55em;color:#94a3b8;font-weight:400;margin-left:12px;">
        Last Sync: <asp:Literal ID="litLastSync" runat="server" Text="–" />
    </span>
</h1>

<!-- ── FILTER SECTION ── -->
<div class="dash-section" id="sec-filters">
    <div class="dash-sec-hdr" onclick="dfmSecTog('sec-filters')">
        <span><i class="bi bi-funnel"></i> Filters</span>
        <i class="bi bi-chevron-down dash-sec-toggle"></i>
    </div>
    <div class="dash-sec-body">
        <div class="horizontal-filter-panel">
            <div class="filter-grid">
                <div class="form-group">
                    <label>Project</label>
                    <asp:DropDownList ID="ddlProject" runat="server" CssClass="form-control"
                        AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                        <asp:ListItem Text="All Projects" Value="ALL" />
                    </asp:DropDownList>
                </div>
                <div class="form-group">
                    <label>Type</label>
                    <asp:DropDownList ID="ddlType" runat="server" CssClass="form-control no-select2"
                        AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                        <asp:ListItem Text="All" Value="ALL" />
                        <asp:ListItem Text="CAPEX" Value="CAPEX" />
                        <asp:ListItem Text="OPEX" Value="OPEX" />
                    </asp:DropDownList>
                </div>
                <div class="form-group">
                    <label>View</label>
                    <asp:DropDownList ID="ddlView" runat="server" CssClass="form-control no-select2"
                        AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                        <asp:ListItem Text="Pending My Action" Value="MYAPPROVAL" />
                        <asp:ListItem Text="My Requests" Value="MYREQUESTS" />
                        <asp:ListItem Text="All Items" Value="ALL" />
                    </asp:DropDownList>
                </div>
                <div class="form-group">
                    <label>Status</label>
                    <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-control no-select2"
                        AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
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
                    <asp:TextBox ID="txtFromDate" runat="server" CssClass="form-control" TextMode="Date" />
                </div>
                <div class="form-group">
                    <label>To Date</label>
                    <asp:TextBox ID="txtToDate" runat="server" CssClass="form-control" TextMode="Date" />
                </div>
            </div>
            <div class="filter-actions">
                <asp:Button ID="btnApply" runat="server" Text="Apply Filters" CssClass="btn btn-primary"
                    OnClick="Filter_Changed" />
                <asp:Button ID="btnReset" runat="server" Text="Reset" CssClass="btn btn-default"
                    OnClick="btnResetFilters_Click" CausesValidation="false" />
            </div>
        </div>
    </div>
</div>

<!-- ── KPI CARDS ── -->
<div class="kpi-row">
    <div class="kpi-card kpi-blue">
        <i class="bi bi-folder2-open kpi-icon"></i>
        <div><div class="kpi-label">Total Projects</div><div class="kpi-val"><asp:Literal ID="litProjects" runat="server" Text="0" /></div></div>
    </div>
    <div class="kpi-card kpi-green">
        <i class="bi bi-file-earmark-text kpi-icon"></i>
        <div><div class="kpi-label">Total Requests</div><div class="kpi-val"><asp:Literal ID="litPET" runat="server" Text="0" /></div></div>
    </div>
    <div class="kpi-card kpi-orange">
        <i class="bi bi-hourglass-split kpi-icon"></i>
        <div><div class="kpi-label">Pending</div><div class="kpi-val"><asp:Literal ID="litPending" runat="server" Text="0" /></div></div>
    </div>
    <div class="kpi-card kpi-teal">
        <i class="bi bi-check2-circle kpi-icon"></i>
        <div><div class="kpi-label">Approved</div><div class="kpi-val"><asp:Literal ID="litApproved" runat="server" Text="0" /></div></div>
    </div>
    <div class="kpi-card kpi-red">
        <i class="bi bi-x-circle kpi-icon"></i>
        <div><div class="kpi-label">Rejected</div><div class="kpi-val"><asp:Literal ID="litRejected" runat="server" Text="0" /></div></div>
    </div>
    <div class="kpi-card kpi-green">
        <i class="bi bi-currency-dollar kpi-icon"></i>
        <div><div class="kpi-label">CAPEX Budget</div><div class="kpi-val"><asp:Literal ID="litCapexBudget" runat="server" Text="0" /></div></div>
    </div>
    <div class="kpi-card kpi-blue">
        <i class="bi bi-receipt kpi-icon"></i>
        <div><div class="kpi-label">OPEX Budget</div><div class="kpi-val"><asp:Literal ID="litOpexBudget" runat="server" Text="0" /></div></div>
    </div>
</div>

<!-- ── CAPEX/OPEX DATA SUMMARY ── -->
<div class="dash-section" id="sec-capex-opex">
    <div class="dash-sec-hdr" onclick="dfmSecTog('sec-capex-opex')">
        <span><i class="bi bi-pie-chart"></i> CAPEX vs OPEX Summary</span>
        <i class="bi bi-chevron-down dash-sec-toggle"></i>
    </div>
    <div class="dash-sec-body">
        <asp:Literal ID="litCapexOpexSummary" runat="server" />
    </div>
</div>

<!-- ── REGISTERED PROJECTS ── -->
<div class="dash-section" id="sec-projects">
    <div class="dash-sec-hdr" onclick="dfmSecTog('sec-projects')">
        <span><i class="bi bi-folder2-open"></i> Registered Projects
            <span style="background:#93c5fd;color:#1e3a5f;border-radius:10px;padding:1px 8px;font-size:.8em;margin-left:6px;">
                <asp:Literal ID="litRegisteredProjectsCount" runat="server" Text="0" />
            </span>
        </span>
        <i class="bi bi-chevron-down dash-sec-toggle"></i>
    </div>
    <div class="dash-sec-body">
        <div class="card-panel" style="border-top:none;border-radius:0 0 8px 8px;margin:0;padding:0;overflow-x:auto;">
            <table class="dfm-table" style="width:100%;">
                <thead>
                    <tr>
                        <th>Project Name</th>
                        <th>Project Type</th>
                        <th>Accountable Exec Lead</th>
                        <th>SME Lead</th>
                        <th>Project Size</th>
                        <th>Project Manager</th>
                        <th>Requestor</th>
                        <th>Status</th>
                        <th>Created Date</th>
                        <th>Action</th>
                    </tr>
                </thead>
                <tbody>
                    <asp:Literal ID="litRegisteredProjectRows" runat="server" />
                </tbody>
            </table>
            <div class="page-nav">
                <asp:LinkButton ID="btnPrevPage" runat="server" CssClass="btn btn-default btn-sm"
                    Text="&#8249; Prev" OnClick="btnPrevPage_Click" CausesValidation="false" />
                <asp:Literal ID="litPageInfo" runat="server" />
                <asp:LinkButton ID="btnNextPage" runat="server" CssClass="btn btn-default btn-sm"
                    Text="Next &#8250;" OnClick="btnNextPage_Click" CausesValidation="false" />
            </div>
        </div>
    </div>
</div>

<div style="margin-top:10px;">
    <a href="<%= ResolveUrl("~/Forms/ProjectRegistration.aspx?new=1") %>" class="btn btn-default">
        <i class="bi bi-folder-plus"></i> New Project
    </a>
    <a href="<%= ResolveUrl("~/Forms/PetWorkflow.aspx") %>" class="btn btn-primary">
        <i class="bi bi-plus-circle"></i> New Spend Request
    </a>
</div>

<!-- ── Project Action Hidden Fields + Buttons ── -->
<asp:UpdatePanel ID="updDashboardActions" runat="server" UpdateMode="Conditional">
<ContentTemplate>
<asp:HiddenField ID="hfActionProjectId" runat="server" Value="" />
<asp:Button ID="btnShowSpendRequests" runat="server" Text="_sr" style="display:none;"
    OnClick="btnShowSpendRequests_Click" CausesValidation="false" />
<asp:Button ID="btnShowBudget" runat="server" Text="_bgt" style="display:none;"
    OnClick="btnShowBudget_Click" CausesValidation="false" />
<asp:Button ID="btnShowInvoices" runat="server" Text="_inv" style="display:none;"
    OnClick="btnShowInvoices_Click" CausesValidation="false" />

<!-- ── SPEND REQUEST MODAL ── -->
<div class="modal fade" id="spendRequestModal" tabindex="-1" role="dialog">
    <div class="modal-dialog modal-lg" role="document" style="max-width:95%;width:95%;">
        <div class="modal-content">
            <div class="modal-header" style="background:#2F5597;color:#fff;">
                <button type="button" class="close" data-dismiss="modal" style="color:#fff;opacity:.8;">&times;</button>
                <h4 class="modal-title"><i class="bi bi-file-earmark-text"></i> Spend Requests &mdash; <asp:Literal ID="litSRModalProject" runat="server" /></h4>
            </div>
            <div class="modal-body" style="padding:16px;max-height:75vh;overflow-y:auto;">
                <div class="card-panel panel-spend-request" style="margin-bottom:14px;">
                    <div class="card-panel-hdr"><i class="bi bi-file-earmark-text"></i> Spend Requests (including Draft)</div>
                    <div class="card-panel-body" style="padding:0;overflow-x:auto;">
                        <asp:GridView ID="gvModalSpendRequests" runat="server" AutoGenerateColumns="false"
                            CssClass="dfm-table action-modal-grid" GridLines="None" EmptyDataText="No Spend Requests for this project.">
                            <Columns>
                                <asp:BoundField DataField="PetRefNo"        HeaderText="Ref No" />
                                <asp:BoundField DataField="CapexOpexType"   HeaderText="Type" />
                                <asp:BoundField DataField="Title"           HeaderText="Title" />
                                <asp:BoundField DataField="Status"          HeaderText="Status" />
                                <asp:BoundField DataField="CreatedBy"       HeaderText="Requestor" />
                                <asp:BoundField DataField="CreatedDate"     HeaderText="Created" DataFormatString="{0:dd-MMM-yyyy}" />
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
                        <asp:GridView ID="gvModalLineItems" runat="server" AutoGenerateColumns="false"
                            CssClass="dfm-table action-modal-grid" GridLines="None" EmptyDataText="No line items.">
                            <Columns>
                                <asp:BoundField DataField="PetRefNo"     HeaderText="Request" />
                                <asp:BoundField DataField="SerialNo"     HeaderText="#" ItemStyle-Width="30px" />
                                <asp:BoundField DataField="ExpHead"      HeaderText="Head" />
                                <asp:BoundField DataField="Topic"        HeaderText="Topic" />
                                <asp:BoundField DataField="VendorName"   HeaderText="Vendor" />
                                <asp:BoundField DataField="CostType"     HeaderText="Cost Type" />
                                <asp:BoundField DataField="Units"        HeaderText="Units" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                                <asp:BoundField DataField="UnitPrice"    HeaderText="Unit Price" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                                <asp:BoundField DataField="BaseCurrency" HeaderText="CCY" />
                                <asp:BoundField DataField="AmtFCY"       HeaderText="FCY Amt" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                                <asp:BoundField DataField="FinalAmtLCY"  HeaderText="Final AED" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-default" data-dismiss="modal"><i class="bi bi-x-lg"></i> Close</button>
            </div>
        </div>
    </div>
</div>

<!-- ── BUDGET MODAL ── -->
<div class="modal fade" id="budgetActionModal" tabindex="-1" role="dialog">
    <div class="modal-dialog modal-lg" role="document" style="max-width:95%;width:95%;">
        <div class="modal-content">
            <div class="modal-header" style="background:#548235;color:#fff;">
                <button type="button" class="close" data-dismiss="modal" style="color:#fff;opacity:.8;">&times;</button>
                <h4 class="modal-title"><i class="bi bi-cash-coin"></i> Budget &mdash; <asp:Literal ID="litBgtModalProject" runat="server" /></h4>
            </div>
            <div class="modal-body" style="padding:16px;max-height:75vh;overflow-y:auto;">
                <div class="card-panel panel-budget-line-items" style="margin-bottom:14px;">
                    <div class="card-panel-hdr"><i class="bi bi-cash-coin"></i> Budget Line Items</div>
                    <div class="card-panel-body" style="padding:0;overflow-x:auto;">
                        <asp:GridView ID="gvModalBudgetLines" runat="server" AutoGenerateColumns="false"
                            CssClass="dfm-table action-modal-grid" GridLines="None" DataKeyNames="BudgetLineID"
                            OnRowCommand="gvModalBudgetLines_RowCommand"
                            EmptyDataText="No budget lines for this project.">
                            <Columns>
                                <asp:BoundField DataField="PetRefNo"      HeaderText="Request Ref" />
                                <asp:BoundField DataField="VendorName"    HeaderText="Vendor" />
                                <asp:BoundField DataField="Justification" HeaderText="Justification" />
                                <asp:BoundField DataField="Cost"           HeaderText="Cost" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                                <asp:BoundField DataField="Currency"       HeaderText="CCY" />
                                <asp:BoundField DataField="CamStatus"      HeaderText="CAM Status" />
                                <asp:BoundField DataField="LpoStatus"      HeaderText="LPO Status" />
                                <asp:BoundField DataField="InvoiceTotal"   HeaderText="Invoiced" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                                <asp:TemplateField HeaderText="Invoices">
                                    <ItemTemplate>
                                        <asp:LinkButton runat="server" CssClass="btn btn-xs btn-info" CommandName="ShowInvoice" CommandArgument='<%# Eval("BudgetLineID") %>'>
                                            <i class="bi bi-receipt"></i> Invoice
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
                <asp:Panel ID="pnlBudgetInvoiceDetail" runat="server" Visible="false">
                <div class="card-panel panel-budget-invoice">
                    <div class="card-panel-hdr"><i class="bi bi-receipt"></i> Invoices for Budget Line #<asp:Literal ID="litBgtInvLineId" runat="server" /></div>
                    <div class="card-panel-body" style="padding:0;overflow-x:auto;">
                        <asp:GridView ID="gvModalBudgetInvoices" runat="server" AutoGenerateColumns="false"
                            CssClass="dfm-table action-modal-grid" GridLines="None" EmptyDataText="No invoices for this budget line.">
                            <Columns>
                                <asp:BoundField DataField="InvoiceID"     HeaderText="ID" />
                                <asp:BoundField DataField="InvoiceNo"     HeaderText="Invoice No" />
                                <asp:BoundField DataField="InvoiceAmount" HeaderText="Amount" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                                <asp:BoundField DataField="InvoiceStatus" HeaderText="Status" />
                                <asp:BoundField DataField="PaymentDate"   HeaderText="Payment Date" DataFormatString="{0:dd-MMM-yyyy}" />
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
                </asp:Panel>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-default" data-dismiss="modal"><i class="bi bi-x-lg"></i> Close</button>
            </div>
        </div>
    </div>
</div>

<!-- ── INVOICE MODAL ── -->
<div class="modal fade" id="invoiceActionModal" tabindex="-1" role="dialog">
    <div class="modal-dialog modal-lg" role="document" style="max-width:90%;width:90%;">
        <div class="modal-content">
            <div class="modal-header" style="background:#C55A11;color:#fff;">
                <button type="button" class="close" data-dismiss="modal" style="color:#fff;opacity:.8;">&times;</button>
                <h4 class="modal-title"><i class="bi bi-receipt"></i> Invoices &mdash; <asp:Literal ID="litInvModalProject" runat="server" /></h4>
            </div>
            <div class="modal-body" style="padding:16px;max-height:75vh;overflow-y:auto;">
                <div class="card-panel panel-budget-invoice">
                    <div class="card-panel-hdr"><i class="bi bi-receipt"></i> All Invoices for this Project</div>
                    <div class="card-panel-body" style="padding:0;overflow-x:auto;">
                        <asp:GridView ID="gvModalInvoices" runat="server" AutoGenerateColumns="false"
                            CssClass="dfm-table action-modal-grid" GridLines="None" EmptyDataText="No invoices for this project.">
                            <Columns>
                                <asp:BoundField DataField="PetRefNo"      HeaderText="Request Ref" />
                                <asp:BoundField DataField="VendorName"    HeaderText="Vendor" />
                                <asp:BoundField DataField="InvoiceNo"     HeaderText="Invoice No" />
                                <asp:BoundField DataField="InvoiceAmount" HeaderText="Amount" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                                <asp:BoundField DataField="InvoiceStatus" HeaderText="Status" />
                                <asp:BoundField DataField="PaymentDate"   HeaderText="Payment Date" DataFormatString="{0:dd-MMM-yyyy}" />
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-default" data-dismiss="modal"><i class="bi bi-x-lg"></i> Close</button>
            </div>
        </div>
    </div>
</div>

<!-- ── DASHBOARD PROJECT MODAL ── -->
<div class="modal fade project-frame-modal" id="dashboardProjectModal" tabindex="-1" role="dialog">
    <div class="modal-dialog" role="document">
        <div class="modal-content">
            <div class="modal-header" style="background:#1a3c5e;color:#fff;">
                <button type="button" class="close" data-dismiss="modal" style="color:#fff;opacity:.8;">&times;</button>
                <h4 class="modal-title"><i class="bi bi-folder-plus"></i> Project Portfolio</h4>
            </div>
            <div class="modal-body">
                <iframe id="dashboardProjectFrame" title="Project Portfolio"></iframe>
            </div>
        </div>
    </div>
</div>

<!-- ── PET Delete Hidden Fields + Modal ── -->
<asp:HiddenField ID="hfDeletePetId" runat="server" Value="0" />
<asp:Button ID="btnConfirmDeletePet" runat="server" Text="_del" style="display:none;"
    OnClick="btnConfirmDeletePet_Click" CausesValidation="false" />

<div class="modal fade" id="petDelModal" tabindex="-1" role="dialog">
    <div class="modal-dialog" role="document" style="max-width:460px;">
        <div class="modal-content">
            <div class="modal-header" style="background:#b91c1c;color:#fff;">
                <button type="button" class="close" data-dismiss="modal" style="color:#fff;opacity:.8;">&times;</button>
                <h4 class="modal-title"><i class="bi bi-exclamation-triangle-fill"></i> Confirm Delete Spend Request</h4>
            </div>
            <div class="modal-body" style="padding:24px;text-align:center;">
                <div style="font-size:2.8em;color:#dc2626;margin-bottom:10px;"><i class="bi bi-trash3-fill"></i></div>
                <p style="font-size:.94em;font-weight:600;color:#1e293b;margin-bottom:4px;">Are you sure you want to delete Spend Request</p>
                <p id="petDelRefNo" class="del-pet-name"></p>
                <p style="font-size:.82em;color:#64748b;margin-top:8px;">The Spend Request will be marked as <strong>Deleted</strong> and removed from all views. Workflow history is retained.</p>

            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-default" data-dismiss="modal"><i class="bi bi-x-lg"></i> Cancel</button>
                <button type="button" class="btn btn-danger" onclick="dfmPetDelConfirm();"><i class="bi bi-trash"></i> Yes, Delete</button>
            </div>
        </div>
    </div>
</div>

<script>
function dfmSecTog(id) {
    var el = document.getElementById(id);
    if (el) el.classList.toggle('collapsed');
}
function dfmTog(cls) {
    var rows = document.querySelectorAll('.tree-row.' + cls);
    var hidden = rows.length > 0 && rows[0].classList.contains('tree-hidden');
    for (var i = 0; i < rows.length; i++) {
        if (hidden) rows[i].classList.remove('tree-hidden');
        else rows[i].classList.add('tree-hidden');
    }
    var btns = document.querySelectorAll('[data-tog="' + cls + '"]');
    for (var j = 0; j < btns.length; j++)
        btns[j].innerHTML = hidden ? '&#9660;' : '&#9658;';
}
function dfmProjectTog(cls) {
    var rows = document.querySelectorAll('.project-child-row.' + cls);
    if (!rows.length) return false;
    var hidden = rows[0].classList.contains('tree-hidden');
    for (var i = 0; i < rows.length; i++) {
        if (hidden) rows[i].classList.remove('tree-hidden');
        else rows[i].classList.add('tree-hidden');
    }
    var btns = document.querySelectorAll('[data-project-tog="' + cls + '"]');
    for (var j = 0; j < btns.length; j++)
        btns[j].innerHTML = hidden ? '&#9660;' : '&#9658;';
    return false;
}
function dfmPetDel(id, refNo) {
    document.getElementById('<%= hfDeletePetId.ClientID %>').value = id;
    document.getElementById('petDelRefNo').textContent = refNo;
    jQuery('#petDelModal').modal('show');
}
function dfmPetDelConfirm() {
    jQuery('#petDelModal').modal('hide');
    document.getElementById('<%= btnConfirmDeletePet.ClientID %>').click();
}
function dfmShowSR(projId) {
    document.getElementById('<%= hfActionProjectId.ClientID %>').value = projId;
    document.getElementById('<%= btnShowSpendRequests.ClientID %>').click();
}
function dfmShowBgt(projId) {
    document.getElementById('<%= hfActionProjectId.ClientID %>').value = projId;
    document.getElementById('<%= btnShowBudget.ClientID %>').click();
}
function dfmShowInv(projId) {
    document.getElementById('<%= hfActionProjectId.ClientID %>').value = projId;
    document.getElementById('<%= btnShowInvoices.ClientID %>').click();
}
function dfmOpenProject(projId) {
    var frame = document.getElementById('dashboardProjectFrame');
    frame.src = '<%= ResolveUrl("~/Forms/ProjectRegistration.aspx") %>?pid=' + encodeURIComponent(projId) + '&embed=1';
    jQuery('#dashboardProjectModal').modal('show');
    return false;
}
jQuery('#dashboardProjectModal').on('hidden.bs.modal', function () {
    document.getElementById('dashboardProjectFrame').src = 'about:blank';
});
</script>
</ContentTemplate>
</asp:UpdatePanel>
</asp:Content>
