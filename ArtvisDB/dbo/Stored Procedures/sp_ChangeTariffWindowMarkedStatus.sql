

CREATE   PROCEDURE [dbo].[sp_ChangeTariffWindowMarkedStatus]
(
@priceListID int,
@startDate datetime,
@endDate datetime,
@time smalldatetime,
@monday tinyint,
@tuesday tinyint,
@wednesday tinyint,
@thursday tinyint,
@friday tinyint,
@saturday tinyint,
@sunday tinyint,
@flag bit 
)
AS
BEGIN
SET NOCOUNT ON;
	
SET DATEFIRST 1

update 
	TariffWindow 
set 
	isMarked = @flag
from
	Tariff t
	inner join Pricelist pl on t.pricelistID = pl.pricelistID and pl.pricelistID = @priceListID 
where 
	t.tariffID = TariffWindow.tariffId
	and TariffWindow.dayOriginal between @startDate and @endDate
	and (DatePart(dw, TariffWindow.dayOriginal) = 1 and @monday = 1 
		or DatePart(dw, TariffWindow.dayOriginal) = 2 and @tuesday = 1 
		or DatePart(dw, TariffWindow.dayOriginal) = 3 and @wednesday = 1 
		or DatePart(dw, TariffWindow.dayOriginal) = 4 and @thursday = 1 
		or DatePart(dw, TariffWindow.dayOriginal) = 5 and @friday = 1 
		or DatePart(dw, TariffWindow.dayOriginal) = 6 and @saturday = 1 
		or DatePart(dw, TariffWindow.dayOriginal) = 7 and @sunday = 1 )
	and datepart(hour, t.[time]) = datepart(hour, @time)
	and datepart(minute, t.[time]) = datepart(minute, @time)
END


