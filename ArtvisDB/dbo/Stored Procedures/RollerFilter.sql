

CREATE   PROC [dbo].[RollerFilter]
WITH EXECUTE AS OWNER
AS
Set Nocount ON

-- 1. Roller types
SELECT [rolTypeID] as id, [name] FROM [iRolType] ORDER BY [name]

-- 2. Firm
CREATE TABLE #Firm(firmID int)
INSERT INTO #Firm(firmID)
SELECT DISTINCT firmID FROM roller

Exec sl_Firms



