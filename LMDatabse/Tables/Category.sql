CREATE TABLE [dbo].[Category]
(
    [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [budget_id] INT NOT NULL,              -- FK to Budget
    [user_id] INT NOT NULL,                -- Owner user id (denormalized for quick filtering)
    [name] NVARCHAR(150) NOT NULL,         -- Category name
    [description] NVARCHAR(500) NULL,      -- Optional description
    [is_income] BIT NOT NULL DEFAULT 0,    -- Income vs expense
    [exclude_from_budget] BIT NOT NULL DEFAULT 0,   -- Exclude from budgeting calculations
    [exclude_from_totals] BIT NOT NULL DEFAULT 0,   -- Exclude from total progress/summary
    [archived] BIT NOT NULL DEFAULT 0,     -- Archived flag
    [archived_on] DATETIME NULL,           -- When archived
    [updated_at] DATETIME NOT NULL DEFAULT (GETUTCDATE()),            -- Last update timestamp
    [created_at] DATETIME NOT NULL DEFAULT (GETUTCDATE()) -- Creation timestamp
);
GO

ALTER TABLE [dbo].[Category]
ADD CONSTRAINT [FK_Category_Budget] FOREIGN KEY ([budget_id]) REFERENCES [dbo].[Budget]([id]) ON DELETE CASCADE;
GO

-- Indexes for common access patterns
CREATE INDEX [IX_Category_budget_id] ON [dbo].[Category] ([budget_id]);
GO
CREATE INDEX [IX_Category_user_id] ON [dbo].[Category] ([user_id]);
GO
CREATE INDEX [IX_Category_active] ON [dbo].[Category] ([budget_id], [archived]) INCLUDE ([name]);
GO
