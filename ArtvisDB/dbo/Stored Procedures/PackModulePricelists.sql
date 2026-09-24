






CREATE      PROC [dbo].[PackModulePricelists]
(
@packModuleID smallint = null,
@pricelistID smallint = null,
@hidePLInThePast bit = 0,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
as

SET NOCOUNT ON
DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ')
DECLARE @tTo NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' до ')
SELECT DISTINCT
	pl.[pricelistID], 
	pl.[packModuleID],
	pl.[startDate],
	@tPricelistFrom + CONVERT(varchar(10), pl.[startDate], 104) + @tTo + CONVERT(varchar(10), pl.finishDate, 104) as name,
	pl.[finishDate],
	pl.[price],
	pl.[extraChargeFirstRoller],
	pl.[extraChargeSecondRoller],
	pl.[extraChargeLastRoller],
	pl.rollerID,
	mm.[roltypeID]
FROM 
	[PackModulePriceList] pl
	left JOIN [PackModuleContent] pmc ON pl.[priceListID] = pmc.[pricelistID]
	left JOIN [Module] m ON pmc.[moduleID] = m.[moduleID]
	left JOIN [MassMedia] mm ON m.[massmediaID] = mm.[massmediaID]
WHERE
	pl.packModuleID = Coalesce(@packModuleID, pl.packModuleID) And
	pl.[pricelistID] = Coalesce(@pricelistID, pl.[pricelistID])
	And (@hidePLInThePast = 0 or pl.finishDate > GETDATE())
ORDER BY 
	pl.[startDate] DESC