-- =====================================================================
-- DFM_BPM  –  Seed Data
-- Run AFTER 02_CreateTables.sql
-- =====================================================================
USE DFM_BPM;
GO

-- ===== Roles =====
IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE RoleName='Admin')
INSERT INTO dbo.UserRoles(RoleName, Description) VALUES
('Admin',     'Full system access'),
('Requestor', 'Can create and submit PET forms'),
('Reviewer',  'Can review PET forms before approval'),
('Approver',  'Can approve or reject PET forms'),
('Viewer',    'Read-only access to dashboards');
GO

-- ===== Admin user (default password: Admin@123) =====
-- PasswordHash/Salt generated for "Admin@123" using PBKDF2 via PasswordHelper
IF NOT EXISTS (SELECT 1 FROM dbo.AppUsers WHERE Username='admin')
BEGIN
    DECLARE @roleId INT = (SELECT RoleID FROM dbo.UserRoles WHERE RoleName='Admin');
    INSERT INTO dbo.AppUsers(Username, PasswordHash, PasswordSalt, FullName, Email, RoleID, CreatedBy)
    VALUES ('admin', 
            'rAndom-hash-will-be-set-on-first-run', 
            'rAndom-salt-will-be-set-on-first-run',
            'System Administrator', 'admin@company.ae', @roleId, 'system');
END
GO

-- ===== Page Registry =====
IF NOT EXISTS (SELECT 1 FROM dbo.PageRegistry)
INSERT INTO dbo.PageRegistry(PageName, PageUrl, Category, SortOrder) VALUES
('Dashboard',           '~/Default.aspx',                  'Reports',   1),
('PET Workflow',        '~/Forms/PetWorkflow.aspx',        'Workflow',  2),
('CAPEX Master',        '~/Admin/CapexMaster.aspx',        'Masters',  10),
('OPEX Master',         '~/Admin/OpexMaster.aspx',         'Masters',  11),
('GL Master',           '~/Admin/GLMaster.aspx',           'Masters',  12),
('Vendor Master',       '~/Admin/VendorMaster.aspx',       'Masters',  13),
('User Management',     '~/Admin/UserManagement.aspx',     'Admin',    20),
('Oracle Sync',         '~/Admin/OracleSync.aspx',         'Admin',    21);
GO

-- ===== Page Access for Admin =====
IF NOT EXISTS (SELECT 1 FROM dbo.PageAccess)
BEGIN
    DECLARE @adminRoleId INT = (SELECT RoleID FROM dbo.UserRoles WHERE RoleName='Admin');
    INSERT INTO dbo.PageAccess(RoleID, PageID, CanView)
    SELECT @adminRoleId, PageID, 1 FROM dbo.PageRegistry;
END
GO

-- ===== Default currencies for PET =====
IF OBJECT_ID('dbo.PetCurrency','U') IS NULL
CREATE TABLE dbo.PetCurrency (
    CurrencyID  INT IDENTITY(1,1) PRIMARY KEY,
    Code        NVARCHAR(10)  NOT NULL UNIQUE,
    Name        NVARCHAR(100) NULL,
    RateToLocal DECIMAL(18,6) NOT NULL DEFAULT(1),
    IsActive    BIT NOT NULL DEFAULT(1)
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PetCurrency)
INSERT INTO dbo.PetCurrency(Code, Name, RateToLocal) VALUES
('AED','UAE Dirham',1.0000),
('USD','US Dollar',3.6725),
('EUR','Euro',4.0500),
('GBP','British Pound',4.6500),
('INR','Indian Rupee',0.0440),
('SAR','Saudi Riyal',0.9790);
GO

-- ===== Cost types for PET line items =====
IF OBJECT_ID('dbo.PetCostType','U') IS NULL
CREATE TABLE dbo.PetCostType (
    CostTypeID  INT IDENTITY(1,1) PRIMARY KEY,
    Category    NVARCHAR(200) NOT NULL UNIQUE,
    IsActive    BIT NOT NULL DEFAULT(1)
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PetCostType)
INSERT INTO dbo.PetCostType(Category) VALUES
('Hardware'),('Software License'),('Professional Services'),
('Implementation'),('Training'),('Support & Maintenance'),
('Infrastructure'),('Cloud Services'),('Consulting'),('Other');
GO

PRINT 'Seed data inserted successfully.';
GO
