

CREATE   PROCEDURE [dbo].[PaymentStudioOrderIUD]
(
@paymentID int = NULL,
@firmID smallint = NULL,
@summa money = NULL,
@paymentDate datetime = NULL,
@loggedUserID smallint = NULL,
@paymentTypeID smallint = NULL,
@agencyID smallint = NULL,
@isEnabled bit = NULL,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
as
set nocount on 
IF @actionName = 'AddItem' BEGIN
	INSERT INTO [PaymentStudioOrder](firmID, summa, paymentDate, userID, paymentTypeID, agencyID, isEnabled)
	VALUES(@firmID, @summa, @paymentDate, @loggedUserID, @paymentTypeID, @agencyID, @isEnabled)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @paymentID = SCOPE_IDENTITY()

	EXEC PaymentStudioOrders @paymentID = @paymentID, @loggedUserID = @loggedUserID
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [PaymentStudioOrder] WHERE paymentID = @paymentID
ELSE IF @actionName = 'UpdateItem' BEGIN
	-- Some attributes can't be changed if this payment
	-- already in use

	IF EXISTS(SELECT * FROM PaymentStudioOrderAction WHERE paymentID = @paymentID) BEGIN
		DECLARE @oldFirmID smallint,
			@oldAgencyID smallint, 
			@oldPaymentTypeID smallint,
			@consumed money
	
		SELECT @consumed = SUM(pa.[summa]) FROM [PaymentStudioOrderAction] pa WHERE pa.[paymentID] = @paymentID
		
		SELECT 
			@oldFirmID = firmID,
			@oldAgencyID = agencyID,
			@oldPaymentTypeID = paymentTypeID
		FROM 
			[PaymentStudioOrder]
		WHERE
			paymentID = @paymentID

		IF @oldFirmID <> @firmID OR @oldAgencyID <> @agencyID OR @oldPaymentTypeID <> @paymentTypeID BEGIN
			RAISERROR('PaymentInUse', 16, 1)
			RETURN
		END

		IF @consumed > @summa BEGIN
			RAISERROR('ConsumedIsMoreThanSumma', 16, 1)
			RETURN
		END
		
	END
	
	UPDATE	
		[PaymentStudioOrder]
	SET			
		firmID = @firmID, 
		summa = @summa, 
		paymentDate = @paymentDate, 
		paymentTypeID = @paymentTypeID, 
		agencyID = @agencyID, 
		isEnabled = @isEnabled
	WHERE		
		paymentID = @paymentID

	EXEC PaymentStudioOrders @paymentID = @paymentID, @loggedUserID = @loggedUserID
	END






