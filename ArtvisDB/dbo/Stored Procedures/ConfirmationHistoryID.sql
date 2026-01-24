CREATE PROCEDURE [dbo].[ConfirmationHistoryID]
(
@confirmationID int = NULL,
@confirmationTypeID tinyint = NULL,
@userID smallint = NULL,
@grantorID smallint = NULL,
@description nvarchar(2000) = NULL,
@actionName varchar(32)
)
as
set nocount on
IF @actionName = 'AddItem' BEGIN
	INSERT INTO [ConfirmationHistory](confirmationTypeID, userID, grantorID, description)
	VALUES(@confirmationTypeID, @userID, @grantorID, @description)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @confirmationID = SCOPE_IDENTITY()
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [ConfirmationHistory] WHERE confirmationID = @confirmationID



