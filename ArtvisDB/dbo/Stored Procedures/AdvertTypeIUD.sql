CREATE PROCEDURE [dbo].[AdvertTypeIUD]
(
@advertTypeID smallint OUT,
@name nvarchar(256) = NULL,
@parentID smallint = NULL,
@loggedUserID smallint,
@actionName varchar(32)
)
as
SET NOCOUNT ON
IF @actionName = 'AddItem' BEGIN
	INSERT INTO [AdvertType](name, parentID)
	VALUES(@name, @parentID)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @advertTypeID = SCOPE_IDENTITY()
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [AdvertType] WHERE AdvertTypeID = @AdvertTypeID
ELSE IF @actionName = 'UpdateItem'
	Begin
	declare @IsAdmin bit 
	set @IsAdmin = dbo.f_IsAdmin(@loggedUserID)

	-- менять "родителя" может только Админ
	If @IsAdmin = 0 And Exists (Select 1 From [AdvertType] Where AdvertTypeID = @AdvertTypeID And parentID <> @parentID)
	Begin
	Raiserror('AdvertTypeParentChahgeAttempt', 16, 1)
	Return
	End

	UPDATE	[AdvertType]
	SET			name = @name, parentID = @parentID 
	WHERE		AdvertTypeID = @AdvertTypeID
	End



