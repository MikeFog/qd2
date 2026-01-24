-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 23.12.2008
-- Description:	Удаляет неактивированные акции в прошлом
-- =============================================
create procedure DeleteActionsInPast 
(
	@loggedUserID smallint = 0
)
as 
begin 
	set nocount on;

	declare @isAdmin bit
	set @isAdmin = dbo.f_IsAdmin(@loggedUserID)    
	
	delete from a
	from [Action] a
	where a.isConfirmed = 0
		and a.finishDate < getdate()
		and (a.userID = @loggedUserID
				or @isAdmin = 1)
end
