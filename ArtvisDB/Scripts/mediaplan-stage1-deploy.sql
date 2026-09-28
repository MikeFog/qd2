/*
    ДЕПЛОЙ: медиаплан, этап 1 плана docs/tasks/web-mediaplan.md (чистка десктопа).

    1) dbo.GetUniqueMMsForPackModuleCampaign — строка на станцию (GROUP BY,
       порядок первого выпуска) вместо строки на каждый выпуск. Единственный
       вызыватель — Client\Classes\CampaignPackModule.cs (GetUniqueMassmedias).
       Колонки date/rollerID сохранены (MIN), поэтому совместимо и со старым
       клиентом, и с новым — порядок накатки клиента и скрипта не важен.
       Сверка на ArtvisDev: 299 пакетных кампаний, набор (campaignID,
       massmediaID, name) совпадает, 0 расхождений; строк 37169 -> 1483.

    2) dbo.MediaPlanRetrieve (v1) — удаляется. Из кода не вызывается (клиент зовёт
       только MediaPlanRetrieve_v2), зависимостей в sys.sql_expression_dependencies
       и строк в iStoredProcedure нет (проверено на Artvis и ArtvisDev).
*/
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

ALTER PROCEDURE [dbo].[GetUniqueMMsForPackModuleCampaign]
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
GO

IF OBJECT_ID(N'dbo.MediaPlanRetrieve', N'P') IS NOT NULL
    DROP PROCEDURE dbo.MediaPlanRetrieve;
GO
