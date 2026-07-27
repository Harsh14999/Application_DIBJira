-- =====================================================================
-- DFM_BPM  –  Rename Ref No prefix from "PET-" to "SR-" (Spend Request)
-- Safe to re-run: only alters the column if it still uses the old "PET-" prefix.
-- =====================================================================
USE DFM_BPM;
GO

IF EXISTS (
    SELECT 1 FROM sys.computed_columns
    WHERE object_id = OBJECT_ID('dbo.PetForm') AND name = 'PetRefNo' AND definition LIKE '%''PET-''%'
)
BEGIN
    ALTER TABLE dbo.PetForm DROP COLUMN PetRefNo;
    ALTER TABLE dbo.PetForm ADD PetRefNo AS ('SR-' + RIGHT('00000' + CAST(PetFormID AS VARCHAR(10)), 5)) PERSISTED;
END
GO
