
CREATE  PROC [dbo].[RolStyleIUD]
(
@rolStyleID smallint = NULL,
@name varchar(256) = NULL,
@IsActive bit = 0,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
IF @actionName = 'AddItem' BEGIN
	INSERT INTO  RolStyle(name, isActive) 
	VALUES(@name, @isActive)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @RolStyleID = SCOPE_IDENTITY()

	EXEC RollerStyles @RolStyleID = @RolStyleID
	END
ELSE IF @actionName = 'DeleteItem' BEGIN
	DELETE FROM RolStyle WHERE RolStyleID = @RolStyleID
	END
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE RolStyle
	SET 	name = @name, 
			isActive = @isActive
	WHERE RolStyleID = @RolStyleID

	EXEC RollerStyles @RolStyleID = @RolStyleID
	END




