CREATE  FUNCTION [dbo].[IsActionEnabled]
(
@userID smallint,
@entityActionID smallint
)
RETURNS bit
AS
BEGIN
IF dbo.f_IsAdmin(@userID) = 1 RETURN 1

IF EXISTS (
	SELECT * 
	FROM iEntityAction ea
		inner join iEntity e on ea.entityID = e.entityID
	WHERE ea.entityActionID = @entityActionID AND (ea.isGrantingAllowed = 0 or e.isGrantingAllowed = 0)
	)
	RETURN 1

declare @isGrand bit 

select @isGrand = ur.isGrant
from UserAdditionRight ur 
where ur.userID = @userID 
		and ur.entityActionID = @entityActionID 

IF @isGrand = 1 or (EXISTS(
	SELECT 
		*
	FROM 
		GroupRight gr
		INNER JOIN GroupMember mem ON mem.groupID = gr.groupID
			And gr.entityActionID = @entityActionID
	WHERE
		mem.userID = @userID
	) 
	and @isGrand is null)
	RETURN 1
	
RETURN 0
END
