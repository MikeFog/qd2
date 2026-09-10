-- =============================================
-- Description:	Фактическое содержимое получасового слота веера: выпуски акции
--              по указанным кампаниям. В отличие от «Добавленных выпусков»
--              (пересечение слотов по всем выбранным кампаниям) показывает и
--              частичные слоты — те, где выпуск есть не во всех кампаниях
--              (в сетке они красные).
--              Одна строка на выпуск; группировку «ролик + позиция» и список
--              кампаний собирает C# (TariffWithRangeGrid.GetSlotIssueGroups).
-- =============================================
CREATE PROCEDURE [dbo].[RangeSlotIssues]
(
	@actionID int,
	@issueDate datetime,
	-- Список кампаний акции (CSV campaignID). NULL/пусто — все линейные кампании акции.
	@campaignIDs varchar(max) = NULL
)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		i.issueID,
		i.campaignID,
		i.rollerID,
		r.[name] AS rollerName,
		r.duration,
		dbo.fn_Int2Time(r.duration) AS durationString,
		i.positionId
	FROM dbo.Issue i
		INNER JOIN dbo.Campaign c ON c.campaignID = i.campaignID
		INNER JOIN dbo.Roller r ON r.rollerID = i.rollerID
		-- Слот определяется тем же окном, что и в MasterIssueDelete: выпуск ищется
		-- по originalWindowID, а получас — по фактическому времени выхода окна.
		INNER JOIN dbo.TariffWindow tw ON tw.windowId = i.originalWindowID
	WHERE c.actionID = @actionID
		AND c.campaignTypeID = 1
		AND (@campaignIDs IS NULL
			OR c.campaignID IN (SELECT CONVERT(int, value) FROM STRING_SPLIT(@campaignIDs, ',')))
		AND tw.windowDateActual BETWEEN @issueDate AND DATEADD(second, -1, DATEADD(minute, 30, @issueDate))
	ORDER BY i.rollerID, i.positionId, i.campaignID;
END
