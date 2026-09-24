

CREATE        PROC [dbo].[ModulePriceLists]
(
@moduleID smallint = NULL,
@modulePriceListID smallint = NULL,
@hideModulePLInThePast bit = 0,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)

AS
SET NOCOUNT ON
DECLARE @tModule NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Модуль ')
DECLARE @tPriceFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' (прайс от ')
SELECT 
	mpl.*, 
	pl.broadcastStart,
	@tModule + CONVERT(varchar(10), mpl.startDate, 104) + ' - ' + CONVERT(varchar(10), mpl.finishDate, 104) + @tPriceFrom + CONVERT(varchar(10), pl.startDate, 104) + ')' as NAME,
	mm.[roltypeID]
FROM 
	[ModulePriceList] mpl
	INNER JOIN PriceList pl ON pl.priceListID = mpl.priceListID
	INNER JOIN [MassMedia] mm ON pl.[massmediaID] = mm.[massmediaID]
WHERE
	mpl.moduleID = Coalesce(@moduleID, mpl.moduleID) And
	mpl.modulePriceListID = Coalesce(@modulePriceListID, mpl.modulePriceListID)
	AND (@hideModulePLInThePast = 0 OR mpl.finishDate >= CAST(GETDATE() AS DATE))
ORDER BY
	mpl.startDate ASC
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[ModulePriceLists] TO PUBLIC
    AS [dbo];

