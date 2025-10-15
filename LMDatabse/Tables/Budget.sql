CREATE TABLE [dbo].[Budget]
(
    [id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [user_id] INT NOT NULL,
    [name] NVARCHAR(150) NOT NULL,
    [user_name] NVARCHAR(150) NOT NULL,
    [additional_currencies] NVARCHAR(1000) NULL,
    [transfer_category_id] BIGINT NULL,
    [cross_currency_transfer_category_id] BIGINT NULL,
    [is_active] BIT NOT NULL DEFAULT 0
);
GO

-- Indexes
CREATE INDEX [IX_Budget_user_id] ON [dbo].[Budget] ([user_id]);
GO
CREATE INDEX [IX_Budget_is_active] ON [dbo].[Budget] ([is_active]);
GO
