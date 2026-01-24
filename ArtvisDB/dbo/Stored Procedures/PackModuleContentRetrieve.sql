


CREATE    PROC [dbo].[PackModuleContentRetrieve]
(
@pricelistID smallint = NULL,
@packModuleContentId smallint = NULL
)
AS	
SET NOCOUNT ON
SELECT 
	pmc.*,
	m.[name], 
	m.[name] as moduleName,
	mm.[name] as massmediaName,
	mm.[massmediaID],
	mm.groupName
FROM 
	[PackModuleContent] pmc
	INNER JOIN Module m ON m.moduleID = pmc.moduleID
	INNER JOIN vMassmedia mm ON mm.massmediaID = m.massmediaID
WHERE
	pmc.pricelistID = Coalesce(@pricelistID, pmc.pricelistID)
	AND pmc.packModuleContentId = Coalesce(@packModuleContentId, pmc.packModuleContentId)
ORDER BY
	massmediaName,
	moduleName
	
	



