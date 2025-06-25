CREATE TABLE [dbo].[LMAccountLicense]
(
    [lm_account_id] BIGINT NOT NULL PRIMARY KEY,
    [date_from] DateTime NOT NULL, 
    [date_to] DATETIME NOT NULL, 
    [is_paid] BIT NOT NULL, 
    [created] DATETIME NOT NULL,
    [modified] DATETIME NOT NULL, 
    [by_user_id] INT NOT NULL, 
    [paid_license_id] INT NULL
)
