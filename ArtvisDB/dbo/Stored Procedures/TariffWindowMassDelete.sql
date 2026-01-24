CREATE  PROC [dbo].[TariffWindowMassDelete]
(
@massmediaID smallint,
@pricelistId smallint,
@startDate datetime,
@finishDate datetime,
@time varchar(5) = null
)
AS
SET NOCOUNT ON

DECLARE @colonPos INT = CHARINDEX(':', @time)
DECLARE @hour int = CAST(LEFT(@time, @colonPos - 1) AS INT)
DECLARE @minute int = CAST(SUBSTRING(@time, @colonPos + 1, LEN(@time)) AS INT)

DELETE tw
from
	[TariffWindow] tw
	inner join [Tariff] t on t.[tariffID] = tw.[tariffId]
	left join Issue i on i.[actualWindowID] = tw.[windowId]
	left join Issue i2 on  i2.[originalWindowID] = tw.[windowId]
WHERE 
	tw.massmediaID = @massmediaID
	and t.[pricelistID] = @pricelistId
	and tw.dayOriginal between @startDate and @finishDate 
	and i.issueID is null and i2.issueID is null
	and DATEPART(HOUR, tw.windowDateOriginal) = IsNull(@hour, DATEPART(HOUR, tw.windowDateOriginal))
	and DATEPART(MINUTE, tw.windowDateOriginal) = IsNull(@minute, DATEPART(MINUTE, tw.windowDateOriginal))
