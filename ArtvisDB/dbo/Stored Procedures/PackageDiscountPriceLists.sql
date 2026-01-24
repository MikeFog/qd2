-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 01.02.2008
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[PackageDiscountPriceLists]
(
	@packageDiscountPriceListId INT = NULL,
	@packageDiscountID INT = NULL 
)
AS
BEGIN
	SET NOCOUNT ON;

    SELECT pdpl.*,
		'Скидки от ' + convert(varchar,pdpl.startDate,104) + case when pdpl.finishDate is null then space(0) else ' до ' + convert(varchar,pdpl.finishDate,104) end as name
    FROM 
		[PackageDiscountPriceList] pdpl 
    WHERE 
		pdpl.[packageDiscountID] = ISNULL(@packageDiscountID, pdpl.[packageDiscountID])
		AND pdpl.[packageDiscountPriceListID] = ISNULL(@packageDiscountPriceListID, pdpl.[packageDiscountPriceListID])
	ORDER BY
		pdpl.startDate desc
END

