


CREATE    PROCEDURE [dbo].[ModuleIUD]
(
@moduleID smallint OUT,
@massmediaID smallint = NULL,
@name nvarchar(32) = NULL,
@path NVARCHAR(255) = NULL, 
@actionName varchar(32)
)
as
SET NOCOUNT on
IF @actionName = 'AddItem' BEGIN
	INSERT INTO [Module](massmediaID, NAME, [path])
	VALUES(@massmediaID, @name, @path)
	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @moduleID = SCOPE_IDENTITY()
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [Module] WHERE moduleID = @moduleID
ELSE IF @actionName = 'UpdateItem'
	UPDATE	
		[Module]
	SET	
		massmediaID = @massmediaID, 
		name = @name,
		path = @path
	WHERE		
		moduleID = @moduleID






