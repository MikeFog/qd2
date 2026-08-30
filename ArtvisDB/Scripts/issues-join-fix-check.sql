/*
    Проверка правки формы соединения в GetIssuesPrice / SetIssueRatio
    (ветка feature/recalc-join-fix).

    Что проверяем: правка меняет ТОЛЬКО план запроса, результат обязан
    совпадать бит в бит. Действующие акции исторически пересчитывались на
    каждом клике, то есть уже находятся в согласованном состоянии, поэтому
    прогон нового ActionRecalculate на них НЕ ДОЛЖЕН менять ни одного поля.

    Скрипт ничего не пишет: каждая акция пересчитывается в транзакции с
    откатом, снимки хранятся в табличных переменных (они переживают ROLLBACK).

    Порядок:
      1. Накатить dbo/Stored Procedures/GetIssuesPrice.sql и SetIssueRatio.sql.
      2. Выполнить этот скрипт. Ожидаемый результат — «расхождений: 0».

    Из сравнения исключены modTime / modUser / modDate — они меняются при
    каждом прогоне по определению.
*/
SET NOCOUNT ON;

DECLARE @sampleSize INT = 50;

DECLARE @actions TABLE (actionID INT PRIMARY KEY);
INSERT INTO @actions (actionID)
SELECT TOP (@sampleSize) a.actionID
FROM dbo.[Action] a
WHERE EXISTS (SELECT 1 FROM dbo.Campaign c WHERE c.actionID = a.actionID)
ORDER BY a.modDate DESC;

DECLARE @snap TABLE
(
    phase       CHAR(6),
    actionID    INT,
    campaignID  INT,
    tariffPrice DECIMAL(18,2),
    discount    DECIMAL(9,4),
    issuesCount INT,
    issuesDuration INT,
    programsCount  INT,
    timeBonus   INT,
    startDate   DATETIME,
    finishDate  DATETIME,
    managerDiscount DECIMAL(18,10),
    finalPrice  DECIMAL(18,2),
    ratioSum    DECIMAL(38,10)
);

DECLARE @act TABLE
(
    phase       CHAR(6),
    actionID    INT,
    tariffPrice DECIMAL(18,2),
    discount    DECIMAL(9,4),
    startDate   DATETIME,
    finishDate  DATETIME,
    totalPrice  DECIMAL(18,2),
    priceSumByCampaigns DECIMAL(18,2)
);

DECLARE @errors TABLE (actionID INT, msg NVARCHAR(400));

DECLARE @a INT, @dummy DECIMAL(18,2);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT actionID FROM @actions;
OPEN cur;
FETCH NEXT FROM cur INTO @a;

WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        BEGIN TRAN;

        INSERT INTO @snap
        SELECT 'before', c.actionID, c.campaignID, c.tariffPrice, c.discount, c.issuesCount,
               c.issuesDuration, c.programsCount, c.timeBonus, c.startDate, c.finishDate,
               c.managerDiscount, c.finalPrice,
               (SELECT ISNULL(SUM(i.ratio), 0) FROM dbo.Issue i WHERE i.campaignID = c.campaignID)
        FROM dbo.Campaign c WHERE c.actionID = @a;

        INSERT INTO @act
        SELECT 'before', a.actionID, a.tariffPrice, a.discount, a.startDate, a.finishDate,
               a.totalPrice, a.priceSumByCampaigns
        FROM dbo.[Action] a WHERE a.actionID = @a;

        EXEC dbo.ActionRecalculate @actionID = @a, @loggedUserID = NULL, @totalPrice = @dummy OUTPUT;

        INSERT INTO @snap
        SELECT 'after', c.actionID, c.campaignID, c.tariffPrice, c.discount, c.issuesCount,
               c.issuesDuration, c.programsCount, c.timeBonus, c.startDate, c.finishDate,
               c.managerDiscount, c.finalPrice,
               (SELECT ISNULL(SUM(i.ratio), 0) FROM dbo.Issue i WHERE i.campaignID = c.campaignID)
        FROM dbo.Campaign c WHERE c.actionID = @a;

        INSERT INTO @act
        SELECT 'after', a.actionID, a.tariffPrice, a.discount, a.startDate, a.finishDate,
               a.totalPrice, a.priceSumByCampaigns
        FROM dbo.[Action] a WHERE a.actionID = @a;

        ROLLBACK;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        INSERT INTO @errors (actionID, msg) VALUES (@a, ERROR_MESSAGE());
    END CATCH

    FETCH NEXT FROM cur INTO @a;
END

CLOSE cur;
DEALLOCATE cur;

/* ---- отчёт ---- */
SELECT [акций в выборке] = (SELECT COUNT(*) FROM @actions),
       [ошибок пересчёта] = (SELECT COUNT(*) FROM @errors);

;WITH b AS (SELECT * FROM @snap WHERE phase = 'before'),
      a AS (SELECT * FROM @snap WHERE phase = 'after'),
      diff AS
      (
          SELECT actionID, campaignID, tariffPrice, discount, issuesCount, issuesDuration,
                 programsCount, timeBonus, startDate, finishDate, managerDiscount, finalPrice, ratioSum FROM b
          EXCEPT
          SELECT actionID, campaignID, tariffPrice, discount, issuesCount, issuesDuration,
                 programsCount, timeBonus, startDate, finishDate, managerDiscount, finalPrice, ratioSum FROM a
      )
SELECT [расхождений по кампаниям] = COUNT(*) FROM diff;

;WITH b AS (SELECT * FROM @act WHERE phase = 'before'),
      a AS (SELECT * FROM @act WHERE phase = 'after'),
      diff AS
      (
          SELECT actionID, tariffPrice, discount, startDate, finishDate, totalPrice, priceSumByCampaigns FROM b
          EXCEPT
          SELECT actionID, tariffPrice, discount, startDate, finishDate, totalPrice, priceSumByCampaigns FROM a
      )
SELECT [расхождений по акциям] = COUNT(*) FROM diff;

-- детализация, если что-то разошлось
SELECT * FROM @snap s
WHERE EXISTS
(
    SELECT 1 FROM @snap x
    WHERE x.campaignID = s.campaignID AND x.phase <> s.phase
      AND (x.tariffPrice <> s.tariffPrice OR x.discount <> s.discount
        OR x.issuesCount <> s.issuesCount OR x.finalPrice <> s.finalPrice
        OR x.ratioSum <> s.ratioSum)
)
ORDER BY s.campaignID, s.phase;

SELECT * FROM @errors;
