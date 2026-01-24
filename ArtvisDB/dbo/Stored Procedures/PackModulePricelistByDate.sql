CREATE PROC [dbo].[PackModulePricelistByDate]
(
@massmediaID SMALLINT = NULL,
@theDate DATETIME,
@packModuleID SMALLINT = NULL
)
AS
SET NOCOUNT ON
SELECT 
	mpl.[priceListID],
	m.massmediaID,
	mpl.[startDate],
	'Прайс-лист от ' + CONVERT(varchar(10), mpl.[startDate], 104) as name,
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
