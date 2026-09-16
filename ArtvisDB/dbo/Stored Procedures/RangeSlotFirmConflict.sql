-- =============================================
-- Description:	Проверка перед переносом выпуска в веере (drag-and-drop):
--              есть ли в целевом получасе, на станциях переносимых кампаний,
--              выпуск ЭТОЙ ЖЕ ФИРМЫ — из любой акции, не только текущей.
--              Не блокирует перенос — только сигнал для диалога подтверждения
--              (EditIssuesForm.RangeGrid_DragDrop), реальную проверку при
--              самой записи по-прежнему делает AddRangeIssues.
--              Условие "есть выпуск" то же самое, что и в AddRangeIssues.sql
--              (IssueWithTheSameFirmExists): подтверждённый, либо неподтверждённый
--              из неудалённой акции.
-- =============================================
CREATE PROCEDURE [dbo].[RangeSlotFirmConflict]
(
	@actionID int,
	@issueDate datetime,
	-- Кампании, чьи станции проверяются (CSV campaignID) — участники переноса.
	@campaignIDs varchar(max)
)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @firmID smallint = (SELECT firmID FROM dbo.[Action] WHERE actionID = @actionID);

	-- anyConfirmed — про акцию (a.isConfirmed), не про сам выпуск: заказчику важно различать
	-- текст "акция подтверждена/не подтверждена", а не признак конкретного Issue.
	SELECT
		hasConflict  = CONVERT(bit, CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END),
		anyConfirmed = CONVERT(bit, CASE WHEN SUM(CASE WHEN a.isConfirmed = 1 THEN 1 ELSE 0 END) > 0 THEN 1 ELSE 0 END)
	FROM dbo.Campaign sc
		INNER JOIN dbo.TariffWindow tw ON tw.massmediaID = sc.massmediaID
		INNER JOIN dbo.Tariff t ON t.tariffID = tw.tariffID AND t.isForModuleOnly = 0
		INNER JOIN dbo.Issue i ON i.actualWindowID = tw.windowId
		INNER JOIN dbo.Campaign c ON c.campaignID = i.campaignID
		INNER JOIN dbo.[Action] a ON a.actionID = c.actionID AND a.firmID = @firmID
	WHERE sc.campaignID IN (SELECT CONVERT(int, value) FROM STRING_SPLIT(@campaignIDs, ','))
		AND tw.maxCapacity = 0
		AND tw.isDisabled = 0
		AND tw.windowDateActual BETWEEN @issueDate AND DATEADD(second, -1, DATEADD(minute, 30, @issueDate))
		AND (i.isConfirmed = 1 OR a.deleteDate IS NULL);
END
