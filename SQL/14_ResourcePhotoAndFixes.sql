-- ============================================================
-- 14_ResourcePhotoAndFixes.sql
-- Adds:
--  1) dbo.PortfolioResource.Photo / PhotoContentType — avatar image for the Org Chart
--  2) Confirms dbo.ProjectSizing exists (safety net if 13_... wasn't run in some environments)
-- ============================================================
USE DFM_BPM;
GO

IF COL_LENGTH('dbo.PortfolioResource','Photo') IS NULL
    ALTER TABLE dbo.PortfolioResource ADD Photo VARBINARY(MAX) NULL;
GO
IF COL_LENGTH('dbo.PortfolioResource','PhotoContentType') IS NULL
    ALTER TABLE dbo.PortfolioResource ADD PhotoContentType NVARCHAR(100) NULL;
GO

PRINT '14_ResourcePhotoAndFixes.sql completed successfully.';
