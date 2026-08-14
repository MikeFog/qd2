CREATE PROCEDURE [dbo].[ComboModuleContentIUD]
(
@comboModuleContentID smallint = NULL,
@comboModuleID smallint = NULL,
@moduleID smallint = NULL,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

-- Один и тот же модуль не может входить в комбо-модуль дважды
IF (@actionName IN ('AddItem', 'UpdateItem'))
BEGIN
	IF EXISTS(
		SELECT 1
		FROM [ComboModuleContent] cmc
		WHERE
			cmc.comboModuleID = @comboModuleID AND
			cmc.moduleID = @moduleID AND
			(@actionName <> 'UpdateItem' OR cmc.comboModuleContentID <> IsNull(@comboModuleContentID, 0))
		)
	BEGIN
		raiserror('ComboModuleContentDuplicate', 16, 1)
		return
	END
END

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [ComboModuleContent](comboModuleID, moduleID)
	VALUES(@comboModuleID, @moduleID)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return
	end

	SET @comboModuleContentID = SCOPE_IDENTITY()

	EXEC ComboModuleContentRetrieve @comboModuleContentID = @comboModuleContentID
END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [ComboModuleContent]
	WHERE comboModuleContentID = @comboModuleContentID
ELSE IF @actionName = 'UpdateItem'
begin
	UPDATE	[ComboModuleContent]
	SET		[moduleID] = @moduleID
	WHERE	comboModuleContentID = @comboModuleContentID

	EXEC ComboModuleContentRetrieve @comboModuleContentID = @comboModuleContentID
END
