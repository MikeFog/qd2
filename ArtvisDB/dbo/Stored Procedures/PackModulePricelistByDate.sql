CREATE PROC [dbo].[PackModulePricelistByDate]
(
@massmediaID SMALLINT = NULL,
@theDate DATETIME,
@packModuleID SMALLINT = NULL,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ')
SELECT 
	mpl.[priceListID],
	m.massmediaID,
	mpl.[startDate],
	@tPricelistFrom + CONVERT(varchar(10), mpl.[startDate], 104) as name,
	mpl.[finishDate],
	@packModuleID as packModuleID,
	mpl.[price], 
	mpl.rollerID
FROM 
	[PackModulePriceList] mpl
	INNER JOIN [PackModuleContent] pmc ON mpl.pricelistID = pmc.pricelistID
	INNER JOIN [Module] m ON pmc.moduleID = m.moduleID 
		AND m.massmediaID = ISNULL(@massmediaID, m.massmediaID)
WHERE
	mpl.[packModuleID] = ISNULL(@packModuleID, mpl.[packModuleID]) AND
	mpl.[startDate] <= @theDate AND mpl.[finishDate] >= @theDate -- Не находит прайс лист когда редактируешь кампанию
