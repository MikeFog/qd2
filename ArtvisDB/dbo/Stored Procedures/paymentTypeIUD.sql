



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
	-- «Не включать в счет на предоплату» (isHidden) — вид оплаты, в акции все кампании одного
	-- вида. Пока тип стоит в акции рядом с другим типом оплаты, признак не меняется.
	IF EXISTS (SELECT 1 FROM paymentType WHERE paymentTypeID = @paymentTypeID AND isHidden <> @isHidden)
		AND EXISTS (SELECT 1 FROM Campaign c
						INNER JOIN Campaign c2 ON c2.actionID = c.actionID AND c2.paymentTypeID <> c.paymentTypeID
					WHERE c.paymentTypeID = @paymentTypeID)
	BEGIN
		RAISERROR('PaymentTypeKindInUse', 16, 1)
		RETURN
	END

	UPDATE paymentType
	SET 	name = @name, 
			isActive = @isActive,
			isHidden = @isHidden
	WHERE paymentTypeID = @paymentTypeID

	END
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[paymentTypeIUD] TO PUBLIC
    AS [dbo];

