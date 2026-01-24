-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 31.01.2008
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[PackageDiscountMassmedias]
(
@packageDiscountPriceListID INT = NULL,
@packageDiscountMassmediaID INT = null 
)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT 
		pdm.*, 
		mm.name as nameWithoutGroup, 
		mm.groupName, 
		mm.nameWithGroup as name
	FROM 
		PackageDiscountMassmedia pdm 
		INNER JOIN [vMassmedia] mm ON pdm.massmediaID = mm.[massmediaID]
	WHERE 
		pdm.packageDiscountMassmediaID = ISNULL(@packageDiscountMassmediaID, pdm.packageDiscountMassmediaID)
		AND pdm.packageDiscountPriceListID = ISNULL(@packageDiscountPriceListID, pdm.packageDiscountPriceListID)
	ORDER BY 
		mm.nameWithGroup
END
