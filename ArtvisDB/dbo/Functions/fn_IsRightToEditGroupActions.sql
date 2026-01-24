-- =============================================
-- Author:		Gladkikh Denis (dgladkikh@fogsoft.ru)
-- Create date: 19.12.2008
-- Description:	Проверка на право редактировать рекламные акции своей группы
-- =============================================
CREATE FUNCTION fn_IsRightToEditGroupActions
(
@userId smallint
)
RETURNS BIT 
AS
BEGIN
	DECLARE @entityActionID smallint,
		@ENTITY_ACTION smallint

	SET @ENTITY_ACTION = 77

	SELECT @entityActionID = entityActionID FROM iEntityAction 
	WHERE entityID = @ENTITY_ACTION AND name = 'RightsToEditGroupActions'

	RETURN dbo.IsActionEnabled(@userId, @entityActionID)

END
