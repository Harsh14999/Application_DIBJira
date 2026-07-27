using System;
using System.Data;
using System.Data.SqlClient;

namespace DFM_BPM.App_Code.DAL
{
    /// <summary>
    /// Data access for Oracle-synced master tables:
    /// CapexMaster, OpexMaster, GLMaster, VendorMaster (all read-only in the UI).
    /// </summary>
    public static class MastersDAL
    {
        // ===== CAPEX =====
        public static DataTable GetCapex(string search = null)
        {
            string sql = @"SELECT CapexID, BudgetedAmount, UtilizedAmount, AvailableAmount,
                                  LockedAmount, GLNumbers, LastSyncDate, IsActive
                           FROM dbo.CapexMaster";
            if (!string.IsNullOrWhiteSpace(search))
                sql += " WHERE CapexID LIKE @s OR GLNumbers LIKE @s";
            sql += " ORDER BY CapexID";
            return string.IsNullOrWhiteSpace(search)
                ? Db.Query(sql)
                : Db.Query(sql, Db.P("@s", "%" + search.Trim() + "%"));
        }

        public static DataRow GetCapexById(string capexId)
        {
            return Db.QueryRow(
                "SELECT * FROM dbo.CapexMaster WHERE CapexID = @id",
                Db.P("@id", capexId));
        }

        // ===== OPEX =====
        public static DataTable GetOpex(string search = null)
        {
            string sql = @"SELECT OpexID, BudgetedAmount, UtilizedAmount, AvailableAmount,
                                  LockedAmount, Contracts, LastSyncDate, IsActive
                           FROM dbo.OpexMaster";
            if (!string.IsNullOrWhiteSpace(search))
                sql += " WHERE OpexID LIKE @s OR Contracts LIKE @s";
            sql += " ORDER BY OpexID";
            return string.IsNullOrWhiteSpace(search)
                ? Db.Query(sql)
                : Db.Query(sql, Db.P("@s", "%" + search.Trim() + "%"));
        }

        public static DataRow GetOpexById(string opexId)
        {
            return Db.QueryRow(
                "SELECT * FROM dbo.OpexMaster WHERE OpexID = @id",
                Db.P("@id", opexId));
        }

        // ===== GL =====
        public static DataTable GetGL(string search = null)
        {
            string sql = @"SELECT GLNumber, GLDescription, GLOpenedDate, BudgetedAmount,
                                  BPMLockedAmount, AMSLockedAmount, UtilizedAmount,
                                  BalanceAmount, CapitalizedAmount, InvoiceProcessedAmt, LastSyncDate
                           FROM dbo.GLMaster WHERE IsActive=1";
            if (!string.IsNullOrWhiteSpace(search))
                sql += " AND (GLNumber LIKE @s OR GLDescription LIKE @s)";
            sql += " ORDER BY GLNumber";
            return string.IsNullOrWhiteSpace(search)
                ? Db.Query(sql)
                : Db.Query(sql, Db.P("@s", "%" + search.Trim() + "%"));
        }

        public static DataRow GetGLByNumber(string glNumber)
        {
            return Db.QueryRow(
                "SELECT * FROM dbo.GLMaster WHERE GLNumber = @n",
                Db.P("@n", glNumber));
        }

        // ===== Vendor =====
        public static DataTable GetVendors(string search = null)
        {
            string sql = @"SELECT VendorCode, VendorName, LastSyncDate, IsActive
                           FROM dbo.VendorMaster";
            if (!string.IsNullOrWhiteSpace(search))
                sql += " WHERE VendorName LIKE @s OR VendorCode LIKE @s";
            sql += " ORDER BY VendorName";
            return string.IsNullOrWhiteSpace(search)
                ? Db.Query(sql)
                : Db.Query(sql, Db.P("@s", "%" + search.Trim() + "%"));
        }

        // ===== Dropdown helpers =====
        public static DataTable GetCapexDropdown()
        {
            return Db.Query("SELECT CapexID AS ID, CapexID AS Name FROM dbo.CapexMaster WHERE IsActive=1 ORDER BY CapexID");
        }

        public static DataTable GetOpexDropdown()
        {
            return Db.Query("SELECT OpexID AS ID, OpexID AS Name FROM dbo.OpexMaster WHERE IsActive=1 ORDER BY OpexID");
        }

        public static DataTable GetGLDropdown()
        {
            return Db.Query("SELECT GLNumber AS ID, GLNumber + ' - ' + ISNULL(GLDescription,'') AS Name FROM dbo.GLMaster WHERE IsActive=1 ORDER BY GLNumber");
        }

        public static DataTable GetVendorDropdown()
        {
            return Db.Query("SELECT VendorCode AS ID, VendorName AS Name FROM dbo.VendorMaster WHERE IsActive=1 ORDER BY VendorName");
        }

        // ===== JIRA Issues (for project selection dropdown) =====
        public static DataTable GetJiraDropdown()
        {
            return Db.Query(@"SELECT JiraID, JiraID + ' - ' + ISNULL(Summary,'') AS DisplayName
                              FROM dbo.JiraIssues ORDER BY JiraID");
        }

        public static DataRow GetJiraById(string jiraId)
        {
            return Db.QueryRow(
                "SELECT * FROM dbo.JiraIssues WHERE JiraID = @id",
                Db.P("@id", jiraId));
        }

        /// <summary>
        /// JIRA project dropdown for PetWorkflow.aspx, filtered per the "JIRA Filter Enhancement"
        /// requirement: excludes Cancelled/Live/Completed projects, and only includes a project if it's
        /// owned by the fixed IT accountable exec OR its Platform is one of the selected platforms.
        /// Passing an empty/null platform list simply omits the Platform-IN condition (only the fixed
        /// AccountableExec baseline applies) since "Platform IN ()" is not valid SQL with zero values.
        /// </summary>
        public static DataTable GetJiraDropdownFiltered(System.Collections.Generic.IList<string> platforms)
        {
            const string BaselineAccountableExec = "Zahoor Ul Islam (IT Dept)";
            string sql = @"SELECT JiraID, JiraID + ' - ' + ISNULL(Summary,'') AS DisplayName
                           FROM dbo.JiraIssues
                           WHERE ISNULL(OverallStatus,'') NOT IN ('Cancelled','Live','Completed')
                             AND (ISNULL(AccountableExec,'') = @ae";
            var ps = new System.Collections.Generic.List<SqlParameter> { Db.P("@ae", BaselineAccountableExec) };

            if (platforms != null && platforms.Count > 0)
            {
                var names = new System.Collections.Generic.List<string>();
                for (int i = 0; i < platforms.Count; i++)
                {
                    if (string.IsNullOrEmpty(platforms[i])) continue;
                    string pname = "@p" + i;
                    names.Add(pname);
                    ps.Add(Db.P(pname, platforms[i]));
                }
                if (names.Count > 0)
                    sql += " OR Platform IN (" + string.Join(",", names.ToArray()) + ")";
            }
            sql += ") ORDER BY JiraID ASC";

            return Db.Query(sql, ps.ToArray());
        }

        // ===== BPM Projects dropdown =====
        public static DataTable GetProjectDropdown()
        {
            return Db.Query(@"SELECT ProjectID AS ID, ProjectID + ' &ndash; ' + ISNULL(ProjectName,'') AS Name
                              FROM dbo.BPM_Projects ORDER BY ProjectID");
        }

        public static DataRow GetProjectById(string projectId)
        {
            return Db.QueryRow(
                "SELECT * FROM dbo.BPM_Projects WHERE ProjectID = @id",
                Db.P("@id", projectId));
        }

        // ===== PET Cost types & currencies =====
        public static DataTable GetCostTypes()
        {
            return Db.Query("SELECT Category FROM dbo.PetCostType WHERE IsActive=1 ORDER BY Category");
        }

        public static DataTable GetCurrencies()
        {
            return Db.Query("SELECT Code, Name, RateToLocal FROM dbo.PetCurrency WHERE IsActive=1 ORDER BY Code");
        }

        // ===== Reviewer / Approver users for workflow =====
        public static DataTable GetReviewers()
        {
            return Db.Query(@"SELECT DISTINCT u.Username, u.FullName, u.Email
                              FROM dbo.AppUsers u
                              INNER JOIN dbo.UserRoleAssignments a ON a.Username = u.Username
                              WHERE a.RoleType = 'Reviewer' AND u.IsEnabled = 1
                              ORDER BY u.FullName");
        }

        public static DataTable GetApprovers()
        {
            return Db.Query(@"SELECT DISTINCT u.Username, u.FullName, u.Email
                              FROM dbo.AppUsers u
                              INNER JOIN dbo.UserRoleAssignments a ON a.Username = u.Username
                              WHERE a.RoleType = 'Approver' AND u.IsEnabled = 1
                              ORDER BY u.FullName");
        }

        // ================================================================
        // EDITABLE MASTER CRUD + HISTORY
        // ================================================================

        // ── CAPEX ──
        public static DataTable GetCapexFull(string search = null)
        {
            string sql = @"SELECT CapexID, Description, BudgetedAmount, UtilizedAmount,
                                  AvailableAmount, LockedAmount, BudgetAfterLockedAmount,
                                  ClaimAmount, NetBalance, IsActive, LastSyncDate, ModifiedBy, ModifiedDate
                           FROM dbo.CapexMaster";
            if (!string.IsNullOrWhiteSpace(search))
                sql += " WHERE CapexID LIKE @s OR Description LIKE @s OR GLNumbers LIKE @s";
            sql += " ORDER BY CapexID";
            return string.IsNullOrWhiteSpace(search)
                ? Db.Query(sql)
                : Db.Query(sql, Db.P("@s", "%" + search.Trim() + "%"));
        }

        public static void SaveCapex(string capexId, string description, decimal budget,
            decimal util, decimal avail, decimal locked, decimal afterLocked,
            decimal claim, decimal net, bool isActive, string modifiedBy)
        {
            // Archive history
            ArchiveCapexHistory(capexId, modifiedBy);
            // Upsert
            int exists = Convert.ToInt32(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.CapexMaster WHERE CapexID=@id", Db.P("@id", capexId)));
            if (exists == 0)
            {
                Db.Exec(@"INSERT INTO dbo.CapexMaster
                    (CapexID,Description,BudgetedAmount,UtilizedAmount,AvailableAmount,
                     LockedAmount,BudgetAfterLockedAmount,ClaimAmount,NetBalance,IsActive,
                     LastSyncDate,ModifiedBy,ModifiedDate)
                    VALUES(@id,@de,@b,@u,@av,@lk,@al,@cl,@nt,@ia,GETDATE(),@mb,GETDATE())",
                    Db.P("@id", capexId), Db.P("@de", description ?? ""),
                    Db.P("@b", budget), Db.P("@u", util), Db.P("@av", avail),
                    Db.P("@lk", locked), Db.P("@al", afterLocked), Db.P("@cl", claim),
                    Db.P("@nt", net), Db.P("@ia", isActive), Db.P("@mb", modifiedBy));
            }
            else
            {
                Db.Exec(@"UPDATE dbo.CapexMaster SET Description=@de,BudgetedAmount=@b,
                    UtilizedAmount=@u,AvailableAmount=@av,LockedAmount=@lk,
                    BudgetAfterLockedAmount=@al,ClaimAmount=@cl,NetBalance=@nt,
                    IsActive=@ia,LastSyncDate=GETDATE(),ModifiedBy=@mb,ModifiedDate=GETDATE()
                    WHERE CapexID=@id",
                    Db.P("@de", description ?? ""), Db.P("@b", budget), Db.P("@u", util),
                    Db.P("@av", avail), Db.P("@lk", locked), Db.P("@al", afterLocked),
                    Db.P("@cl", claim), Db.P("@nt", net), Db.P("@ia", isActive),
                    Db.P("@mb", modifiedBy), Db.P("@id", capexId));
            }
        }

        internal static void ArchiveCapexHistory(string capexId, string changedBy)
        {
            Db.Exec(@"INSERT INTO dbo.CapexMasterHistory
                (CapexID,Description,BudgetedAmount,UtilizedAmount,AvailableAmount,
                 LockedAmount,BudgetAfterLockedAmount,ClaimAmount,NetBalance,IsActive,ChangedBy,ChangedDate)
                SELECT CapexID,Description,BudgetedAmount,UtilizedAmount,AvailableAmount,
                       LockedAmount,BudgetAfterLockedAmount,ClaimAmount,NetBalance,IsActive,@cb,GETDATE()
                FROM dbo.CapexMaster WHERE CapexID=@id",
                Db.P("@cb", changedBy), Db.P("@id", capexId));
        }

        public static void DeleteCapex(string capexId, string deletedBy)
        {
            ArchiveCapexHistory(capexId, deletedBy);
            Db.Exec("DELETE FROM dbo.CapexMaster WHERE CapexID=@id", Db.P("@id", capexId));
        }

        public static DataTable GetCapexHistory(string capexId)
        {
            return Db.Query(
                "SELECT * FROM dbo.CapexMasterHistory WHERE CapexID=@id ORDER BY ChangedDate DESC",
                Db.P("@id", capexId));
        }

        // ── OPEX ──
        public static DataTable GetOpexFull(string search = null)
        {
            string sql = @"SELECT OpexID, Description, BudgetedAmount, UtilizedAmount,
                                  AvailableAmount, LockedAmount, BudgetAfterLockedAmount,
                                  ClaimAmount, NetBalance, IsActive, LastSyncDate, ModifiedBy, ModifiedDate
                           FROM dbo.OpexMaster";
            if (!string.IsNullOrWhiteSpace(search))
                sql += " WHERE OpexID LIKE @s OR Description LIKE @s";
            sql += " ORDER BY OpexID";
            return string.IsNullOrWhiteSpace(search)
                ? Db.Query(sql)
                : Db.Query(sql, Db.P("@s", "%" + search.Trim() + "%"));
        }

        public static void SaveOpex(string opexId, string description, decimal budget,
            decimal util, decimal avail, decimal locked, decimal afterLocked,
            decimal claim, decimal net, bool isActive, string modifiedBy)
        {
            ArchiveOpexHistory(opexId, modifiedBy);
            int exists = Convert.ToInt32(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.OpexMaster WHERE OpexID=@id", Db.P("@id", opexId)));
            if (exists == 0)
            {
                Db.Exec(@"INSERT INTO dbo.OpexMaster
                    (OpexID,Description,BudgetedAmount,UtilizedAmount,AvailableAmount,
                     LockedAmount,BudgetAfterLockedAmount,ClaimAmount,NetBalance,IsActive,
                     LastSyncDate,ModifiedBy,ModifiedDate)
                    VALUES(@id,@de,@b,@u,@av,@lk,@al,@cl,@nt,@ia,GETDATE(),@mb,GETDATE())",
                    Db.P("@id", opexId), Db.P("@de", description ?? ""),
                    Db.P("@b", budget), Db.P("@u", util), Db.P("@av", avail),
                    Db.P("@lk", locked), Db.P("@al", afterLocked), Db.P("@cl", claim),
                    Db.P("@nt", net), Db.P("@ia", isActive), Db.P("@mb", modifiedBy));
            }
            else
            {
                Db.Exec(@"UPDATE dbo.OpexMaster SET Description=@de,BudgetedAmount=@b,
                    UtilizedAmount=@u,AvailableAmount=@av,LockedAmount=@lk,
                    BudgetAfterLockedAmount=@al,ClaimAmount=@cl,NetBalance=@nt,
                    IsActive=@ia,LastSyncDate=GETDATE(),ModifiedBy=@mb,ModifiedDate=GETDATE()
                    WHERE OpexID=@id",
                    Db.P("@de", description ?? ""), Db.P("@b", budget), Db.P("@u", util),
                    Db.P("@av", avail), Db.P("@lk", locked), Db.P("@al", afterLocked),
                    Db.P("@cl", claim), Db.P("@nt", net), Db.P("@ia", isActive),
                    Db.P("@mb", modifiedBy), Db.P("@id", opexId));
            }
        }

        internal static void ArchiveOpexHistory(string opexId, string changedBy)
        {
            Db.Exec(@"INSERT INTO dbo.OpexMasterHistory
                (OpexID,Description,BudgetedAmount,UtilizedAmount,AvailableAmount,
                 LockedAmount,BudgetAfterLockedAmount,ClaimAmount,NetBalance,IsActive,ChangedBy,ChangedDate)
                SELECT OpexID,Description,BudgetedAmount,UtilizedAmount,AvailableAmount,
                       LockedAmount,BudgetAfterLockedAmount,ClaimAmount,NetBalance,IsActive,@cb,GETDATE()
                FROM dbo.OpexMaster WHERE OpexID=@id",
                Db.P("@cb", changedBy), Db.P("@id", opexId));
        }

        public static void DeleteOpex(string opexId, string deletedBy)
        {
            ArchiveOpexHistory(opexId, deletedBy);
            Db.Exec("DELETE FROM dbo.OpexMaster WHERE OpexID=@id", Db.P("@id", opexId));
        }

        public static DataTable GetOpexHistory(string opexId)
        {
            return Db.Query(
                "SELECT * FROM dbo.OpexMasterHistory WHERE OpexID=@id ORDER BY ChangedDate DESC",
                Db.P("@id", opexId));
        }

        // ── GL Master ──
        public static DataTable GetGLFull(string search = null)
        {
            string sql = @"SELECT GLNumber, GLDescription, GLOpenedDate, BudgetedAmount,
                                  BPMLockedAmount, AMSLockedAmount, UtilizedAmount,
                                  BalanceAmount, CapitalizedAmount, InvoiceProcessedAmt,
                                  IsActive, LastSyncDate, ModifiedBy, ModifiedDate
                           FROM dbo.GLMaster";
            if (!string.IsNullOrWhiteSpace(search))
                sql += " WHERE GLNumber LIKE @s OR GLDescription LIKE @s";
            sql += " ORDER BY GLNumber";
            return string.IsNullOrWhiteSpace(search)
                ? Db.Query(sql)
                : Db.Query(sql, Db.P("@s", "%" + search.Trim() + "%"));
        }

        public static void SaveGL(string glNumber, string desc, DateTime? openedDate,
            decimal budgeted, decimal bpmLocked, decimal amsLocked, decimal utilized,
            decimal balance, decimal capitalized, decimal invoiceAmt, bool isActive, string modifiedBy)
        {
            ArchiveGLHistory(glNumber, modifiedBy);
            int exists = Convert.ToInt32(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.GLMaster WHERE GLNumber=@n", Db.P("@n", glNumber)));
            if (exists == 0)
            {
                Db.Exec(@"INSERT INTO dbo.GLMaster
                    (GLNumber,GLDescription,GLOpenedDate,BudgetedAmount,BPMLockedAmount,AMSLockedAmount,
                     UtilizedAmount,BalanceAmount,CapitalizedAmount,InvoiceProcessedAmt,IsActive,
                     LastSyncDate,ModifiedBy,ModifiedDate)
                    VALUES(@n,@de,@od,@b,@bl,@al,@u,@ba,@ca,@ia2,@ia,GETDATE(),@mb,GETDATE())",
                    Db.P("@n", glNumber), Db.P("@de", desc ?? ""),
                    Db.P("@od", openedDate.HasValue ? (object)openedDate.Value : DBNull.Value),
                    Db.P("@b", budgeted), Db.P("@bl", bpmLocked), Db.P("@al", amsLocked),
                    Db.P("@u", utilized), Db.P("@ba", balance), Db.P("@ca", capitalized),
                    Db.P("@ia2", invoiceAmt), Db.P("@ia", isActive), Db.P("@mb", modifiedBy));
            }
            else
            {
                Db.Exec(@"UPDATE dbo.GLMaster SET GLDescription=@de,GLOpenedDate=@od,
                    BudgetedAmount=@b,BPMLockedAmount=@bl,AMSLockedAmount=@al,
                    UtilizedAmount=@u,BalanceAmount=@ba,CapitalizedAmount=@ca,
                    InvoiceProcessedAmt=@ia2,IsActive=@ia,LastSyncDate=GETDATE(),
                    ModifiedBy=@mb,ModifiedDate=GETDATE() WHERE GLNumber=@n",
                    Db.P("@de", desc ?? ""),
                    Db.P("@od", openedDate.HasValue ? (object)openedDate.Value : DBNull.Value),
                    Db.P("@b", budgeted), Db.P("@bl", bpmLocked), Db.P("@al", amsLocked),
                    Db.P("@u", utilized), Db.P("@ba", balance), Db.P("@ca", capitalized),
                    Db.P("@ia2", invoiceAmt), Db.P("@ia", isActive),
                    Db.P("@mb", modifiedBy), Db.P("@n", glNumber));
            }
        }

        private static void ArchiveGLHistory(string glNumber, string changedBy)
        {
            Db.Exec(@"INSERT INTO dbo.GLMasterHistory
                (GLNumber,GLDescription,BudgetedAmount,UtilizedAmount,BalanceAmount,IsActive,ChangedBy,ChangedDate)
                SELECT GLNumber,GLDescription,BudgetedAmount,UtilizedAmount,BalanceAmount,IsActive,@cb,GETDATE()
                FROM dbo.GLMaster WHERE GLNumber=@n",
                Db.P("@cb", changedBy), Db.P("@n", glNumber));
        }

        public static void DeleteGL(string glNumber, string deletedBy)
        {
            ArchiveGLHistory(glNumber, deletedBy);
            Db.Exec("DELETE FROM dbo.GLMaster WHERE GLNumber=@n", Db.P("@n", glNumber));
        }

        public static DataTable GetGLHistory(string glNumber)
        {
            return Db.Query(
                "SELECT * FROM dbo.GLMasterHistory WHERE GLNumber=@n ORDER BY ChangedDate DESC",
                Db.P("@n", glNumber));
        }

        // ── Vendor Master ──
        public static DataTable GetVendorsFull(string search = null)
        {
            string sql = @"SELECT VendorCode, VendorName, ContactEmail, ContactPhone,
                                  IsActive, LastSyncDate, ModifiedBy, ModifiedDate
                           FROM dbo.VendorMaster";
            if (!string.IsNullOrWhiteSpace(search))
                sql += " WHERE VendorName LIKE @s OR VendorCode LIKE @s";
            sql += " ORDER BY VendorName";
            return string.IsNullOrWhiteSpace(search)
                ? Db.Query(sql)
                : Db.Query(sql, Db.P("@s", "%" + search.Trim() + "%"));
        }

        public static void SaveVendor(string vendorCode, string vendorName, string email,
            string phone, bool isActive, string modifiedBy)
        {
            ArchiveVendorHistory(vendorCode, modifiedBy);
            int exists = Convert.ToInt32(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.VendorMaster WHERE VendorCode=@c", Db.P("@c", vendorCode)));
            if (exists == 0)
            {
                Db.Exec(@"INSERT INTO dbo.VendorMaster
                    (VendorCode,VendorName,ContactEmail,ContactPhone,IsActive,LastSyncDate,ModifiedBy,ModifiedDate)
                    VALUES(@c,@n,@e,@p,@ia,GETDATE(),@mb,GETDATE())",
                    Db.P("@c", vendorCode), Db.P("@n", vendorName ?? ""),
                    Db.P("@e", email ?? ""), Db.P("@p", phone ?? ""),
                    Db.P("@ia", isActive), Db.P("@mb", modifiedBy));
            }
            else
            {
                Db.Exec(@"UPDATE dbo.VendorMaster SET VendorName=@n,ContactEmail=@e,ContactPhone=@p,
                    IsActive=@ia,LastSyncDate=GETDATE(),ModifiedBy=@mb,ModifiedDate=GETDATE()
                    WHERE VendorCode=@c",
                    Db.P("@n", vendorName ?? ""), Db.P("@e", email ?? ""),
                    Db.P("@p", phone ?? ""), Db.P("@ia", isActive),
                    Db.P("@mb", modifiedBy), Db.P("@c", vendorCode));
            }
        }

        private static void ArchiveVendorHistory(string vendorCode, string changedBy)
        {
            Db.Exec(@"INSERT INTO dbo.VendorMasterHistory
                (VendorCode,VendorName,IsActive,ChangedBy,ChangedDate)
                SELECT VendorCode,VendorName,IsActive,@cb,GETDATE()
                FROM dbo.VendorMaster WHERE VendorCode=@c",
                Db.P("@cb", changedBy), Db.P("@c", vendorCode));
        }

        public static DataTable GetVendorHistory(string vendorCode)
        {
            return Db.Query(
                "SELECT * FROM dbo.VendorMasterHistory WHERE VendorCode=@c ORDER BY ChangedDate DESC",
                Db.P("@c", vendorCode));
        }

        public static void DeleteVendor(string vendorCode, string deletedBy)
        {
            ArchiveVendorHistory(vendorCode, deletedBy);
            Db.Exec("DELETE FROM dbo.VendorMaster WHERE VendorCode=@c", Db.P("@c", vendorCode));
        }

        // ── Platform Master (drives the JIRA Platform filter on PetWorkflow.aspx) ──
        public static DataTable GetPlatforms(string search = null)
        {
            string sql = @"SELECT PlatformID, PlatformName, IsActive, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate
                           FROM dbo.PlatformMaster";
            if (!string.IsNullOrWhiteSpace(search))
                sql += " WHERE PlatformName LIKE @s";
            sql += " ORDER BY PlatformName";
            return string.IsNullOrWhiteSpace(search)
                ? Db.Query(sql)
                : Db.Query(sql, Db.P("@s", "%" + search.Trim() + "%"));
        }

        public static DataRow GetPlatformById(int platformId)
        {
            return Db.QueryRow("SELECT * FROM dbo.PlatformMaster WHERE PlatformID=@id", Db.P("@id", platformId));
        }

        /// <summary>Active platform names for the multi-select Platform filter dropdown.</summary>
        public static DataTable GetPlatformsDropdown()
        {
            return Db.Query("SELECT PlatformName FROM dbo.PlatformMaster WHERE IsActive=1 ORDER BY PlatformName");
        }

        public static void SavePlatform(int platformId, string platformName, bool isActive, string modifiedBy)
        {
            if (platformId <= 0)
            {
                Db.Exec(@"INSERT INTO dbo.PlatformMaster (PlatformName, IsActive, CreatedBy, ModifiedBy, ModifiedDate)
                    VALUES (@n, @ia, @mb, @mb, GETDATE())",
                    Db.P("@n", platformName), Db.P("@ia", isActive), Db.P("@mb", modifiedBy));
            }
            else
            {
                Db.Exec(@"UPDATE dbo.PlatformMaster SET PlatformName=@n, IsActive=@ia,
                    ModifiedBy=@mb, ModifiedDate=GETDATE() WHERE PlatformID=@id",
                    Db.P("@n", platformName), Db.P("@ia", isActive),
                    Db.P("@mb", modifiedBy), Db.P("@id", platformId));
            }
        }

        public static void DeletePlatform(int platformId)
        {
            Db.Exec("DELETE FROM dbo.PlatformMaster WHERE PlatformID=@id", Db.P("@id", platformId));
        }
    }
}

