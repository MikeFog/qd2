-- =============================================
-- Author:		Gladkikh Denis (dgladkikh@fogsoft.ru)
-- Create date: 19.12.2008
-- Description:	Проверка на право видеть чужие рекламные акции
-- =============================================
CREATE FUNCTION fn_IsRightToViewForeignActions
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
	WHERE entityID = @ENTITY_ACTION AND name = 'RightsToViewForeignActions'

	RETURN dbo.IsActionEnabled(@userId, @entityActionID)

END
