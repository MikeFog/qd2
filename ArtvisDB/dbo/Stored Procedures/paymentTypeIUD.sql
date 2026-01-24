



CREATE     PROC [dbo].[paymentTypeIUD]
(
@paymentTypeID int = NULL OUT,
@name nvarchar(32) = NULL,
@isHidden bit = NULL,
@actionName varchar(32),
@isActive bit = 0
)
AS

SET NOCOUNT ON
IF @actionName = 'AddItem' BEGIN
	INSERT INTO  paymentType(name, isHidden, isActive) 
	VALUES(@name, @isHidden, @isActive)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @paymentTypeID = SCOPE_IDENTITY()
	END
ELSE IF @actionName = 'DeleteItem' BEGIN
	DELETE FROM paymentType WHERE paymentTypeID = @paymentTypeID
	END
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE paymentType
	SET 	name = @name, 
			isActive = @isActive,
			isHidden = @isHidden
	WHERE paymentTypeID = @paymentTypeID

	END







