/*
    Смоук-проверка после наката GetIssuesPrice / SetIssueRatio
    (ветка feature/recalc-join-fix).

    ЧТО ПРОВЕРЯЕМ: ActionRecalculate отрабатывает без ошибок и ИДЕМПОТЕНТЕН -
    второй прогон подряд не меняет ни одного поля. Правка меняет только форму
    плана, поэтому нестабильность результата означала бы ошибку.

    ПОЧЕМУ НЕ «до vs после»: первая версия скрипта сравнивала состояние до
    пересчёта с состоянием после, исходя из того, что действующие акции уже
    согласованы. Это неверно для кампаний, пересекающих границу месяца:
    ActionRecalculate делит период на прошлое и будущее по первому числу
    ТЕКУЩЕГО месяца (@theDate), поэтому кампания, последний раз пересчитанная
    в прошлом месяце, сегодня законно получает другой ratio. На выборке из 121
    акции таких нашлось 32 - расхождение в 8-м знаке ratio, все остальные поля
    совпадали. Проверено, что это разовая коррекция, а не дрейф: прогоны 2 и 3
    дают ровно тот же результат, что прогон 1.

    ГДЕ ДОКАЗАТЕЛЬСТВО ЭКВИВАЛЕНТНОСТИ: прямое сравнение старой и новой версий
    из одинакового исходного состояния - ArtvisDB/Scripts/issues-join-fix-ab.sql.
    Прогнано на восстановленном проде: 121 акция, 336 кампаний, 0 расхождений.

    Скрипт ничего не пишет: каждая акция пересчитывается в транзакции с
    откатом, снимки хранятся в табличных переменных (они переживают ROLLBACK).

    Из сравнения исключены modTime / modUser / modDate - они меняются при
    каждом прогоне по определению.
*/
SET NOCOUNT ON;

DECLARE @perStratum INT = 25;

/*
    Выборка стратифицированная, а не просто «последние N»: правка в
    SetIssueRatio затрагивает кампании ВСЕХ типов, а среди последних по дате
    акций типы 2 и 4 встречаются единицами. Отдельная страта — кампании,
    начавшиеся до текущего месяца: только они проходят через GetPriceByPeriod
    и реально переставляют ratio в фазе 3.
*/
DECLARE @theDate DATETIME = CONVERT(DATETIME, CONVERT(VARCHAR(6), GETDATE(), 112) + '01', 112);

DECLARE @actions TABLE (actionID INT PRIMARY KEY);

INSERT INTO @actions (actionID)
SELECT x.actionID
FROM
(
    SELECT c.actionID,
           rn = ROW_NUMBER() OVER (PARTITION BY c.campaignTypeID ORDER BY MAX(a.modDate) DESC)
    FROM dbo.Campaign c
        INNER JOIN dbo.[Action] a ON a.actionID = c.actionID
    GROUP BY c.actionID, c.campaignTypeID
) x
WHERE x.rn <= @perStratum
GROUP BY x.actionID;

INSERT INTO @actions (actionID)
SELECT x.actionID
FROM
(
    SELECT c.actionID,
           rn = ROW_NUMBER() OVER (ORDER BY MAX(a.modDate) DESC)
    FROM dbo.Campaign c
        INNER JOIN dbo.[Action] a ON a.actionID = c.actionID
    WHERE c.startDate < @theDate AND c.finishDate >= @theDate
    GROUP BY c.actionID
) x
WHERE x.rn <= @perStratum
  AND NOT EXISTS (SELECT 1 FROM @actions z WHERE z.actionID = x.actionID)
GROUP BY x.actionID;

DECLARE @snap TABLE
(
    phase       CHAR(4),
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
    phase       CHAR(4),
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

        EXEC dbo.ActionRecalculate @actionID = @a, @loggedUserID = NULL, @totalPrice = @dummy OUTPUT;

        INSERT INTO @snap
        SELECT 'run1', c.actionID, c.campaignID, c.tariffPrice, c.discount, c.issuesCount,
               c.issuesDuration, c.programsCount, c.timeBonus, c.startDate, c.finishDate,
               c.managerDiscount, c.finalPrice,
               (SELECT ISNULL(SUM(i.ratio), 0) FROM dbo.Issue i WHERE i.campaignID = c.campaignID)
        FROM dbo.Campaign c WHERE c.actionID = @a;

        INSERT INTO @act
        SELECT 'run1', a.actionID, a.tariffPrice, a.discount, a.startDate, a.finishDate,
               a.totalPrice, a.priceSumByCampaigns
        FROM dbo.[Action] a WHERE a.actionID = @a;

        EXEC dbo.ActionRecalculate @actionID = @a, @loggedUserID = NULL, @totalPrice = @dummy OUTPUT;

        INSERT INTO @snap
        SELECT 'run2', c.actionID, c.campaignID, c.tariffPrice, c.discount, c.issuesCount,
               c.issuesDuration, c.programsCount, c.timeBonus, c.startDate, c.finishDate,
               c.managerDiscount, c.finalPrice,
               (SELECT ISNULL(SUM(i.ratio), 0) FROM dbo.Issue i WHERE i.campaignID = c.campaignID)
        FROM dbo.Campaign c WHERE c.actionID = @a;

        INSERT INTO @act
        SELECT 'run2', a.actionID, a.tariffPrice, a.discount, a.startDate, a.finishDate,
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

;WITH b AS (SELECT * FROM @snap WHERE phase = 'run1'),
      a AS (SELECT * FROM @snap WHERE phase = 'run2'),
      diff AS
      (
          SELECT actionID, campaignID, tariffPrice, discount, issuesCount, issuesDuration,
                 programsCount, timeBonus, startDate, finishDate, managerDiscount, finalPrice, ratioSum FROM b
          EXCEPT
          SELECT actionID, campaignID, tariffPrice, discount, issuesCount, issuesDuration,
                 programsCount, timeBonus, startDate, finishDate, managerDiscount, finalPrice, ratioSum FROM a
      )
SELECT [расхождений между прогонами (кампании)] = COUNT(*) FROM diff;

;WITH b AS (SELECT * FROM @act WHERE phase = 'run1'),
      a AS (SELECT * FROM @act WHERE phase = 'run2'),
      diff AS
      (
          SELECT actionID, tariffPrice, discount, startDate, finishDate, totalPrice, priceSumByCampaigns FROM b
          EXCEPT
          SELECT actionID, tariffPrice, discount, startDate, finishDate, totalPrice, priceSumByCampaigns FROM a
      )
SELECT [расхождений между прогонами (акции)] = COUNT(*) FROM diff;

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
