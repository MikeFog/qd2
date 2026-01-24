
CREATE procedure [dbo].[TariffWindowChangeDuration] 
(
@time datetime, 
@newDuration int,
@newDuration_total int,
@startdate datetime,
@finishdate datetime,
@pricelistid int,
@monday bit = 0,
@tuesday bit = 0,
@wednesday bit = 0,
@thursday bit = 0,
@friday bit = 0,
@saturday bit = 0,
@sunday bit = 0
)
as 
begin 

set nocount on;
SET DATEFIRST 1; -- Устанавливает понедельник как первый день недели

declare @needaddday bit 
	
if exists(select * 
	from Pricelist pl 
	where pl.PricelistID = @pricelistID
		and @time < pl.broadcastStart)
	set @needaddday = 1
else 
	set @needaddday = 0

update 
	tw 
set 
	tw.duration = @newDuration,
	tw.duration_total = @newDuration_total
from 
	TariffWindow tw 
	inner join Pricelist pl on tw.massmediaID = pl.massmediaID and pl.pricelistID = @pricelistid
where 
	tw.dayOriginal >= @startdate 
	and tw.dayOriginal <= @finishdate
	and tw.windowDateOriginal = convert(datetime, left(convert(varchar, case @needaddday when 1 then dateadd(day, 1, tw.dayOriginal) else tw.dayOriginal end, 120),11) 
				+ right(convert(varchar, @time, 120), 8), 120)
	and (
	(@monday = 1 and DATEPART(weekday, tw.dayOriginal) = 1)
	or (@tuesday = 1 and DATEPART(weekday, tw.dayOriginal) = 2)
	or (@wednesday = 1 and DATEPART(weekday, tw.dayOriginal) = 3)
	or (@thursday = 1 and DATEPART(weekday, tw.dayOriginal) = 4)
	or (@friday = 1 and DATEPART(weekday, tw.dayOriginal) = 5)
	or (@saturday = 1 and DATEPART(weekday, tw.dayOriginal) = 6)
	or (@sunday = 1 and DATEPART(weekday, tw.dayOriginal) = 7)
	)
end
