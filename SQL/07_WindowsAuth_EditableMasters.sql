-- ============================================================
-- 07_WindowsAuth_EditableMasters.sql
-- Adds:
--  1) Windows Auth support (removes PasswordHash/Salt, keeps roles)
--  2) CapexMasterHistory, OpexMasterHistory, GLMasterHistory, VendorMasterHistory
--  3) Extended columns for editable masters (Description, ModifiedBy, ModifiedDate)
--  4) SyncLog columns (PulledCount, InsertedCount, UpdatedCount, FailedCount, TriggeredBy)
--  5) AppUsers - allow null password columns (Windows Auth)
-- ============================================================
USE DFM_BPM;
GO

-- ============================================================
-- 1. AppUsers: make password columns nullable (Windows Auth users have no password)
-- ============================================================
IF COL_LENGTH('dbo.AppUsers','PasswordHash') IS NOT NULL
BEGIN
    ALTER TABLE dbo.AppUsers ALTER COLUMN PasswordHash NVARCHAR(256) NULL;
    ALTER TABLE dbo.AppUsers ALTER COLUMN PasswordSalt NVARCHAR(100) NULL;
END
GO

-- ============================================================
-- 2. CapexMaster: add editable columns if missing
-- ============================================================
IF COL_LENGTH('dbo.CapexMaster','Description') IS NULL
    ALTER TABLE dbo.CapexMaster ADD Description NVARCHAR(1000) NULL;
GO
IF COL_LENGTH('dbo.CapexMaster','BudgetAfterLockedAmount') IS NULL
    ALTER TABLE dbo.CapexMaster ADD BudgetAfterLockedAmount DECIMAL(18,4) NULL;
GO
IF COL_LENGTH('dbo.CapexMaster','ClaimAmount') IS NULL
    ALTER TABLE dbo.CapexMaster ADD ClaimAmount DECIMAL(18,4) NULL;
GO
IF COL_LENGTH('dbo.CapexMaster','NetBalance') IS NULL
    ALTER TABLE dbo.CapexMaster ADD NetBalance DECIMAL(18,4) NULL;
GO
IF COL_LENGTH('dbo.CapexMaster','ModifiedBy') IS NULL
    ALTER TABLE dbo.CapexMaster ADD ModifiedBy NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.CapexMaster','ModifiedDate') IS NULL
    ALTER TABLE dbo.CapexMaster ADD ModifiedDate DATETIME NULL;
GO

-- CapexMasterHistory
IF OBJECT_ID('dbo.CapexMasterHistory','U') IS NULL
CREATE TABLE dbo.CapexMasterHistory (
    HistoryID              INT           IDENTITY(1,1) PRIMARY KEY,
    CapexID                NVARCHAR(100) NOT NULL,
    Description            NVARCHAR(1000) NULL,
    BudgetedAmount         DECIMAL(18,4) NULL,
    UtilizedAmount         DECIMAL(18,4) NULL,
    AvailableAmount        DECIMAL(18,4) NULL,
    LockedAmount           DECIMAL(18,4) NULL,
    BudgetAfterLockedAmount DECIMAL(18,4) NULL,
    ClaimAmount            DECIMAL(18,4) NULL,
    NetBalance             DECIMAL(18,4) NULL,
    IsActive               BIT           NULL,
    ChangedBy              NVARCHAR(100) NOT NULL,
    ChangedDate            DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

-- ============================================================
-- 3. OpexMaster: add editable columns
-- ============================================================
IF COL_LENGTH('dbo.OpexMaster','Description') IS NULL
    ALTER TABLE dbo.OpexMaster ADD Description NVARCHAR(1000) NULL;
GO
IF COL_LENGTH('dbo.OpexMaster','BudgetAfterLockedAmount') IS NULL
    ALTER TABLE dbo.OpexMaster ADD BudgetAfterLockedAmount DECIMAL(18,4) NULL;
GO
IF COL_LENGTH('dbo.OpexMaster','ClaimAmount') IS NULL
    ALTER TABLE dbo.OpexMaster ADD ClaimAmount DECIMAL(18,4) NULL;
GO
IF COL_LENGTH('dbo.OpexMaster','NetBalance') IS NULL
    ALTER TABLE dbo.OpexMaster ADD NetBalance DECIMAL(18,4) NULL;
GO
IF COL_LENGTH('dbo.OpexMaster','ModifiedBy') IS NULL
    ALTER TABLE dbo.OpexMaster ADD ModifiedBy NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.OpexMaster','ModifiedDate') IS NULL
    ALTER TABLE dbo.OpexMaster ADD ModifiedDate DATETIME NULL;
GO

-- OpexMasterHistory
IF OBJECT_ID('dbo.OpexMasterHistory','U') IS NULL
CREATE TABLE dbo.OpexMasterHistory (
    HistoryID              INT           IDENTITY(1,1) PRIMARY KEY,
    OpexID                 NVARCHAR(100) NOT NULL,
    Description            NVARCHAR(1000) NULL,
    BudgetedAmount         DECIMAL(18,4) NULL,
    UtilizedAmount         DECIMAL(18,4) NULL,
    AvailableAmount        DECIMAL(18,4) NULL,
    LockedAmount           DECIMAL(18,4) NULL,
    BudgetAfterLockedAmount DECIMAL(18,4) NULL,
    ClaimAmount            DECIMAL(18,4) NULL,
    NetBalance             DECIMAL(18,4) NULL,
    IsActive               BIT           NULL,
    ChangedBy              NVARCHAR(100) NOT NULL,
    ChangedDate            DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

-- ============================================================
-- 4. GLMaster: add editable columns
-- ============================================================
IF COL_LENGTH('dbo.GLMaster','ModifiedBy') IS NULL
    ALTER TABLE dbo.GLMaster ADD ModifiedBy NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.GLMaster','ModifiedDate') IS NULL
    ALTER TABLE dbo.GLMaster ADD ModifiedDate DATETIME NULL;
GO

-- GLMasterHistory
IF OBJECT_ID('dbo.GLMasterHistory','U') IS NULL
CREATE TABLE dbo.GLMasterHistory (
    HistoryID              INT           IDENTITY(1,1) PRIMARY KEY,
    GLNumber               NVARCHAR(50)  NOT NULL,
    GLDescription          NVARCHAR(500) NULL,
    BudgetedAmount         DECIMAL(18,4) NULL,
    UtilizedAmount         DECIMAL(18,4) NULL,
    BalanceAmount          DECIMAL(18,4) NULL,
    IsActive               BIT           NULL,
    ChangedBy              NVARCHAR(100) NOT NULL,
    ChangedDate            DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

-- ============================================================
-- 5. VendorMaster: add editable columns
-- ============================================================
IF COL_LENGTH('dbo.VendorMaster','ContactEmail') IS NULL
    ALTER TABLE dbo.VendorMaster ADD ContactEmail NVARCHAR(200) NULL;
GO
IF COL_LENGTH('dbo.VendorMaster','ContactPhone') IS NULL
    ALTER TABLE dbo.VendorMaster ADD ContactPhone NVARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.VendorMaster','ModifiedBy') IS NULL
    ALTER TABLE dbo.VendorMaster ADD ModifiedBy NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.VendorMaster','ModifiedDate') IS NULL
    ALTER TABLE dbo.VendorMaster ADD ModifiedDate DATETIME NULL;
GO

-- VendorMasterHistory
IF OBJECT_ID('dbo.VendorMasterHistory','U') IS NULL
CREATE TABLE dbo.VendorMasterHistory (
    HistoryID   INT           IDENTITY(1,1) PRIMARY KEY,
    VendorCode  NVARCHAR(50)  NOT NULL,
    VendorName  NVARCHAR(200) NULL,
    IsActive    BIT           NULL,
    ChangedBy   NVARCHAR(100) NOT NULL,
    ChangedDate DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

-- ============================================================
-- 6. SyncLog: add tracking columns for JIRA sync
-- ============================================================
IF COL_LENGTH('dbo.SyncLog','PulledCount') IS NULL
    ALTER TABLE dbo.SyncLog ADD PulledCount INT NULL;
GO
IF COL_LENGTH('dbo.SyncLog','InsertedCount') IS NULL
    ALTER TABLE dbo.SyncLog ADD InsertedCount INT NULL;
GO
IF COL_LENGTH('dbo.SyncLog','UpdatedCount') IS NULL
    ALTER TABLE dbo.SyncLog ADD UpdatedCount INT NULL;
GO
IF COL_LENGTH('dbo.SyncLog','FailedCount') IS NULL
    ALTER TABLE dbo.SyncLog ADD FailedCount INT NULL;
GO
IF COL_LENGTH('dbo.SyncLog','TriggeredBy') IS NULL
    ALTER TABLE dbo.SyncLog ADD TriggeredBy NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.SyncLog','ErrorDetail') IS NULL
    ALTER TABLE dbo.SyncLog ADD ErrorDetail NVARCHAR(MAX) NULL;
GO

-- ============================================================
-- 7. UserRoleAssignments: ensure Admin role type supported
-- ============================================================
-- No schema change needed; RoleType is NVARCHAR so 'Admin' already works.

-- ============================================================
-- 8. Seed default roles (Requestor, Reviewer, Approver, Admin)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE RoleName='Requestor')
    INSERT INTO dbo.UserRoles(RoleName,Description,IsActive) VALUES('Requestor','Default Windows user - can raise PET',1);
IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE RoleName='Reviewer')
    INSERT INTO dbo.UserRoles(RoleName,Description,IsActive) VALUES('Reviewer','First-level PET reviewer',1);
IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE RoleName='Approver')
    INSERT INTO dbo.UserRoles(RoleName,Description,IsActive) VALUES('Approver','PET final approver',1);
IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE RoleName='Admin')
    INSERT INTO dbo.UserRoles(RoleName,Description,IsActive) VALUES('Admin','System administrator',1);
GO

PRINT '07_WindowsAuth_EditableMasters.sql completed successfully.';
