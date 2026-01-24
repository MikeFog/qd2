
CREATE   FUNCTION [dbo].[fn_IsRightForMinus]
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
	WHERE entityID = @ENTITY_ACTION AND name = 'RightForMinus'

	RETURN dbo.IsActionEnabled(@userId, @entityActionID)
END

