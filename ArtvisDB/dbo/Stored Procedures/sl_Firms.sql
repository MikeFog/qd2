

CREATE   Proc dbo.sl_Firms
AS
SET NOCOUNT ON
SELECT 
	f.*
FROM 
	[Firm] f
	JOIN #Firm f2 On f2.firmID = f.firmID
ORDER BY 
	f.name



