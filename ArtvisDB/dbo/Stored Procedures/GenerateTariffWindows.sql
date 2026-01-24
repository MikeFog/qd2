


CREATE    PROC [dbo].[GenerateTariffWindows]
(
@pricelistId smallint,
@startDate datetime,
@finishDate datetime
)
WITH EXECUTE AS OWNER
AS
Set Nocount On

Declare 
	@currentDate datetime,
	@massmediaId smallint,
	@broadcastStart datetime

Select @broadcastStart = broadcastStart From Pricelist Where pricelistID = @pricelistId
Set @currentDate = @startDate
Select @massmediaId = massmediaId From Pricelist Where pricelistId = @pricelistId

While @currentDate <= @finishDate Begin	
	Exec sl_GenerateTariffWindowsDay @massmediaId, @pricelistId, @currentDate, @broadcastStart
	Set @currentDate = @currentDate + 1
End

	



