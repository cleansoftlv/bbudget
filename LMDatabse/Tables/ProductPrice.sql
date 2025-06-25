CREATE TABLE [dbo].[ProductPrice]
(
	[id] INT NOT NULL PRIMARY KEY IDENTITY (1,1),
	[product_id] INT NOT NULL,
	[price] DECIMAL(18, 2) NOT NULL,
	[currency] CHAR(3) NOT NULL, 
    [is_archived] BIT NOT NULL DEFAULT 0
)
