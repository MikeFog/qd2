-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 31.01.2008
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[PackageDiscountMassmediaID]
(
	@massmediaID SMALLINT,
	@packageDiscountPricelistId INT,
	@packageDiscountMassmediaID INT = NULL,
	@isForType1 bit = 0,
	@isForType2 bit = 0,
	@isForType3 bit = 0,
	@actionName VARCHAR(32)
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;
		
    IF @actionName = 'AddItem'
    BEGIN
		INSERT INTO [PackageDiscountMassmedia] (packageDiscountPricelistId, [massmediaID], isForType1, isForType2, isForType3) 
		VALUES (@packageDiscountPricelistId, @massmediaID, @isForType1, @isForType2, @isForType3 ) 
		
		if @@rowcount <> 1
		begin
			raiserror('InternalError', 16, 1)
			return 
		end 

		SET @packageDiscountMassmediaID = SCOPE_IDENTITY()
		
		EXEC [PackageDiscountMassmedias]
			@packageDiscountMassmediaID = @packageDiscountMassmediaID
	END
	ELSE IF @actionName = 'DeleteItem'
	BEGIN
		DELETE FROM [PackageDiscountMassmedia] WHERE [packageDiscountMassmediaID] = @packageDiscountMassmediaID
	END
	ELSE IF @actionName = 'UpdateItem'
	BEGIN
		UPDATE 
			[PackageDiscountMassmedia]
		SET 
			[massmediaID] = @massmediaID
			,[isForType1] = @isForType1
			,[isForType2] = @isForType2
			,[isForType3] = @isForType3
		WHERE 
			[packageDiscountMassmediaID] = @packageDiscountMassmediaID

		EXEC [PackageDiscountMassmedias]
			@packageDiscountMassmediaID = @packageDiscountMassmediaID
	END
END

