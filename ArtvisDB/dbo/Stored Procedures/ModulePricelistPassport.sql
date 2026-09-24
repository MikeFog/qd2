


CREATE    PROC [dbo].[ModulePricelistPassport]
(
@moduleID smallint,
@modulePriceListID smallint = NULL,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ')
SELECT 
	DISTINCT(p.pricelistID) as ID,
	p.[startDate],
	@tPricelistFrom + CONVERT(varchar(10), p.startDate, 104) as name 
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
	







