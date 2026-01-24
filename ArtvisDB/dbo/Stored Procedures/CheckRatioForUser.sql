-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 
-- Description:	
-- =============================================
create procedure CheckRatioForUser 
(
	@userID smallint, 
	@ratio float,
	@startDate datetime,
	@finishDate datetime
)
as 
begin 
	set nocount on;

    select dbo.[fn_IsAcceptRatioForUser](@userID, @ratio, @startDate, @finishDate)
end
