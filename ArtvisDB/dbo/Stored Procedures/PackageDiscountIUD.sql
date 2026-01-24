-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 31.01.2008
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[PackageDiscountIUD]
(
	@packageDiscountId INT = NULL,
	@name NVARCHAR(32) = NULL,
	@count TINYINT = null,
	@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

    IF @actionName = 'AddItem'
    BEGIN
		INSERT INTO PackageDiscount([NAME], [count]) 
			VALUES(@name, @count)
		
		if @@rowcount <> 1
		begin
			raiserror('InternalError', 16, 1)
			return 
		end 

		SET @packageDiscountId = SCOPE_IDENTITY()
		
		EXEC PackageDiscounts @packageDiscountId
	END
	ELSE IF @actionName = 'UpdateItem'
	BEGIN
		UPDATE PackageDiscount
		SET [NAME] = @name, [count] = @count
		WHERE packageDiscountID = @packageDiscountId
		
		EXEC PackageDiscounts @packageDiscountId
	END
	ELSE IF @actionName = 'DeleteItem'
	BEGIN
		DELETE FROM PackageDiscount WHERE packageDiscountID = @packageDiscountId
	END
END


