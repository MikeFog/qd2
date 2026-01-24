-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 02.02.2009
-- Description:	Шаблонный перенос времени
-- =============================================
create procedure TariffWindowMoveTime 
(
	@time datetime, 
	@newtime datetime,
	@startdate datetime,
	@finishdate datetime,
	@pricelistid int
)
as 
begin 
	set nocount on;

	declare @needaddday bit 
	
	if exists(select * 
		from Pricelist pl 
		where pl.PricelistID = @pricelistID
			and @time < pl.broadcastStart)
		set @needaddday = 1
	else 
		set @needaddday = 0
	

	update tw 
		set tw.windowDateActual = 
				convert(datetime, left(convert(varchar, case @needaddday when 1 then dateadd(day, 1, tw.dayOriginal) else tw.dayOriginal end, 120),11) 
						+ right(convert(varchar, @newtime, 120), 8), 120)
		from TariffWindow tw 
			inner join Pricelist pl on tw.massmediaID = pl.massmediaID
				and pl.pricelistID = @pricelistid
		where tw.dayOriginal between @startdate and @finishdate
			and tw.windowDateOriginal = convert(datetime, left(convert(varchar, case @needaddday when 1 then dateadd(day, 1, tw.dayOriginal) else tw.dayOriginal end, 120),11) 
						+ right(convert(varchar, @time, 120), 8), 120)
    
end
