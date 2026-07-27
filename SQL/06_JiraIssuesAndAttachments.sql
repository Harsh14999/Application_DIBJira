-- ============================================================
-- 06_JiraIssuesAndAttachments.sql
-- Adds JiraIssues table (project selection source) and
-- PetAttachments table (file uploads per PET form).
-- Run against DFM_BPM database.
-- ============================================================
USE DFM_BPM;
GO



-- ----------------------------------------------------------------
-- PetAttachments: drop old schema if created by 02_CreateTables.sql
-- Old schema used AttachID/StoredPath/SizeBytes/UploadedDate.
-- New schema uses AttachmentID/ContentType/FileContent/UploadedAt.
-- ----------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.PetAttachments') AND name = 'AttachID'
)
BEGIN
    DROP TABLE dbo.PetAttachments;
END
GO

IF OBJECT_ID('dbo.PetAttachments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PetAttachments (
        AttachmentID  INT           IDENTITY(1,1) PRIMARY KEY,
        PetFormID     INT           NOT NULL,
        FileName      NVARCHAR(260) NOT NULL,
        ContentType   NVARCHAR(100) NULL,
        FileContent   VARBINARY(MAX) NOT NULL,
        UploadedBy    NVARCHAR(100) NULL,
        UploadedAt    DATETIME      NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_PetAttach_PetForm FOREIGN KEY (PetFormID)
            REFERENCES dbo.PetForm(PetFormID)
    );
END
GO

-- ----------------------------------------------------------------
-- PetSizing: project sizing assessment history per PET form
-- ----------------------------------------------------------------
IF OBJECT_ID('dbo.PetSizing', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PetSizing (
        AssessmentID         INT           IDENTITY(1,1) PRIMARY KEY,
        PetFormID            INT           NOT NULL,
        Q1Score              DECIMAL(5,2)  NULL,
        Q2Score              DECIMAL(5,2)  NULL,
        Q3Score              DECIMAL(5,2)  NULL,
        Q4Score              DECIMAL(5,2)  NULL,
        Q5Score              DECIMAL(5,2)  NULL,
        Q6Score              DECIMAL(5,2)  NULL,
        Q7Score              DECIMAL(5,2)  NULL,
        TotalWeightedScore   DECIMAL(6,4)  NULL,
        SizeResult           NVARCHAR(5)   NULL,
        CapacityConsumption  NVARCHAR(50)  NULL,
        CreatedBy            NVARCHAR(100) NULL,
        CreatedDate          DATETIME      NOT NULL DEFAULT GETDATE(),
        ModifiedBy           NVARCHAR(100) NULL,
        ModifiedDate         DATETIME      NULL,
        CONSTRAINT FK_PetSizing_PetForm FOREIGN KEY (PetFormID)
            REFERENCES dbo.PetForm(PetFormID)
    );
END
GO
