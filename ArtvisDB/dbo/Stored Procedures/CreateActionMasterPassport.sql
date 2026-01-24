-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 10.04.2009
-- Description:	
-- =============================================
CREATE procedure [dbo].[CreateActionMasterPassport]
(
	@loggedUserID smallint
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;

	exec sl_LookupRolType 

	exec sl_LookupMassmediaGroupd 

	exec massmediaList @loggedUserID = @loggedUserID, @ShowActive = 1, @checkCanAdd = 1
		
	select pt.paymentTypeID as id, pt.[name] from dbo.PaymentType pt where pt.isActive = 1
end

