-- Веер: RangeSlotIssues — неделя веера у больших акций считалась 20–45 с и всё это время держала
-- блокировку на всю таблицу Issue (чужие записи выпусков ждали).
--
-- Процедура отдаёт выпуски акции в выбранных получасовых слотах; при включённых номерах роликов клиент
-- зовёт её на всю неделю (~250 слотов) после каждой перерисовки веера. Список дат приходил строкой и
-- разбирался STRING_SPLIT прямо в соединении по диапазону времени — сервер разбирал строку заново на
-- каждый выпуск акции (выпуски × слоты: 11,5 тыс. × 252 ≈ 2,9 млн разборов). Пока запрос шёл, S-блокировки
-- выпусков укрупнялись до блокировки всей таблицы Issue. Прод, 29.09.2026: у agv (копии больших акций
-- на 2027 год) 20–29 с при тайм-ауте 30 с, у kegorova в те же секунды IssueIUD 19 с и RollerSubstitute 10 с.
-- Теперь даты разбираются один раз во временную таблицу с индексом. На копии прода: 40 с → 0,1–0,6 с,
-- результат совпадает со старой версией строка в строку (5 наборов параметров, в том числе из лога).
--
-- Идемпотентен (CREATE OR ALTER), данные не трогает, клиент не нужен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 18_veer-range-slot-issues.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

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
CREATE OR ALTER PROCEDURE [dbo].[RangeSlotIssues]
(
	@actionID int,
	@issueDates varchar(max),
	-- Список кампаний акции (CSV campaignID). NULL/пусто — все линейные кампании акции.
	@campaignIDs varchar(max) = NULL
)
AS
BEGIN
	SET NOCOUNT ON;

	-- Даты слотов разбираются один раз во временную таблицу. Раньше STRING_SPLIT стоял
	-- прямо в соединении по диапазону времени, и сервер разбирал всю строку дат заново на
	-- каждый выпуск акции (выпуски × слоты): неделя веера у акции на 11 тыс. выпусков
	-- считалась 20–45 с и всё это время держала S-блокировку на всю таблицу Issue —
	-- чужие IssueIUD/RollerSubstitute ждали (прод, 29.09.2026).
	CREATE TABLE #requested (issueDate datetime NOT NULL, INDEX IX_requested CLUSTERED (issueDate));
	INSERT INTO #requested (issueDate)
	SELECT CONVERT(datetime, value, 126)
	FROM STRING_SPLIT(@issueDates, ',');

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
	FROM #requested req
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
GO

PRINT N'=== ГОТОВО: RangeSlotIssues обновлена (даты слотов во временной таблице).';
GO
