/*
    Проверка эквивалентности: hlp_CampaignRecalc == фаза 1 (1A–1F) процедуры ActionRecalculate.

    Читает данные, все изменения откатываются. Безопасно на любой базе с боевыми данными.
    Пустой результат обеих проверок = ОК.

    Тест-акции ниже — с дева (restore прода 29.08.2026). На другой базе подставить свои:
      тип 1 многокампанийная, тип 2 (ProgramIssue + Issue), тип 3, тип 4.
*/
SET NOCOUNT ON;

DECLARE @A1 INT = 185931,  -- тип 1, 9 кампаний
        @A2 INT = 185785,  -- тип 2 (спонсор), ProgramIssue + Issue
        @A3 INT = 185937,  -- тип 3 (модульная)
        @A4 INT = 185908;  -- тип 4 (пакетная модульная)

DECLARE @C1 INT = 411026,  -- кампания из @A1
        @C2 INT = 410509,  -- кампания из @A2
        @Clast INT = 409587, @Alast INT = 185497;  -- кампания с 1 выпуском (переход N->0), из акции с >1 кампанией

DECLARE @p DECIMAL(18,2);

DECLARE @r TABLE (
    scenario VARCHAR(40), path VARCHAR(10), campaignID INT, campaignTypeID TINYINT,
    tariffPrice DECIMAL(18,2), issuesCount INT, issuesDuration INT,
    startDate DATETIME, finishDate DATETIME, discount DECIMAL(9,4),
    timeBonus INT, programsCount INT, managerDiscount DECIMAL(18,10), price DECIMAL(18,2)
);

-- ===========================================================================
-- Проверка 1: идемпотентность. ActionRecalculate -> снимок -> hlp_CampaignRecalc
-- по каждой кампании -> снимок. Ничего не менялось => совпадение.
-- ===========================================================================
DECLARE @actionID INT, @campaignID INT;
DECLARE ac CURSOR LOCAL FAST_FORWARD FOR SELECT v FROM (VALUES (@A1),(@A2),(@A3),(@A4)) t(v);
OPEN ac; FETCH NEXT FROM ac INTO @actionID;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC dbo.ActionRecalculate @actionID = @actionID, @loggedUserID = 3, @totalPrice = @p OUTPUT;
    INSERT @r SELECT 'idempotent','full',campaignID,campaignTypeID,tariffPrice,issuesCount,issuesDuration,startDate,finishDate,discount,timeBonus,programsCount,managerDiscount,price FROM dbo.Campaign WHERE actionID = @actionID;

    DECLARE cc CURSOR LOCAL FAST_FORWARD FOR SELECT campaignID FROM dbo.Campaign WHERE actionID = @actionID;
    OPEN cc; FETCH NEXT FROM cc INTO @campaignID;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC dbo.hlp_CampaignRecalc @campaignID = @campaignID, @loggedUserID = 3;
        FETCH NEXT FROM cc INTO @campaignID;
    END
    CLOSE cc; DEALLOCATE cc;

    INSERT @r SELECT 'idempotent','helper',campaignID,campaignTypeID,tariffPrice,issuesCount,issuesDuration,startDate,finishDate,discount,timeBonus,programsCount,managerDiscount,price FROM dbo.Campaign WHERE actionID = @actionID;
    FETCH NEXT FROM ac INTO @actionID;
END
CLOSE ac; DEALLOCATE ac;

-- ===========================================================================
-- Проверка 2: возмущение. Удаляем выпуск(и), сравниваем helper vs full, откат.
-- ===========================================================================

-- 2a: удаление одного выпуска, тип 1
EXEC dbo.ActionRecalculate @actionID = @A1, @loggedUserID = 3, @totalPrice = @p OUTPUT;
DECLARE @iss2a INT = (SELECT MIN(issueID) FROM Issue WHERE campaignID = @C1);
BEGIN TRAN;
    DELETE FROM Issue WHERE issueID = @iss2a;
    EXEC dbo.hlp_CampaignRecalc @campaignID = @C1, @loggedUserID = 3;
    INSERT @r SELECT '2a del type1','helper',campaignID,campaignTypeID,tariffPrice,issuesCount,issuesDuration,startDate,finishDate,discount,timeBonus,programsCount,managerDiscount,price FROM dbo.Campaign WHERE campaignID = @C1;
ROLLBACK;
BEGIN TRAN;
    DELETE FROM Issue WHERE issueID = @iss2a;
    EXEC dbo.ActionRecalculate @actionID = @A1, @loggedUserID = 3, @totalPrice = @p OUTPUT;
    INSERT @r SELECT '2a del type1','full',campaignID,campaignTypeID,tariffPrice,issuesCount,issuesDuration,startDate,finishDate,discount,timeBonus,programsCount,managerDiscount,price FROM dbo.Campaign WHERE campaignID = @C1;
ROLLBACK;

-- 2b: удаление ПОСЛЕДНЕГО выпуска (N->0, менеджерская скидка пересчитывается)
EXEC dbo.ActionRecalculate @actionID = @Alast, @loggedUserID = 3, @totalPrice = @p OUTPUT;
DECLARE @iss2b INT = (SELECT MIN(issueID) FROM Issue WHERE campaignID = @Clast);
BEGIN TRAN;
    DELETE FROM Issue WHERE issueID = @iss2b;
    EXEC dbo.hlp_CampaignRecalc @campaignID = @Clast, @loggedUserID = 3;
    INSERT @r SELECT '2b N->0 manager','helper',campaignID,campaignTypeID,tariffPrice,issuesCount,issuesDuration,startDate,finishDate,discount,timeBonus,programsCount,managerDiscount,price FROM dbo.Campaign WHERE campaignID = @Clast;
ROLLBACK;
BEGIN TRAN;
    DELETE FROM Issue WHERE issueID = @iss2b;
    EXEC dbo.ActionRecalculate @actionID = @Alast, @loggedUserID = 3, @totalPrice = @p OUTPUT;
    INSERT @r SELECT '2b N->0 manager','full',campaignID,campaignTypeID,tariffPrice,issuesCount,issuesDuration,startDate,finishDate,discount,timeBonus,programsCount,managerDiscount,price FROM dbo.Campaign WHERE campaignID = @Clast;
ROLLBACK;

-- 2c: тип 2 — удаление ProgramIssue + Issue (startDate/finishDate из обоих источников)
EXEC dbo.ActionRecalculate @actionID = @A2, @loggedUserID = 3, @totalPrice = @p OUTPUT;
DECLARE @pi2c INT = (SELECT MIN(issueID) FROM ProgramIssue WHERE campaignID = @C2);
DECLARE @is2c INT = (SELECT MIN(issueID) FROM Issue        WHERE campaignID = @C2);
BEGIN TRAN;
    DELETE FROM ProgramIssue WHERE issueID = @pi2c;
    DELETE FROM Issue        WHERE issueID = @is2c;
    EXEC dbo.hlp_CampaignRecalc @campaignID = @C2, @loggedUserID = 3;
    INSERT @r SELECT '2c type2 mixed','helper',campaignID,campaignTypeID,tariffPrice,issuesCount,issuesDuration,startDate,finishDate,discount,timeBonus,programsCount,managerDiscount,price FROM dbo.Campaign WHERE campaignID = @C2;
ROLLBACK;
BEGIN TRAN;
    DELETE FROM ProgramIssue WHERE issueID = @pi2c;
    DELETE FROM Issue        WHERE issueID = @is2c;
    EXEC dbo.ActionRecalculate @actionID = @A2, @loggedUserID = 3, @totalPrice = @p OUTPUT;
    INSERT @r SELECT '2c type2 mixed','full',campaignID,campaignTypeID,tariffPrice,issuesCount,issuesDuration,startDate,finishDate,discount,timeBonus,programsCount,managerDiscount,price FROM dbo.Campaign WHERE campaignID = @C2;
ROLLBACK;

-- восстановить нормальное состояние тест-акций
EXEC dbo.ActionRecalculate @actionID = @A1,    @loggedUserID = 3, @totalPrice = @p OUTPUT;
EXEC dbo.ActionRecalculate @actionID = @A2,    @loggedUserID = 3, @totalPrice = @p OUTPUT;
EXEC dbo.ActionRecalculate @actionID = @Alast, @loggedUserID = 3, @totalPrice = @p OUTPUT;

-- ===========================================================================
-- РЕЗУЛЬТАТ
-- ===========================================================================
PRINT '=== РАСХОЖДЕНИЯ (обе таблицы должны быть пустыми) ===';

;WITH diff AS (
    SELECT h.scenario, h.campaignID, h.campaignTypeID,
           h.tariffPrice hTP, f.tariffPrice fTP, h.issuesCount hIC, f.issuesCount fIC,
           h.issuesDuration hID, f.issuesDuration fID,
           h.startDate hSD, f.startDate fSD, h.finishDate hFD, f.finishDate fFD,
           h.discount hDsc, f.discount fDsc, h.timeBonus hTB, f.timeBonus fTB,
           h.programsCount hPC, f.programsCount fPC,
           h.managerDiscount hMD, f.managerDiscount fMD, h.price hPrice, f.price fPrice
    FROM @r h
    JOIN @r f ON f.scenario = h.scenario AND f.campaignID = h.campaignID AND f.path = 'full'
    WHERE h.path = 'helper'
)
SELECT * FROM diff
WHERE ISNULL(hTP,-1)  <> ISNULL(fTP,-1)  OR ISNULL(hIC,-1) <> ISNULL(fIC,-1)
   OR ISNULL(hID,-1)  <> ISNULL(fID,-1)
   OR ISNULL(hSD,'1900') <> ISNULL(fSD,'1900') OR ISNULL(hFD,'1900') <> ISNULL(fFD,'1900')
   OR ISNULL(hDsc,-1) <> ISNULL(fDsc,-1) OR ISNULL(hTB,-1) <> ISNULL(fTB,-1)
   OR ISNULL(hPC,-1)  <> ISNULL(fPC,-1)  OR ISNULL(hMD,-1) <> ISNULL(fMD,-1)
   OR ISNULL(hPrice,-1) <> ISNULL(fPrice,-1);

PRINT '=== сводка проверенного ===';
SELECT scenario, campaignTypeID, COUNT(*) / 2 AS campaigns_checked
FROM @r GROUP BY scenario, campaignTypeID ORDER BY scenario, campaignTypeID;
