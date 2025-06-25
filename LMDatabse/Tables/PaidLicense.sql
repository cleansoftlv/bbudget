CREATE TABLE [dbo].[PaidLicense]
(
	[id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[user_id] INT NOT NULL,
	[created] DATETIME NOT NULL,
	[date_from] DATETIME NOT NULL,
	[date_to] DATETIME NOT NULL, 
    [order_id] INT NOT NULL, 
    [is_cancelled] BIT NOT NULL DEFAULT 0 
)

GO

CREATE INDEX [IX_PaidLicense_userid] ON [dbo].[PaidLicense] ([user_id])
INCLUDE ([date_from])
