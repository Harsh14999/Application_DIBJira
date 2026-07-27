-- ============================================================
-- 13_ProjectSizingAndEngineers.sql
-- Adds:
--  1) dbo.ProjectSizing — 1 sizing assessment per registered Project (independent of PetForm)
--  2) dbo.Project.AccountableExecLead, SmeLead columns (denormalized from JIRA for grid display)
--  3) dbo.Project.ProjectSize column (derived from ProjectSizing for fast grid queries)
--  4) Seed ProjectSizing from existing PetSizing (latest assessment per project) for migration
-- ============================================================
USE DFM_BPM;
GO

-- ============================================================
-- 1. Project-level Sizing (1 per project, upsert model)
-- ============================================================
IF OBJECT_ID('dbo.ProjectSizing','U') IS NULL
CREATE TABLE dbo.ProjectSizing (
    ProjectID            NVARCHAR(100) NOT NULL PRIMARY KEY REFERENCES dbo.Project(ProjectID),
    Q1Score              DECIMAL(5,2)  NULL,
    Q2Score              DECIMAL(5,2)  NULL,
    Q3Score              DECIMAL(5,2)  NULL,
    Q4Score              DECIMAL(5,2)  NULL,
    Q5Score              DECIMAL(5,2)  NULL,
    Q6Score              DECIMAL(5,2)  NULL,
    Q7Score              DECIMAL(5,2)  NULL,
    TotalWeightedScore   DECIMAL(6,4)  NULL,
    SizeResult           NVARCHAR(5)   NULL,   -- XS/S/M/L/XL
    CapacityConsumption  NVARCHAR(50)  NULL,
    ModifiedBy           NVARCHAR(100) NULL,
    ModifiedDate         DATETIME      NULL
);
GO

-- ============================================================
-- 2. Add denormalized JIRA hierarchy fields + ProjectSize to dbo.Project for grid display
-- ============================================================
IF COL_LENGTH('dbo.Project','AccountableExecLead') IS NULL
    ALTER TABLE dbo.Project ADD AccountableExecLead NVARCHAR(200) NULL;
GO
IF COL_LENGTH('dbo.Project','SmeLead') IS NULL
    ALTER TABLE dbo.Project ADD SmeLead NVARCHAR(200) NULL;
GO
IF COL_LENGTH('dbo.Project','ProjectSize') IS NULL
    ALTER TABLE dbo.Project ADD ProjectSize NVARCHAR(5) NULL;
GO

-- ============================================================
-- 3. Seed ProjectSizing from the latest PetSizing per ProjectID (migration only)
-- ============================================================
IF OBJECT_ID('dbo.ProjectSizing','U') IS NOT NULL AND OBJECT_ID('dbo.PetSizing','U') IS NOT NULL
BEGIN
    INSERT INTO dbo.ProjectSizing (ProjectID, Q1Score, Q2Score, Q3Score, Q4Score, Q5Score, Q6Score, Q7Score,
                                   TotalWeightedScore, SizeResult, CapacityConsumption, ModifiedBy, ModifiedDate)
    SELECT pr.ProjectID, s.Q1Score, s.Q2Score, s.Q3Score, s.Q4Score, s.Q5Score, s.Q6Score, s.Q7Score,
           s.TotalWeightedScore, s.SizeResult, s.CapacityConsumption, s.CreatedBy, s.CreatedDate
    FROM dbo.Project pr
    INNER JOIN dbo.PetForm pf ON pf.ProjectID = pr.ProjectID
    INNER JOIN dbo.PetSizing s ON s.PetFormID = pf.PetFormID
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ProjectSizing ps WHERE ps.ProjectID = pr.ProjectID)
      AND s.AssessmentID = (SELECT MAX(s2.AssessmentID) FROM dbo.PetSizing s2
                            INNER JOIN dbo.PetForm pf2 ON pf2.PetFormID = s2.PetFormID
                            WHERE pf2.ProjectID = pr.ProjectID);
END
GO

-- Update Project.ProjectSize from ProjectSizing
UPDATE p SET p.ProjectSize = ps.SizeResult
FROM dbo.Project p
INNER JOIN dbo.ProjectSizing ps ON ps.ProjectID = p.ProjectID
WHERE ISNULL(p.ProjectSize,'') <> ISNULL(ps.SizeResult,'');
GO

-- Back-fill AccountableExecLead/SmeLead from JiraIssues for existing JIRA projects
UPDATE p SET
    p.AccountableExecLead = j.AccountableExecLead,
    p.SmeLead = j.SmeLead
FROM dbo.Project p
INNER JOIN dbo.JiraIssues j ON j.JiraID = p.ProjectID
WHERE p.IsNonJiraProject = 0;
GO

PRINT '13_ProjectSizingAndEngineers.sql completed successfully.';
