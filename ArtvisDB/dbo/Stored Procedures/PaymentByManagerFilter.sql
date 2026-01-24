-- =============================================
-- Author:		D.Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 11.01.2009
-- Description:	Фильм для журнала оплат по менеджерам
-- =============================================
CREATE PROCEDURE [dbo].[PaymentByManagerFilter]
(
	@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
AS
BEGIN
	set nocount on;

   exec [UserListByRights]
	@loggedUserID = @loggedUserID --  smallint
END
