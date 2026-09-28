CREATE PROCEDURE [dbo].[GetUniqueMMsForPackModuleCampaign]
(
	@campaignID int,
	@isFact bit = 1
)
AS
begin
SET NOCOUNT on
	-- Станции пакетной кампании, по строке на станцию, в порядке первого выпуска.
	-- Раньше отдавала строку на КАЖДЫЙ выпуск, а клиент всё равно сводил их к
	-- списку станций. date/rollerID клиент больше не читает; оставлены (MIN) для
	-- совместимости со старыми клиентами (до этапа 1 медиаплана), которые их парсят.
	SELECT
		mm.[massmediaID], mm.[name],
		MIN(CASE WHEN @isFact = 1 THEN tw.windowDateActual ELSE tw.windowDateOriginal END) AS [date],
		MIN(i.[rollerID]) AS [rollerID]
	FROM
		Issue i
		inner join TariffWindow tw on tw.windowId = CASE WHEN @isFact = 1 THEN i.actualWindowID ELSE i.originalWindowID END
		INNER JOIN [vMassmedia] mm ON tw.[massmediaID] = mm.[massmediaID]
	WHERE
		i.campaignID = @campaignID
	GROUP BY mm.[massmediaID], mm.[name]
	ORDER BY MIN(i.issueID)
END
