select * from LMAccountLicense

INSERT INTO LMAccountLicense 
SELECT 
	DISTINCT lm_account_id,
	GETUTCDATE(), 
	DATEADD(day, 365, GETUTCDATE()),
	0, 
	GETUTCDATE(),
	GETUTCDATE(),
	1
FROM LMAccount