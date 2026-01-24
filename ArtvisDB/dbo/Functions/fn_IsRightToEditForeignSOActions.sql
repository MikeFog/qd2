CREATE   FUNCTION [dbo].[fn_IsRightToEditForeignSOActions]
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
	WHERE entityID = @ENTITY_ACTION AND name = 'RightsToEditForeignActions'

	RETURN dbo.IsActionEnabled(@userId, @entityActionID)
END
