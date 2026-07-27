-- ============================================================
-- 08_MailConfig.sql
-- Email configuration table + Email log table
-- Run once against DFM_BPM database
-- ============================================================

-- 1. Email SMTP Configuration (key-value pairs, sensitive values encrypted)
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name='EmailConfig' AND type='U')
BEGIN
    CREATE TABLE dbo.EmailConfig (
        ConfigID    INT IDENTITY(1,1) PRIMARY KEY,
        ConfigKey   NVARCHAR(100) NOT NULL,
        ConfigValue NVARCHAR(2000) NULL,
        IsEncrypted BIT NOT NULL DEFAULT 0,
        UpdatedBy   NVARCHAR(100) NULL,
        UpdatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT UQ_EmailConfig_Key UNIQUE (ConfigKey)
    );
    -- Seed default (empty) values
    INSERT INTO dbo.EmailConfig (ConfigKey, ConfigValue, IsEncrypted) VALUES
        ('SmtpEnabled',     'false',    0),
        ('SmtpHost',        '',         0),
        ('SmtpPort',        '587',      0),
        ('SmtpEnableSsl',   'true',     0),
        ('SmtpUser',        '',         0),
        ('SmtpPassword',    '',         1),
        ('SmtpFromAddress', '',         0),
        ('SmtpFromName',    'DFM BPM',  0);
    PRINT 'EmailConfig table created and seeded.';
END
ELSE PRINT 'EmailConfig already exists.';

-- 2. Email transmission log
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name='EmailLog' AND type='U')
BEGIN
    CREATE TABLE dbo.EmailLog (
        LogID          INT IDENTITY(1,1) PRIMARY KEY,
        SentDate       DATETIME NOT NULL DEFAULT GETDATE(),
        ToAddress      NVARCHAR(1000) NULL,
        CcAddress      NVARCHAR(1000) NULL,
        Subject        NVARCHAR(500) NULL,
        Body           NVARCHAR(MAX) NULL,
        Status         NVARCHAR(50) NOT NULL DEFAULT 'Pending',  -- Sent | Failed
        ErrorMessage   NVARCHAR(2000) NULL,
        TriggerEvent   NVARCHAR(200) NULL,
        PetFormID      INT NULL,
        SentBy         NVARCHAR(100) NULL
    );
    PRINT 'EmailLog table created.';
END
ELSE PRINT 'EmailLog already exists.';
