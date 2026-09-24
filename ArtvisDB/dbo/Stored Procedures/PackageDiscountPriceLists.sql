-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 01.02.2008
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[PackageDiscountPriceLists]
(
	@packageDiscountPriceListId INT = NULL,
	@packageDiscountID INT = NULL,
	@hidePLInThePast bit = 0,
	@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @tDiscountsFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Скидки от ');
	DECLARE @tTo NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' до ');

    SELECT pdpl.*,
		@tDiscountsFrom + convert(varchar,pdpl.startDate,104) + case when pdpl.finishDate is null then space(0) else @tTo + convert(varchar,pdpl.finishDate,104) end as name
    FROM 
		[PackageDiscountPriceList] pdpl 
    WHERE 
		pdpl.[packageDiscountID] = ISNULL(@packageDiscountID, pdpl.[packageDiscountID])
		AND pdpl.[packageDiscountPriceListID] = ISNULL(@packageDiscountPriceListID, pdpl.[packageDiscountPriceListID])
		And (@hidePLInThePast = 0 or pdpl.finishDate > GETDATE())
	ORDER BY
		pdpl.startDate desc
END