
/*
Modified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008
*/
CREATE   PROC [dbo].[hlp_GetStartFinishFromIssueDateAndBroadcastStart]
(
@massmediaID smallint,
@issueDate datetime,
@startDate datetime out,
@finishDate datetime out
)
AS
SET NOCOUNT ON
DECLARE @broadcastStart smalldatetime

declare @priceListID int 
select @priceListID = dbo.fn_GetPricelistIDByDate(@massmediaID, @issueDate, 0)

SELECT @broadcastStart = broadcastStart
FROM Pricelist 
WHERE priceListID = @priceListID

SET @startDate = dbo.ToShortDate(@issueDate)
SET @startDate = DATEADD(hh, DATEPART(hh, @broadcastStart), @startDate)
SET @startDate = DATEADD(mi, DATEPART(mi, @broadcastStart), @startDate)

SET @finishDate = dbo.ToShortDate(@issueDate) + 1
SET @finishDate = DATEADD(hh, DATEPART(hh, @broadcastStart), @finishDate)
SET @finishDate = DATEADD(mi, DATEPART(mi, @broadcastStart), @finishDate)
SET @finishDate = DATEADD(ss, -1, @finishDate)




