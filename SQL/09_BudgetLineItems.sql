-- =====================================================================
-- DFM_BPM  –  Budget Line Items & Invoices (Post-Approval PET Budget Tracking)
-- Run AFTER 02_CreateTables.sql
-- =====================================================================
USE DFM_BPM;
GO

-- ===================================================================
-- Budget Line Items — added by the Requestor once a PET is Approved.
-- One PET Form can have many Budget Lines (against the approved project).
-- ===================================================================
IF OBJECT_ID('dbo.PetBudgetLine','U') IS NULL
CREATE TABLE dbo.PetBudgetLine (
    BudgetLineID   INT IDENTITY(1,1) PRIMARY KEY,
    PetFormID      INT NOT NULL REFERENCES dbo.PetForm(PetFormID),
    SerialNo       INT NOT NULL DEFAULT(0),
    VendorName     NVARCHAR(300)  NULL,
    Justification  NVARCHAR(500)  NULL,
    Cost           DECIMAL(18,2)  NOT NULL DEFAULT(0),
    Currency       NVARCHAR(10)   NOT NULL DEFAULT('AED'),
    GLNumber       NVARCHAR(50)   NULL,
    PetRef         NVARCHAR(100)  NULL,
    CamId          NVARCHAR(100)  NULL,
    CamStatus      NVARCHAR(100)  NULL,
    CamComments    NVARCHAR(500)  NULL,
    LpoRequest     NVARCHAR(100)  NULL,
    LpoStatus      NVARCHAR(100)  NULL,
    LpoComments    NVARCHAR(500)  NULL,
    CreatedBy      NVARCHAR(100)  NULL,
    CreatedDate    DATETIME NOT NULL DEFAULT(GETDATE()),
    ModifiedBy     NVARCHAR(100)  NULL,
    ModifiedDate   DATETIME NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_PetBudgetLine_PetFormID' AND object_id=OBJECT_ID('dbo.PetBudgetLine'))
CREATE INDEX IX_PetBudgetLine_PetFormID ON dbo.PetBudgetLine(PetFormID);
GO

-- ===================================================================
-- Budget Invoices — a Budget Line can have MULTIPLE invoices raised
-- against it over time (partial payments, multiple deliveries, etc.)
-- ===================================================================
IF OBJECT_ID('dbo.PetBudgetInvoice','U') IS NULL
CREATE TABLE dbo.PetBudgetInvoice (
    InvoiceID      INT IDENTITY(1,1) PRIMARY KEY,
    BudgetLineID   INT NOT NULL REFERENCES dbo.PetBudgetLine(BudgetLineID),
    InvoiceNo      NVARCHAR(100)  NULL,
    InvoiceAmount  DECIMAL(18,2)  NOT NULL DEFAULT(0),
    InvoiceStatus  NVARCHAR(100)  NULL,
    PaymentDate    DATETIME NULL,
    CreatedBy      NVARCHAR(100)  NULL,
    CreatedDate    DATETIME NOT NULL DEFAULT(GETDATE()),
    ModifiedBy     NVARCHAR(100)  NULL,
    ModifiedDate   DATETIME NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_PetBudgetInvoice_BudgetLineID' AND object_id=OBJECT_ID('dbo.PetBudgetInvoice'))
CREATE INDEX IX_PetBudgetInvoice_BudgetLineID ON dbo.PetBudgetInvoice(BudgetLineID);
GO
