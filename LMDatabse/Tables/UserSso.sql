CREATE TABLE [dbo].[UserSso]
(
	[provider] NVARCHAR(100) NOT NULL , 
    [sso_id] NVARCHAR(100) NOT NULL, 
    [user_id] INT NOT NULL, 
    [name] NVARCHAR(250) NULL, 
    PRIMARY KEY ([provider], [sso_id])
)

GO

CREATE INDEX [IX_UserSso_userid] ON [dbo].[UserSso] ([user_id])
