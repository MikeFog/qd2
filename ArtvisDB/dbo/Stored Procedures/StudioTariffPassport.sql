


CREATE    PROC [dbo].[StudioTariffPassport]
(
@tariffID	int = NULL
)
AS
SET NOCOUNT ON
-- 1. Roller type
SELECT distinct rt.rolTypeID as id, rt.name, rt.isLoadable 
FROM iRolType rt
	join [RolStyle] rs on rs.rolTypeID = rt.rolTypeID
	LEFT JOIN StudioTariff r on r.RolStyleID = rs.RolStyleID and r.TariffID = @TariffID
WHERE	dbo.f_IsActiveChildFilter(r.RolStyleID, rt.isActive, 1) = 1
ORDER BY rt.name

-- 2. roller style
SELECT rs.[rolStyleID] as id, rs.[name], rs.[rolTypeID] 
FROM [RolStyle] rs
	LEFT JOIN StudioTariff r on r.RolStyleID = rs.RolStyleID and r.TariffID = @TariffID
WHERE	dbo.f_IsActiveChildFilter(r.RolStyleID, rs.isActive, 1) = 1
ORDER BY rs.name

SELECT [tariffTypeID] as id, [name] FROM [iStudioTariffType] ORDER BY [name]

