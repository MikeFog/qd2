-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 31.01.2008
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[PackageDiscounts]
(
	@packageDiscountId INT = NULL 
)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT * 
	FROM PackageDiscount pd 
	WHERE pd.packageDiscountID = ISNULL(@packageDiscountId, pd.packageDiscountID)
	ORDER BY pd.[name]
END
