


CREATE    PROC [dbo].[ModulePricelistPassport]
(
@moduleID smallint,
@modulePriceListID smallint = NULL
)
AS
SET NOCOUNT ON
SELECT 
	DISTINCT(p.pricelistID) as ID,
	p.[startDate],
	'Прайс-лист от ' + CONVERT(varchar(10), p.startDate, 104) as name 
FROM 
	Module m
	INNER JOIN Pricelist p ON p.massmediaID = m.massmediaID
	LEFT JOIN ModulePriceList mp ON m.moduleID = mp.moduleID 
		And p.priceListID = mp.priceListID 
		And (mp.modulePriceListID <> @modulePriceListID OR @modulePriceListID IS NULL) 
WHERE 
	m.moduleID = @moduleID 
ORDER BY 
	p.startDate DESC
	







