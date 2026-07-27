// Auto-generated designer file for Admin/UserManagement.aspx
namespace DFM_BPM.Admin
{
    public partial class UserManagement
    {
        protected global::System.Web.UI.WebControls.Label          lblMsg;

        // Add User form
        protected global::System.Web.UI.WebControls.HiddenField    hfEditUserId;
        protected global::System.Web.UI.WebControls.TextBox        txtUsername;
        protected global::System.Web.UI.WebControls.TextBox        txtFullName;
        protected global::System.Web.UI.WebControls.TextBox        txtEmail;
        protected global::System.Web.UI.WebControls.TextBox        txtDept;
        protected global::System.Web.UI.WebControls.DropDownList   ddlRole;
        protected global::System.Web.UI.WebControls.TextBox        txtPwd;
        protected global::System.Web.UI.WebControls.Button         btnSaveUser;

        // Users grid
        protected global::System.Web.UI.WebControls.GridView       gvUsers;

        // Role assignment panel
        protected global::System.Web.UI.WebControls.Panel          pnlAssign;
        protected global::System.Web.UI.WebControls.Literal        litAssignUser;
        protected global::System.Web.UI.WebControls.CheckBox       chkRequestor;
        protected global::System.Web.UI.WebControls.CheckBox       chkReviewer;
        protected global::System.Web.UI.WebControls.CheckBox       chkApprover;
        protected global::System.Web.UI.WebControls.CheckBox       chkAdmin;
        protected global::System.Web.UI.WebControls.HiddenField    hfAssignUsername;
        protected global::System.Web.UI.WebControls.Button         btnSaveAssign;
        protected global::System.Web.UI.WebControls.Button         btnCancelAssign;

        // Roles tab
        protected global::System.Web.UI.WebControls.GridView       gvRoles;
        protected global::System.Web.UI.WebControls.GridView       gvAssignments;
        protected global::System.Web.UI.WebControls.TextBox        txtNewRole;
        protected global::System.Web.UI.WebControls.TextBox        txtRoleDesc;
        protected global::System.Web.UI.WebControls.Button         btnSaveRole;

        // Page access tab
        protected global::System.Web.UI.WebControls.DropDownList   ddlPageRole;
        protected global::System.Web.UI.WebControls.GridView       gvPageAccess;
    }
}
