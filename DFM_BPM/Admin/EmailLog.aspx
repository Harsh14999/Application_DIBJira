<%@ Page Title="Email Log" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="EmailLog.aspx.cs" Inherits="DFM_BPM.Admin.EmailLog" %>

<asp:Content ID="HeadCt" ContentPlaceHolderID="HeadContent" runat="server">
<style>
.status-Sent   { color:#16a34a; font-weight:700; }
.status-Failed { color:#dc2626; font-weight:700; }
.status-Disabled { color:#64748b; font-weight:700; }
.status-Pending { color:#d97706; font-weight:700; }
</style>
</asp:Content>

<asp:Content ID="MainCt" ContentPlaceHolderID="MainContent" runat="server">
<div class="container-fluid">
    <div class="row">
        <div class="col-sm-12">
            <h2 class="page-header"><span class="glyphicon glyphicon-list-alt"></span> Email Log</h2>
        </div>
    </div>

    <%-- Filter --%>
    <div class="row">
        <div class="col-sm-12">
            <div class="panel panel-default">
                <div class="panel-body">
                    <div class="form-inline">
                        <div class="form-group">
                            <label>PET Form ID</label>
                            <asp:TextBox ID="txtFilterPetId" runat="server" CssClass="form-control input-sm" placeholder="(any)" style="width:100px;" />
                        </div>
                        &nbsp;
                        <div class="form-group">
                            <label>Top N</label>
                            <asp:DropDownList ID="ddlTopN" runat="server" CssClass="form-control input-sm">
                                <asp:ListItem Text="100"  Value="100"  Selected="True" />
                                <asp:ListItem Text="200"  Value="200" />
                                <asp:ListItem Text="500"  Value="500" />
                                <asp:ListItem Text="1000" Value="1000" />
                            </asp:DropDownList>
                        </div>
                        &nbsp;
                        <asp:Button ID="btnFilter" runat="server" Text="Filter" CssClass="btn btn-primary btn-sm"
                            OnClick="btnFilter_Click" />
                    </div>
                </div>
            </div>
        </div>
    </div>

    <%-- Grid --%>
    <div class="row">
        <div class="col-sm-12">
            <div class="panel panel-default">
                <div class="panel-body">
                    <asp:GridView ID="gvLog" runat="server" AutoGenerateColumns="false"
                        CssClass="table table-bordered table-hover table-condensed"
                        DataKeyNames="LogID"
                        OnRowCommand="gvLog_RowCommand">
                        <Columns>
                            <asp:BoundField DataField="LogID"       HeaderText="ID" />
                            <asp:BoundField DataField="SentDate"    HeaderText="Date"    DataFormatString="{0:dd-MMM-yyyy HH:mm}" />
                            <asp:BoundField DataField="TriggerEvent" HeaderText="Event" />
                            <asp:BoundField DataField="PetFormID"   HeaderText="PET ID" />
                            <asp:BoundField DataField="ToAddress"   HeaderText="To" />
                            <asp:BoundField DataField="Subject"     HeaderText="Subject" />
                            <asp:TemplateField HeaderText="Status">
                                <ItemTemplate>
                                    <span class='<%# "status-" + Eval("Status") %>'><%# Eval("Status") %></span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="SentBy"      HeaderText="Sent By" />
                            <asp:TemplateField HeaderText="">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbView" runat="server" CssClass="btn btn-xs btn-default"
                                        CommandName="ViewDetail" CommandArgument='<%# Eval("LogID") %>'>
                                        <span class="glyphicon glyphicon-eye-open"></span> View
                                    </asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </div>
        </div>
    </div>
</div>

<%-- Detail Modal --%>
<div class="modal fade" id="logDetailModal" tabindex="-1" role="dialog">
    <div class="modal-dialog modal-lg" role="document">
        <div class="modal-content">
            <div class="modal-header">
                <button type="button" class="close" data-dismiss="modal"><span>&times;</span></button>
                <h4 class="modal-title">Email Detail</h4>
            </div>
            <div class="modal-body">
                <asp:Panel ID="pnlDetail" runat="server">
                    <table class="table table-bordered table-condensed">
                        <tr><td style="width:130px;"><strong>Log ID</strong></td>    <td><asp:Literal ID="litDLogID"    runat="server" /></td></tr>
                        <tr><td><strong>Date</strong></td>         <td><asp:Literal ID="litDDate"     runat="server" /></td></tr>
                        <tr><td><strong>Event</strong></td>        <td><asp:Literal ID="litDEvent"    runat="server" /></td></tr>
                        <tr><td><strong>PET Form ID</strong></td>  <td><asp:Literal ID="litDPetID"    runat="server" /></td></tr>
                        <tr><td><strong>To</strong></td>           <td><asp:Literal ID="litDTo"       runat="server" /></td></tr>
                        <tr><td><strong>CC</strong></td>           <td><asp:Literal ID="litDCc"       runat="server" /></td></tr>
                        <tr><td><strong>Subject</strong></td>      <td><asp:Literal ID="litDSubject"  runat="server" /></td></tr>
                        <tr><td><strong>Status</strong></td>       <td><asp:Literal ID="litDStatus"   runat="server" /></td></tr>
                        <tr><td><strong>Error</strong></td>        <td><asp:Literal ID="litDError"    runat="server" /></td></tr>
                        <tr><td><strong>Sent By</strong></td>      <td><asp:Literal ID="litDSentBy"   runat="server" /></td></tr>
                    </table>
                    <div class="panel panel-default">
                        <div class="panel-heading"><strong>Email Body</strong></div>
                        <div class="panel-body" style="max-height:400px;overflow-y:auto;">
                            <asp:Literal ID="litDBody" runat="server" />
                        </div>
                    </div>
                </asp:Panel>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-default" data-dismiss="modal">Close</button>
            </div>
        </div>
    </div>
</div>
</asp:Content>
