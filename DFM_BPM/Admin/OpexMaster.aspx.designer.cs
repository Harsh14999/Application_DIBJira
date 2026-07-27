// Auto-generated designer file for Admin/OpexMaster.aspx
namespace DFM_BPM.Admin
{
    public partial class OpexMaster
    {
        protected global::System.Web.UI.WebControls.Label      lblMsg;
        protected global::System.Web.UI.WebControls.TextBox    txtSearch;
        protected global::System.Web.UI.WebControls.LinkButton btnSearch;
        protected global::System.Web.UI.WebControls.LinkButton btnExport;
        protected global::System.Web.UI.WebControls.Button     btnNewEntry;
        protected global::System.Web.UI.WebControls.Literal    litTotalBudget;
        protected global::System.Web.UI.WebControls.Literal    litTotalUtil;
        protected global::System.Web.UI.WebControls.Literal    litTotalLocked;
        protected global::System.Web.UI.WebControls.Literal    litTotalAvail;
        protected global::System.Web.UI.WebControls.Literal    litCount;
        protected global::System.Web.UI.WebControls.GridView   gv;

        // Editable master controls
        protected global::System.Web.UI.WebControls.Panel          pnlForm;
        protected global::System.Web.UI.WebControls.HiddenField    hfEditId;
        protected global::System.Web.UI.WebControls.TextBox        txtId;
        protected global::System.Web.UI.WebControls.TextBox        txtDesc;
        protected global::System.Web.UI.WebControls.TextBox        txtBudget;
        protected global::System.Web.UI.WebControls.TextBox        txtUtil;
        protected global::System.Web.UI.WebControls.TextBox        txtAvail;
        protected global::System.Web.UI.WebControls.TextBox        txtLocked;
        protected global::System.Web.UI.WebControls.TextBox        txtAfterLock;
        protected global::System.Web.UI.WebControls.TextBox        txtClaim;
        protected global::System.Web.UI.WebControls.TextBox        txtNet;
        protected global::System.Web.UI.WebControls.DropDownList   ddlActive;
        protected global::System.Web.UI.WebControls.Button         btnSave;
        protected global::System.Web.UI.WebControls.Button         btnReset;

        // CSV import
        protected global::System.Web.UI.WebControls.FileUpload     fuOpex;
        protected global::System.Web.UI.WebControls.Button         btnImport;
        protected global::System.Web.UI.WebControls.Label          lblImportResult;

        // History controls (modal div - not a server Panel)
        protected global::System.Web.UI.WebControls.Literal    litHistId;
        protected global::System.Web.UI.WebControls.GridView   gvHistory;
    }
}
