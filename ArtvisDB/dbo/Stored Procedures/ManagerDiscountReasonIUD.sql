CREATE   PROCEDURE [dbo].[ManagerDiscountReasonIUD]
(
@ManagerDiscountReasonID smallint Out,
@name nvarchar(128) = NULL,
@actionName varchar(32)
)
AS
SET NOCOUNT ON

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [ManagerDiscountReason](name)
	VALUES(@name)
	
	SET @ManagerDiscountReasonID = SCOPE_IDENTITY()
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [ManagerDiscountReason] WHERE ManagerDiscountReasonID = @ManagerDiscountReasonID
ELSE IF @actionName = 'UpdateItem'
	UPDATE	[ManagerDiscountReason]
	SET		name = @name
	WHERE	ManagerDiscountReasonID = @ManagerDiscountReasonID