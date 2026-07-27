using System;
using System.Data;

namespace DFM_BPM.App_Code.DAL
{
    /// <summary>
    /// Dashboard data access &ndash; implements all Q1&ndash;Q7 queries for the
    /// Project Financial Overview dashboard.
    /// Data is read from SQL Server tables that were synced from Oracle.
    /// </summary>
    public static class DashboardDAL
    {
        // ===================================================================
        // Q2 &ndash; GL Summary
        // ===================================================================
        public static DataTable GetGLSummary(string glNumber = null)
        {
            string sql = @"SELECT GLNumber, GLDescription, GLOpenedDate, BudgetedAmount,
                                  BPMLockedAmount, AMSLockedAmount, UtilizedAmount,
                                  BalanceAmount, CapitalizedAmount, InvoiceProcessedAmt, LastSyncDate
                           FROM dbo.BPM_GL WHERE 1=1";
            if (!string.IsNullOrEmpty(glNumber)) sql += " AND GLNumber=@gl";
            sql += " ORDER BY GLNumber";
            return string.IsNullOrEmpty(glNumber)
                ? Db.Query(sql)
                : Db.Query(sql, Db.P("@gl", glNumber));
        }

        // ===================================================================
        // Q3 &ndash; LPO Fact
        // ===================================================================
        public static DataTable GetLPOFact(string projectId = null, string status = null, string vendor = null)
        {
            string sql = @"SELECT WiName, LPONo, LPODesc, Department, InitiationDate, CurrentStage,
                                  InitiatorName, EFormNo, VendorName, LCAmount, Currency, FCAmount,
                                  GLNumber, LPOStatus, BPMStatus, BudgetAmount, BPMLockedAmount,
                                  AMSLockedAmount, UtilizedAmount, AvailableBalance, ActionDate
                           FROM dbo.BPM_LPO WHERE 1=1";
            var ps = new System.Collections.Generic.List<System.Data.SqlClient.SqlParameter>();
            if (!string.IsNullOrEmpty(projectId)) { sql += " AND GLNumber IN (SELECT DISTINCT GLNumber FROM dbo.GLMaster WHERE IsActive=1)"; }
            if (!string.IsNullOrEmpty(status))    { sql += " AND LPOStatus=@s"; ps.Add(Db.P("@s", status)); }
            if (!string.IsNullOrEmpty(vendor))    { sql += " AND VendorName LIKE @v"; ps.Add(Db.P("@v", "%" + vendor + "%")); }
            sql += " ORDER BY InitiationDate DESC";
            return Db.Query(sql, ps.ToArray());
        }

        // ===================================================================
        // Q4 &ndash; Invoice Fact
        // ===================================================================
        public static DataTable GetInvoiceFact(string status = null, string vendor = null)
        {
            string sql = @"SELECT WiName, InvoiceType, Department, InitiationDate, InitiatorName,
                                  EFormNo, VendorName, InvoiceNumber, LCAmount, Currency, FCAmount,
                                  InvoiceDate, InvoiceRefNo, InvoiceRefDesc, AMSInvoiceStatus,
                                  BPMLastStatus, LastActionBy, PendingAt, PendingWith, ActionDate
                           FROM dbo.BPM_Invoice WHERE 1=1";
            var ps = new System.Collections.Generic.List<System.Data.SqlClient.SqlParameter>();
            if (!string.IsNullOrEmpty(status)) { sql += " AND AMSInvoiceStatus=@s"; ps.Add(Db.P("@s", status)); }
            if (!string.IsNullOrEmpty(vendor)) { sql += " AND VendorName LIKE @v"; ps.Add(Db.P("@v", "%" + vendor + "%")); }
            sql += " ORDER BY InitiationDate DESC";
            return Db.Query(sql, ps.ToArray());
        }

        // ===================================================================
        // Q5/Q6 &ndash; CAPEX/OPEX Master
        // ===================================================================
        public static DataTable GetCapexMaster(string capexId = null)
        {
            string sql = @"SELECT CapexID, BudgetedAmount, UtilizedAmount, AvailableAmount,
                                     LockedAmount, GLNumbers, LastSyncDate
                              FROM dbo.CapexMaster WHERE IsActive=1";
            if (!string.IsNullOrEmpty(capexId)) sql += " AND CapexID=@cid";
            sql += " ORDER BY CapexID";
            return string.IsNullOrEmpty(capexId)
                ? Db.Query(sql)
                : Db.Query(sql, Db.P("@cid", capexId));
        }

        public static DataTable GetOpexMaster()
        {
            return Db.Query(@"SELECT OpexID, BudgetedAmount, UtilizedAmount, AvailableAmount,
                                     LockedAmount, Contracts, LastSyncDate
                              FROM dbo.OpexMaster WHERE IsActive=1 ORDER BY OpexID");
        }

        // ===================================================================
        // Q7 &ndash; Project Financial Overview (main dashboard query)
        // ===================================================================
        public static DataTable GetProjectFinancialOverview(string projectId = null, string petRef = null,
                                                             string capexId = null, string itemType = null,
                                                             string vendor = null)
        {
            string sql = @"SELECT cod.ItemType, cod.ItemID, cod.ItemDescription,
                                  cod.BudgetedAmount, cod.UtilizedAmount, cod.LockedAmount,
                                  cod.AvailableAmount, cod.ClaimAmount, cod.BalClaimAmt,
                                  cod.OldClaimAmount, cod.PIDCapexID, cod.ProjectID,
                                  cod.ProjectName, cod.PetReference, cod.PetApprovedAmt,
                                  cod.VendorName, cod.InitiatorDept, cod.EFormDate,
                                  p.ProjectManager, p.ProjectAmount, p.BalanceAmt,
                                  p.BPMLockedAmt, p.AMSLockedAmt, p.ProjectStatus, p.CapexID
                           FROM dbo.BPM_CapexOpexDetails cod
                           LEFT JOIN dbo.BPM_Projects p ON p.ProjectID = cod.ProjectID
                           WHERE 1=1";
            var ps = new System.Collections.Generic.List<System.Data.SqlClient.SqlParameter>();
            if (!string.IsNullOrEmpty(projectId)) { sql += " AND cod.ProjectID=@pid"; ps.Add(Db.P("@pid", projectId)); }
            if (!string.IsNullOrEmpty(petRef))    { sql += " AND cod.PetReference=@pet"; ps.Add(Db.P("@pet", petRef)); }
            if (!string.IsNullOrEmpty(capexId))   { sql += " AND (cod.PIDCapexID=@cid OR p.CapexID=@cid)"; ps.Add(Db.P("@cid", capexId)); }
            if (!string.IsNullOrEmpty(itemType))  { sql += " AND cod.ItemType=@it";   ps.Add(Db.P("@it", itemType));  }
            if (!string.IsNullOrEmpty(vendor))    { sql += " AND cod.VendorName LIKE @vn"; ps.Add(Db.P("@vn", "%" + vendor + "%")); }
            sql += " ORDER BY cod.ProjectID, cod.ItemType, cod.ItemID";
            return Db.Query(sql, ps.ToArray());
        }

        // ===================================================================
        // Q7-3 &ndash; Project details with KPIs
        // ===================================================================
        public static DataRow GetProjectDetails(string projectId)
        {
            return Db.QueryRow(@"SELECT p.*,
                                        (SELECT COUNT(*) FROM dbo.BPM_PET WHERE ProjectID=p.ProjectID) AS PETCount,
                                        (SELECT COUNT(*) FROM dbo.BPM_LPO WHERE GLNumber IN
                                            (SELECT GLNumber FROM dbo.GLMaster WHERE IsActive=1)) AS LPOCount,
                                        (SELECT COUNT(*) FROM dbo.BPM_Invoice WHERE InvoiceRefNo LIKE '%'+p.ProjectID+'%') AS InvoiceCount
                                 FROM dbo.BPM_Projects p WHERE p.ProjectID=@id",
                Db.P("@id", projectId));
        }

        // ===================================================================
        // Summary KPIs for dashboard header
        // ===================================================================
        public static DataRow GetDashboardKPIs()
        {
            return Db.QueryRow(@"SELECT
                (SELECT COUNT(DISTINCT JiraID) FROM dbo.JiraIssues)              AS TotalProjects,
                (SELECT COUNT(*)               FROM dbo.PetForm)                 AS TotalPET,
                (SELECT COUNT(*)               FROM dbo.BPM_LPO)                 AS TotalLPO,
                (SELECT COUNT(*)               FROM dbo.BPM_Invoice)             AS TotalInvoice,
                (SELECT COUNT(*)               FROM dbo.PetForm WHERE Status IN ('PendingReview','PendingApproval')) AS TotalPending,
                (SELECT COUNT(*)               FROM dbo.PetForm WHERE Status='Approved')  AS TotalApproved,
                (SELECT COUNT(*)               FROM dbo.PetForm WHERE Status='Rejected')  AS TotalRejected,
                (SELECT SUM(BudgetedAmount)    FROM dbo.CapexMaster)             AS TotalCapexBudget,
                (SELECT SUM(BudgetedAmount)    FROM dbo.OpexMaster)              AS TotalOpexBudget,
                (SELECT MAX(LastSyncDate)      FROM dbo.CapexMaster)             AS LastMasterSync");
        }

        // ===================================================================
        // Contract (Q1) facts
        // ===================================================================
        public static DataTable GetContractFact(string projectId = null, string status = null, string vendor = null)
        {
            string sql = @"SELECT WiName, Reference, Department, InitiationDate, InitiatorName,
                                  EFormNo, CurrentStage, Currency, LCAmount, FCAmount,
                                  BPMLockedAmount, AMSLockedAmount, UtilizedAmount, ContractBalance,
                                  OpexID, VendorName, RequestType, ContractNo, ContractStartDate,
                                  ContractEndDate, ContractStatus, BPMLastStatus, LastActionBy,
                                  PendingWith, LastActionDate, TechFinanceStatus
                           FROM dbo.BPM_Contract WHERE 1=1";
            var ps = new System.Collections.Generic.List<System.Data.SqlClient.SqlParameter>();
            if (!string.IsNullOrEmpty(projectId)) { sql += " AND OpexID IN (SELECT DISTINCT OpexID FROM dbo.BPM_Projects WHERE ProjectID=@pid)"; ps.Add(Db.P("@pid", projectId)); }
            if (!string.IsNullOrEmpty(status))    { sql += " AND ContractStatus=@s"; ps.Add(Db.P("@s", status)); }
            if (!string.IsNullOrEmpty(vendor))    { sql += " AND VendorName LIKE @v"; ps.Add(Db.P("@v", "%" + vendor + "%")); }
            sql += " ORDER BY InitiationDate DESC";
            return Db.Query(sql, ps.ToArray());
        }

        // ===================================================================
        // Distinct filter values for slicers
        // ===================================================================
        public static DataTable GetDistinctProjects()
        {
            return Db.Query("SELECT DISTINCT ProjectID, ISNULL(ProjectName,'') AS ProjectName FROM dbo.BPM_Projects ORDER BY ProjectID");
        }

        public static DataTable GetDistinctPetRefs()
        {
            return Db.Query("SELECT DISTINCT PETReferenceNo FROM dbo.BPM_PET ORDER BY PETReferenceNo");
        }

        public static DataTable GetDistinctCapexIds()
        {
            return Db.Query("SELECT DISTINCT CapexID FROM dbo.CapexMaster ORDER BY CapexID");
        }

        public static DataTable GetDistinctDepartments()
        {
            return Db.Query("SELECT DISTINCT Department FROM dbo.BPM_Contract WHERE Department IS NOT NULL ORDER BY Department");
        }

        public static DataTable GetDistinctVendors()
        {
            return Db.Query("SELECT DISTINCT VendorName FROM dbo.VendorMaster WHERE VendorName IS NOT NULL ORDER BY VendorName");
        }
    }
}
