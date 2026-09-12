-- =============================================
-- Description:	Фактическое содержимое одного или нескольких получасовых слотов веера:
--              выпуски акции по указанным кампаниям. В отличие от «Добавленных выпусков»
--              (пересечение слотов по всем выбранным кампаниям) показывает и
--              частичные слоты — те, где выпуск есть не во всех кампаниях
--              (в сетке они красные).
--              @issueDates — CSV дат в ISO 8601 с "T" (ГГГГ-ММ-ДДTчч:мм:сс), не через
--              дефис-пробел: на сервере с русским @@LANGUAGE 'YYYY-MM-DD HH:MM:SS'
--              парсится как 'YYYY-DD-MM' (день/месяц переставлены) — с "T" формат
--              однозначен независимо от языка сессии.
--              Один вызов — сразу все выделенные окна (было по одному @issueDate на
--              вызов; при выделении по Ctrl+R/Delete десятков окон это давало заметную
--              паузу перед диалогом подтверждения — по круговому запросу на окно).
--              Одна строка на выпуск; requestedIssueDate — какому из @issueDates эта
--              строка соответствует (нужно вызывающему, чтобы разложить обратно по
--              окнам). Группировку «ролик + позиция» собирает C#
--              (TariffWithRangeGrid.GetSlotIssueGroups).
--              originalWindowID/windowDayOriginal — для RollerSubstitute (массовая
--              замена ролика, TariffWithRangeGrid.GetSlotIssueRows): её #days требует
--              именно windowId + dayOriginal той же строки TariffWindow, а не дату слота.
-- =============================================
CREATE PROCEDURE [dbo].[RangeSlotIssues]
(
	@actionID int,
	@issueDates varchar(max),
	-- Список кампаний акции (CSV campaignID). NULL/пусто — все линейные кампании акции.
	@campaignIDs varchar(max) = NULL
)
AS
BEGIN
	SET NOCOUNT ON;

	;WITH requested AS
	(
		SELECT CONVERT(datetime, value, 126) AS issueDate
		FROM STRING_SPLIT(@issueDates, ',')
	)
	SELECT
		req.issueDate AS requestedIssueDate,
		i.issueID,
		i.campaignID,
		i.rollerID,
		r.[name] AS rollerName,
		r.duration,
		dbo.fn_Int2Time(r.duration) AS durationString,
		i.positionId,
		tw.windowId AS originalWindowID,
		tw.dayOriginal AS windowDayOriginal
	FROM requested req
		-- Слот определяется тем же окном, что и в MasterIssueDelete: выпуск ищется
		-- по originalWindowID, а получас — по фактическому времени выхода окна.
		INNER JOIN dbo.TariffWindow tw
			ON tw.windowDateActual BETWEEN req.issueDate AND DATEADD(second, -1, DATEADD(minute, 30, req.issueDate))
		INNER JOIN dbo.Issue i ON i.originalWindowID = tw.windowId
		INNER JOIN dbo.Campaign c ON c.campaignID = i.campaignID
		INNER JOIN dbo.Roller r ON r.rollerID = i.rollerID
	WHERE c.actionID = @actionID
		AND c.campaignTypeID = 1
		AND (@campaignIDs IS NULL
			OR c.campaignID IN (SELECT CONVERT(int, value) FROM STRING_SPLIT(@campaignIDs, ',')))
	ORDER BY req.issueDate, i.rollerID, i.positionId, i.campaignID;
END
