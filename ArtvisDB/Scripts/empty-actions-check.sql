/*
    ДИАГНОСТИКА (только чтение): «пустые» рекламные акции — без кампаний или с кампаниями
    без единого выпуска. Жалоба 15.10.2026: акция пропала из макетов и из активированных,
    в базе осталась одна строка Action.

    Известный путь к пустой акции — ActionActivate: выпуски, не прошедшие проверку
    (прошлое, дедлайн, переполнение окна…), удаляются через IssueIUD, пока акция ещё
    макет, поэтому в LogDeletedIssue не попадают. Если не прошёл ни один, акция становится
    активированной без дат (startDate/finishDate = NULL) и выпадает из журналов по периоду.
    Кампании при этом остаются. Подпись такого случая в выборке 1:
        isConfirmed = 1, camps > 0, issues = 0, startDate NULL, logDeleted = 0.
    Акция без кампаний (camps = 0) этим путём НЕ объясняется.

    @actionID — номер акции из жалобы для подробного разбора (выборки 2–4); NULL — пропустить.
    Ничего не меняет, READ UNCOMMITTED — не блокирует работу.
*/
SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

DECLARE @actionID int = NULL;          -- номер акции из жалобы
DECLARE @sinceDays int = 60;           -- акции, созданные за последние N дней

-- 1. Все пустые акции за период
SELECT
	a.actionID, a.isConfirmed, a.createDate, a.modDate, a.startDate, a.finishDate,
	creator = u.loginName, a.firmID,
	camps      = (SELECT COUNT(*) FROM Campaign c WHERE c.actionID = a.actionID),
	issues     = (SELECT COUNT(*) FROM Campaign c JOIN Issue i ON i.campaignID = c.campaignID WHERE c.actionID = a.actionID),
	progIssues = (SELECT COUNT(*) FROM Campaign c JOIN ProgramIssue i ON i.campaignID = c.campaignID WHERE c.actionID = a.actionID),
	logDeleted = (SELECT COUNT(*) FROM LogDeletedIssue l WHERE l.actionId = a.actionID),
	transfers  = (SELECT COUNT(*) FROM TransferLog t WHERE t.actionID = a.actionID),
	payments   = (SELECT COUNT(*) FROM PaymentAction pa WHERE pa.actionID = a.actionID)
FROM [Action] a
	LEFT JOIN [User] u ON u.userID = a.userID
WHERE a.isSpecial = 0
	AND a.deleteDate IS NULL
	AND a.createDate >= DATEADD(DAY, -@sinceDays, GETDATE())
	AND NOT EXISTS (SELECT 1 FROM Campaign c JOIN Issue i ON i.campaignID = c.campaignID WHERE c.actionID = a.actionID)
	AND NOT EXISTS (SELECT 1 FROM Campaign c JOIN ProgramIssue i ON i.campaignID = c.campaignID WHERE c.actionID = a.actionID)
ORDER BY a.actionID DESC;

IF @actionID IS NULL RETURN;

-- 2. Строка акции целиком
SELECT a.*, creator = u.loginName
FROM [Action] a LEFT JOIN [User] u ON u.userID = a.userID
WHERE a.actionID = @actionID;

-- 3. Кампании акции (кто и когда менял последним)
SELECT c.campaignID, c.campaignTypeID, c.massmediaID, c.startDate, c.finishDate,
	c.issuesCount, c.finalPrice, c.modTime, modUser = u.loginName,
	issues = (SELECT COUNT(*) FROM Issue i WHERE i.campaignID = c.campaignID)
FROM Campaign c LEFT JOIN [User] u ON u.userID = c.modUser
WHERE c.actionID = @actionID;

-- 4. Следы удалений и переносов по акции
SELECT src = 'LogDeletedIssue', l.[date], who = u.loginName, l.issueDate, l.massmediaID, l.rollerId
FROM LogDeletedIssue l LEFT JOIN [User] u ON u.userID = l.userId
WHERE l.actionId = @actionID
UNION ALL
SELECT 'TransferLog', t.transferDate, u.loginName, t.newDate, NULL, t.issueID
FROM TransferLog t LEFT JOIN [User] u ON u.userID = t.userID
WHERE t.actionID = @actionID
ORDER BY 2;
