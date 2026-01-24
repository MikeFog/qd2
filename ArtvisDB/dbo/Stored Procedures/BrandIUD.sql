CREATE PROCEDURE [dbo].[BrandIUD]
(
@brandID smallint Out,
@name nvarchar(64) = NULL,
@actionName varchar(32)
)
AS
SET NOCOUNT ON

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [Brand](name)
	VALUES(@name)
	
	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @brandID = SCOPE_IDENTITY()
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [Brand] WHERE BrandID = @BrandID
ELSE IF @actionName = 'UpdateItem'
	UPDATE	[Brand]
	SET		name = @name
	WHERE	BrandID = @BrandID


