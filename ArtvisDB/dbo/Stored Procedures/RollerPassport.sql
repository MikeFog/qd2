
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

-- 2. roller style — справочник «стиль ролика» удалён вместе с модулем
--    «Производство роликов»; паспорт ролика поле стиля не показывает.
--    Пустой набор нужной формы, чтобы не сдвигать позиции iTableAlias.
SELECT CAST(NULL AS smallint) as id, CAST(NULL AS nvarchar(64)) as name
WHERE 1 = 0

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


