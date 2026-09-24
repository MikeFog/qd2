CREATE  PROCEDURE [dbo].[PackModuleContentPassport]
(
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
DECLARE @tModule NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Модуль ')
DECLARE @tPriceFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' (прайс от ')
-- 1. Massmedia
SELECT massmediaID as [id], nameWithGroup as [name] FROM vMassmedia  where isActive = 1 ORDER BY [name]

-- 2. Modules
EXEC ModuleList

-- 3. PriceLists
SELECT 
	mpl.[modulePriceListID], 
	mpl.[moduleID],
	@tModule + CONVERT(varchar(10), mpl.startDate, 104) + ' - ' + CONVERT(varchar(10), mpl.finishDate, 104) + @tPriceFrom + CONVERT(varchar(10), pl.startDate, 104) + ')' as NAME
FROM 
	[ModulePriceList] mpl
	INNER JOIN PriceList pl ON pl.priceListID = mpl.priceListID
ORDER BY
	mpl.startDate asc
