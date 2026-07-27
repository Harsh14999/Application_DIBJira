-- ============================================================
-- 05_HistoryTables.sql
-- Creates tables to store Oracle BPM history snapshots:
--   CapexOpexHistory   <- DIBPROD1.H_MEMO_CAPEX_OPEX
--   CapexOpexDetails   <- DIBPROD1.MEMO_CAPEX_OPEX_DETAILS
--   GLHistory          <- DIBPROD1.H_MEMO_GL_DETAILS
-- These are replaced on each sync (TRUNCATE + INSERT).
-- ============================================================
USE DFM_BPM;
GO

-- ------------------------------------------------------------
-- CapexOpexHistory
-- Source: DIBPROD1.H_MEMO_CAPEX_OPEX
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.CapexOpexHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CapexOpexHistory (
        RowID               INT            IDENTITY(1,1) PRIMARY KEY,
        HistoryRowID        NVARCHAR(100)  NULL,
        CF_Sts              NVARCHAR(50)   NULL,
        CF_CreatedBy        NVARCHAR(200)  NULL,
        CF_CreatedDateTime  DATETIME       NULL,
        CF_ModifiedBy       NVARCHAR(200)  NULL,
        CF_ModifiedDateTime DATETIME       NULL,
        DepartmentName      NVARCHAR(500)  NULL,
        CapexOpexDescription NVARCHAR(500) NULL,
        ItemType            NVARCHAR(20)   NULL,   -- 'Capex' or 'Opex'
        CapexOpexID         NVARCHAR(100)  NULL,
        BudgetedAmount      DECIMAL(18,4)  NULL,
        UtilizedAmount      DECIMAL(18,4)  NULL,
        Balance             DECIMAL(18,4)  NULL,
        LockedAmount        DECIMAL(18,4)  NULL,
        AvailableAmount     DECIMAL(18,4)  NULL,
        SyncedAt            DATETIME       NOT NULL DEFAULT GETDATE()
    );
END
GO

-- ------------------------------------------------------------
-- CapexOpexDetails
-- Source: DIBPROD1.MEMO_CAPEX_OPEX_DETAILS
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.CapexOpexDetails', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CapexOpexDetails (
        RowID               INT            IDENTITY(1,1) PRIMARY KEY,
        InsertionOrderID    NVARCHAR(100)  NULL,
        ItemType            NVARCHAR(50)   NULL,
        ItemID              NVARCHAR(100)  NULL,
        ItemDescription     NVARCHAR(500)  NULL,
        BudgetedAmount      DECIMAL(18,4)  NULL,
        UtilizedAmount      DECIMAL(18,4)  NULL,
        Balance             DECIMAL(18,4)  NULL,
        LockedAmount        DECIMAL(18,4)  NULL,
        AvailableAmount     DECIMAL(18,4)  NULL,
        LockedStatus        NVARCHAR(50)   NULL,
        WIName              NVARCHAR(200)  NULL,
        ClaimAmount         DECIMAL(18,4)  NULL,
        CpOpID              NVARCHAR(100)  NULL,
        BalClaimAmt         DECIMAL(18,4)  NULL,
        OldClaimAmount      DECIMAL(18,4)  NULL,
        SyncedAt            DATETIME       NOT NULL DEFAULT GETDATE()
    );
END
GO

-- ------------------------------------------------------------
-- GLHistory
-- Source: DIBPROD1.H_MEMO_GL_DETAILS
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.GLHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GLHistory (
        RowID               INT            IDENTITY(1,1) PRIMARY KEY,
        HistoryRowID        NVARCHAR(100)  NULL,
        CF_Sts              NVARCHAR(50)   NULL,
        CF_CreatedBy        NVARCHAR(200)  NULL,
        CF_CreatedDateTime  DATETIME       NULL,
        CF_ModifiedBy       NVARCHAR(200)  NULL,
        CF_ModifiedDateTime DATETIME       NULL,
        GLNumber            NVARCHAR(100)  NULL,
        GLDescription       NVARCHAR(500)  NULL,
        GLStatus            NVARCHAR(50)   NULL,
        GLOpenedDate        DATETIME       NULL,
        GLBudgetedAmount    DECIMAL(18,4)  NULL,
        GLUtilizedAmount    DECIMAL(18,4)  NULL,
        GLAvailableAmount   DECIMAL(18,4)  NULL,
        GLLockedAmount      DECIMAL(18,4)  NULL,
        GLTopupAmount       DECIMAL(18,4)  NULL,
        GLAmountPostTopup   DECIMAL(18,4)  NULL,
        AmsLockedAmt        DECIMAL(18,4)  NULL,
        CapitalizedAmount   DECIMAL(18,4)  NULL,
        InvoiceAmtProcessed DECIMAL(18,4)  NULL,
        CapexOpexID         NVARCHAR(100)  NULL,
        CapexBudgetedAmount DECIMAL(18,4)  NULL,
        CommittedAmt        DECIMAL(18,4)  NULL,
        SyncedAt            DATETIME       NOT NULL DEFAULT GETDATE()
    );
END
GO

-- ------------------------------------------------------------
-- Stored procedures for history sync (TRUNCATE + INSERT)
-- ------------------------------------------------------------

-- sp_SyncCapexOpexHistory
IF OBJECT_ID('dbo.sp_SyncCapexOpexHistory', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SyncCapexOpexHistory;
GO
CREATE PROCEDURE dbo.sp_SyncCapexOpexHistory
    @HistoryRowID        NVARCHAR(100),
    @CF_Sts              NVARCHAR(50),
    @CF_CreatedBy        NVARCHAR(200),
    @CF_CreatedDateTime  DATETIME,
    @CF_ModifiedBy       NVARCHAR(200),
    @CF_ModifiedDateTime DATETIME,
    @DepartmentName      NVARCHAR(500),
    @CapexOpexDescription NVARCHAR(500),
    @ItemType            NVARCHAR(20),
    @CapexOpexID         NVARCHAR(100),
    @BudgetedAmount      DECIMAL(18,4),
    @UtilizedAmount      DECIMAL(18,4),
    @Balance             DECIMAL(18,4),
    @LockedAmount        DECIMAL(18,4),
    @AvailableAmount     DECIMAL(18,4)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.CapexOpexHistory
        (HistoryRowID, CF_Sts, CF_CreatedBy, CF_CreatedDateTime,
         CF_ModifiedBy, CF_ModifiedDateTime, DepartmentName, CapexOpexDescription,
         ItemType, CapexOpexID, BudgetedAmount, UtilizedAmount,
         Balance, LockedAmount, AvailableAmount)
    VALUES
        (@HistoryRowID, @CF_Sts, @CF_CreatedBy, @CF_CreatedDateTime,
         @CF_ModifiedBy, @CF_ModifiedDateTime, @DepartmentName, @CapexOpexDescription,
         @ItemType, @CapexOpexID, @BudgetedAmount, @UtilizedAmount,
         @Balance, @LockedAmount, @AvailableAmount);
END
GO

-- sp_SyncCapexOpexDetails
IF OBJECT_ID('dbo.sp_SyncCapexOpexDetails', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SyncCapexOpexDetails;
GO
CREATE PROCEDURE dbo.sp_SyncCapexOpexDetails
    @InsertionOrderID NVARCHAR(100),
    @ItemType         NVARCHAR(50),
    @ItemID           NVARCHAR(100),
    @ItemDescription  NVARCHAR(500),
    @BudgetedAmount   DECIMAL(18,4),
    @UtilizedAmount   DECIMAL(18,4),
    @Balance          DECIMAL(18,4),
    @LockedAmount     DECIMAL(18,4),
    @AvailableAmount  DECIMAL(18,4),
    @LockedStatus     NVARCHAR(50),
    @WIName           NVARCHAR(200),
    @ClaimAmount      DECIMAL(18,4),
    @CpOpID           NVARCHAR(100),
    @BalClaimAmt      DECIMAL(18,4),
    @OldClaimAmount   DECIMAL(18,4)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.CapexOpexDetails
        (InsertionOrderID, ItemType, ItemID, ItemDescription,
         BudgetedAmount, UtilizedAmount, Balance, LockedAmount, AvailableAmount,
         LockedStatus, WIName, ClaimAmount, CpOpID, BalClaimAmt, OldClaimAmount)
    VALUES
        (@InsertionOrderID, @ItemType, @ItemID, @ItemDescription,
         @BudgetedAmount, @UtilizedAmount, @Balance, @LockedAmount, @AvailableAmount,
         @LockedStatus, @WIName, @ClaimAmount, @CpOpID, @BalClaimAmt, @OldClaimAmount);
END
GO

-- sp_SyncGLHistory
IF OBJECT_ID('dbo.sp_SyncGLHistory', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SyncGLHistory;
GO
CREATE PROCEDURE dbo.sp_SyncGLHistory
    @HistoryRowID        NVARCHAR(100),
    @CF_Sts              NVARCHAR(50),
    @CF_CreatedBy        NVARCHAR(200),
    @CF_CreatedDateTime  DATETIME,
    @CF_ModifiedBy       NVARCHAR(200),
    @CF_ModifiedDateTime DATETIME,
    @GLNumber            NVARCHAR(100),
    @GLDescription       NVARCHAR(500),
    @GLStatus            NVARCHAR(50),
    @GLOpenedDate        DATETIME,
    @GLBudgetedAmount    DECIMAL(18,4),
    @GLUtilizedAmount    DECIMAL(18,4),
    @GLAvailableAmount   DECIMAL(18,4),
    @GLLockedAmount      DECIMAL(18,4),
    @GLTopupAmount       DECIMAL(18,4),
    @GLAmountPostTopup   DECIMAL(18,4),
    @AmsLockedAmt        DECIMAL(18,4),
    @CapitalizedAmount   DECIMAL(18,4),
    @InvoiceAmtProcessed DECIMAL(18,4),
    @CapexOpexID         NVARCHAR(100),
    @CapexBudgetedAmount DECIMAL(18,4),
    @CommittedAmt        DECIMAL(18,4)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.GLHistory
        (HistoryRowID, CF_Sts, CF_CreatedBy, CF_CreatedDateTime,
         CF_ModifiedBy, CF_ModifiedDateTime, GLNumber, GLDescription,
         GLStatus, GLOpenedDate, GLBudgetedAmount, GLUtilizedAmount,
         GLAvailableAmount, GLLockedAmount, GLTopupAmount, GLAmountPostTopup,
         AmsLockedAmt, CapitalizedAmount, InvoiceAmtProcessed,
         CapexOpexID, CapexBudgetedAmount, CommittedAmt)
    VALUES
        (@HistoryRowID, @CF_Sts, @CF_CreatedBy, @CF_CreatedDateTime,
         @CF_ModifiedBy, @CF_ModifiedDateTime, @GLNumber, @GLDescription,
         @GLStatus, @GLOpenedDate, @GLBudgetedAmount, @GLUtilizedAmount,
         @GLAvailableAmount, @GLLockedAmount, @GLTopupAmount, @GLAmountPostTopup,
         @AmsLockedAmt, @CapitalizedAmount, @InvoiceAmtProcessed,
         @CapexOpexID, @CapexBudgetedAmount, @CommittedAmt);
END
GO
