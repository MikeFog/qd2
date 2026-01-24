-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 18.09.2008
-- Description:	passport for edit special action and filter it 
-- =============================================
CREATE procedure [dbo].[SpecialActionPassport] 
(
	@loggedUserID smallint
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;

	select paymentTypeID as id, [name] from PaymentType order by [name]
	
	exec [UserListByRights]	@loggedUserID = @loggedUserID
end
