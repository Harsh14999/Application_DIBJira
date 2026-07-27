<%@ Page Title="Email Configuration" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="EmailConfig.aspx.cs" Inherits="DFM_BPM.Admin.EmailConfig" %>

<asp:Content ID="HeadCt" ContentPlaceHolderID="HeadContent" runat="server">
<style>
.config-table td, .config-table th { vertical-align: middle !important; }
.masked { font-family: monospace; letter-spacing: 2px; color: #64748b; }
</style>
</asp:Content>

<asp:Content ID="MainCt" ContentPlaceHolderID="MainContent" runat="server">
<div class="container-fluid">
    <div class="row">
        <div class="col-sm-12">
            <h2 class="page-header"><span class="glyphicon glyphicon-envelope"></span> Email Configuration</h2>
        </div>
    </div>

    <%-- Alert area --%>
    <asp:Panel ID="pnlAlert" runat="server" Visible="false">
        <div class="alert" role="alert">
            <asp:Literal ID="litAlert" runat="server" />
        </div>
    </asp:Panel>

    <%-- Config Grid --%>
    <div class="row">
        <div class="col-sm-12">
            <div class="panel panel-default">
                <div class="panel-heading"><strong>SMTP Settings</strong></div>
                <div class="panel-body">
                    <asp:GridView ID="gvConfig" runat="server" AutoGenerateColumns="false"
                        CssClass="table table-bordered table-hover config-table"
                        DataKeyNames="ConfigID"
                        OnRowCommand="gvConfig_RowCommand">
                        <Columns>
                            <asp:BoundField DataField="ConfigKey"   HeaderText="Key"           ReadOnly="true" />
                            <asp:TemplateField HeaderText="Value">
                                <ItemTemplate>
                                    <asp:Label ID="lblVal" runat="server"
                                        Text='<%# Eval("IsEncrypted") != null && Convert.ToBoolean(Eval("IsEncrypted")) ? "●●●●●●●●" : Eval("ConfigValue") %>'
                                        CssClass='<%# Eval("IsEncrypted") != null && Convert.ToBoolean(Eval("IsEncrypted")) ? "masked" : "" %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Encrypted">
                                <ItemTemplate>
                                    <span class='badge <%# Convert.ToBoolean(Eval("IsEncrypted")) ? "label-warning" : "label-default" %>'>
                                        <%# Convert.ToBoolean(Eval("IsEncrypted")) ? "Yes" : "No" %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="UpdatedBy"   HeaderText="Updated By" />
                            <asp:BoundField DataField="UpdatedDate" HeaderText="Updated"    DataFormatString="{0:dd-MMM-yyyy HH:mm}" />
                            <asp:TemplateField HeaderText="Action">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbEdit" runat="server" CssClass="btn btn-xs btn-primary"
                                        CommandName="EditRow" CommandArgument='<%# Eval("ConfigKey") %>'>
                                        <span class="glyphicon glyphicon-pencil"></span> Edit
                                    </asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </div>
        </div>
    </div>

    <%-- Edit Panel --%>
    <asp:Panel ID="pnlEdit" runat="server" Visible="false">
    <div class="row">
        <div class="col-sm-6">
            <div class="panel panel-info">
                <div class="panel-heading"><strong>Edit Setting</strong></div>
                <div class="panel-body">
                    <div class="form-group">
                        <label>Key</label>
                        <asp:TextBox ID="txtEditKey" runat="server" CssClass="form-control" ReadOnly="true" />
                    </div>
                    <div class="form-group">
                        <label>Value <small class="text-muted">(for passwords, enter plain text — check "Encrypt" below)</small></label>
                        <asp:TextBox ID="txtEditValue" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="2" />
                    </div>
                    <div class="form-group">
                        <div class="checkbox">
                            <label>
                                <asp:CheckBox ID="chkEditEncrypt" runat="server" /> Encrypt this value before saving
                            </label>
                        </div>
                    </div>
                    <asp:Button ID="btnSaveConfig" runat="server" Text="Save" CssClass="btn btn-success"
                        OnClick="btnSaveConfig_Click" />
                    &nbsp;
                    <asp:Button ID="btnCancelEdit" runat="server" Text="Cancel" CssClass="btn btn-default"
                        OnClick="btnCancelEdit_Click" CausesValidation="false" />
                </div>
            </div>
        </div>
    </div>
    </asp:Panel>

    <%-- Encrypt / Decrypt Tool --%>
    <div class="row">
        <div class="col-sm-6">
            <div class="panel panel-default">
                <div class="panel-heading"><strong>Encrypt / Decrypt Tool</strong>
                    <span class="text-muted" style="font-size:.85em;"> — for manual inspection</span></div>
                <div class="panel-body">
                    <div class="form-group">
                        <asp:TextBox ID="txtToolInput" runat="server" CssClass="form-control" placeholder="Enter plain text or cipher text" />
                    </div>
                    <asp:Button ID="btnEncrypt" runat="server" Text="Encrypt" CssClass="btn btn-warning btn-sm"
                        OnClick="btnEncrypt_Click" CausesValidation="false" />
                    &nbsp;
                    <asp:Button ID="btnDecrypt" runat="server" Text="Decrypt" CssClass="btn btn-default btn-sm"
                        OnClick="btnDecrypt_Click" CausesValidation="false" />
                    <asp:Panel ID="pnlToolResult" runat="server" Visible="false" style="margin-top:10px;">
                        <div class="alert alert-info" style="word-break:break-all;">
                            <strong>Result:</strong>
                            <asp:Literal ID="litToolResult" runat="server" />
                        </div>
                    </asp:Panel>
                </div>
            </div>
        </div>

        <%-- Test SMTP --%>
        <div class="col-sm-6">
            <div class="panel panel-default">
                <div class="panel-heading"><strong>Test SMTP</strong></div>
                <div class="panel-body">
                    <div class="form-group">
                        <label>Send test email to</label>
                        <asp:TextBox ID="txtTestTo" runat="server" CssClass="form-control" placeholder="recipient@example.com" />
                    </div>
                    <asp:Button ID="btnTestSmtp" runat="server" Text="Send Test Email" CssClass="btn btn-primary btn-sm"
                        OnClick="btnTestSmtp_Click" CausesValidation="false" />
                    <asp:Panel ID="pnlTestResult" runat="server" Visible="false" style="margin-top:10px;">
                        <div class="alert">
                            <asp:Literal ID="litTestResult" runat="server" />
                        </div>
                    </asp:Panel>
                </div>
            </div>
        </div>
    </div>

</div>
</asp:Content>
