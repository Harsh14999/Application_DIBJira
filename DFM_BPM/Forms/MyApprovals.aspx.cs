using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using DFM_BPM.App_Code.DAL;
using DFM_BPM.App_Code.Helpers;

namespace DFM_BPM.Forms
{
    public partial class MyApprovals : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindPending();
                BindProcessed();
            }
        }

        private void BindPending()
        {
            string user = AuthHelper.CurrentUserShort;
            DataTable dt = Db.Query(@"
                SELECT f.PetFormID, f.PetRefNo, f.ProjectID, f.Title, f.CreatedBy,
                       f.Status, f.SubmittedDate
                FROM dbo.PetForm f
                WHERE f.Status IN ('PendingApproval','PendingReview')
                  AND (
                      (f.Status = 'PendingApproval' AND f.ApproverUsername = @u)
                   OR (f.Status = 'PendingReview'   AND f.ReviewerUsername = @u)
                  )
                ORDER BY f.SubmittedDate",
                Db.P("@u", user));

            litPendingCount.Text = dt.Rows.Count.ToString();
            rptPending.DataSource = dt;
            rptPending.DataBind();

            pnlEmptyPending.Visible = (dt.Rows.Count == 0);
        }

        private void BindProcessed()
        {
            string user = AuthHelper.CurrentUserShort;
            DataTable dt = Db.Query(@"
                SELECT TOP 50
                       f.PetFormID, f.PetRefNo, f.ProjectID, f.Title, f.CreatedBy,
                       f.Status,
                       (SELECT MAX(h.ActionDate) FROM dbo.PetWorkflowHistory h WHERE h.PetFormID = f.PetFormID AND h.ActionBy = @u) AS LastActionDate
                FROM dbo.PetForm f
                WHERE f.Status IN ('Approved','Rejected','SentBack','Draft')
                  AND (f.ApproverUsername = @u OR f.ReviewerUsername = @u)
                  AND EXISTS (SELECT 1 FROM dbo.PetWorkflowHistory h WHERE h.PetFormID = f.PetFormID AND h.ActionBy = @u)
                ORDER BY LastActionDate DESC",
                Db.P("@u", user));

            gvProcessed.DataSource = dt;
            gvProcessed.DataBind();
        }
    }
}
