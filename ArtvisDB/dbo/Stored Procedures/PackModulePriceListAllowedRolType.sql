CREATE PROC [dbo].[PackModulePriceListAllowedRolType]
(
@pricelistID SMALLINT
)
as

SET NOCOUNT ON
SELECT TOP 1
	 mm.[roltypeID]
FROM 
	[PackModuleContent] pmc
	INNER JOIN [Module] m ON pmc.[moduleID] = m.[moduleID]
	INNER JOIN [MassMedia] mm ON m.[massmediaID] = mm.[massmediaID]
WHERE 
	pmc.[pricelistID] = @pricelistID	
		


