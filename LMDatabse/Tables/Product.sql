﻿CREATE TABLE [dbo].[Product]
(
	[id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[name] NVARCHAR(255) NOT NULL,
	[days] INT NOT NULL, 
    [is_archived] BIT NOT NULL DEFAULT 0
)
