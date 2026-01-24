-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 29.09.2008
-- Description:	Show Disabled Windows for pricelise
-- =============================================
create procedure ShowDisabledWindows 
(
	@priceListID int, 
	@startDate datetime,
	@finishDate datetime
)
as 
begin 
	set nocount on;

    select tw.*
    from TariffWindow tw 
		inner join Tariff t on tw.tariffId = t.tariffID
    where tw.dayOriginal between @startDate and @finishDate
		and t.pricelistID = @priceListID and tw.isDisabled = 1
	order by tw.windowDateOriginal, tw.windowDateActual
end

