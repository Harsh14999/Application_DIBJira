<%@ Page Title="Engineer Master" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="EngineerMaster.aspx.cs" Inherits="DFM_BPM.Admin.EngineerMaster" %>

<asp:Content ID="MainCt" ContentPlaceHolderID="MainContent" runat="server">
    <h1 class="page-title"><i class="bi bi-person-gear"></i> Engineer Master
        <small style="font-size:.55em;color:#64748b;font-weight:400;">Engineers appear as leaf nodes in the Portfolio Hierarchy</small>
    </h1>
    <asp:Label ID="lblMsg" runat="server" CssClass="alert-info" Visible="false" />

    <div style="display:flex;gap:8px;flex-wrap:wrap;margin-bottom:10px;">
        <div class="form-group" style="flex:2;"><label>Search by Name</label>
            <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control" /></div>
        <div class="form-group"><label>&nbsp;</label>
            <asp:LinkButton ID="btnSearch" runat="server" CssClass="btn btn-primary" OnClick="btnSearch_Click"><i class="bi bi-search"></i> Search</asp:LinkButton></div>
    </div>

    <!-- Add/Edit -->
    <div class="card-panel">
        <div class="dfm-panel-hdr" onclick="DFM.togglePanel('engFormBody','engChev')">
            <i id="engChev" class="bi bi-chevron-right dfm-panel-chev <%= FormChevClass %>"></i>
            <i class="bi bi-pencil-square"></i> Add / Edit Engineer
        </div>
        <div id="engFormBody" class="dfm-panel-body" style="<%= FormBodyStyle %>">
            <asp:HiddenField ID="hfEditId" runat="server" Value="0" />
            <div class="form-grid-4">
                <div class="form-group"><label>Engineer Name *</label><asp:TextBox ID="txtName" runat="server" CssClass="form-control" /></div>
                <div class="form-group"><label>Reports Under (Hierarchy)</label>
                    <asp:DropDownList ID="ddlParent" runat="server" CssClass="form-control select2-enable" /></div>
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
        <div class="card-panel-hdr"><i class="bi bi-table"></i> Engineers (<asp:Literal ID="litCount" runat="server" Text="0" />)</div>
        <div class="card-panel-body" style="padding:0;overflow-x:auto;">
            <asp:GridView ID="gv" runat="server" AutoGenerateColumns="false"
                CssClass="dfm-table" GridLines="None" DataKeyNames="ResourceID"
                OnRowCommand="gv_RowCommand"
                AllowPaging="true" PageSize="20" OnPageIndexChanging="gv_PageIndexChanging"
                EmptyDataText="No engineers configured yet. Add one above.">
                <PagerStyle CssClass="dfm-pager" HorizontalAlign="Center" />
                <PagerSettings Mode="NumericFirstLast" PageButtonCount="5" FirstPageText="&amp;laquo;" LastPageText="&amp;raquo;" />
                <Columns>
                    <asp:BoundField DataField="ResourceName"  HeaderText="Engineer Name" />
                    <asp:BoundField DataField="ParentName"    HeaderText="Reports Under" />
                    <asp:TemplateField HeaderText="Active" ItemStyle-CssClass="text-center">
                        <ItemTemplate><%# Convert.ToBoolean(Eval("IsActive") ?? false) ? "<span class='badge-success'>Yes</span>" : "<span class='badge-danger'>No</span>" %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="ModifiedBy"   HeaderText="By" />
                    <asp:BoundField DataField="ModifiedDate" HeaderText="Modified" DataFormatString="{0:dd-MMM-yy}" />
                    <asp:TemplateField HeaderText="Actions"><ItemStyle CssClass="action-cell" />
                        <ItemTemplate><div class="gv-acts">
                            <asp:LinkButton runat="server" CssClass="btn btn-xs btn-primary" CommandName="EditRow" CommandArgument='<%# Eval("ResourceID") %>'><i class="bi bi-pencil"></i> Edit</asp:LinkButton>
                            <asp:LinkButton runat="server" CssClass="btn btn-xs btn-danger" CommandName="DeleteRow" CommandArgument='<%# Eval("ResourceID") %>' OnClientClick="return confirm('Delete this engineer?');"><i class="bi bi-trash"></i></asp:LinkButton>
                        </div></ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </div>
</asp:Content>
