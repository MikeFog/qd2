-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 29.12.2008
-- Description:	[fn_GetUserGroups]
-- =============================================
CREATE FUNCTION [dbo].[fn_GetUserGroups] 
(	
	@userID int
)
RETURNS TABLE 
AS
RETURN 
(
	select distinct groupID as id 
		from GroupMember 
		where dbo.f_IsAdmin(@userID) = 1 or userID = @userID
)
