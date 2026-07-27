-- ============================================================
-- 15_ProjectEngineersMultiSelect.sql
-- Adds a many-to-many "which Engineers are staffed on this Project" mapping, since the Engineer picker
-- on Project Registration now supports selecting SEVERAL engineers at once (previously it re-used the
-- single Project.ResourceID column, which only supports ONE resource -- fine for the strict single-parent
-- Accountable Exec / Exec Lead / SME Lead hierarchy, but not for a multi-engineer staffing list).
--
-- Project.ResourceID continues to represent the single hierarchy placement (deepest of
-- Exec/ExecLead/SmeLead) used for org-chart rollups and the Dashboard resource filter -- it is no longer
-- ever set to an Engineer-level node going forward.
-- ============================================================
USE DFM_BPM;
GO

IF OBJECT_ID('dbo.ProjectEngineer','U') IS NULL
CREATE TABLE dbo.ProjectEngineer (
    ProjectID   NVARCHAR(100) NOT NULL REFERENCES dbo.Project(ProjectID),
    ResourceID  INT NOT NULL REFERENCES dbo.PortfolioResource(ResourceID),
    CreatedBy   NVARCHAR(100) NULL,
    CreatedDate DATETIME NOT NULL DEFAULT(GETDATE()),
    CONSTRAINT PK_ProjectEngineer PRIMARY KEY (ProjectID, ResourceID)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_ProjectEngineer_Resource' AND object_id=OBJECT_ID('dbo.ProjectEngineer'))
    CREATE INDEX IX_ProjectEngineer_Resource ON dbo.ProjectEngineer(ResourceID);
GO

-- One-time backfill: any Project whose ResourceID currently points DIRECTLY at an Engineer-titled
-- resource (the old single-select behaviour) becomes a ProjectEngineer link instead, with the Project's
-- ResourceID rolled back up to that engineer's parent (the SME Lead) so it still shows correctly under
-- the 3-level hierarchy picker. Safe to run more than once -- the 2nd run matches zero rows.
IF OBJECT_ID('dbo.ProjectEngineer','U') IS NOT NULL AND OBJECT_ID('dbo.Project','U') IS NOT NULL
BEGIN
    INSERT INTO dbo.ProjectEngineer (ProjectID, ResourceID, CreatedBy)
    SELECT p.ProjectID, p.ResourceID, 'migration'
    FROM dbo.Project p
    INNER JOIN dbo.PortfolioResource r ON r.ResourceID = p.ResourceID
    WHERE r.Title = 'Engineer'
      AND NOT EXISTS (SELECT 1 FROM dbo.ProjectEngineer pe WHERE pe.ProjectID = p.ProjectID AND pe.ResourceID = p.ResourceID);

    UPDATE p
    SET p.ResourceID = r.ParentResourceID
    FROM dbo.Project p
    INNER JOIN dbo.PortfolioResource r ON r.ResourceID = p.ResourceID
    WHERE r.Title = 'Engineer';
END
GO

PRINT '15_ProjectEngineersMultiSelect.sql completed successfully.';
