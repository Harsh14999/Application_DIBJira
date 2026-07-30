using System;
using System.Data;

namespace DFM_BPM.App_Code.DAL
{
    /// <summary>
    /// Data access for the Portfolio Hierarchy (self-referencing org/reporting-line tree of "resources")
    /// used to assign ownership of registered Projects. Auto-mapped for JIRA projects from
    /// AccountableExec / AccountableExecLead / SmeLead (see EnsureHierarchyPath); maintained manually by
    /// Admins for everything else (e.g. engineers reporting under a lead, Non-JIRA project owners).
    /// </summary>
    public static class PortfolioDAL
    {
        /// <summary>Full resource tree (active + inactive) with computed depth (Lvl, 0-based) for rendering,
        /// via a recursive CTE ordered so children always follow their parent (needed for a simple recursive
        /// server-side tree render). ProjectCount includes projects assigned anywhere in the resource's own
        /// sub-tree (self + all descendants), matching the "click a node to see their team's projects" filter.</summary>
        public static DataTable GetResourceTree()
        {
            return Db.Query(@"
                 with resourcetree as ( 
    -- Anchor Member
    select 
        resourceid, 
        resourcename, 
        title, 
        parentresourceid, 
        isactive, 
        0 as lvl, 
        cast(right('000000' + cast(resourceid as varchar(6)), 6) as varchar(4000)) as sortpath 
    from dbo.portfolioresource 
    where parentresourceid is null 
    
    union all 
    
    -- Recursive Member
    select 
        c.resourceid, 
        c.resourcename, 
        c.title, 
        c.parentresourceid, 
        c.isactive, 
        t.lvl + 1, 
        -- Crucial Fix: Explicitly cast the concatenated string back to varchar(4000)
        cast(t.sortpath + '.' + right('000000' + cast(c.resourceid as varchar(6)), 6) as varchar(4000))
    from dbo.portfolioresource c 
    inner join resourcetree t on c.parentresourceid = t.resourceid 
),
descendants as (
    select resourceid as rootid, resourceid as descid from dbo.portfolioresource
    union all
    select d.rootid, c.resourceid
    from dbo.portfolioresource c
    inner join descendants d on c.parentresourceid = d.descid
)
select 
    resourcetree.resourceid, 
    resourcetree.resourcename, 
    resourcetree.title, 
    resourcetree.parentresourceid, 
    resourcetree.isactive, 
    resourcetree.lvl, 
    (select count(*) from dbo.portfolioresource ch where ch.parentresourceid = resourcetree.resourceid) as childcount, 
    (select count(*) from dbo.project pr
     where pr.resourceid in (select d.descid from descendants d where d.rootid = resourcetree.resourceid)) as projectcount,
    (select count(*) from dbo.portfolioresource r2 where r2.resourceid = resourcetree.resourceid and r2.photo is not null) as hasphoto
from resourcetree 
order by sortpath
option (maxrecursion 100)");
        }

        public static DataRow GetResourceById(int resourceId)
        {
            return Db.QueryRow("SELECT * FROM dbo.PortfolioResource WHERE ResourceID=@id", Db.P("@id", resourceId));
        }

        public static DataRow GetResourceByName(string name)
        {
            return Db.QueryRow("SELECT * FROM dbo.PortfolioResource WHERE ResourceName=@n", Db.P("@n", name));
        }

        /// <summary>Flat, hierarchically-indented dropdown of active resources for the Portfolio-assignment picker.</summary>
        public static DataTable GetResourceDropdown()
        {
            DataTable tree = GetResourceTree();
            var dt = new DataTable();
            dt.Columns.Add("ResourceID", typeof(int));
            dt.Columns.Add("DisplayName", typeof(string));
            foreach (DataRow r in tree.Rows)
            {
                if (r["IsActive"] != DBNull.Value && !Convert.ToBoolean(r["IsActive"])) continue;
                int lvl = Convert.ToInt32(r["Lvl"]);
                string indent = lvl > 0 ? new string('-', lvl * 2) + " " : "";
                dt.Rows.Add(r["ResourceID"], indent + r["ResourceName"]);
            }
            return dt;
        }

        public static int SaveResource(int resourceId, string resourceName, string title, int? parentResourceId, bool isActive, string modifiedBy)
        {
            if (resourceId <= 0)
            {
                return Convert.ToInt32(Db.Scalar(@"
                    INSERT INTO dbo.PortfolioResource (ResourceName, Title, ParentResourceID, IsActive, CreatedBy, ModifiedBy, ModifiedDate)
                    OUTPUT INSERTED.ResourceID
                    VALUES (@n, @t, @p, @ia, @mb, @mb, GETDATE())",
                    Db.P("@n", resourceName), Db.P("@t", title ?? (object)DBNull.Value),
                    Db.P("@p", parentResourceId.HasValue ? (object)parentResourceId.Value : DBNull.Value),
                    Db.P("@ia", isActive), Db.P("@mb", modifiedBy)));
            }

            Db.Exec(@"UPDATE dbo.PortfolioResource SET ResourceName=@n, Title=@t, ParentResourceID=@p,
                      IsActive=@ia, ModifiedBy=@mb, ModifiedDate=GETDATE() WHERE ResourceID=@id",
                Db.P("@n", resourceName), Db.P("@t", title ?? (object)DBNull.Value),
                Db.P("@p", parentResourceId.HasValue ? (object)parentResourceId.Value : DBNull.Value),
                Db.P("@ia", isActive), Db.P("@mb", modifiedBy), Db.P("@id", resourceId));
            return resourceId;
        }

        /// <summary>True if the resource has child resources or assigned projects and therefore cannot be deleted
        /// until those are re-parented / reassigned.</summary>
        public static bool HasChildrenOrProjects(int resourceId)
        {
            int childCount = Convert.ToInt32(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.PortfolioResource WHERE ParentResourceID=@id", Db.P("@id", resourceId)));
            int projCount = Convert.ToInt32(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.Project WHERE ResourceID=@id", Db.P("@id", resourceId)));
            return childCount > 0 || projCount > 0;
        }

        public static void DeleteResource(int resourceId)
        {
            Db.Exec("DELETE FROM dbo.PortfolioResource WHERE ResourceID=@id", Db.P("@id", resourceId));
        }

        /// <summary>All Projects assigned anywhere within a resource's own sub-tree (self + all descendants) --
        /// this is the "team" view used both by the Org Chart drill-down and the Dashboard resource filter.</summary>
        public static DataTable GetProjectsByResource(int resourceId)
        {
            return Db.Query(@"
                WITH descendants AS (
                    SELECT ResourceID FROM dbo.PortfolioResource WHERE ResourceID=@id
                    UNION ALL
                    SELECT c.ResourceID FROM dbo.PortfolioResource c
                    INNER JOIN descendants d ON c.ParentResourceID = d.ResourceID
                )
                SELECT p.ProjectID, p.ProjectName, p.IsNonJiraProject, p.ProjectManager, p.IsActive, p.CreatedBy, p.CreatedDate
                FROM dbo.Project p
                WHERE p.ResourceID IN (SELECT ResourceID FROM descendants)
                ORDER BY p.CreatedDate DESC
                OPTION (MAXRECURSION 100)",
                Db.P("@id", resourceId));
        }

        /// <summary>All ResourceIDs in a resource's own sub-tree (itself + every descendant) -- used when a
        /// Portfolio Hierarchy view needs team roll-up semantics instead of an exact single-node match.</summary>
        public static System.Collections.Generic.List<int> GetDescendantResourceIds(int resourceId)
        {
            DataTable dt = Db.Query(@"
                WITH descendants AS (
                    SELECT ResourceID FROM dbo.PortfolioResource WHERE ResourceID=@id
                    UNION ALL
                    SELECT c.ResourceID FROM dbo.PortfolioResource c
                    INNER JOIN descendants d ON c.ParentResourceID = d.ResourceID
                )
                SELECT ResourceID FROM descendants OPTION (MAXRECURSION 100)",
                Db.P("@id", resourceId));
            var ids = new System.Collections.Generic.List<int>();
            foreach (DataRow r in dt.Rows) ids.Add(Convert.ToInt32(r["ResourceID"]));
            return ids;
        }

        /// <summary>Top-level resources (e.g. Accountable Execs) -- level 0 of the cascading hierarchy picker.</summary>
        public static DataTable GetRootResources()
        {
            return Db.Query("SELECT ResourceID, ResourceName FROM dbo.PortfolioResource WHERE ParentResourceID IS NULL AND IsActive=1 ORDER BY ResourceName");
        }

        /// <summary>Direct active children of a resource -- one level of the cascading hierarchy picker.</summary>
        public static DataTable GetChildResources(int parentId)
        {
            return Db.Query("SELECT ResourceID, ResourceName FROM dbo.PortfolioResource WHERE ParentResourceID=@pid AND IsActive=1 ORDER BY ResourceName",
                Db.P("@pid", parentId));
        }

        /// <summary>Root-to-leaf breadcrumb display path for a resource, e.g. "Zahoor &gt; Naveed &gt; Raheel".</summary>
        public static string GetResourcePath(int resourceId)
        {
            DataRow r = GetResourceById(resourceId);
            if (r == null) return "";
            var names = new System.Collections.Generic.List<string> { r["ResourceName"].ToString() };
            object parent = r["ParentResourceID"];
            int guard = 0;
            while (parent != DBNull.Value && parent != null && guard++ < 20)
            {
                DataRow pr = GetResourceById(Convert.ToInt32(parent));
                if (pr == null) break;
                names.Insert(0, pr["ResourceName"].ToString());
                parent = pr["ParentResourceID"];
            }
            return string.Join(" &rsaquo; ", names.ToArray());
        }

        /// <summary>
        /// Auto-maps the JIRA "AccountableExec / AccountableExecLead / SmeLead" chain onto the Portfolio
        /// Hierarchy, creating any missing nodes (idempotent, matched by name), and returns the deepest
        /// (most specific) resource — used as the DEFAULT Portfolio assignment for a JIRA project. Admins
        /// can still manually re-point a project elsewhere afterwards (e.g. onto an engineer added later).
        /// </summary>
        public static int? EnsureHierarchyPath(string accountableExec, string accountableExecLead, string smeLead, string modifiedBy)
        {
            int? current = null;
            current = EnsureNode(accountableExec, current, modifiedBy);
            current = EnsureNode(accountableExecLead, current, modifiedBy);
            current = EnsureNode(smeLead, current, modifiedBy);
            return current;
        }

        private static int? EnsureNode(string name, int? parentId, string modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(name)) return parentId;
            name = name.Trim();
            DataRow existing = GetResourceByName(name);
            if (existing != null) return Convert.ToInt32(existing["ResourceID"]);
            return SaveResource(0, name, null, parentId, true, modifiedBy);
        }

        // ===================================================================
        // PHOTO / AVATAR (Org Chart node image)
        // ===================================================================

        public static void SaveResourcePhoto(int resourceId, byte[] photo, string contentType)
        {
            Db.Exec("UPDATE dbo.PortfolioResource SET Photo=@p, PhotoContentType=@ct WHERE ResourceID=@id",
                Db.P("@p", photo ?? (object)DBNull.Value), Db.P("@ct", contentType ?? (object)DBNull.Value),
                Db.P("@id", resourceId));
        }

        public static byte[] GetResourcePhoto(int resourceId, out string contentType)
        {
            DataRow r = Db.QueryRow("SELECT Photo, PhotoContentType FROM dbo.PortfolioResource WHERE ResourceID=@id", Db.P("@id", resourceId));
            if (r == null || r["Photo"] == DBNull.Value) { contentType = null; return null; }
            contentType = r["PhotoContentType"] == DBNull.Value ? "image/jpeg" : r["PhotoContentType"].ToString();
            return (byte[])r["Photo"];
        }
    }
}
