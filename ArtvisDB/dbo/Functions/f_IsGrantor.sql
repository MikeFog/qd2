

CREATE   FUNCTION [dbo].[f_IsGrantor]
(
@userID smallint
)
RETURNS bit
AS
BEGIN
IF EXISTS (
	SELECT * 
	FROM [User]
	WHERE userID = @userID AND isGrantor = 1
	)
	RETURN 1

RETURN 0	

END



