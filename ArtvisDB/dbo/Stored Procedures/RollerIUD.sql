CREATE  PROCEDURE [dbo].[RollerIUD]
(
@rollerID int = null out,
@name nvarchar(64) = NULL,
@duration int = NULL,
@firmID smallint = NULL,
@rolTypeID smallint = NULL,
@rolStyleID smallint = NULL,
@path nvarchar(1024) = NULL,
@isEnabled tinyint = NULL,
@actionName varchar(32),
@createDate datetime = NULL,
@studioOrderID INT = NULL,
@isCommon BIT = NULL,
@rolActionTypeID TINYINT = NULL,
@loggedUserID INT = NULL,
@compositionName nvarchar(512) = null,
@compositionAuthor nvarchar(512) = null,
@advertTypeID smallint = NULL
)
WITH EXECUTE AS OWNER
AS
BEGIN
SET NOCOUNT ON
IF @actionName = 'AddItem' OR @actionName = 'UpdateItem'
BEGIN
	IF (@isEnabled = 1 AND @duration <= 0)
	BEGIN
		RAISERROR('Roller_NullDuration', 16, 1)
		RETURN
	END	

	IF @studioOrderID IS NULL
		SELECT @studioOrderID = so.[studioOrderID] FROM [StudioOrder] so INNER JOIN [Roller] r ON so.[rollerID] = r.[rollerID] WHERE r.[rollerID] = @rollerID
		
	if exists(SELECT * FROM [StudioOrder] WHERE NAME = @name AND (@studioOrderID IS NULL OR [studioOrderID] <> @studioOrderID) AND ([rollerID] IS NULL OR @rollerID IS NULL OR [rollerID] <> @rollerID))
		or exists(select * from Roller where [name] = @name and (@rollerID is null or rollerID <> @rollerID) and isMute = 0)
	BEGIN
		RAISERROR('RollerName_Unique', 16, 1)
		RETURN
	END
END

IF @actionName = 'AddItem' BEGIN
	IF @isCommon = 1 AND dbo.f_IsAdmin(@loggedUserID) = 0
	BEGIN
		RAISERROR('RollerIsCommon', 16, 1)
		RETURN
	END

	INSERT INTO [Roller]([name], duration, firmID, rolTypeID, rolStyleID, path, isEnabled, isCommon, rolActionTypeID, compositionName, compositionAuthor, advertTypeID)
	VALUES(@name, @duration, @firmID, @rolTypeID, @rolStyleID, @path, @isEnabled, @isCommon, @rolActionTypeID, @compositionName, @compositionAuthor, @advertTypeID)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @rollerID = SCOPE_IDENTITY()
	
	IF @studioOrderID IS NULL
		EXEC Rollers @rollerID = @rollerID
END
ELSE IF @actionName = 'DeleteItem'
	Begin
	DELETE FROM [Roller] WHERE parentID = @rollerID
	DELETE FROM [Roller] WHERE rollerID = @rollerID
	End
ELSE IF @actionName = 'UpdateItem' 
BEGIN
	If @advertTypeID Is Null And dbo.IsRollerInUseByActivatedAction(@rollerId) = 1
		Begin
			raiserror('IX_RollerMustHaveAdvertType', 16, 1)
			return 
		End

	DECLARE @oldIsCommon bit, @canEditVideoName bit 
	
	set @canEditVideoName = dbo.IsActionEnabled(@loggedUserID, 681) -- Roller - ChangeVideoRollerName
	
	SELECT @oldIsCommon = isCommon FROM [Roller] WHERE [rollerID] = @rollerID

	IF @oldIsCommon <> @isCommon AND dbo.f_IsAdmin(@loggedUserID) = 0
	BEGIN
		RAISERROR('RollerIsCommon', 16, 1)
		RETURN
	end
	
	if exists(select * 
				from Issue i 
					inner join Roller r on i.rollerID = r.rollerID
				where i.rollerID = @rollerID
					and ((r.name <> @name and @canEditVideoName <> 1)
					or r.duration <> @duration
					or r.rolTypeID <> @rolTypeID
					or r.rolActionTypeID <> @rolActionTypeID
					or r.rolTypeID <> @rolTypeID))
	begin 
		RAISERROR('RollerCannotChange', 16, 1)
		RETURN
	end 
	
	if exists(select * 
				from ModulePriceList mpl
					inner join Roller r on mpl.rollerID = r.rollerID
				where r.rollerID = @rollerID
					and (r.name <> @name
					or @isCommon = 0
					or @rolActionTypeID not in (2,3)
					or @isEnabled = 0))
		or exists(select * 
				from PackModulePriceList pmpl
					inner join Roller r on pmpl.rollerID = r.rollerID
				where r.rollerID = @rollerID
					and (r.name <> @name
					or @isCommon = 0
					or @rolActionTypeID not in (2,3)
					or @isEnabled = 0
					or r.rolTypeID <> @rolTypeID))
	begin 
		RAISERROR('RollerCannotChangeWithPriceListPackAndModule', 16, 1)
		RETURN
	end 
	
	UPDATE	[Roller]
	SET			
		[name] = @name, 
		duration = @duration, 
		firmID = @firmID, 
		rolTypeID = @rolTypeID, 
		rolStyleID = @rolStyleID, 
		path = @path, 
		isEnabled = @isEnabled,
		isCommon = @isCommon,
		rolActionTypeID = @rolActionTypeID,
		compositionName = @compositionName,
		compositionAuthor = @compositionAuthor,
		advertTypeID = @advertTypeID
	WHERE		
		rollerID = @RollerID

	EXEC Rollers @rollerID = @rollerID
END

END
