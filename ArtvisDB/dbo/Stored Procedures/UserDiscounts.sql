-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 
-- Description:	
-- =============================================
CREATE procedure [dbo].[UserDiscounts] 
(
	@userID smallint,
	@discountID int = null
)
as 
begin 
	set nocount on;

    select * from UserDiscount ud where ud.userID = @userID and ud.discountID = coalesce(@discountID, ud.discountID) order by ud.startDate desc
end