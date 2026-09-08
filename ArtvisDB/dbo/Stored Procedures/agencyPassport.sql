CREATE          PROC [dbo].[agencyPassport]
(
@agencyID smallint = NULL
)
AS

SET NOCOUNT ON

SELECT 
	mm.isActive,
	mm.[massmediaID], 
	mm.[name],
	mm.groupName as groupName2,	
	Cast(
		CASE 
			WHEN am.massmediaID Is NULL then 0
			ELSE 1
		END As Bit) isObjectSelected
FROM 
	[vMassmedia] mm
	LEFT JOIN AgencyMassmedia am ON am.massmediaID = mm.massmediaID
		AND am.agencyID = @agencyID
ORDER BY
	isObjectSelected desc, mm.[name]
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[agencyPassport] TO PUBLIC
    AS [dbo];

