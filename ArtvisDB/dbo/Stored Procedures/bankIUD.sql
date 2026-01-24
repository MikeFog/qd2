CREATE PROC [dbo].[bankIUD]
(
@bankId int = NULL OUT,
@name nvarchar(255) = NULL,
@bik varchar(32) = NULL,
@corAccount varchar(32) = NULL,
@actionName varchar(32)
)
AS

SET NOCOUNT ON
IF @actionName = 'AddItem' BEGIN
	INSERT INTO  bank(name, bik, corAccount) 
	VALUES(@name, @bik, @corAccount)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @bankId = SCOPE_IDENTITY()
	END
ELSE IF @actionName = 'DeleteItem' BEGIN
	DELETE FROM bank WHERE bankID = @bankID
	END
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE 	bank
	SET 		name = @name, 
					bik = @bik, 
					corAccount = @corAccount
	WHERE bankID = @bankID

	END
