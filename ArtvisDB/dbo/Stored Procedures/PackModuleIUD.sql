
CREATE  PROCEDURE [dbo].[PackModuleIUD]
(
@packModuleID smallint = NULL OUT,
@name nvarchar(64) = NULL,
@path NVARCHAR(255) = NULL,
@actionName varchar(32)
)
as
SET NOCOUNT ON
IF @actionName = 'AddItem' BEGIN
	INSERT INTO [PackModule](NAME, [path])
	VALUES(@name, @path)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @packModuleID = SCOPE_IDENTITY()
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [PackModule] WHERE packModuleID = @packModuleID
ELSE IF @actionName = 'UpdateItem'
	UPDATE	[PackModule]
	SET		[name] = @name, path = @path
	WHERE	packModuleID = @packModuleID




