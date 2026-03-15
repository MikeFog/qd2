-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 31.01.2008
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[PackageDiscounts]
(
	@packageDiscountId INT = NULL,
	@hidePLInThePast bit = 0
)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT * 
	FROM PackageDiscount pd 
	WHERE 
		pd.packageDiscountID = ISNULL(@packageDiscountId, pd.packageDiscountID)
		And (@hidePLInThePast = 0 
		or Exists(Select 1 From PackageDiscountPriceList pdpl Where pdpl.packageDiscountID = pd.packageDiscountId And pdpl.finishDate > GETDATE()))
	ORDER BY pd.[name]
END