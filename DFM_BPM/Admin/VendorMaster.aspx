<%@ Page Title="Vendor Master" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="VendorMaster.aspx.cs" Inherits="DFM_BPM.Admin.VendorMaster" %>

<asp:Content ID="HeadCt" ContentPlaceHolderID="HeadContent" runat="server">
<style>.history-row { background:#fef9f0 !important; font-size:.8em; color:#64748b; }</style>
</asp:Content>

<asp:Content ID="MainCt" ContentPlaceHolderID="MainContent" runat="server">
    <h1 class="page-title"><i class="bi bi-building"></i> Vendor Master
        <small style="font-size:.55em;color:#64748b;font-weight:400;">Editable – with history</small>
    </h1>
    <asp:Label ID="lblMsg" runat="server" CssClass="alert-info" Visible="false" />

    <div style="display:flex;gap:8px;flex-wrap:wrap;margin-bottom:10px;">
        <div class="form-group" style="flex:2;"><label>Search by Name or Code</label>
            <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control" /></div>
        <div class="form-group"><label>&nbsp;</label>
            <asp:LinkButton ID="btnSearch" runat="server" CssClass="btn btn-primary" OnClick="btnSearch_Click"><i class="bi bi-search"></i> Search</asp:LinkButton></div>
        <div class="form-group"><label>&nbsp;</label>
            <asp:LinkButton ID="btnExport" runat="server" CssClass="btn btn-success" OnClick="btnExport_Click"><i class="bi bi-file-excel"></i> Excel</asp:LinkButton></div>
    </div>

    <!-- Add/Edit -->
    <div class="card-panel">
        <div class="dfm-panel-hdr" onclick="DFM.togglePanel('vendorFormBody','vendorChev')">
            <i id="vendorChev" class="bi bi-chevron-right dfm-panel-chev <%= FormChevClass %>"></i>
            <i class="bi bi-pencil-square"></i> Add / Edit Vendor
        </div>
        <div id="vendorFormBody" class="dfm-panel-body" style="<%= FormBodyStyle %>">
            <asp:HiddenField ID="hfEditId" runat="server" Value="" />
            <div class="form-grid-4">
                <div class="form-group"><label>Vendor Code *</label><asp:TextBox ID="txtCode" runat="server" CssClass="form-control" /></div>
                <div class="form-group"><label>Vendor Name *</label><asp:TextBox ID="txtName" runat="server" CssClass="form-control" /></div>
                <div class="form-group"><label>Contact Email</label><asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" /></div>
                <div class="form-group"><label>Contact Phone</label><asp:TextBox ID="txtPhone" runat="server" CssClass="form-control" /></div>
            </div>
            <div class="form-grid-4">
                <div class="form-group"><label>Active</label>
                    <asp:DropDownList ID="ddlActive" runat="server" CssClass="form-control">
                        <asp:ListItem Value="Yes">Yes</asp:ListItem><asp:ListItem Value="No">No</asp:ListItem></asp:DropDownList></div>
                <div class="form-group" style="align-self:end;">
                    <asp:Button ID="btnSave" runat="server" CssClass="btn btn-primary" Text="Save" OnClick="btnSave_Click" />
                    <asp:Button ID="btnReset" runat="server" CssClass="btn btn-default" Text="Cancel" CausesValidation="false" OnClick="btnReset_Click" />
                </div>
            </div>
        </div>
    </div>

    <!-- Grid -->
    <div class="card-panel">
        <div class="card-panel-hdr"><i class="bi bi-table"></i> Vendors (<asp:Literal ID="litCount" runat="server" Text="0" />)</div>
        <div class="card-panel-body" style="padding:0;overflow-x:auto;">
            <asp:GridView ID="gv" runat="server" AutoGenerateColumns="false"
                CssClass="dfm-table" GridLines="None" DataKeyNames="VendorCode"
                OnRowCommand="gv_RowCommand"
                AllowPaging="true" PageSize="20" OnPageIndexChanging="gv_PageIndexChanging"
                EmptyDataText="No vendor records.">
                <PagerStyle CssClass="dfm-pager" HorizontalAlign="Center" />
                <PagerSettings Mode="NumericFirstLast" PageButtonCount="5" FirstPageText="&amp;laquo;" LastPageText="&amp;raquo;" />
                <Columns>
                    <asp:BoundField DataField="VendorCode"  HeaderText="Code" />
                    <asp:BoundField DataField="VendorName"  HeaderText="Vendor Name" />
                    <asp:BoundField DataField="ContactEmail" HeaderText="Email" />
                    <asp:BoundField DataField="ContactPhone" HeaderText="Phone" />
                    <asp:TemplateField HeaderText="Active" ItemStyle-CssClass="text-center">
                        <ItemTemplate><%# Convert.ToBoolean(Eval("IsActive") ?? false) ? "<span class='badge-success'>Yes</span>" : "<span class='badge-danger'>No</span>" %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="ModifiedBy"   HeaderText="By" />
                    <asp:BoundField DataField="ModifiedDate" HeaderText="Modified" DataFormatString="{0:dd-MMM-yy}" />
                    <asp:TemplateField HeaderText="Actions"><ItemStyle CssClass="action-cell" />
                        <ItemTemplate><div class="gv-acts">
                            <asp:LinkButton runat="server" CssClass="btn btn-xs btn-primary" CommandName="EditRow" CommandArgument='<%# Eval("VendorCode") %>'><i class="bi bi-pencil"></i> Edit</asp:LinkButton>
                            <asp:LinkButton runat="server" CssClass="btn btn-xs btn-secondary" CommandName="ViewHist" CommandArgument='<%# Eval("VendorCode") %>'><i class="bi bi-clock-history"></i> History</asp:LinkButton>
                            <asp:LinkButton runat="server" CssClass="btn btn-xs btn-danger" CommandName="DeleteRow" CommandArgument='<%# Eval("VendorCode") %>' OnClientClick="return confirm('Delete?');"><i class="bi bi-trash"></i></asp:LinkButton>
                        </div></ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </div>

    <!-- History -->
    <asp:Panel ID="pnlHistory" runat="server" Visible="false" CssClass="card-panel">
        <div class="card-panel-hdr"><i class="bi bi-clock-history"></i> History for: <strong><asp:Literal ID="litHistId" runat="server" /></strong>
            <asp:Button ID="btnCloseHist" runat="server" CssClass="btn btn-xs btn-default" Text="Close" CausesValidation="false" OnClick="btnCloseHist_Click" style="margin-left:auto;" /></div>
        <div class="card-panel-body" style="padding:0;overflow-x:auto;">
            <asp:GridView ID="gvHistory" runat="server" AutoGenerateColumns="false" CssClass="dfm-table" GridLines="None" EmptyDataText="No history.">
                <RowStyle CssClass="history-row" />
                <Columns>
                    <asp:BoundField DataField="ChangedDate" HeaderText="Changed" DataFormatString="{0:dd-MMM-yyyy HH:mm}" />
                    <asp:BoundField DataField="ChangedBy"   HeaderText="By" />
                    <asp:BoundField DataField="VendorName"  HeaderText="Vendor Name" />
                    <asp:BoundField DataField="IsActive"    HeaderText="Active" />
                </Columns>
            </asp:GridView>
        </div>
    </asp:Panel>
</asp:Content>
