-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 13.01.2009
-- Description:	Для фильтра статистических журналов
-- =============================================
CREATE procedure [dbo].[statManagerFilter]
(
	@loggedUserID smallint
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;

    exec UserListByRights @loggedUserID = @loggedUserID
end
