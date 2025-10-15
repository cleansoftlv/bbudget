CREATE TABLE [dbo].[Asset]
(
    [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [budget_id] INT NOT NULL,
    [user_id] INT NOT NULL,
    [type] TINYINT NOT NULL,                 -- Enum mapping (e.g. Cash=1, Credit=2, etc.)
    [subtype] TINYINT NULL,                  -- Optional subtype enum
    [name] NVARCHAR(150) NOT NULL,
    [balance] DECIMAL(19,4) NOT NULL DEFAULT 0, -- 4 decimal places supports FX, rounding safety
    [currency] CHAR(3) NOT NULL,             -- ISO 4217 code
    [institution_name] NVARCHAR(150) NULL,
    [exclude_transactions] BIT NOT NULL DEFAULT 0,
    [balance_as_of] DATETIME NULL,
    [closed_on] DATE NULL,
    [created_at] DATETIME NOT NULL DEFAULT GETUTCDATE(),
    [is_active] BIT NOT NULL DEFAULT 1
);
GO

ALTER TABLE [dbo].[Asset]
ADD CONSTRAINT [FK_Asset_Budget] FOREIGN KEY ([budget_id]) REFERENCES [dbo].[Budget]([id]) ON DELETE CASCADE;
GO

-- Indexes
CREATE INDEX [IX_Asset_budget_id] ON [dbo].[Asset] ([budget_id]);
GO
CREATE INDEX [IX_Asset_user_id] ON [dbo].[Asset] ([user_id]);
GO
CREATE INDEX [IX_Asset_is_active] ON [dbo].[Asset] ([is_active]);
GO
CREATE INDEX [IX_Asset_type] ON [dbo].[Asset] ([type]);
GO

-- Note: Use DECIMAL(19,4) for balance to allow minor FX precision; change to (19,2) if only standard currency cents are needed.
