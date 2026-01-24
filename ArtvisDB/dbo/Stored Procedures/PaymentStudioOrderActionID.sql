


CREATE    PROCEDURE [dbo].[PaymentStudioOrderActionID]
(
@paymentID int,
@actionID int,
@summa money = NULL,
@actionName varchar(32)
)
as
set nocount on 
IF @actionName In ('AddItem', 'UpdateItem') BEGIN
	IF NOT EXISTS (
		SELECT * FROM PaymentStudioOrderAction WHERE paymentID = @paymentID AND actionID = @actionID
		)
		INSERT INTO [PaymentStudioOrderAction](paymentID, actionID, summa)
		VALUES(@paymentID, @actionID, @summa)
	ELSE
		UPDATE [PaymentStudioOrderAction]
		SET summa = summa + @summa
		WHERE paymentID = @paymentID AND actionID = @actionID

	END
ELSE IF @actionName = 'DeleteItem' BEGIN
	DELETE FROM [PaymentStudioOrderAction] 
	WHERE paymentID = @paymentID And actionID = @actionID

	END





