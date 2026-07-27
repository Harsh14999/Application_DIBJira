-- ============================================================
-- 12_PortfolioHierarchyAndProjectRegistration.sql
-- Adds:
--  1) dbo.PortfolioResource — self-referencing org/reporting hierarchy ("Portfolio Hierarchy" master)
--  2) dbo.Project — the standalone Project Registration master (independent of PetForm/Spend Request)
--  3) Back-fills dbo.Project from any pre-existing dbo.PetForm rows so historical Spend Requests keep
--     pointing at an already-"registered" project after this migration.
-- ============================================================
USE DFM_BPM;
GO

-- ============================================================
-- 1. Portfolio Hierarchy (self-referencing tree of resources/managers)
-- ============================================================
IF OBJECT_ID('dbo.PortfolioResource','U') IS NULL
CREATE TABLE dbo.PortfolioResource (
    ResourceID       INT IDENTITY(1,1) PRIMARY KEY,
    ResourceName     NVARCHAR(200) NOT NULL,
    Title            NVARCHAR(200) NULL,
    ParentResourceID INT NULL REFERENCES dbo.PortfolioResource(ResourceID),
    IsActive         BIT NOT NULL DEFAULT(1),
    CreatedBy        NVARCHAR(100) NULL,
    CreatedDate      DATETIME NOT NULL DEFAULT(GETDATE()),
    ModifiedBy       NVARCHAR(100) NULL,
    ModifiedDate     DATETIME NULL,
    CONSTRAINT UQ_PortfolioResource_Name UNIQUE (ResourceName)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_PortfolioResource_Parent' AND object_id=OBJECT_ID('dbo.PortfolioResource'))
    CREATE INDEX IX_PortfolioResource_Parent ON dbo.PortfolioResource(ParentResourceID);
GO

-- ============================================================
-- 2. Project Registration master — the single source of truth for "registered projects".
--    ProjectID is the business key: the JiraID for JIRA projects, or a free-text ID for Non-JIRA ones.
-- ============================================================
IF OBJECT_ID('dbo.Project','U') IS NULL
CREATE TABLE dbo.Project (
    ProjectID        NVARCHAR(100) NOT NULL PRIMARY KEY,
    ProjectName      NVARCHAR(300) NULL,
    IsNonJiraProject BIT NOT NULL DEFAULT(0),
    ProjectManager   NVARCHAR(200) NULL,
    ResourceID       INT NULL REFERENCES dbo.PortfolioResource(ResourceID),  -- Portfolio assignment (1 resource at a time)
    IsActive         BIT NOT NULL DEFAULT(1),
    CreatedBy        NVARCHAR(100) NULL,
    CreatedDate      DATETIME NOT NULL DEFAULT(GETDATE()),
    ModifiedBy       NVARCHAR(100) NULL,
    ModifiedDate     DATETIME NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Project_Resource' AND object_id=OBJECT_ID('dbo.Project'))
    CREATE INDEX IX_Project_Resource ON dbo.Project(ResourceID);
GO

-- Back-fill from any pre-existing PetForm rows (each distinct ProjectID becomes a registered Project row),
-- so existing Spend Requests keep pointing at an already-registered project post-migration.
IF OBJECT_ID('dbo.Project','U') IS NOT NULL AND OBJECT_ID('dbo.PetForm','U') IS NOT NULL
BEGIN
    INSERT INTO dbo.Project (ProjectID, ProjectName, IsNonJiraProject, CreatedBy, CreatedDate)
    SELECT x.ProjectID, x.ProjectName, x.IsNonJiraProject, x.CreatedBy, x.CreatedDate
    FROM (
        SELECT p.ProjectID,
               MAX(ISNULL(j.Summary, p.ProjectName)) AS ProjectName,
               MAX(CONVERT(INT,p.IsNonJiraProject)) AS IsNonJiraProject,
               MIN(p.CreatedBy) AS CreatedBy,
               MIN(p.CreatedDate) AS CreatedDate
        FROM dbo.PetForm p
        LEFT JOIN dbo.JiraIssues j ON j.JiraID = p.ProjectID
        WHERE ISNULL(p.ProjectID,'') <> ''
        GROUP BY p.ProjectID
    ) x
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Project pr WHERE pr.ProjectID = x.ProjectID);
END
GO

PRINT '12_PortfolioHierarchyAndProjectRegistration.sql completed successfully.';
