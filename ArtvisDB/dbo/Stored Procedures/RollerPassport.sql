
CREATE        PROC [dbo].[RollerPassport] (
@RollerId int = null
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON	

-- 1. Roller type
SELECT rt.rolTypeID as id, rt.name, rt.isLoadable 
FROM iRolType rt
ORDER BY rt.name

-- 2. roller style
SELECT rs.[rolStyleID] as id, rs.[name]
FROM [RolStyle] rs
	LEFT JOIN Roller r on r.RolStyleID = rs.RolStyleID and r.RollerId = @RollerId
WHERE	dbo.f_IsActiveChildFilter(r.RolStyleID, rs.isActive, 1) = 1
ORDER BY rs.name

-- 3. firms
CREATE TABLE #Firm(firmID int)
INSERT INTO #Firm SELECT firmID FROM Firm
EXEC sl_Firms

-- 4. Firm Brands
select 	b.*, fb.firmID
from 	Roller r
		join FirmBrand fb
			on r.FirmId = fb.FirmId
		join Brand b
			on fb.BrandId = b.BrandId	
where
		r.RollerId = @RollerID

--5. Roller ActionType
SELECT rat.rolActionTypeID AS id, rat.NAME AS name FROM dbo.iRollerActionType rat


