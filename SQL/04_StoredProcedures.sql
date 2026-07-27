-- =====================================================================
-- DFM_BPM  –  Stored Procedures
-- =====================================================================
USE DFM_BPM;
GO

-- =====================================================================
-- sp_SyncCapex  – Upsert CAPEX master rows from staging
-- Called by OracleSync.aspx.cs after pulling from Oracle
-- =====================================================================
IF OBJECT_ID('dbo.sp_SyncCapex','P') IS NOT NULL DROP PROCEDURE dbo.sp_SyncCapex;
GO
CREATE PROCEDURE dbo.sp_SyncCapex
    @CapexID        NVARCHAR(100),
    @BudgetedAmount DECIMAL(18,2),
    @UtilizedAmount DECIMAL(18,2),
    @AvailableAmount DECIMAL(18,2),
    @LockedAmount   DECIMAL(18,2),
    @GLNumbers      NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.CapexMaster WHERE CapexID = @CapexID)
        UPDATE dbo.CapexMaster SET
            BudgetedAmount = @BudgetedAmount, UtilizedAmount = @UtilizedAmount,
            AvailableAmount = @AvailableAmount, LockedAmount = @LockedAmount,
            GLNumbers = @GLNumbers, LastSyncDate = GETDATE()
        WHERE CapexID = @CapexID;
    ELSE
        INSERT INTO dbo.CapexMaster(CapexID,BudgetedAmount,UtilizedAmount,AvailableAmount,LockedAmount,GLNumbers,LastSyncDate)
        VALUES(@CapexID,@BudgetedAmount,@UtilizedAmount,@AvailableAmount,@LockedAmount,@GLNumbers,GETDATE());
END
GO

-- =====================================================================
-- sp_SyncOpex
-- =====================================================================
IF OBJECT_ID('dbo.sp_SyncOpex','P') IS NOT NULL DROP PROCEDURE dbo.sp_SyncOpex;
GO
CREATE PROCEDURE dbo.sp_SyncOpex
    @OpexID         NVARCHAR(100),
    @BudgetedAmount DECIMAL(18,2),
    @UtilizedAmount DECIMAL(18,2),
    @AvailableAmount DECIMAL(18,2),
    @LockedAmount   DECIMAL(18,2),
    @Contracts      NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.OpexMaster WHERE OpexID = @OpexID)
        UPDATE dbo.OpexMaster SET
            BudgetedAmount = @BudgetedAmount, UtilizedAmount = @UtilizedAmount,
            AvailableAmount = @AvailableAmount, LockedAmount = @LockedAmount,
            Contracts = @Contracts, LastSyncDate = GETDATE()
        WHERE OpexID = @OpexID;
    ELSE
        INSERT INTO dbo.OpexMaster(OpexID,BudgetedAmount,UtilizedAmount,AvailableAmount,LockedAmount,Contracts,LastSyncDate)
        VALUES(@OpexID,@BudgetedAmount,@UtilizedAmount,@AvailableAmount,@LockedAmount,@Contracts,GETDATE());
END
GO

-- =====================================================================
-- sp_SyncGL
-- =====================================================================
IF OBJECT_ID('dbo.sp_SyncGL','P') IS NOT NULL DROP PROCEDURE dbo.sp_SyncGL;
GO
CREATE PROCEDURE dbo.sp_SyncGL
    @GLNumber           NVARCHAR(50),
    @GLDescription      NVARCHAR(500),
    @GLOpenedDate       DATETIME,
    @BudgetedAmount     DECIMAL(18,2),
    @BPMLockedAmount    DECIMAL(18,2),
    @AMSLockedAmount    DECIMAL(18,2),
    @UtilizedAmount     DECIMAL(18,2),
    @CapitalizedAmount  DECIMAL(18,2),
    @InvoiceProcessed   DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Balance DECIMAL(18,2) = @BudgetedAmount - ISNULL(@BPMLockedAmount,0) 
                                    - ISNULL(@AMSLockedAmount,0) - ISNULL(@UtilizedAmount,0);
    IF EXISTS (SELECT 1 FROM dbo.GLMaster WHERE GLNumber = @GLNumber)
        UPDATE dbo.GLMaster SET GLDescription=@GLDescription, GLOpenedDate=@GLOpenedDate,
            BudgetedAmount=@BudgetedAmount, BPMLockedAmount=@BPMLockedAmount,
            AMSLockedAmount=@AMSLockedAmount, UtilizedAmount=@UtilizedAmount,
            BalanceAmount=@Balance, CapitalizedAmount=@CapitalizedAmount,
            InvoiceProcessedAmt=@InvoiceProcessed, LastSyncDate=GETDATE()
        WHERE GLNumber=@GLNumber;
    ELSE
        INSERT INTO dbo.GLMaster(GLNumber,GLDescription,GLOpenedDate,BudgetedAmount,BPMLockedAmount,AMSLockedAmount,UtilizedAmount,BalanceAmount,CapitalizedAmount,InvoiceProcessedAmt,LastSyncDate)
        VALUES(@GLNumber,@GLDescription,@GLOpenedDate,@BudgetedAmount,@BPMLockedAmount,@AMSLockedAmount,@UtilizedAmount,@Balance,@CapitalizedAmount,@InvoiceProcessed,GETDATE());
END
GO

-- =====================================================================
-- sp_SyncVendor
-- =====================================================================
IF OBJECT_ID('dbo.sp_SyncVendor','P') IS NOT NULL DROP PROCEDURE dbo.sp_SyncVendor;
GO
CREATE PROCEDURE dbo.sp_SyncVendor
    @VendorCode NVARCHAR(50),
    @VendorName NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.VendorMaster WHERE VendorCode = @VendorCode)
        UPDATE dbo.VendorMaster SET VendorName=@VendorName, LastSyncDate=GETDATE()
        WHERE VendorCode=@VendorCode;
    ELSE
        INSERT INTO dbo.VendorMaster(VendorCode,VendorName,LastSyncDate)
        VALUES(@VendorCode,@VendorName,GETDATE());
END
GO

-- =====================================================================
-- sp_GetHierarchy  – Project > PET/Contract > LPO > Invoice rollup
-- =====================================================================
IF OBJECT_ID('dbo.sp_GetHierarchy','P') IS NOT NULL DROP PROCEDURE dbo.sp_GetHierarchy;
GO
CREATE PROCEDURE dbo.sp_GetHierarchy
    @ProjectID NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Level 1: Projects
    SELECT p.ProjectID, p.ProjectName, p.ProjectManager, p.ProjectAmount,
           p.UtilizedAmt, p.BalanceAmt, p.BPMLockedAmt, p.AMSLockedAmt,
           p.CapexID, p.ProjectStatus
    FROM dbo.BPM_Projects p
    WHERE (@ProjectID IS NULL OR p.ProjectID = @ProjectID)
    ORDER BY p.ProjectID;

    -- Level 2a: PET linked to this project
    SELECT pet.PETReferenceNo, pet.Description, pet.PETApprovedAmt,
           pet.BPMLockedAmount, pet.Utilized, pet.Balance, pet.ProjectID
    FROM dbo.BPM_PET pet
    WHERE (@ProjectID IS NULL OR pet.ProjectID = @ProjectID)
    ORDER BY pet.PETReferenceNo;

    -- Level 2b: Contracts (Memo) linked to project via CAPEX ID
    SELECT c.WiName, c.Reference, c.Department, c.ContractNo, c.VendorName,
           c.LCAmount, c.ContractBalance, c.ContractStatus, c.OpexID
    FROM dbo.BPM_Contract c
    INNER JOIN dbo.BPM_Projects p ON p.CapexID = c.OpexID OR p.ProjectID = c.OpexID
    WHERE (@ProjectID IS NULL OR p.ProjectID = @ProjectID);

    -- Level 3: LPO linked to project's GL numbers
    SELECT l.WiName, l.LPONo, l.LPODesc, l.VendorName, l.LCAmount,
           l.LPOStatus, l.BudgetAmount, l.AvailableBalance, l.GLNumber
    FROM dbo.BPM_LPO l
    INNER JOIN dbo.GLMaster g ON l.GLNumber = g.GLNumber
    INNER JOIN dbo.BPM_Projects p ON p.CapexID LIKE '%' + g.GLNumber + '%'
    WHERE (@ProjectID IS NULL OR p.ProjectID = @ProjectID);

    -- Level 4: Invoices
    SELECT i.WiName, i.InvoiceNumber, i.InvoiceType, i.VendorName,
           i.LCAmount, i.AMSInvoiceStatus, i.InvoiceDate, i.InvoiceRefNo
    FROM dbo.BPM_Invoice i
    WHERE (@ProjectID IS NULL OR i.InvoiceRefNo LIKE '%' + @ProjectID + '%');
END
GO

PRINT 'Stored procedures created successfully.';
GO
