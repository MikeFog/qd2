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

SELECT 
	s.[studioID],
	s.[name],
	Cast(
		CASE 
			WHEN sa.[studioID] Is NULL then 0
			ELSE 1
		END As Bit) isObjectSelected	
FROM 
	[vStudio] s
	LEFT JOIN [StudioAgency] sa ON s.[studioID] = sa.[studioID]
		AND sa.[agencyID] = @agencyID
ORDER BY
	isObjectSelected desc, s.[name]
