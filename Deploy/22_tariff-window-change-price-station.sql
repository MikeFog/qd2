-- «Изменить цену...» у строки времени в генерации окон (десктоп TariffWindowGrid, веб TariffWindowWeek):
-- TariffWindowChangePrice меняла цену окон ВСЕХ станций с тем же временем по расписанию и той же ценой
-- за период, а не только станции прайс-листа. Условие соединения `pl.pricelistID = @pricelistid` не
-- касалось TariffWindow — по сути CROSS JOIN с одной строкой. Теперь окно связано со станцией
-- прайс-листа (`tw.massmediaID = pl.massmediaID`), как в TariffWindowChangeDuration и TariffWindowMoveTime.
--
-- Пример (ArtvisDev, 07.10.2026): прайс-лист 21531 «Ретро FM (Кострома)», строка 11:42 / 10 ₽, неделя
-- 12.10–18.10.2026 — задевала 7 окон Костромы и 7 окон «Ретро FM (Переславль)». У 50 424 из 151 450
-- будущих окон есть такой «двойник» на другой станции. Ошибка — с начала истории процедуры.
--
-- Параметры и выдача не меняются. Идемпотентен. Клиент не нужен (десктоп и веб зовут ту же процедуру).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 22_tariff-window-change-price-station.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [dbo].[TariffWindowChangePrice] 
(
	@time datetime, 
	@price decimal(18,2),
	@newPrice decimal(18,2),
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
	-- Только окна станции прайс-листа. Без tw.massmediaID условие соединения не трогало tw, и цена
	-- менялась у окон всех станций с тем же временем и ценой (как у TariffWindowChangeDuration).
	inner join Pricelist pl on tw.massmediaID = pl.massmediaID and pl.pricelistID = @pricelistid
where
	tw.price = @price
	and tw.dayOriginal between @startdate and @finishdate
	and tw.windowDateOriginal = convert(datetime, left(convert(varchar, case @needaddday when 1 then dateadd(day, 1, tw.dayOriginal) else tw.dayOriginal end, 120),11) 
					+ right(convert(varchar, @time, 120), 8), 120)
		
    
end

GO

-- Проверка: в новой версии есть условие станции.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowChangePrice')) LIKE N'%tw.massmediaID = pl.massmediaID%'
    PRINT N'ГОТОВО: TariffWindowChangePrice ограничена станцией прайс-листа.';
ELSE
    RAISERROR(N'TariffWindowChangePrice: новая версия не применена.', 16, 1);
GO
