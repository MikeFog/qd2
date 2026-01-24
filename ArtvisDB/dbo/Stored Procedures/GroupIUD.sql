

CREATE  PROCEDURE [dbo].[GroupIUD]
(
@groupID smallint OUT,
@name nvarchar(32) = NULL,
@actionName varchar(32)
)
as
SET NOCOUNT on
IF @actionName = 'AddItem' BEGIN
	INSERT INTO [Group](name)
	VALUES(@name)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @groupID = SCOPE_IDENTITY()
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [Group] WHERE groupID = @GroupID
ELSE IF @actionName = 'UpdateItem'
begin 
	UPDATE	
		[Group]
	SET
		[name] = @name
	WHERE		
		groupID = @GroupID
		
end 


