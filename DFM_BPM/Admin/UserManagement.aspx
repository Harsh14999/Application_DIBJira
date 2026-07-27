<%@ Page Title="User Management" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="UserManagement.aspx.cs" Inherits="DFM_BPM.Admin.UserManagement" %>

<asp:Content ID="HeadCt" ContentPlaceHolderID="HeadContent" runat="server">
<style>
.tab-pane { padding-top: 14px; }
.role-badge { display:inline-block; padding:2px 8px; border-radius:10px; font-size:.78em; font-weight:700; margin:1px; }
.role-requestor { background:#dbeafe; color:#1d4ed8; }
.role-reviewer  { background:#d1fae5; color:#065f46; }
.role-approver  { background:#fef3c7; color:#92400e; }
.role-admin     { background:#fee2e2; color:#991b1b; }
</style>
</asp:Content>

<asp:Content ID="MainCt" ContentPlaceHolderID="MainContent" runat="server">
    <h1 class="page-title"><i class="bi bi-people"></i> User Management
        <small style="font-size:.5em;color:#64748b;font-weight:400;">(Windows Authentication)</small>
    </h1>
    <asp:Label ID="lblMsg" runat="server" CssClass="alert-info" Visible="false" />

    <!-- Bootstrap Tabs: Users | Roles -->
    <ul class="nav nav-tabs" role="tablist">
        <li class="active"><a href="#tabUsers" data-toggle="tab"><i class="bi bi-person"></i> Users &amp; Role Assignments</a></li>
        <li><a href="#tabRoles" data-toggle="tab"><i class="bi bi-shield"></i> System Roles</a></li>
    </ul>

    <div class="tab-content">
        <!-- ============ TAB 1: USERS ============ -->
        <div class="tab-pane active" id="tabUsers">

            <!-- Add/lookup Windows user -->
            <div class="card-panel">
                <div class="dfm-panel-hdr" onclick="DFM.togglePanel('userFormBody','userChev')">
                    <i id="userChev" class="bi bi-chevron-right dfm-panel-chev"></i>
                    <i class="bi bi-person-plus"></i> Register Windows User / Update Profile
                </div>
                <div id="userFormBody" class="dfm-panel-body">
                    <p style="font-size:.85em;color:#64748b;margin-bottom:10px;">
                        Enter the Windows username (e.g. <code>jdoe</code> or <code>DOMAIN\jdoe</code>).
                        The user will be auto-provisioned as Requestor on first login.
                        Use this form to pre-register or update their display name and primary role.
                    </p>
                    <asp:HiddenField ID="hfEditUserId" runat="server" Value="0" />
                    <div class="form-grid-4">
                        <div class="form-group">
                            <label>Windows Username *</label>
                            <asp:TextBox ID="txtUsername" runat="server" CssClass="form-control" placeholder="e.g. jdoe" />
                        </div>
                        <div class="form-group">
                            <label>Display Name</label>
                            <asp:TextBox ID="txtFullName" runat="server" CssClass="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Email</label>
                            <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Department</label>
                            <asp:TextBox ID="txtDept" runat="server" CssClass="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Primary Role *</label>
                            <asp:DropDownList ID="ddlRole" runat="server" CssClass="form-control" />
                        </div>
                        <div class="form-group" style="align-self:end;">
                            <asp:Button ID="btnSaveUser" runat="server" CssClass="btn btn-primary" Text="Save User" OnClick="btnSaveUser_Click" />
                            <asp:Button ID="btnCancelEdit" runat="server" CssClass="btn btn-default" Text="Clear" CausesValidation="false" OnClick="btnCancelEdit_Click" />
                        </div>
                    </div>
                </div>
            </div>

            <!-- Users Grid -->
            <div class="card-panel">
                <div class="card-panel-hdr">
                    <i class="bi bi-table"></i> All Registered Users
                    <asp:Button ID="btnExportUsers" runat="server" CssClass="btn btn-xs btn-success" style="margin-left:auto;"
                        Text="Export Excel" OnClick="btnExportUsers_Click" />
                </div>
                <div class="card-panel-body" style="padding:0;overflow-x:auto;">
                    <asp:GridView ID="gvUsers" runat="server" AutoGenerateColumns="false"
                        CssClass="dfm-table" GridLines="None"
                        OnRowCommand="gvUsers_RowCommand"
                        EmptyDataText="No users registered yet.">
                        <Columns>
                            <asp:BoundField DataField="Username"   HeaderText="Windows User" />
                            <asp:BoundField DataField="FullName"   HeaderText="Display Name" />
                            <asp:BoundField DataField="Email"      HeaderText="Email" />
                            <asp:BoundField DataField="Department" HeaderText="Dept" />
                            <asp:BoundField DataField="RoleName"   HeaderText="Primary Role" />
                            <asp:TemplateField HeaderText="Workflow Roles">
                                <ItemTemplate>
                                    <asp:Literal ID="litRoles" runat="server"
                                        Text='<%# GetRoleBadges(Eval("Username").ToString()) %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Enabled">
                                <ItemTemplate>
                                    <span class='<%# ((bool)Eval("IsEnabled")) ? "badge-success" : "badge-danger" %>'>
                                        <%# ((bool)Eval("IsEnabled")) ? "Yes" : "No" %></span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="LastLoginDate" HeaderText="Last Login" DataFormatString="{0:dd-MMM-yyyy HH:mm}" />
                            <asp:TemplateField HeaderText="Actions">
                                <ItemTemplate>
                                    <div class="gv-acts">
                                        <asp:LinkButton runat="server" CssClass="btn btn-xs btn-default"
                                            CommandName="EditUser" CommandArgument='<%# Eval("UserID") %>'
                                            title="Edit user profile">
                                            <i class="bi bi-pencil"></i>
                                        </asp:LinkButton>
                                        <asp:LinkButton runat="server" CssClass="btn btn-xs btn-warning"
                                            CommandName="ToggleEnable" CommandArgument='<%# Eval("UserID") %>'
                                            OnClientClick="return confirm('Toggle enabled status?')">
                                            <i class="bi bi-toggle-on"></i>
                                        </asp:LinkButton>
                                        <asp:LinkButton runat="server" CssClass="btn btn-xs btn-default"
                                            CommandName="Assign" CommandArgument='<%# Eval("Username") %>'
                                            title="Manage workflow role assignments">
                                            <i class="bi bi-person-badge"></i> Roles
                                        </asp:LinkButton>
                                        <asp:LinkButton runat="server" CssClass="btn btn-xs btn-danger"
                                            CommandName="DeleteUser" CommandArgument='<%# Eval("UserID") %>'
                                            OnClientClick="return confirm('Remove this user from the application?')">
                                            <i class="bi bi-trash"></i>
                                        </asp:LinkButton>
                                    </div>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </div>

            <!-- Role Assignment panel -->
            <asp:Panel ID="pnlAssign" runat="server" Visible="false" CssClass="card-panel">
                <div class="card-panel-hdr"><i class="bi bi-person-badge"></i>
                    Workflow Role Assignments for: <strong><asp:Literal ID="litAssignUser" runat="server" /></strong>
                </div>
                <div class="card-panel-body">
                    <p style="font-size:.88em;color:#64748b;">
                        Assign workflow roles in addition to the primary role.<br />
                        <strong>Requestor</strong> = can raise PET forms &nbsp;|&nbsp;
                        <strong>Reviewer</strong> = can review before approval &nbsp;|&nbsp;
                        <strong>Approver</strong> = can approve / reject PET &nbsp;|&nbsp;
                        <strong>Admin</strong> = full administration access
                    </p>
                    <div class="form-grid-4">
                        <div class="form-group">
                            <asp:CheckBox ID="chkRequestor" runat="server" Text=" Requestor" />
                        </div>
                        <div class="form-group">
                            <asp:CheckBox ID="chkReviewer" runat="server" Text=" Reviewer" />
                        </div>
                        <div class="form-group">
                            <asp:CheckBox ID="chkApprover" runat="server" Text=" Approver" />
                        </div>
                        <div class="form-group">
                            <asp:CheckBox ID="chkAdmin" runat="server" Text=" Admin" />
                        </div>
                    </div>
                    <asp:HiddenField ID="hfAssignUsername" runat="server" />
                    <asp:Button ID="btnSaveAssign" runat="server" CssClass="btn btn-primary" Text="Save Role Assignments" OnClick="btnSaveAssign_Click" />
                    <asp:Button ID="btnCancelAssign" runat="server" CssClass="btn btn-default" Text="Cancel" OnClick="btnCancelAssign_Click" CausesValidation="false" />
                </div>
            </asp:Panel>
        </div><!-- /tabUsers -->

        <!-- ============ TAB 2: ROLES ============ -->
        <div class="tab-pane" id="tabRoles">
            <div class="card-panel">
                <div class="card-panel-hdr"><i class="bi bi-shield"></i> System Roles</div>
                <div class="card-panel-body">
                    <p style="font-size:.85em;color:#64748b;">
                        The four workflow roles and their access levels in the system.
                    </p>
                    <table class="dfm-table">
                        <thead><tr><th>Role</th><th>Description</th><th>Permissions</th></tr></thead>
                        <tbody>
                            <tr><td><span class="role-badge role-requestor">Requestor</span></td><td>Default role for all Windows users</td><td>Raise &amp; edit own PET forms</td></tr>
                            <tr><td><span class="role-badge role-reviewer">Reviewer</span></td><td>Optional first-level review</td><td>Review PET forms before approval; send back or forward to Approver</td></tr>
                            <tr><td><span class="role-badge role-approver">Approver</span></td><td>Final approval authority</td><td>Approve / Reject / Send Back PET forms</td></tr>
                            <tr><td><span class="role-badge role-admin">Admin</span></td><td>System administrator</td><td>All pages; User Management; Master data; JIRA Sync</td></tr>
                        </tbody>
                    </table>
                </div>
            </div>
            <div class="card-panel">
                <div class="card-panel-hdr"><i class="bi bi-list-check"></i> Add Custom Role</div>
                <div class="card-panel-body">
                    <div class="form-grid-4">
                        <div class="form-group">
                            <label>Role Name</label>
                            <asp:TextBox ID="txtNewRole" runat="server" CssClass="form-control" placeholder="e.g. Auditor" />
                        </div>
                        <div class="form-group">
                            <label>Description</label>
                            <asp:TextBox ID="txtRoleDesc" runat="server" CssClass="form-control" />
                        </div>
                        <div class="form-group" style="align-self:end;">
                            <asp:Button ID="btnSaveRole" runat="server" CssClass="btn btn-primary" Text="Add Role" OnClick="btnSaveRole_Click" />
                        </div>
                    </div>
                    <div class="card-panel-body" style="padding:0;overflow-x:auto;margin-top:10px;">
                        <asp:GridView ID="gvRoles" runat="server" AutoGenerateColumns="false"
                            CssClass="dfm-table" GridLines="None" EmptyDataText="No custom roles.">
                            <Columns>
                                <asp:BoundField DataField="RoleID"      HeaderText="ID" />
                                <asp:BoundField DataField="RoleName"    HeaderText="Role Name" />
                                <asp:BoundField DataField="Description" HeaderText="Description" />
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
            </div>
        </div><!-- /tabRoles -->
    </div><!-- tab-content -->
</asp:Content>
