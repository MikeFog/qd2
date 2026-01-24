CREATE     PROCEDURE [dbo].[UserIUD]
(
@userID smallint OUT,
@groupID smallint = NULL,
@firstName nvarchar(32) = NULL,
@secondName nvarchar(32) = NULL,
@lastName nvarchar(32) = NULL,
@loginName nvarchar(32) = NULL,
@password binary(16) = NULL,
@confirmPassword binary(16) = NULL,
@newpassword binary(16) = NULL,
@birthday datetime = NULL,
@isActive bit = null,
@email  nvarchar(64) = NULL,
@phone  nvarchar(32) = NULL,
@isAdmin bit = 0,
@isTrafficManager bit = 0,
@isGrantor bit = 0,
@isBookkeeper bit = 0,
@actionName varchar(32)
)
as
SET NOCOUNT on

IF @actionName = 'AddItem' BEGIN
	IF @password <> @confirmPassword
	BEGIN
		RAISERROR('ConfirmationPassword', 16, 1)
		RETURN
	END

	IF @email Is Not Null And Trim(@email) <> '' And dbo.CheckEmail(@email) = 0
	BEGIN
		RAISERROR('InvalidEmail', 16, 1)
		RETURN
	END
	
	INSERT INTO [User](firstName, secondName, lastName, loginName, passwordHash, birthday, isActive, email, phone, [isAdmin], [isTrafficManager], [isGrantor], isBookkeeper)
	VALUES(@firstName, @secondName, @lastName, @loginName, @password, @birthday, @isActive, @email, @phone, @isAdmin, @isTrafficManager, @isGrantor, @isBookkeeper)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @userID = SCOPE_IDENTITY()

	IF NOT @groupID IS NULL
		INSERT INTO [GroupMember]([groupID], [userID])
		VALUES(@groupID, @userID)		

	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [User] WHERE UserID = @UserID
ELSE IF @actionName = 'UpdateItem' BEGIN
	SELECT @password = passwordHash FROM [User] WHERE [userID] = @userID

	IF --@oldpassword IS NOT NULL OR 
		@confirmPassword IS NOT NULL OR @newpassword IS NOT NULL
	BEGIN
--		IF ISNULL(@oldpassword, '') <> ISNULL(@password, '')
--		BEGIN
--			RAISERROR('ConfirmationOldPassword', 16, 1)
--			RETURN
--		END
		
		IF @newpassword <> @confirmPassword
		BEGIN
			RAISERROR('ConfirmationPassword', 16, 1)
			RETURN
		END
		
		SET @password = @newpassword
	END

	IF @email Is Not Null And Trim(@email) <> '' And dbo.CheckEmail(@email) = 0
	BEGIN
		RAISERROR('InvalidEmail', 16, 1)
		RETURN
	END

	UPDATE	
		[User]
	SET	
		firstName = @firstName, 
		secondName = @secondName, 
		lastName = @lastName, 
		loginName = @loginName, 
		birthday = @birthday,
		[passwordHash] = @password,
		isActive = @isActive,
		email = @email,
		phone = @phone,
		[isAdmin] = @isAdmin,
		[isTrafficManager] =@isTrafficManager,
		isBookkeeper=@isBookkeeper,
		[isGrantor] = @isGrantor
	WHERE		
		UserID = @UserID

	END
