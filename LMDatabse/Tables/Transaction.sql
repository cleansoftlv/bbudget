CREATE TABLE [dbo].[Transaction]
(
    [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY, -- switched to BIGINT for larger transaction volume
    [budget_id] INT NOT NULL,                -- FK to Budget
    [user_id] INT NOT NULL,                  -- Owner user id
    [date] DATE NOT NULL,                    -- Transaction date
    [payee] NVARCHAR(150) NULL,              -- Payee
    [amount] DECIMAL(19,4) NOT NULL,         -- Amount (use DECIMAL(19,2) for cents only if needed)
    [currency] CHAR(3) NOT NULL,             -- ISO 4217 currency code
    [category_id] INT NULL,                  -- FK to Category
    [notes] NVARCHAR(1000) NULL,             -- Notes
    [asset_id] INT NULL,                     -- FK to Asset
    [is_income] BIT NOT NULL DEFAULT 0,      -- Income/expense flag
    [parent_group_id] BIGINT NULL,           -- Optional parent group id for grouped transactions
    [transaction_type] TINYINT NOT NULL DEFAULT 0, -- Enum for transaction type (0=Simple,1=Transfer,2=Split,...)
    [created_at] DATETIME NOT NULL DEFAULT (GETUTCDATE()),
    [updated_at] DATETIME NOT NULL DEFAULT (GETUTCDATE())
);
GO

ALTER TABLE [dbo].[Transaction]
ADD CONSTRAINT [FK_Transaction_Budget] FOREIGN KEY ([budget_id]) REFERENCES [dbo].[Budget]([id]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[Transaction]
ADD CONSTRAINT [FK_Transaction_Category] FOREIGN KEY ([category_id]) REFERENCES [dbo].[Category]([id]) ON DELETE SET NULL;
GO

ALTER TABLE [dbo].[Transaction]
ADD CONSTRAINT [FK_Transaction_Asset] FOREIGN KEY ([asset_id]) REFERENCES [dbo].[Asset]([id]) ON DELETE SET NULL;
GO

ALTER TABLE [dbo].[Transaction]
ADD CONSTRAINT [FK_Transaction_ParentGroup] FOREIGN KEY ([parent_group_id]) REFERENCES [dbo].[Transaction]([id]) ON DELETE SET NULL;
GO

CREATE INDEX [IX_Transaction_budget_id] ON [dbo].[Transaction] ([budget_id]);
GO
CREATE INDEX [IX_Transaction_user_id] ON [dbo].[Transaction] ([user_id]);
GO
CREATE INDEX [IX_Transaction_category_id] ON [dbo].[Transaction] ([category_id]);
GO
CREATE INDEX [IX_Transaction_asset_id] ON [dbo].[Transaction] ([asset_id]);
GO
CREATE INDEX [IX_Transaction_date] ON [dbo].[Transaction] ([date]);
GO
CREATE INDEX [IX_Transaction_parent_group_id] ON [dbo].[Transaction] ([parent_group_id]);
GO
CREATE INDEX [IX_Transaction_transaction_type] ON [dbo].[Transaction] ([transaction_type]);
GO
