-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 29.09.2008
-- Description:	GetMassmediasForUser
-- =============================================
CREATE FUNCTION [dbo].[fn_GetMassmediasForUserMassmedia] 
(	
	@userID int,
	@massmediaID smallint=NULL
)
RETURNS TABLE 
AS
RETURN 
(
	-- Add the SELECT statement with parameter references here
	select mm.massmediaID, case when dbo.f_IsAdmin(@userID) = 1 then 1 else coalesce(um.canAdd, 0) end as myMassmedia, case when dbo.f_IsAdmin(@userID) = 1 then 1 else coalesce(um.canWork, 0) end as foreignMassmedia
	from MassMedia mm 
		left join UserMassmedia um on um.userID = @userID and mm.massmediaID = um.massmediaID
	where (dbo.f_IsAdmin(@userID) = 1 or (um.massmediaID is not null and (um.canAdd = 1 or um.canWork = 1) )) 
			AND (@massmediaID IS NULL OR mm.massmediaID=@massmediaID)
)
