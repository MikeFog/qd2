-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 31.10.2008
-- Description:	Repair AdvertAgUser
-- =============================================
CREATE procedure [dbo].[RepairUser]
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;

    EXEC sp_change_users_login @Action='Update_One',
		@UserNamePattern='AdvertAgUser', @LoginName='AdvertAgUser'
end
