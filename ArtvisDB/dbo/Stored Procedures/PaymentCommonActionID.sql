



CREATE PROCEDURE [dbo].[PaymentCommonActionID]
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
		SELECT * FROM PaymentAction WHERE paymentID = @paymentID AND actionID = @actionID
		)
		INSERT INTO [PaymentAction](paymentID, actionID, summa)
		VALUES(@paymentID, @actionID, @summa)
	ELSE
		UPDATE [PaymentAction]
		SET summa = summa + @summa
		WHERE paymentID = @paymentID AND actionID = @actionID

	END
ELSE IF @actionName = 'DeleteItem' BEGIN
	DELETE FROM [PaymentAction]
	WHERE paymentID = @paymentID And actionID = @actionID

	END






