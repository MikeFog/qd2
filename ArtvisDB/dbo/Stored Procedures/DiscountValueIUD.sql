

CREATE   PROCEDURE [dbo].[DiscountValueIUD]
(
@discountValueID smallint = NULL,
@discountReleaseID int = NULL,
@summa money = NULL,
@discount float = NULL,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
as
set nocount on
IF @actionName = 'AddItem' BEGIN
	INSERT INTO [DiscountValue](discountReleaseID, summa, discount)
	VALUES(@discountReleaseID, @summa, @discount)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @discountValueID = SCOPE_IDENTITY()

	EXEC DiscountValues @discountValueID = @discountValueID
END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [DiscountValue] WHERE discountValueID = @discountValueID
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE	
		[DiscountValue]
	SET			
		discountReleaseID = @discountReleaseID, 
		summa = @summa, 
		discount = @discount
	WHERE		
		discountValueID = @discountValueID

	EXEC DiscountValues @discountValueID = @discountValueID
END





