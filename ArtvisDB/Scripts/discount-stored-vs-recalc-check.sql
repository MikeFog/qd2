/*
    ДИАГНОСТИКА (только чтение): у каких акций/кампаний сохранённая скидка не совпадает с той,
    что даст следующий ActionRecalculate. Ничего не меняет, READ UNCOMMITTED — не блокирует работу.

    Пакетная: логика hlp_ActionDiscountCalculate по дате начала акции (MIN начала кампаний),
              только акции с 2+ кампаниями — как в ActionRecalculate.
    Простая:  логика hlp_CompanyDiscountCalculate по Campaign.startDate. Работает и до, и после
              перехода DiscountRelease на явную finishDate (схема определяется по колонке).

    Только подтверждённые акции (Action.isConfirmed = 1), которые идут сейчас или закончились
    в текущем месяце: Action.finishDate >= 1-е число текущего месяца. Будущие акции тоже попадают.

    Время: на копии прода 15–30 с (курсор по акциям).
*/
SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

-- 1 — finishDate включительно (новая схема), 0 — старая «начало следующего / NULL»
DECLARE @inclusive int = CASE WHEN COLUMNPROPERTY(OBJECT_ID('dbo.DiscountRelease'), 'finishDate', 'AllowsNull') = 0 THEN 1 ELSE 0 END;

-- Начало текущего месяца: акции, закончившиеся раньше, не интересуют
DECLARE @fromDate datetime = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);

/* ---------- Пакетная скидка ---------- */
CREATE TABLE #pack (actionID int PRIMARY KEY, startDate datetime, stored decimal(9,4), recalc decimal(9,4));

DECLARE @actionID int, @startDate datetime, @avgDuration float, @campaignsCount int, @price decimal(18,2), @d decimal(9,4);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT c.actionID, dbo.ToShortDate(MIN(c.startDate))
    FROM Campaign c
    JOIN [Action] a ON a.actionID = c.actionID AND a.isConfirmed = 1 AND a.finishDate >= @fromDate
    GROUP BY c.actionID
    HAVING COUNT(*) > 1;
OPEN cur;
FETCH NEXT FROM cur INTO @actionID, @startDate;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @price = SUM(price) FROM Campaign WHERE actionID = @actionID;

    SELECT @avgDuration = AVG(CAST(c.issuesDuration AS float)), @campaignsCount = COUNT(*)
    FROM Campaign c
    WHERE c.actionID = @actionID AND c.campaignTypeID < 4
      AND (ISNULL(c.issuesCount, 0) + ISNULL(c.programsCount, 0)) > 0;
    SELECT @avgDuration = COALESCE(@avgDuration, 0), @campaignsCount = COALESCE(@campaignsCount, 0);

    SELECT @d = COALESCE(MIN(pl.discount), 1)
    FROM (
        SELECT m.packageDiscountPriceListID, COUNT(DISTINCT c.massmediaID) AS campaignsCount
        FROM Campaign c
            JOIN (PackageDiscountMassmedia m
                  JOIN PackageDiscountPriceList p ON p.packageDiscountPriceListID = m.packageDiscountPriceListID)
              ON c.massmediaID = m.massmediaID
             AND ((c.campaignTypeID = 1 AND m.isForType1 = 1)
               OR (c.campaignTypeID = 2 AND m.isForType2 = 1)
               OR (c.campaignTypeID = 3 AND m.isForType3 = 1))
             AND CAST(c.issuesDuration AS float) >= @avgDuration * p.eachVolume / 100
        WHERE c.actionID = @actionID
          AND (ISNULL(c.issuesCount, 0) + ISNULL(c.programsCount, 0)) > 0
        GROUP BY m.packageDiscountPriceListID
        HAVING COUNT(c.massmediaID) = @campaignsCount
    ) t
    JOIN PackageDiscountPriceList pl ON pl.packageDiscountPriceListID = t.packageDiscountPriceListID
    JOIN PackageDiscount pd ON pd.packageDiscountId = pl.packageDiscountID
    WHERE pd.count = t.campaignsCount
      AND @startDate BETWEEN pl.startDate AND pl.finishDate
      AND pl.value <= @price;

    INSERT INTO #pack SELECT @actionID, @startDate, a.discount, @d FROM [Action] a WHERE a.actionID = @actionID;

    FETCH NEXT FROM cur INTO @actionID, @startDate;
END
CLOSE cur; DEALLOCATE cur;

/* ---------- Простая скидка ---------- */
SELECT c.campaignID, c.actionID, c.massmediaID, c.startDate, c.discount AS stored,
       ISNULL((SELECT TOP 1 dv.discount
               FROM DiscountRelease dr
               JOIN DiscountValue dv ON dv.discountReleaseID = dr.discountReleaseID
               WHERE dr.massmediaID = c.massmediaID
                 AND c.startDate >= dr.startDate
                 AND (dr.finishDate IS NULL OR c.startDate < DATEADD(DAY, @inclusive, dr.finishDate))
                 AND dv.summa <= c.tariffPrice
                 AND ((dr.isForType1 = 1 AND c.campaignTypeID = 1)
                   OR (dr.isForType2 = 1 AND c.campaignTypeID = 2)
                   OR (dr.isForType3 = 1 AND c.campaignTypeID = 3))
               ORDER BY dv.summa DESC), 1) AS recalc
INTO #simple
FROM Campaign c
JOIN [Action] a ON a.actionID = c.actionID AND a.isConfirmed = 1 AND a.finishDate >= @fromDate
WHERE c.startDate IS NOT NULL;

/* ---------- Итоги ---------- */
SELECT N'пакетная' AS kind, YEAR(startDate) AS [year], COUNT(*) AS n, MAX(startDate) AS latest
FROM #pack WHERE stored <> recalc GROUP BY YEAR(startDate)
UNION ALL
SELECT N'простая', YEAR(startDate), COUNT(*), MAX(startDate)
FROM #simple WHERE stored <> recalc GROUP BY YEAR(startDate)
ORDER BY kind, [year];

-- Пакетная: список
SELECT p.actionID, CONVERT(varchar(10), p.startDate, 104) AS startDate, f.name AS firm,
       p.stored, p.recalc, a.isConfirmed, a.modDate
FROM #pack p
JOIN [Action] a ON a.actionID = p.actionID
LEFT JOIN Firm f ON f.firmID = a.firmID
WHERE p.stored <> p.recalc
ORDER BY p.startDate DESC;

-- Простая: список
SELECT s.campaignID, s.actionID, mm.name AS massmedia, CONVERT(varchar(10), s.startDate, 104) AS startDate,
       f.name AS firm, s.stored, s.recalc, a.isConfirmed
FROM #simple s
JOIN [Action] a ON a.actionID = s.actionID
LEFT JOIN Firm f ON f.firmID = a.firmID
LEFT JOIN MassMedia mm ON mm.massmediaID = s.massmediaID
WHERE s.stored <> s.recalc
ORDER BY s.startDate DESC;

DROP TABLE #pack;
DROP TABLE #simple;
