-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 16.10.2008
-- Description:	Изменяет пароль главному администратору системы
-- =============================================
create procedure AdminChangePassword 
(
	@password binary(16)
)
as 
begin 
	set nocount on;

    update [User] set passwordHash = @password where userID = 0
end

