



CREATE     PROC dbo.ModuleTariffSelection 
(
@modulePriceListID smallint
)
AS
SET NOCOUNT ON
SELECT 
	t.tariffID,
	CONVERT(varchar(5), t.[time], 108) as timeString, 
	t.[comment],
	dbo.fn_Int2Time(t.[duration]) as durationString, 
	t.isForModuleOnly,
	CAST(
	CASE 
		WHEN mt.tariffID Is NULL THEN 0
		ELSE 1
	END AS BIT) as isObjectSelected,
	t.[monday],
	t.[tuesday],
	t.[wednesday],
	t.[thursday],
	t.[friday],
	t.[saturday],
	t.[sunday]
FROM 
	ModulePriceList mpl
	INNER JOIN Tariff t ON t.pricelistID = mpl.pricelistID
	LEFT JOIN [ModuleTariff] mt ON mt.tariffID = t.tariffID AND
		mt.modulePriceListID = @modulePriceListID
WHERE
	mpl.modulePriceListID = @modulePriceListID
ORDER BY 
	t.[time]






