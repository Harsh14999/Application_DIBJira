<%@ Page Title="CAPEX Master" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="CapexMaster.aspx.cs" Inherits="DFM_BPM.Admin.CapexMaster" %>

<asp:Content ID="HeadCt" ContentPlaceHolderID="HeadContent" runat="server">
<style>
.history-row { background:#fef9f0 !important; font-size:.8em; color:#64748b; }
</style>
</asp:Content>

<asp:Content ID="MainCt" ContentPlaceHolderID="MainContent" runat="server">
    <h1 class="page-title"><i class="bi bi-currency-dollar"></i> CAPEX Master
        <small style="font-size:.55em;color:#64748b;font-weight:400;">Editable &ndash; with history</small>
    </h1>

    <asp:Label ID="lblMsg" runat="server" CssClass="alert-info" Visible="false" />

    <!-- Toolbar -->
    <div class="filter-row" style="display:flex;gap:8px;flex-wrap:wrap;margin-bottom:10px;">
        <div class="form-group" style="flex:2;">
            <label>Search by CAPEX ID or Description</label>
            <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control" placeholder="e.g. IT_NP14..." />
        </div>
        <div class="form-group">
            <label>&nbsp;</label>
            <asp:LinkButton ID="btnSearch" runat="server" CssClass="btn btn-primary" OnClick="btnSearch_Click">
                <i class="bi bi-search"></i> Search
            </asp:LinkButton>
        </div>
        <div class="form-group">
            <label>&nbsp;</label>
            <asp:LinkButton ID="btnExport" runat="server" CssClass="btn btn-success" OnClick="btnExport_Click">
                <i class="bi bi-file-excel"></i> Excel
            </asp:LinkButton>
        </div>
        <div class="form-group">
            <label>&nbsp;</label>
            <asp:Button ID="btnNewEntry" runat="server" CssClass="btn btn-info" Text="+ New CAPEX" OnClick="btnNewEntry_Click" CausesValidation="false" />
        </div>
    </div>

    <!-- ── Dynamic CSV Upload ── -->
    <div class="card-panel">
        <div class="dfm-panel-hdr" onclick="DFM.togglePanel('capexUploadBody','capexUpChev')">
            <i id="capexUpChev" class="bi bi-chevron-right dfm-panel-chev"></i>
            <i class="bi bi-upload"></i> Import from CSV
            <small style="font-size:.8em;color:#94a3b8;margin-left:8px;">
                Expected format: Type,ID,Description,Budget,Utilization,Available Budget,Locked Amt,Budget after Locked Amt,Claim Amt,Net Balance
            </small>
        </div>
        <div id="capexUploadBody" class="dfm-panel-body" style="display:none;">
            <div style="display:flex;gap:12px;align-items:flex-end;flex-wrap:wrap;padding:12px;">
                <div class="form-group" style="flex:1;min-width:220px;">
                    <label>Select CAPEX CSV File</label>
                    <asp:FileUpload ID="fuCapex" runat="server" CssClass="form-control" Accept=".csv" />
                </div>
                <div class="form-group">
                    <asp:Button ID="btnImport" runat="server" CssClass="btn btn-secondary" Text="Import CSV" OnClick="btnImport_Click" />
                </div>
                <div class="form-group">
                    <a href="<%= ResolveUrl("~/App_Code/Helpers/SampleCapex.csv") %>" class="btn btn-link btn-xs">
                        <i class="bi bi-download"></i> Download template
                    </a>
                </div>
            </div>
            <asp:Label ID="lblImportResult" runat="server" Visible="false" CssClass="alert-info" style="margin:0 12px 12px;" />
        </div>
    </div>

    <!-- ── Grid ── -->
    <div class="card-panel">
        <div class="card-panel-hdr">
            <i class="bi bi-table"></i> CAPEX Entries (<asp:Literal ID="litCount" runat="server" Text="0" />)
        </div>
        <div class="card-panel-body" style="padding:0;overflow-x:auto;">
            <asp:GridView ID="gv" runat="server" AutoGenerateColumns="false"
                CssClass="dfm-table" GridLines="None"
                DataKeyNames="CapexID" OnRowCommand="gv_RowCommand"
                AllowPaging="true" PageSize="20" OnPageIndexChanging="gv_PageIndexChanging"
                EmptyDataText="No CAPEX entries. Use Import CSV or Add form above.">
                <PagerStyle CssClass="dfm-pager" HorizontalAlign="Center" />
                <PagerSettings Mode="NumericFirstLast" PageButtonCount="5" FirstPageText="&amp;laquo;" LastPageText="&amp;raquo;" />
                <Columns>
                    <asp:BoundField DataField="CapexID"                  HeaderText="CAPEX ID" />
                    <asp:BoundField DataField="Description"              HeaderText="Description" />
                    <asp:BoundField DataField="BudgetedAmount"           HeaderText="Budget" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                    <asp:BoundField DataField="UtilizedAmount"           HeaderText="Utilized" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                    <asp:BoundField DataField="AvailableAmount"          HeaderText="Available" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                    <asp:BoundField DataField="LockedAmount"             HeaderText="Locked" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                    <asp:BoundField DataField="BudgetAfterLockedAmount"  HeaderText="After Locked" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                    <asp:BoundField DataField="ClaimAmount"              HeaderText="Claim" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                    <asp:BoundField DataField="NetBalance"               HeaderText="Net Balance" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                    <asp:TemplateField HeaderText="Active" ItemStyle-CssClass="text-center">
                        <ItemTemplate>
                            <%# Convert.ToBoolean(Eval("IsActive") ?? false) ?
                                "<span class='badge-success'>Yes</span>" :
                                "<span class='badge-danger'>No</span>" %>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="ModifiedBy"   HeaderText="By" />
                    <asp:BoundField DataField="ModifiedDate" HeaderText="Modified" DataFormatString="{0:dd-MMM-yy}" />
                    <asp:TemplateField HeaderText="Actions">
                        <ItemStyle CssClass="action-cell" />
                        <ItemTemplate>
                            <div class="gv-acts">
                                <asp:LinkButton runat="server" CssClass="btn btn-xs btn-primary" CommandName="EditRow"
                                    CommandArgument='<%# Eval("CapexID") %>'>
                                    <i class="bi bi-pencil"></i>                                </asp:LinkButton>
                                <asp:LinkButton runat="server" CssClass="btn btn-xs btn-warning" CommandName="ViewHist"
                                    CommandArgument='<%# Eval("CapexID") %>'>
                                    <i class="bi bi-clock-history"></i></asp:LinkButton>
                                <asp:LinkButton runat="server" CssClass="btn btn-xs btn-danger" CommandName="DeleteRow"
                                    CommandArgument='<%# Eval("CapexID") %>'
                                    OnClientClick="return dfmDelConfirm(this);">
                                    <i class="bi bi-trash"></i>
                                </asp:LinkButton>
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </div>

    <!-- ── Edit Modal ── -->
    <div class="modal fade" id="editModal" tabindex="-1" role="dialog" aria-labelledby="editModalLabel">
        <div class="modal-dialog" role="document">
            <div class="modal-content">
                <div class="modal-header" style="background:#1a3c5e;color:#fff;">
                    <button type="button" class="close" data-dismiss="modal" style="color:#fff;opacity:.8;">&times;</button>
                    <h4 class="modal-title" id="editModalLabel">
                        <i class="bi bi-pencil-square"></i> <%= EditModalTitle %>
                    </h4>
                </div>
                <div class="modal-body" style="padding:16px;">
                    <asp:HiddenField ID="hfEditId" runat="server" Value="" />
                    <div class="form-grid-5">
                        <div class="form-group">
                            <label>CAPEX ID <span style="color:#dc2626;">*</span></label>
                            <asp:TextBox ID="txtId" runat="server" CssClass="form-control" />
                        </div>
                        <div class="form-group col-span-3">
                            <label>Description</label>
                            <asp:TextBox ID="txtDesc" runat="server" CssClass="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Active</label>
                            <asp:DropDownList ID="ddlActive" runat="server" CssClass="form-control">
                                <asp:ListItem Value="Yes">Yes</asp:ListItem>
                                <asp:ListItem Value="No">No</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                    </div>
                    <div class="form-grid-5">
                        <div class="form-group"><label>Budget</label><asp:TextBox ID="txtBudget" runat="server" CssClass="form-control" Text="0" /></div>
                        <div class="form-group"><label>Utilization</label><asp:TextBox ID="txtUtil" runat="server" CssClass="form-control" Text="0" /></div>
                        <div class="form-group"><label>Available</label><asp:TextBox ID="txtAvail" runat="server" CssClass="form-control" Text="0" /></div>
                        <div class="form-group"><label>Locked Amt</label><asp:TextBox ID="txtLocked" runat="server" CssClass="form-control" Text="0" /></div>
                        <div class="form-group"><label>After Locked</label><asp:TextBox ID="txtAfterLock" runat="server" CssClass="form-control" Text="0" /></div>
                    </div>
                    <div class="form-grid-5">
                        <div class="form-group"><label>Claim Amount</label><asp:TextBox ID="txtClaim" runat="server" CssClass="form-control" Text="0" /></div>
                        <div class="form-group"><label>Net Balance</label><asp:TextBox ID="txtNet" runat="server" CssClass="form-control" Text="0" /></div>
                    </div>
                </div>
                <div class="modal-footer">
                    <asp:Button ID="btnSave" runat="server" CssClass="btn btn-success" Text="Save" OnClick="btnSave_Click" />
                    <asp:Button ID="btnReset" runat="server" CssClass="btn btn-default" Text="Cancel" CausesValidation="false" OnClick="btnReset_Click" />
                </div>
            </div>
        </div>
    </div>

    <!-- ── History Modal ── -->
    <div class="modal fade" id="histModal" tabindex="-1" role="dialog" aria-labelledby="histModalLabel">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header" style="background:#1a3c5e;color:#fff;">
                    <button type="button" class="close" data-dismiss="modal" style="color:#fff;opacity:.8;">&times;</button>
                    <h4 class="modal-title" id="histModalLabel">
                        <i class="bi bi-clock-history"></i> Change History &ndash;
                        <asp:Literal ID="litHistId" runat="server" />
                    </h4>
                </div>
                <div class="modal-body" style="padding:0;overflow-x:auto;">
                    <asp:GridView ID="gvHistory" runat="server" AutoGenerateColumns="false"
                        CssClass="dfm-table" GridLines="None" EmptyDataText="No history records.">
                        <RowStyle CssClass="history-row" />
                        <Columns>
                            <asp:BoundField DataField="ChangedDate"             HeaderText="Changed" DataFormatString="{0:dd-MMM-yyyy HH:mm}" />
                            <asp:BoundField DataField="ChangedBy"               HeaderText="By" />
                            <asp:BoundField DataField="Description"             HeaderText="Description" />
                            <asp:BoundField DataField="BudgetedAmount"          HeaderText="Budget" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                            <asp:BoundField DataField="UtilizedAmount"          HeaderText="Utilized" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                            <asp:BoundField DataField="AvailableAmount"         HeaderText="Available" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                            <asp:BoundField DataField="LockedAmount"            HeaderText="Locked" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                            <asp:BoundField DataField="BudgetAfterLockedAmount" HeaderText="After Locked" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                            <asp:BoundField DataField="ClaimAmount"             HeaderText="Claim" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                            <asp:BoundField DataField="NetBalance"              HeaderText="Net" DataFormatString="{0:N2}" ItemStyle-CssClass="text-right" />
                        </Columns>
                    </asp:GridView>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-default" data-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

    <!-- Delete Confirmation Modal -->
    <div class="modal fade" id="delModal" tabindex="-1" role="dialog" aria-labelledby="delModalLabel">
        <div class="modal-dialog" role="document" style="max-width:460px;">
            <div class="modal-content">
                <div class="modal-header" style="background:#b91c1c;color:#fff;">
                    <button type="button" class="close" data-dismiss="modal" style="color:#fff;opacity:.8;">&times;</button>
                    <h4 class="modal-title" id="delModalLabel">
                        <i class="bi bi-exclamation-triangle-fill"></i> Confirm Delete
                    </h4>
                </div>
                <div class="modal-body" style="padding:24px;text-align:center;">
                    <div style="font-size:2.8em;color:#dc2626;margin-bottom:10px;"><i class="bi bi-trash3-fill"></i></div>
                    <p style="font-size:.97em;font-weight:600;color:#1e293b;margin-bottom:4px;">Are you sure you want to delete</p>
                    <p id="delConfirmId" style="font-size:1.1em;font-weight:800;color:#dc2626;word-break:break-all;"></p>
                    <p style="font-size:.82em;color:#64748b;margin-top:8px;">This action <strong>cannot be undone</strong>. The record will be permanently removed.</p>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-default" data-dismiss="modal"><i class="bi bi-x-lg"></i> Cancel</button>
                    <button type="button" class="btn btn-danger" onclick="dfmDoDelete();"><i class="bi bi-trash"></i> Yes, Delete</button>
                </div>
            </div>
        </div>
    </div>
<script>
var _dfmDelTarget = null, _dfmDelArg = null;
function dfmDelConfirm(btn) {
    var href = btn.href || '';
    var m = href.match(/__doPostBack\('([^']+)','([^']*)'\)/);
    if (m) { _dfmDelTarget = m[1]; _dfmDelArg = m[2]; }
    var row = typeof btn.closest === 'function' ? btn.closest('tr') : null;
    var id = row && row.cells[0] ? row.cells[0].textContent.trim() : 'this entry';
    var el = document.getElementById('delConfirmId');
    if (el) el.textContent = id;
    if (typeof jQuery !== 'undefined') jQuery('#delModal').modal('show');
    return false;
}
function dfmDoDelete() {
    if (typeof jQuery !== 'undefined') jQuery('#delModal').modal('hide');
    var t = _dfmDelTarget, a = _dfmDelArg;
    if (t) setTimeout(function() { __doPostBack(t, a); }, 200);
}
</script>
</asp:Content>
