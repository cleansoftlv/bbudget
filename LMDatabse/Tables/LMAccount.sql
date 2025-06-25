CREATE TABLE [dbo].[LMAccount]
(
	[id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[user_id] INT NOT NULL,
	[name] NVARCHAR(150) NOT NULL,
	[token] NVARCHAR(300) NOT NULL,
	[user_name] NVARCHAR(150) NOT NULL,
	[additional_currencies] NVARCHAR(1000) NULL,
	[transfer_category_id] BIGINT NULL,
	[cross_currency_transfer_category_id] BIGINT NULL,
	[sort_tran_on_load_more] BIT NOT NULL DEFAULT 0, 
    [is_active] BIT NOT NULL DEFAULT 0, 
    [lm_account_id] BIGINT NOT NULL
)

GO

CREATE INDEX [IX_LMAccount_userid] ON [dbo].[LMAccount] ([user_id])
