
CREATE    PROC [dbo].[massmediaPassport]
(
@massmediaID smallint = NULL
)
WITH EXECUTE AS OWNER
AS

SET NOCOUNT ON

-- 1. bank
EXEC banks

-- 2. Massmedia
SELECT 
	a.*, 
	Cast(
	CASE 
		WHEN am.massmediaID Is NULL then 0
		ELSE 1
	END as Bit) isObjectSelected
FROM 
	[Agency] a
	LEFT JOIN AgencyMassmedia am ON am.agencyID = a.agencyID
		AND am.massmediaID = @massmediaID
--WHERE
--	(a.[IsActive] = 1 or am.agencyID Is Not Null)
ORDER BY 
	isObjectSelected desc, a.[name] 

-- 3. rolType
SELECT rt.rolTypeID as id, rt.name 
FROM 	iRolType rt
	left join vMassMedia m on m.rolTypeID = rt.rolTypeID and m.massmediaID = @massmediaID
WHERE 	dbo.f_IsActiveChildFilter(m.rolTypeID, rt.isActive, @massmediaID) = 1
ORDER BY rt.name

-- 4. Groups
select *, mg.massmediaGroupID as id
	from MassmediaGroup mg order by mg.name

