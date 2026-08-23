CREATE PROCEDURE [dbo].[ComboModuleIUD]
(
@comboModuleID smallint = NULL OUT,
@name nvarchar(64) = NULL,
@actionName varchar(32)
)
AS
SET NOCOUNT ON

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [ComboModule]([name])
	VALUES(@name)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return
	end

	SET @comboModuleID = SCOPE_IDENTITY()
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [ComboModule] WHERE comboModuleID = @comboModuleID
ELSE IF @actionName = 'UpdateItem'
	UPDATE	[ComboModule]
	SET		[name] = @name
	WHERE	comboModuleID = @comboModuleID
