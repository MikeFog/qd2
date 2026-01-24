CREATE  PROCEDURE [dbo].[PackModuleContentPassport]
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
-- 1. Massmedia
SELECT massmediaID as [id], nameWithGroup as [name] FROM vMassmedia  where isActive = 1 ORDER BY [name]

-- 2. Modules
EXEC ModuleList

-- 3. PriceLists
SELECT 
	mpl.[modulePriceListID], 
	mpl.[moduleID],
	'Модуль ' + CONVERT(varchar(10), mpl.startDate, 104) + ' - ' + CONVERT(varchar(10), mpl.finishDate, 104) + ' (прайс от ' + CONVERT(varchar(10), pl.startDate, 104) + ')' as NAME
FROM 
	[ModulePriceList] mpl
	INNER JOIN PriceList pl ON pl.priceListID = mpl.priceListID
ORDER BY
	mpl.startDate asc
