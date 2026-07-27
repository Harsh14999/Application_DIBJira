-- ============================================================
-- 11_NonJiraProjectsAndPlatformMaster.sql
-- Adds:
--  1) Non-JIRA project registration support on PetForm
--     (ApproverUsername becomes optional, new IsNonJiraProject / ProjectName columns)
--  2) PlatformMaster table (editable master, seeded from JiraIssues), used to
--     filter the JIRA ID dropdown on PetWorkflow.aspx
-- ============================================================
USE DFM_BPM;
GO

-- ============================================================
-- 1. PetForm: Approver becomes optional (Spend Request Approval is optional
--    when a project is registered purely for tracking purposes)
-- ============================================================
IF EXISTS (
    SELECT 1 FROM sys.columns c
    JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.name = 'PetForm' AND c.name = 'ApproverUsername' AND c.is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.PetForm ALTER COLUMN ApproverUsername NVARCHAR(100) NULL;
END
GO

-- ============================================================
-- 2. PetForm: Non-JIRA project registration columns
-- ============================================================
IF COL_LENGTH('dbo.PetForm','IsNonJiraProject') IS NULL
    ALTER TABLE dbo.PetForm ADD IsNonJiraProject BIT NOT NULL DEFAULT(0);
GO
IF COL_LENGTH('dbo.PetForm','ProjectName') IS NULL
    ALTER TABLE dbo.PetForm ADD ProjectName NVARCHAR(300) NULL;
GO

-- ============================================================
-- 3. Platform Master (editable) - drives the Platform filter on PetWorkflow.aspx
-- ============================================================
IF OBJECT_ID('dbo.PlatformMaster','U') IS NULL
CREATE TABLE dbo.PlatformMaster (
    PlatformID   INT IDENTITY(1,1) PRIMARY KEY,
    PlatformName NVARCHAR(200) NOT NULL,
    IsActive     BIT NOT NULL DEFAULT(1),
    CreatedBy    NVARCHAR(100) NULL,
    CreatedDate  DATETIME NOT NULL DEFAULT(GETDATE()),
    ModifiedBy   NVARCHAR(100) NULL,
    ModifiedDate DATETIME NULL,
    CONSTRAINT UQ_PlatformMaster_Name UNIQUE (PlatformName)
);
GO

-- Seed from distinct Platform values already present in JiraIssues
IF OBJECT_ID('dbo.PlatformMaster','U') IS NOT NULL AND OBJECT_ID('dbo.JiraIssues','U') IS NOT NULL
BEGIN
    INSERT INTO dbo.PlatformMaster (PlatformName, IsActive, CreatedBy)
    SELECT DISTINCT j.Platform, 1, 'System'
    FROM dbo.JiraIssues j
    WHERE ISNULL(j.Platform,'') <> ''
      AND NOT EXISTS (SELECT 1 FROM dbo.PlatformMaster pm WHERE pm.PlatformName = j.Platform);
END
GO

PRINT '11_NonJiraProjectsAndPlatformMaster.sql completed successfully.';
