-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 01.02.2008
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[PackageDiscountPriceListIUD]
(
	@packageDiscountPriceListId INT = NULL,
	@packageDiscountId INT = NULL,
	@startDate DATETIME = NULL,
	@finishDate datetime = null,
	@value MONEY = NULL,
	@discount FLOAT = NULL,
	@eachVolume TINYINT = NULL,
	@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
begin
SET NOCOUNT on
DECLARE 
	@Id int,
	@date datetime
IF @actionName IN('AddItem', 'UpdateItem', 'Clone') BEGIN
	IF @startDate > @finishDate BEGIN
		RAISERROR('StartFinishDateError', 16, 1)
		RETURN
	end
END
	if @actionName in ('AddItem', 'UpdateItem') 
		and exists(select * 
	          from PackageDiscountPriceList pdpl 
		           where pdpl.packageDiscountID = @packageDiscountId and 
				(@packageDiscountPriceListId is null or pdpl.packageDiscountPriceListID <> @packageDiscountPriceListId)
				and (pdpl.startDate <= @finishDate
				and pdpl.finishDate >= @startDate))
	begin
		raiserror('PackageDiscountsCross',16,1)
		return 
	end

	IF @actionName = 'AddItem' BEGIN
		INSERT INTO [PackageDiscountPriceList](packageDiscountId, startDate, finishDate, [value], discount, eachVolume)
		VALUES(@packageDiscountId, @startDate, @finishDate, @value, @discount, @eachVolume)

		if @@rowcount <> 1
		begin
			raiserror('InternalError', 16, 1)
			return 
		end 

		SET @packageDiscountPriceListId = SCOPE_IDENTITY()
		
		EXEC [PackageDiscountPriceLists] @packageDiscountPriceListId = @packageDiscountPriceListId
	END
	ELSE IF @actionName = 'DeleteItem' BEGIN
		SELECT @date = finishDate	FROM PackageDiscountPriceList
		WHERE	packageDiscountPriceListId = @packageDiscountPriceListId

		DELETE FROM PackageDiscountPriceList WHERE packageDiscountPriceListId = @packageDiscountPriceListId

		UPDATE PackageDiscountPriceList SET finishDate = @date 
		WHERE [packageDiscountID] = @packageDiscountID AND finishDate = @startDate	
	END
	ELSE IF @actionName = 'UpdateItem' BEGIN

		UPDATE	
			PackageDiscountPriceList
		SET			
			startDate = @startDate,
			finishDate = @finishDate, 
			[value] = @value,
			discount = @discount,
			eachVolume = @eachVolume
		WHERE		
			packageDiscountPriceListId = @packageDiscountPriceListId

		EXEC PackageDiscountPriceLists @packageDiscountPriceListId = @packageDiscountPriceListId
	END
END

