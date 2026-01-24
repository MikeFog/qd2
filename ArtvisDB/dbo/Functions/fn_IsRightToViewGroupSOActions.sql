-- =============================================
-- Author:		Gladkikh Denis (dgladkikh@fogsoft.ru)
-- Create date: 19.12.2008
-- Description:	Проверка на право видеть рекламные акции своей группы
-- =============================================
CREATE FUNCTION fn_IsRightToViewGroupSOActions
(
@userId smallint
)
RETURNS BIT 
AS
BEGIN
	DECLARE @entityActionID smallint,
		@ENTITY_ACTION smallint

	SET @ENTITY_ACTION = 116

	SELECT @entityActionID = entityActionID FROM iEntityAction 
	WHERE entityID = @ENTITY_ACTION AND name = 'RightsToViewGroupActions'

	RETURN dbo.IsActionEnabled(@userId, @entityActionID)

END
