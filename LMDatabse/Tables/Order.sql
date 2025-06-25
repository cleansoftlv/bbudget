CREATE TABLE [dbo].[Order]
(
	[id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[product_id] INT NOT NULL,
	[user_id] INT NOT NULL,
	[created] DATETIME NOT NULL,
	[modified] DATETIME NOT NULL,
	[status] TINYINT NOT NULL DEFAULT(0), 
    [amount] DECIMAL(18, 2) NOT NULL, 
    [currency] CHAR(3) NOT NULL, 
    [product_price_id] INT NOT NULL, 
    [external_id] NVARCHAR(100) NULL, 
    [external_status] NVARCHAR(2048) NULL
)
