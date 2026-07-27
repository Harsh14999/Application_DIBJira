using System;
using System.Data;

namespace DFM_BPM.App_Code.DAL
{
    /// <summary>
    /// Data access for the Project Registration master (dbo.Project) — the single source of truth for
    /// "registered projects". A project (JIRA-based or Non-JIRA) is registered here independently of any
    /// Spend Request (PET); PetWorkflow.aspx may only create a PET against an already-registered project.
    /// </summary>
    public static class ProjectDAL
    {
        /// <summary>Grid data for the "Registered Projects" dashboard grid (Default.aspx) and the
        /// Project Registration listing -- one row per registered project, Portfolio resource resolved.
        /// The resource filter is "team roll-up" -- it includes the selected resource AND every descendant
        /// underneath it (so clicking a manager shows their whole team's projects, not just an exact match).</summary>
        public static DataTable GetProjects(string search = null, int? resourceId = null)
        {
            string sql = @"SELECT p.ProjectID, p.ProjectName, p.IsNonJiraProject, p.ProjectManager,
                                  p.ResourceID, r.ResourceName AS PortfolioName, p.IsActive,
                                  p.CreatedBy, p.CreatedDate, p.ModifiedBy, p.ModifiedDate,
                                  p.AccountableExecLead, p.SmeLead, p.ProjectSize
                           FROM dbo.Project p
                           LEFT JOIN dbo.PortfolioResource r ON r.ResourceID = p.ResourceID
                           WHERE 1=1";
            var ps = new System.Collections.Generic.List<System.Data.SqlClient.SqlParameter>();
            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " AND (p.ProjectID LIKE @s OR p.ProjectName LIKE @s OR p.AccountableExecLead LIKE @s OR p.SmeLead LIKE @s OR p.ProjectManager LIKE @s OR p.CreatedBy LIKE @s)";
                ps.Add(Db.P("@s", "%" + search.Trim() + "%"));
            }
            if (resourceId.HasValue)
            {
                var descendantIds = PortfolioDAL.GetDescendantResourceIds(resourceId.Value);
                if (descendantIds.Count == 0) descendantIds.Add(resourceId.Value);
                var names = new System.Collections.Generic.List<string>();
                for (int i = 0; i < descendantIds.Count; i++)
                {
                    string pname = "@rid" + i;
                    names.Add(pname);
                    ps.Add(Db.P(pname, descendantIds[i]));
                }
                sql += " AND p.ResourceID IN (" + string.Join(",", names.ToArray()) + ")";
            }
            sql += " ORDER BY p.CreatedDate DESC";
            return Db.Query(sql, ps.ToArray());
        }

        public static DataRow GetProjectById(string projectId)
        {
            return Db.QueryRow(@"SELECT p.*, r.ResourceName AS PortfolioName
                                 FROM dbo.Project p
                                 LEFT JOIN dbo.PortfolioResource r ON r.ResourceID = p.ResourceID
                                 WHERE p.ProjectID=@id",
                Db.P("@id", projectId));
        }

        public static bool ProjectExists(string projectId)
        {
            return Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM dbo.Project WHERE ProjectID=@id", Db.P("@id", projectId))) > 0;
        }

        /// <summary>Registered-project dropdown for PetWorkflow.aspx — every PET must be created against one
        /// of these (JIRA and Non-JIRA projects alike), never directly against raw JIRA data.</summary>
        public static DataTable GetRegisteredProjectDropdown()
        {
            return Db.Query(@"SELECT ProjectID, ProjectID + ' - ' + ISNULL(ProjectName,'') AS DisplayName
                              FROM dbo.Project WHERE IsActive=1 ORDER BY ProjectID");
        }

        public static void SaveProject(string projectId, string projectName, bool isNonJiraProject,
            string projectManager, int? resourceId, bool isActive, string userName,
            string accountableExecLead = null, string smeLead = null)
        {
            if (!ProjectExists(projectId))
            {
                Db.Exec(@"INSERT INTO dbo.Project
                    (ProjectID, ProjectName, IsNonJiraProject, ProjectManager, ResourceID, IsActive, CreatedBy, ModifiedBy, ModifiedDate, AccountableExecLead, SmeLead)
                    VALUES (@id, @n, @nj, @pm, @rid, @ia, @cb, @cb, GETDATE(), @ael, @sl)",
                    Db.P("@id", projectId), Db.P("@n", projectName ?? ""), Db.P("@nj", isNonJiraProject),
                    Db.P("@pm", projectManager ?? (object)DBNull.Value),
                    Db.P("@rid", resourceId.HasValue ? (object)resourceId.Value : DBNull.Value),
                    Db.P("@ia", isActive), Db.P("@cb", userName),
                    Db.P("@ael", string.IsNullOrEmpty(accountableExecLead) ? (object)DBNull.Value : accountableExecLead),
                    Db.P("@sl", string.IsNullOrEmpty(smeLead) ? (object)DBNull.Value : smeLead));
            }
            else
            {
                Db.Exec(@"UPDATE dbo.Project SET ProjectName=@n, IsNonJiraProject=@nj, ProjectManager=@pm,
                          ResourceID=@rid, IsActive=@ia, ModifiedBy=@mb, ModifiedDate=GETDATE(),
                          AccountableExecLead=@ael, SmeLead=@sl
                          WHERE ProjectID=@id",
                    Db.P("@n", projectName ?? ""), Db.P("@nj", isNonJiraProject),
                    Db.P("@pm", projectManager ?? (object)DBNull.Value),
                    Db.P("@rid", resourceId.HasValue ? (object)resourceId.Value : DBNull.Value),
                    Db.P("@ia", isActive), Db.P("@mb", userName), Db.P("@id", projectId),
                    Db.P("@ael", string.IsNullOrEmpty(accountableExecLead) ? (object)DBNull.Value : accountableExecLead),
                    Db.P("@sl", string.IsNullOrEmpty(smeLead) ? (object)DBNull.Value : smeLead));
            }
        }

        /// <summary>True if any Spend Request (PET) already references this project — blocks hard delete.</summary>
        public static bool HasPetForms(string projectId)
        {
            return Convert.ToInt32(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.PetForm WHERE ProjectID=@id AND Status<>'Deleted'", Db.P("@id", projectId))) > 0;
        }

        public static void DeleteProject(string projectId)
        {
            Db.Exec("DELETE FROM dbo.ProjectEngineer WHERE ProjectID=@id", Db.P("@id", projectId));
            Db.Exec("DELETE FROM dbo.ProjectSizing WHERE ProjectID=@id", Db.P("@id", projectId));
            Db.Exec("DELETE FROM dbo.Project WHERE ProjectID=@id", Db.P("@id", projectId));
        }

        // ===================================================================
        // PROJECT ENGINEERS (many-to-many "who is staffed on this project" -- separate from the single
        // Exec/ExecLead/SmeLead hierarchy ResourceID, since several engineers can work the same project)
        // ===================================================================

        public static System.Collections.Generic.List<int> GetProjectEngineerIds(string projectId)
        {
            DataTable dt = Db.Query("SELECT ResourceID FROM dbo.ProjectEngineer WHERE ProjectID=@id", Db.P("@id", projectId));
            var ids = new System.Collections.Generic.List<int>();
            foreach (DataRow r in dt.Rows) ids.Add(Convert.ToInt32(r["ResourceID"]));
            return ids;
        }

        /// <summary>Replaces the full set of Engineers staffed on a Project (delete-then-insert-all).</summary>
        public static void SaveProjectEngineers(string projectId, System.Collections.Generic.List<int> resourceIds, string modifiedBy)
        {
            Db.Exec("DELETE FROM dbo.ProjectEngineer WHERE ProjectID=@id", Db.P("@id", projectId));
            if (resourceIds == null) return;
            foreach (int rid in resourceIds)
            {
                Db.Exec("INSERT INTO dbo.ProjectEngineer (ProjectID, ResourceID, CreatedBy) VALUES (@id, @rid, @cb)",
                    Db.P("@id", projectId), Db.P("@rid", rid), Db.P("@cb", modifiedBy));
            }
        }

        // ===================================================================
        // PROJECT SIZING (1 per project, upsert)
        // ===================================================================

        public static DataRow GetProjectSizing(string projectId)
        {
            return Db.QueryRow("SELECT * FROM dbo.ProjectSizing WHERE ProjectID=@id", Db.P("@id", projectId));
        }

        public static void SaveProjectSizing(string projectId, decimal q1, decimal q2, decimal q3,
            decimal q4, decimal q5, decimal q6, decimal q7,
            decimal weighted, string sizeResult, string capacityConsumption, string modifiedBy)
        {
            int exists = Convert.ToInt32(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.ProjectSizing WHERE ProjectID=@id", Db.P("@id", projectId)));
            if (exists == 0)
            {
                Db.Exec(@"INSERT INTO dbo.ProjectSizing
                    (ProjectID,Q1Score,Q2Score,Q3Score,Q4Score,Q5Score,Q6Score,Q7Score,
                     TotalWeightedScore,SizeResult,CapacityConsumption,ModifiedBy,ModifiedDate)
                    VALUES(@id,@q1,@q2,@q3,@q4,@q5,@q6,@q7,@ws,@sr,@cc,@mb,GETDATE())",
                    Db.P("@id", projectId), Db.P("@q1", q1), Db.P("@q2", q2), Db.P("@q3", q3),
                    Db.P("@q4", q4), Db.P("@q5", q5), Db.P("@q6", q6), Db.P("@q7", q7),
                    Db.P("@ws", weighted), Db.P("@sr", sizeResult), Db.P("@cc", capacityConsumption),
                    Db.P("@mb", modifiedBy));
            }
            else
            {
                Db.Exec(@"UPDATE dbo.ProjectSizing SET Q1Score=@q1,Q2Score=@q2,Q3Score=@q3,Q4Score=@q4,
                    Q5Score=@q5,Q6Score=@q6,Q7Score=@q7,TotalWeightedScore=@ws,SizeResult=@sr,
                    CapacityConsumption=@cc,ModifiedBy=@mb,ModifiedDate=GETDATE()
                    WHERE ProjectID=@id",
                    Db.P("@q1", q1), Db.P("@q2", q2), Db.P("@q3", q3), Db.P("@q4", q4),
                    Db.P("@q5", q5), Db.P("@q6", q6), Db.P("@q7", q7),
                    Db.P("@ws", weighted), Db.P("@sr", sizeResult), Db.P("@cc", capacityConsumption),
                    Db.P("@mb", modifiedBy), Db.P("@id", projectId));
            }
            // Denormalize into Project.ProjectSize for fast grid queries
            Db.Exec("UPDATE dbo.Project SET ProjectSize=@sr WHERE ProjectID=@id",
                Db.P("@sr", sizeResult), Db.P("@id", projectId));
        }
    }
}
