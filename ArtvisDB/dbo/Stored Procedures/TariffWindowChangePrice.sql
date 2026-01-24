
CREATE   procedure [dbo].[TariffWindowChangePrice] 
(
	@time datetime, 
	@price money,
	@newPrice money,
	@startdate datetime,
	@finishdate datetime,
	@pricelistid int
)
as 
begin 

declare @needaddday bit 
	
if exists(select * 
	from Pricelist pl 
	where pl.PricelistID = @pricelistID
		and @time < pl.broadcastStart)
	set @needaddday = 1
else 
	set @needaddday = 0

update tw 
set tw.price = @newPrice
from 
	TariffWindow tw 
	inner join Pricelist pl on pl.pricelistID = @pricelistid
where 
	tw.price = @price
	and tw.dayOriginal between @startdate and @finishdate
	and tw.windowDateOriginal = convert(datetime, left(convert(varchar, case @needaddday when 1 then dateadd(day, 1, tw.dayOriginal) else tw.dayOriginal end, 120),11) 
					+ right(convert(varchar, @time, 120), 8), 120)
		
    
end
