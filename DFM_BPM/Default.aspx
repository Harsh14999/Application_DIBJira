<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="DFM_BPM.DefaultPage" %>

<asp:Content ID="HeadCt" ContentPlaceHolderID="HeadContent" runat="server">
<style>
.home-toolbar { display:flex; justify-content:space-between; align-items:flex-start; gap:18px; margin-bottom:18px; }
.home-eyebrow { color:#2563eb; font-size:.75em; font-weight:800; text-transform:uppercase; letter-spacing:.08em; }
.home-title { margin:2px 0 5px; font-size:2.05em; line-height:1.1; color:#0f172a; font-weight:900; }
.home-subtitle { color:#64748b; font-size:.95em; }
.home-actions { display:flex; gap:8px; flex-wrap:wrap; justify-content:flex-end; }
.home-kpi-grid { display:grid; grid-template-columns:repeat(5,minmax(145px,1fr)); gap:12px; margin-bottom:16px; }
.home-kpi { background:#fff; border:1px solid #e2e8f0; border-radius:8px; padding:14px; box-shadow:0 1px 2px rgba(15,23,42,.04); min-height:104px; display:flex; flex-direction:column; justify-content:space-between; }
.home-kpi-top { display:flex; justify-content:space-between; gap:10px; color:#64748b; font-size:.76em; font-weight:800; text-transform:uppercase; }
.home-kpi-icon { width:32px; height:32px; border-radius:8px; display:flex; align-items:center; justify-content:center; background:#eff6ff; color:#2563eb; font-size:1.1em; }
.home-kpi-value { font-size:1.55em; font-weight:900; color:#0f172a; margin-top:10px; }
.home-kpi-note { color:#64748b; font-size:.78em; margin-top:4px; }
.home-grid { display:grid; grid-template-columns:1.1fr .9fr; gap:14px; margin-bottom:14px; }
.home-panel { background:#fff; border:1px solid #e2e8f0; border-radius:8px; overflow:hidden; box-shadow:0 1px 2px rgba(15,23,42,.04); }
.home-panel-hdr { padding:13px 15px; border-bottom:1px solid #e2e8f0; display:flex; justify-content:space-between; align-items:center; gap:10px; }
.home-panel-title { margin:0; font-size:.95em; font-weight:900; color:#0f172a; display:flex; gap:8px; align-items:center; }
.home-panel-body { padding:14px 15px; }
.viz-bars { display:flex; flex-direction:column; gap:12px; }
.viz-row { display:grid; grid-template-columns:88px 1fr 80px; gap:10px; align-items:center; font-size:.84em; }
.viz-label { color:#475569; font-weight:700; }
.viz-track { height:11px; border-radius:999px; background:#e2e8f0; overflow:hidden; }
.viz-fill { height:100%; border-radius:999px; background:#2563eb; }
.viz-fill.green { background:#059669; }
.viz-fill.orange { background:#ea580c; }
.viz-fill.slate { background:#475569; }
.viz-value { color:#0f172a; font-weight:800; text-align:right; }
.trend-strip { display:grid; grid-template-columns:repeat(6,1fr); gap:8px; align-items:end; min-height:150px; padding-bottom:22px; }
.trend-bar { min-height:24px; border-radius:6px 6px 0 0; background:linear-gradient(180deg,#38bdf8,#2563eb); position:relative; }
.trend-bar span { position:absolute; left:0; right:0; bottom:-22px; text-align:center; font-size:.72em; color:#64748b; }
.approval-list { display:flex; flex-direction:column; gap:9px; }
.approval-item { display:grid; grid-template-columns:32px 1fr auto; gap:10px; align-items:center; padding:10px; background:#f8fafc; border:1px solid #e2e8f0; border-radius:8px; }
.approval-icon { width:32px; height:32px; border-radius:8px; display:flex; align-items:center; justify-content:center; background:#fff7ed; color:#ea580c; }
.approval-title { font-weight:800; color:#0f172a; font-size:.86em; }
.approval-meta { color:#64748b; font-size:.78em; }
.status-pill { display:inline-block; padding:3px 8px; border-radius:999px; font-size:.72em; font-weight:800; white-space:nowrap; }
.status-pending { background:#fef3c7; color:#92400e; }
.status-approved { background:#d1fae5; color:#065f46; }
.status-review { background:#dbeafe; color:#1d4ed8; }
.status-draft { background:#f1f5f9; color:#475569; }
.dashboard-table { margin:0; }
.dashboard-table th { background:#f8fafc !important; color:#475569; font-size:.76em; text-transform:uppercase; letter-spacing:.03em; }
.dashboard-table td { vertical-align:middle !important; font-size:.84em; }
@media (max-width:1100px) { .home-kpi-grid { grid-template-columns:repeat(2,minmax(145px,1fr)); } .home-grid { grid-template-columns:1fr; } }
@media (max-width:700px) { .home-toolbar { flex-direction:column; } .home-actions { justify-content:flex-start; } .home-kpi-grid { grid-template-columns:1fr; } .viz-row { grid-template-columns:72px 1fr; } .viz-value { grid-column:2; text-align:left; } }
</style>
</asp:Content>

<asp:Content ID="MainCt" ContentPlaceHolderID="MainContent" runat="server">
<div class="home-toolbar">
    <div>
        <div class="home-eyebrow">Finance Command Center</div>
        <h1 class="home-title">Dashboard</h1>
        <div class="home-subtitle">Portfolio health, spend activity, approvals and invoice movement in one view. Last Sync: <asp:Literal ID="litLastSync" runat="server" Text="-" /></div>
    </div>
    <div class="home-actions">
        <asp:Button ID="btnExportDashboard" runat="server" CssClass="btn btn-default" Text="Export" OnClick="btnExportDashboard_Click" />
        <a class="btn btn-primary" href="<%= ResolveUrl("~/Forms/PetWorkflow.aspx") %>"><i class="bi bi-plus-circle"></i> New Request</a>
    </div>
</div>

<div class="home-kpi-grid">
    <div class="home-kpi"><div class="home-kpi-top"><span>Total Projects</span><span class="home-kpi-icon"><i class="bi bi-folder2-open"></i></span></div><div><div class="home-kpi-value"><asp:Literal ID="litProjects" runat="server" Text="0" /></div><div class="home-kpi-note">Registered portfolio</div></div></div>
    <div class="home-kpi"><div class="home-kpi-top"><span>Total Requests</span><span class="home-kpi-icon"><i class="bi bi-file-earmark-text"></i></span></div><div><div class="home-kpi-value"><asp:Literal ID="litPET" runat="server" Text="0" /></div><div class="home-kpi-note">Spend requests</div></div></div>
    <div class="home-kpi"><div class="home-kpi-top"><span>Pending</span><span class="home-kpi-icon"><i class="bi bi-hourglass-split"></i></span></div><div><div class="home-kpi-value"><asp:Literal ID="litPending" runat="server" Text="0" /></div><div class="home-kpi-note">Needs attention</div></div></div>
    <div class="home-kpi"><div class="home-kpi-top"><span>Approved</span><span class="home-kpi-icon"><i class="bi bi-check2-circle"></i></span></div><div><div class="home-kpi-value"><asp:Literal ID="litApproved" runat="server" Text="0" /></div><div class="home-kpi-note">Completed approvals</div></div></div>
    <div class="home-kpi"><div class="home-kpi-top"><span>Rejected</span><span class="home-kpi-icon"><i class="bi bi-x-circle"></i></span></div><div><div class="home-kpi-value"><asp:Literal ID="litRejected" runat="server" Text="0" /></div><div class="home-kpi-note">Returned or declined</div></div></div>
    <div class="home-kpi"><div class="home-kpi-top"><span>CAPEX Budget</span><span class="home-kpi-icon"><i class="bi bi-cash-coin"></i></span></div><div><div class="home-kpi-value"><asp:Literal ID="litCapexBudget" runat="server" Text="AED 0" /></div><div class="home-kpi-note">Capital allocation</div></div></div>
    <div class="home-kpi"><div class="home-kpi-top"><span>OPEX Budget</span><span class="home-kpi-icon"><i class="bi bi-receipt"></i></span></div><div><div class="home-kpi-value"><asp:Literal ID="litOpexBudget" runat="server" Text="AED 0" /></div><div class="home-kpi-note">Operating allocation</div></div></div>
    <div class="home-kpi"><div class="home-kpi-top"><span>Invoices</span><span class="home-kpi-icon"><i class="bi bi-journal-check"></i></span></div><div><div class="home-kpi-value"><asp:Literal ID="litInvoiceCount" runat="server" Text="0" /></div><div class="home-kpi-note"><asp:Literal ID="litInvoiceAmount" runat="server" Text="AED 0" /></div></div></div>
    <div class="home-kpi"><div class="home-kpi-top"><span>My Budget Lines</span><span class="home-kpi-icon"><i class="bi bi-list-check"></i></span></div><div><div class="home-kpi-value"><asp:Literal ID="litMyBudgetLines" runat="server" Text="0" /></div><div class="home-kpi-note">Created by me</div></div></div>
    <div class="home-kpi"><div class="home-kpi-top"><span>Active Projects</span><span class="home-kpi-icon"><i class="bi bi-activity"></i></span></div><div><div class="home-kpi-value"><asp:Literal ID="litActiveProjects" runat="server" Text="0" /></div><div class="home-kpi-note">Currently enabled</div></div></div>
</div>

<div class="home-grid">
    <div class="home-panel">
        <div class="home-panel-hdr"><h3 class="home-panel-title"><i class="bi bi-pie-chart"></i> CAPEX vs OPEX</h3></div>
        <div class="home-panel-body"><asp:Literal ID="litCapexOpexChart" runat="server" /></div>
    </div>
    <div class="home-panel">
        <div class="home-panel-hdr"><h3 class="home-panel-title"><i class="bi bi-speedometer2"></i> Budget Consumption</h3></div>
        <div class="home-panel-body"><asp:Literal ID="litBudgetChart" runat="server" /></div>
    </div>
</div>

<div class="home-grid">
    <div class="home-panel">
        <div class="home-panel-hdr"><h3 class="home-panel-title"><i class="bi bi-graph-up-arrow"></i> Monthly Spend</h3></div>
        <div class="home-panel-body"><asp:Literal ID="litMonthlySpendChart" runat="server" /></div>
    </div>
    <div class="home-panel">
        <div class="home-panel-hdr"><h3 class="home-panel-title"><i class="bi bi-receipt-cutoff"></i> Invoice Settlement Trend</h3></div>
        <div class="home-panel-body"><asp:Literal ID="litInvoiceTrendChart" runat="server" /></div>
    </div>
</div>

<div class="home-grid">
    <div class="home-panel">
        <div class="home-panel-hdr"><h3 class="home-panel-title"><i class="bi bi-folder2-open"></i> Recent Projects</h3><a href="<%= ResolveUrl("~/Forms/ProjectRegistration.aspx") %>" class="btn btn-xs btn-default">Project Portfolio</a></div>
        <asp:GridView ID="gvRecentProjects" runat="server" AutoGenerateColumns="false" GridLines="None" CssClass="table dfm-table dashboard-table" EmptyDataText="No projects registered yet.">
            <Columns>
                <asp:TemplateField HeaderText="Project"><ItemTemplate><a href='<%# ResolveUrl("~/Forms/ProjectRegistration.aspx") + "?pid=" + Eval("ProjectID") %>'><%# Eval("ProjectID") %></a><div style="color:#64748b;font-size:.9em;"><%# Eval("ProjectName") %></div></ItemTemplate></asp:TemplateField>
                <asp:BoundField DataField="ProjectManager" HeaderText="Manager" />
                <asp:BoundField DataField="AccountableExecLead" HeaderText="Exec Lead" />
                <asp:BoundField DataField="CreatedDate" HeaderText="Created" DataFormatString="{0:dd-MMM-yyyy}" />
            </Columns>
        </asp:GridView>
    </div>
    <div class="home-panel">
        <div class="home-panel-hdr"><h3 class="home-panel-title"><i class="bi bi-lightning-charge"></i> Pending Approvals</h3></div>
        <div class="home-panel-body"><asp:Literal ID="litPendingApprovals" runat="server" /></div>
    </div>
</div>

<div class="home-grid">
    <div class="home-panel">
        <div class="home-panel-hdr"><h3 class="home-panel-title"><i class="bi bi-file-earmark-text"></i> Spend Requests</h3></div>
        <asp:GridView ID="gvRecentRequests" runat="server" AutoGenerateColumns="false" GridLines="None" CssClass="table dfm-table dashboard-table" EmptyDataText="No spend requests found.">
            <Columns>
                <asp:TemplateField HeaderText="Request"><ItemTemplate><a href='<%# ResolveUrl("~/Forms/PetWorkflow.aspx") + "?id=" + Eval("PetFormID") %>'><%# Eval("PetRefNo") %></a><div style="color:#64748b;font-size:.9em;"><%# Eval("Title") %></div></ItemTemplate></asp:TemplateField>
                <asp:BoundField DataField="ProjectID" HeaderText="Project" />
                <asp:BoundField DataField="CapexOpexType" HeaderText="Type" />
                <asp:BoundField DataField="Status" HeaderText="Status" />
                <asp:BoundField DataField="TotalRequestedAED" HeaderText="AED" DataFormatString="{0:N0}" ItemStyle-CssClass="text-right" />
            </Columns>
        </asp:GridView>
    </div>
    <div class="home-panel">
        <div class="home-panel-hdr"><h3 class="home-panel-title"><i class="bi bi-receipt"></i> Invoices</h3></div>
        <asp:GridView ID="gvRecentInvoices" runat="server" AutoGenerateColumns="false" GridLines="None" CssClass="table dfm-table dashboard-table" EmptyDataText="No invoices found.">
            <Columns>
                <asp:BoundField DataField="InvoiceNo" HeaderText="Invoice" />
                <asp:BoundField DataField="PetRefNo" HeaderText="Request" />
                <asp:BoundField DataField="VendorName" HeaderText="Vendor" />
                <asp:BoundField DataField="InvoiceStatus" HeaderText="Status" />
                <asp:BoundField DataField="InvoiceAmount" HeaderText="Amount" DataFormatString="{0:N0}" ItemStyle-CssClass="text-right" />
            </Columns>
        </asp:GridView>
    </div>
</div>
</asp:Content>
