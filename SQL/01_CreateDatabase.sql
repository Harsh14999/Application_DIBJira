-- =====================================================================
-- DFM_BPM - Create Database
-- ASP.NET 4.5 / SQL Server 2012+
-- Run as sysadmin on the target SQL Server instance
-- =====================================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'DFM_BPM')
BEGIN
    CREATE DATABASE [DFM_BPM]
        COLLATE SQL_Latin1_General_CP1_CI_AS;
END
GO

USE DFM_BPM;
GO
