-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 01.02.2008
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[PackageDiscountPriceLists]
(
	@packageDiscountPriceListId INT = NULL,
	@packageDiscountID INT = NULL,
	@hidePLInThePast bit = 0
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
		And (@hidePLInThePast = 0 or pdpl.finishDate > GETDATE())
	ORDER BY
		pdpl.startDate desc
END