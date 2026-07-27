<%@ Page Title="My Approvals" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="MyApprovals.aspx.cs" Inherits="DFM_BPM.Forms.MyApprovals" %>

<asp:Content ID="HeadCt" ContentPlaceHolderID="HeadContent" runat="server">
<link href="<%= ResolveUrl("~/Content/bootstrap-icons.css") %>" rel="stylesheet" />
<style>
.approval-card { background:#fff; border:1px solid #e2e8f0; border-radius:8px; margin-bottom:8px;
                 padding:12px 16px; transition:box-shadow .15s; }
.approval-card:hover { box-shadow:0 4px 12px rgba(0,0,0,.08); }
.approval-card-hdr { display:flex; justify-content:space-between; align-items:center; margin-bottom:4px; }
.approval-petref { font-weight:800; color:#1a3c5e; font-size:1em; }
.approval-status { font-size:.75em; font-weight:700; padding:3px 10px; border-radius:12px; }
.approval-status.PendingApproval { background:#fef3c7; color:#92400e; }
.approval-status.PendingReview   { background:#dbeafe; color:#1e40af; }
.approval-status.Approved        { background:#d1fae5; color:#065f46; }
.approval-status.Rejected        { background:#fee2e2; color:#991b1b; }
.approval-status.SentBack        { background:#f3f4f6; color:#374151; }
.approval-meta { font-size:.82em; color:#64748b; }
.approval-meta strong { color:#374151; }
</style>
</asp:Content>

<asp:Content ID="MainCt" ContentPlaceHolderID="MainContent" runat="server">
<h1 class="page-title"><i class="bi bi-check2-all"></i> My Approvals</h1>

<asp:Label ID="lblMsg" runat="server" CssClass="alert alert-info" Visible="false" />

<!-- Pending Actions -->
<div class="card-panel">
    <div class="card-panel-hdr"><i class="bi bi-hourglass-split"></i> Pending My Action
        <span class="badge" style="margin-left:8px;background:#f59e0b;color:#fff;">
            <asp:Literal ID="litPendingCount" runat="server" Text="0" />
        </span>
    </div>
    <div class="card-panel-body">
        <asp:Repeater ID="rptPending" runat="server">
            <ItemTemplate>
                <div class="approval-card">
                    <div class="approval-card-hdr">
                        <a href='<%# ResolveUrl("~/Forms/PetWorkflow.aspx?id=" + Eval("PetFormID")) %>' class="approval-petref">
                            <i class="bi bi-file-earmark-text"></i> <%# Eval("PetRefNo") %>
                        </a>
                        <span class='approval-status <%# Eval("Status") %>'><%# Eval("Status") %></span>
                    </div>
                    <div class="approval-meta">
                        <strong>Project:</strong> <%# Eval("ProjectID") %> &nbsp;|&nbsp;
                        <strong>Title:</strong> <%# Eval("Title") %> &nbsp;|&nbsp;
                        <strong>Requestor:</strong> <%# Eval("CreatedBy") %> &nbsp;|&nbsp;
                        <strong>Submitted:</strong> <%# Eval("SubmittedDate") != DBNull.Value ? Convert.ToDateTime(Eval("SubmittedDate")).ToString("dd-MMM-yyyy HH:mm") : "-" %>
                    </div>
                    <div style="margin-top:6px;">
                        <a href='<%# ResolveUrl("~/Forms/PetWorkflow.aspx?id=" + Eval("PetFormID") + "#tabApproval") %>'
                           class="btn btn-sm btn-primary"><i class="bi bi-check2-circle"></i> Take Action</a>
                    </div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
        <asp:Panel ID="pnlEmptyPending" runat="server" Visible="false">
            <div class="alert alert-success"><i class="bi bi-check-circle"></i> No pending actions. All clear!</div>
        </asp:Panel>
    </div>
</div>

<!-- Recently Processed -->
<div class="card-panel">
    <div class="card-panel-hdr"><i class="bi bi-clock-history"></i> Recently Processed</div>
    <div class="card-panel-body" style="padding:0;overflow-x:auto;">
        <asp:GridView ID="gvProcessed" runat="server" AutoGenerateColumns="false"
            CssClass="dfm-table" GridLines="None" EmptyDataText="No recently processed items.">
            <Columns>
                <asp:TemplateField HeaderText="Request Ref#">
                    <ItemTemplate>
                        <a href='<%# ResolveUrl("~/Forms/PetWorkflow.aspx?id=" + Eval("PetFormID")) %>'>
                            <%# Eval("PetRefNo") %>
                        </a>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="ProjectID"     HeaderText="Project" />
                <asp:BoundField DataField="Title"         HeaderText="Title" />
                <asp:BoundField DataField="CreatedBy"     HeaderText="Requestor" />
                <asp:TemplateField HeaderText="Status">
                    <ItemTemplate>
                        <span class='approval-status <%# Eval("Status") %>'><%# Eval("Status") %></span>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="LastActionDate" HeaderText="Action Date" DataFormatString="{0:dd-MMM-yyyy}" />
            </Columns>
        </asp:GridView>
    </div>
</div>
</asp:Content>
