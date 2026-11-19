-- Пакетная скидка: максимальный процент прайма (docs/tasks/package-discount-prime-limit.md, постановка Миши 18.11.2026).
--
-- Менеджеры добирают порог пакетной скидки «Минимальная сумма (с учётом объёмных скидок)» праймом. Новая настройка прайс-листа
-- пакетной скидки «Максимальный процент прайма (%):» (0–100): если доля прайма хоть одной линейной или модульной кампании акции
-- больше порога, этот прайс-лист пакета акции не подходит. Спонсорские кампании не проверяются (вопрос у заказчика).
-- Прайм — окно с максимальной ценой окон станции в этот день (как генератор шаблонов); однотарифный день — прайм целиком.
-- Доля = секунды роликов в прайм-окнах / все секунды роликов кампании, округление до 0,1.
--
-- Что делает скрипт:
--   1. PackageDiscountPriceList.maxPrimePercent tinyint NOT NULL DEFAULT 100 — всем существующим прайс-листам 100
--      (прайм без ограничения), скидки старых акций не меняются;
--   2. Campaign.primePercent decimal(4,1) NULL — доля прайма линейной и модульной кампании;
--   3. процедура hlp_ActionCampaignPrimePercent (правило доли прайма — только здесь);
--   4. ActionRecalculate (выпуски акции — одним проходом для сумм кампаний и доли прайма; пишет долю),
--      hlp_ActionDiscountCalculate (сверяет с порогом), PackageDiscountPriceListIUD (поле;
--      старый клиент без поля порог не сбрасывает), DiscountChangeAffectedActions (окно «Изменение затронет акции» при
--      понижении порога), Actions1 (наибольшая доля прайма для статистики веера qd2);
--   5. паспорт и колонка прайс-листа пакетной скидки (iEntity/iEntityAttribute 191) + испанский;
--   6. заполнение Campaign.primePercent у существующих линейных и модульных кампаний — порциями по 500 акций (на ArtvisDev
--      всё вместе около 1 мин); нужно для статистики и окна предупреждения, скидки не меняет. Повторный запуск
--      пропускает уже заполненные кампании.
--
-- Пересчёт акции (ArtvisDev): до 500 выпусков — как было, 500–2000 — +14 мс, 2000+ — +93 мс в среднем.
-- Строки «Прайм» в статистике — с новыми qd2 и вебом; со старым клиентом всё работает, строк статистики нет, а поле паспорта
-- появится и в старом qd2 (паспорт читается из базы). После наката перезапустить qd2 и веб.
-- После 08, 24, 28, 29, 33, 42, 76, 86, 93 (несут прежние тексты этих процедур). Не перезапускать после 97: их же и
-- ArtvisDB/Scripts/ campaign-finalprice-with-pack-deploy.sql, discount-applied-pricelist-id-deploy.sql, discount-clone-deploy.sql,
-- actions-createdate-end-boundary-deploy.sql, actions1-actionid-userid-deploy.sql, web-i18n-sql-deploy.sql — вернут процедуры
-- без порога и доли прайма; если перезапустили — повторить 97. В любое время, на Artvis и Tumen. Идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 97_package-discount-prime-limit.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

IF OBJECT_ID(N'dbo.PackageDiscountPriceList') IS NULL OR OBJECT_ID(N'dbo.Campaign') IS NULL
BEGIN
    RAISERROR(N'97: нет dbo.PackageDiscountPriceList или dbo.Campaign — не та база. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO

-- =====================================================================
-- 1-2. Колонки
-- =====================================================================
IF COL_LENGTH('dbo.PackageDiscountPriceList', 'maxPrimePercent') IS NULL
BEGIN
    ALTER TABLE dbo.PackageDiscountPriceList ADD maxPrimePercent tinyint NOT NULL
        CONSTRAINT DF_PackageDiscountPriceList_maxPrimePercent DEFAULT (100);
    PRINT N'Колонка PackageDiscountPriceList.maxPrimePercent добавлена, у всех прайс-листов 100.';
END
ELSE
    PRINT N'Колонка PackageDiscountPriceList.maxPrimePercent уже есть — значения не трогаем.';
GO

IF COL_LENGTH('dbo.Campaign', 'primePercent') IS NULL
BEGIN
    ALTER TABLE dbo.Campaign ADD primePercent decimal(4,1) NULL;
    PRINT N'Колонка Campaign.primePercent добавлена.';
END
ELSE
    PRINT N'Колонка Campaign.primePercent уже есть.';
GO

-- =====================================================================
-- 3-4. Процедуры (тексты — из ArtvisDB/dbo)
-- =====================================================================
DROP FUNCTION IF EXISTS dbo.f_ActionCampaignPrimePercent;   -- черновой вариант правила, был только на ArtvisDev
GO

-- Доля прайма линейных и модульных кампаний, % с округлением до 0,1 (docs/tasks/package-discount-prime-limit.md).
-- Правило — только здесь:
--   прайм — окно, цена которого равна максимальной цене окон станции в этот день (как генератор шаблонов, GetMaxPriceByDay);
--   день, где все окна станции по одной цене, — прайм целиком; окно и день — фактические (Issue.actualWindowID, dayActual);
--   доля = секунды роликов в прайм-окнах / все секунды роликов кампании; кампании без выпусков в результат не попадают.
-- Вход — #ActionIssue вызывающего (выпуски с окном выхода: campaignID, massmediaID, dayActual, price, duration), выход —
-- #ActionPrime (campaignID, primePercent). Выпуски собирает вызывающий: ActionRecalculate (тем же проходом считает суммы
-- кампаний — второй проход по выпускам и окнам на большой акции стоил +250 мс) и заполнение в Deploy/97.
-- Максимум цены — один раз на станцию и день (индекс IX_TariffWindow_MassmediaID_DayActual_Price).
CREATE OR ALTER PROCEDURE [dbo].[hlp_ActionCampaignPrimePercent]
AS
SET NOCOUNT ON;

DECLARE @dayMax TABLE (massmediaID SMALLINT NOT NULL, dayActual SMALLDATETIME NOT NULL, maxPrice DECIMAL(18,2) NULL,
    PRIMARY KEY (massmediaID, dayActual));

INSERT INTO @dayMax (massmediaID, dayActual)
SELECT DISTINCT massmediaID, dayActual FROM #ActionIssue;

UPDATE k
SET k.maxPrice = (SELECT MAX(d.price) FROM dbo.TariffWindow d WHERE d.massmediaID = k.massmediaID AND d.dayActual = k.dayActual)
FROM @dayMax k;

INSERT INTO #ActionPrime (campaignID, primePercent)
SELECT
    x.campaignID,
    CAST(ROUND(100.0 * SUM(CASE WHEN x.price = k.maxPrice THEN x.duration ELSE 0 END) / SUM(x.duration), 1) AS DECIMAL(4,1))
FROM #ActionIssue x
    INNER JOIN @dayMax k ON k.massmediaID = x.massmediaID AND k.dayActual = x.dayActual
GROUP BY x.campaignID
HAVING SUM(x.duration) > 0;
GO

CREATE OR ALTER PROC [dbo].[ActionRecalculate]
(
    @actionID INT,
    @loggedUserID INT = NULL,
    @todayDate DATETIME = NULL,
    @totalPrice DECIMAL(18,2) = NULL OUTPUT  -- New OUTPUT parameter
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @discountValue DECIMAL(9,4),
        @discountValueID INT,
        @tariffPrice DECIMAL(18,2),
        @campaignID INT,
        @massmediaID SMALLINT,
        @campaignTypeID TINYINT,
        @startDate DATETIME,
        @finishDate DATETIME,
        @price DECIMAL(18,2),
        @finalPrice DECIMAL(18,2),
        @theDate DATETIME,
        @estimatedPrice DECIMAL(18,2),
        @managerDiscountCampaign DECIMAL(18,10),
        @fixedPrice DECIMAL(18,2),
        @issuesPrice DECIMAL(18,2),
        @ratio DECIMAL(18,10),
        @campaignDiscount DECIMAL(9,4),
        @campaignDiscountReleaseID SMALLINT,   -- набор объёмной скидки, давший campaignDiscount
        @packageDiscountPriceListID INT,       -- прайс-лист пакетной скидки, давший скидку акции
        @timeBonus INT,
        @programsCount INT,
        @issuesCount INT,
        @issueDuration INT,
        @campaignCount INT,
        @isNewCampaign BIT;

    IF OBJECT_ID('tempdb..#CampaignPhase1') IS NOT NULL
        DROP TABLE #CampaignPhase1;

    CREATE TABLE #CampaignPhase1
    (
        campaignID INT NOT NULL PRIMARY KEY,
        campaignTypeID TINYINT NOT NULL,
        massmediaID SMALLINT NULL,
        oldTotalCount INT NOT NULL,
        tariffPrice DECIMAL(18,2) NULL,
        issuesDuration INT NULL,
        issuesCount INT NULL,
        programsCount INT NULL,
        startDate DATETIME NULL,
        finishDate DATETIME NULL,
        timeBonus INT NULL,
        campaignDiscount DECIMAL(9,4) NULL,
        managerDiscountCampaign DECIMAL(18,10) NULL,
        discountReleaseID SMALLINT NULL
    );

    INSERT INTO #CampaignPhase1
    (
        campaignID,
        campaignTypeID,
        massmediaID,
        oldTotalCount,
        tariffPrice,
        issuesDuration,
        issuesCount,
        programsCount,
        startDate,
        finishDate,
        timeBonus,
        campaignDiscount,
        managerDiscountCampaign
    )
    SELECT
        c.campaignID,
        c.campaignTypeID,
        c.massmediaID,
        ISNULL(c.issuesCount, 0) + ISNULL(c.programsCount, 0),
        0,
        0,
        0,
        0,
        NULL,
        NULL,
        0,
        NULL,
        NULL
    FROM dbo.Campaign c
    WHERE c.actionID = @actionID;

    /* =========================
       Phase 1.0. Выпуски линейных и модульных кампаний с окном выхода — одним проходом:
       из них суммы кампаний (1A, 1C) и доля прайма (hlp_ActionCampaignPrimePercent, порог пакетной скидки)
       ========================= */
    IF OBJECT_ID('tempdb..#ActionIssue') IS NOT NULL
        DROP TABLE #ActionIssue;
    IF OBJECT_ID('tempdb..#ActionPrime') IS NOT NULL
        DROP TABLE #ActionPrime;

    CREATE TABLE #ActionIssue
    (
        campaignID INT NOT NULL,
        campaignTypeID TINYINT NOT NULL,
        massmediaID SMALLINT NOT NULL,
        dayActual SMALLDATETIME NOT NULL,
        dayOriginal SMALLDATETIME NOT NULL,
        price DECIMAL(18,2) NOT NULL,
        tariffPrice DECIMAL(18,2) NOT NULL,
        duration INT NOT NULL
    );
    CREATE TABLE #ActionPrime (campaignID INT NOT NULL PRIMARY KEY, primePercent DECIMAL(4,1) NOT NULL);

    INSERT INTO #ActionIssue (campaignID, campaignTypeID, massmediaID, dayActual, dayOriginal, price, tariffPrice, duration)
    SELECT i.campaignID, p.campaignTypeID, tw.massmediaID, tw.dayActual, tw.dayOriginal, tw.price, i.tariffPrice, r.duration
    FROM dbo.Issue i
    INNER JOIN dbo.TariffWindow tw ON tw.windowId = i.actualWindowID -- день выхода (docs/tasks/window-actual-switch.md, этап 4)
    INNER JOIN dbo.Roller r ON r.rollerID = i.rollerID
    INNER JOIN #CampaignPhase1 p ON p.campaignID = i.campaignID AND p.campaignTypeID IN (1, 3)
    -- без подсказки план с хеш-соединением просматривал индекс TariffWindow целиком: +500 мс на любой акции
    OPTION (LOOP JOIN);

    EXEC dbo.hlp_ActionCampaignPrimePercent;

    /* =========================
       Phase 1A. Type 1
       ========================= */
    ;WITH Type1Agg AS
    (
        SELECT
            i.campaignID,
            tariffPrice   = SUM(i.tariffPrice),
            issuesDuration = SUM(i.duration),
            issuesCount   = COUNT(*),
            startDate     = MIN(i.dayOriginal),
            finishDate    = MAX(i.dayOriginal)
        FROM #ActionIssue i
        WHERE i.campaignTypeID = 1
        GROUP BY i.campaignID
    )
    UPDATE p
    SET
        p.tariffPrice    = ISNULL(a.tariffPrice, 0),
        p.issuesDuration = ISNULL(a.issuesDuration, 0),
        p.issuesCount    = ISNULL(a.issuesCount, 0),
        p.programsCount  = 0,
        p.startDate      = dbo.ToShortDate(a.startDate),
        p.finishDate     = dbo.ToShortDate(a.finishDate),
        p.timeBonus      = 0
    FROM #CampaignPhase1 p
    LEFT JOIN Type1Agg a ON a.campaignID = p.campaignID
    WHERE p.campaignTypeID = 1;

    /* =========================
       Phase 1B. Type 2
       ========================= */
    ;WITH ProgramAgg AS
    (
        SELECT
            i.campaignID,
            tariffPrice  = SUM(i.tariffPrice),
            startDate    = MIN(i.issueDate),
            finishDate   = MAX(i.issueDate),
            timeBonus    = SUM(pl.bonus),
            programsCount = COUNT(*)
        FROM dbo.ProgramIssue i
        INNER JOIN dbo.SponsorTariff st ON st.tariffID = i.tariffID
        INNER JOIN dbo.SponsorProgramPricelist pl ON pl.pricelistID = st.pricelistID
        INNER JOIN #CampaignPhase1 p ON p.campaignID = i.campaignID AND p.campaignTypeID = 2
        GROUP BY i.campaignID
    ),
    IssueAgg AS
    (
        SELECT
            i.campaignID,
            issuesDuration = SUM(dbo.f_GetSponsorDuration(r.duration, i.positionId, pl.extraChargeFirstRoller, pl.extraChargeSecondRoller, pl.extraChargeLastRoller)),
            issuesCount    = COUNT(*),
            issueStartDate = MIN(twA.dayOriginal),
            issueFinishDate = MAX(twA.dayOriginal)
        FROM dbo.Issue i
        INNER JOIN dbo.TariffWindow twA ON twA.windowId = i.actualWindowID -- даты — по дню выхода
        INNER JOIN dbo.TariffWindow tw ON tw.windowId = i.originalWindowID
        -- Наценки (расход бонусного времени, f_GetSponsorDuration) — по прайс-листу окна выхода; у окна без тарифа —
        -- по прайс-листу окна постановки (docs/tasks/window-actual-open-questions.md, Р-3 «Уточнено»)
        CROSS APPLY
        (
            SELECT TOP 1 pl.extraChargeFirstRoller, pl.extraChargeSecondRoller, pl.extraChargeLastRoller
            FROM (VALUES (1, twA.tariffId), (2, tw.tariffId)) v (ord, tariffId)
            INNER JOIN dbo.Tariff t ON t.tariffID = v.tariffId
            INNER JOIN dbo.Pricelist pl ON pl.pricelistID = t.pricelistID
            ORDER BY v.ord
        ) pl
        INNER JOIN dbo.Roller r ON r.rollerID = i.rollerID
        INNER JOIN #CampaignPhase1 p ON p.campaignID = i.campaignID AND p.campaignTypeID = 2
        GROUP BY i.campaignID
    )
    UPDATE p
    SET
        p.tariffPrice = ISNULL(pa.tariffPrice, 0),
        p.issuesDuration = ISNULL(ia.issuesDuration, 0),
        p.issuesCount = ISNULL(ia.issuesCount, 0),
        p.programsCount = ISNULL(pa.programsCount, 0),
        p.timeBonus = ISNULL(pa.timeBonus, 0),
        p.startDate = dbo.ToShortDate(
            CASE
                WHEN pa.startDate IS NULL THEN ia.issueStartDate
                WHEN ia.issueStartDate IS NULL THEN pa.startDate
                WHEN pa.startDate < ia.issueStartDate THEN pa.startDate
                ELSE ia.issueStartDate
            END
        ),
        p.finishDate = dbo.ToShortDate(
            CASE
                WHEN pa.finishDate IS NULL THEN ia.issueFinishDate
                WHEN ia.issueFinishDate IS NULL THEN pa.finishDate
                WHEN pa.finishDate > ia.issueFinishDate THEN pa.finishDate
                ELSE ia.issueFinishDate
            END
        )
    FROM #CampaignPhase1 p
    LEFT JOIN ProgramAgg pa ON pa.campaignID = p.campaignID
    LEFT JOIN IssueAgg ia ON ia.campaignID = p.campaignID
    WHERE p.campaignTypeID = 2;

    /* =========================
       Phase 1C. Type 3
       ========================= */
    ;WITH IssueAgg AS
    (
        SELECT
            i.campaignID,
            issuesDuration = SUM(i.duration),
            issuesCount = COUNT(*)
        FROM #ActionIssue i
        WHERE i.campaignTypeID = 3
        GROUP BY i.campaignID
    ),
    ModuleAgg AS
    (
        SELECT
            i.campaignID,
            tariffPrice = SUM(i.tariffPrice),
            startDate = MIN(i.issueDate),
            finishDate = MAX(i.issueDate)
        FROM dbo.ModuleIssue i
        INNER JOIN #CampaignPhase1 p ON p.campaignID = i.campaignID AND p.campaignTypeID = 3
        GROUP BY i.campaignID
    )
    UPDATE p
    SET
        p.tariffPrice = ISNULL(ma.tariffPrice, 0),
        p.issuesDuration = ISNULL(ia.issuesDuration, 0),
        p.issuesCount = ISNULL(ia.issuesCount, 0),
        p.programsCount = 0,
        p.startDate = dbo.ToShortDate(ma.startDate),
        p.finishDate = dbo.ToShortDate(ma.finishDate),
        p.timeBonus = 0
    FROM #CampaignPhase1 p
    LEFT JOIN IssueAgg ia ON ia.campaignID = p.campaignID
    LEFT JOIN ModuleAgg ma ON ma.campaignID = p.campaignID
    WHERE p.campaignTypeID = 3;

    /* =========================
       Phase 1D. Type 4
       ========================= */
    ;WITH IssueAgg AS
    (
        SELECT
            i.campaignID,
            issuesDuration = SUM(r.duration),
            issuesCount = COUNT(*)
        FROM dbo.Issue i
        INNER JOIN dbo.Roller r ON r.rollerID = i.rollerID
        INNER JOIN #CampaignPhase1 p ON p.campaignID = i.campaignID AND p.campaignTypeID = 4
        GROUP BY i.campaignID
    ),
    PackAgg AS
    (
        SELECT
            i.campaignID,
            tariffPrice = SUM(i.tariffPrice),
            startDate = MIN(i.issueDate),
            finishDate = MAX(i.issueDate)
        FROM dbo.PackModuleIssue i
        INNER JOIN #CampaignPhase1 p ON p.campaignID = i.campaignID AND p.campaignTypeID = 4
        GROUP BY i.campaignID
    )
    UPDATE p
    SET
        p.tariffPrice = ISNULL(pa.tariffPrice, 0),
        p.issuesDuration = ISNULL(ia.issuesDuration, 0),
        p.issuesCount = ISNULL(ia.issuesCount, 0),
        p.programsCount = 0,
        p.startDate = dbo.ToShortDate(pa.startDate),
        p.finishDate = dbo.ToShortDate(pa.finishDate),
        p.timeBonus = 0
    FROM #CampaignPhase1 p
    LEFT JOIN IssueAgg ia ON ia.campaignID = p.campaignID
    LEFT JOIN PackAgg pa ON pa.campaignID = p.campaignID
    WHERE p.campaignTypeID = 4;

    /* =========================
       Phase 1E. Per-campaign discount and manager discount
       ========================= */
    DECLARE cur_phase1 CURSOR LOCAL FAST_FORWARD
    FOR
    SELECT
        campaignID,
        campaignTypeID,
        massmediaID,
        oldTotalCount,
        tariffPrice,
        issuesCount,
        programsCount,
        startDate,
        finishDate
    FROM #CampaignPhase1;

    OPEN cur_phase1;
    FETCH NEXT FROM cur_phase1
    INTO @campaignID, @campaignTypeID, @massmediaID, @issuesCount, @tariffPrice, @programsCount, @timeBonus, @startDate, @finishDate;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @oldTotalCount INT, @newIssuesCount INT, @newProgramsCount INT;

        SET @oldTotalCount = @issuesCount;
        SET @newIssuesCount = ISNULL(@programsCount, 0);
        SET @newProgramsCount = ISNULL(@timeBonus, 0);

        EXEC dbo.hlp_CompanyDiscountCalculate
            @massMediaID = @massmediaID,
            @campaignTypeID = @campaignTypeID,
            @startDate = @startDate,
            @tariffPrice = @tariffPrice,
            @discountValue = @campaignDiscount OUTPUT,
            @discountReleaseID = @campaignDiscountReleaseID OUTPUT;

        IF (@oldTotalCount = 0 AND (@newIssuesCount + @newProgramsCount) > 0)
           OR (@oldTotalCount > 0 AND (@newIssuesCount + @newProgramsCount) = 0)
            SELECT @managerDiscountCampaign = dbo.fn_GetMaxUserDiscount(@loggedUserID, @startDate, @finishDate);
        ELSE
            SET @managerDiscountCampaign = NULL;

        UPDATE #CampaignPhase1
        SET
            campaignDiscount = @campaignDiscount,
            managerDiscountCampaign = @managerDiscountCampaign,
            discountReleaseID = @campaignDiscountReleaseID
        WHERE campaignID = @campaignID;

        FETCH NEXT FROM cur_phase1
        INTO @campaignID, @campaignTypeID, @massmediaID, @issuesCount, @tariffPrice, @programsCount, @timeBonus, @startDate, @finishDate;
    END

    CLOSE cur_phase1;
    DEALLOCATE cur_phase1;

    /* =========================
       Phase 1F. Persist campaign phase 1
       ========================= */
    UPDATE c
    SET
        c.tariffPrice = ISNULL(p.tariffPrice, 0),
        c.issuesDuration = ISNULL(p.issuesDuration, 0),
        c.issuesCount = ISNULL(p.issuesCount, 0),
        c.startDate = p.startDate,
        c.finishDate = p.finishDate,
        c.discount = p.campaignDiscount,
        c.timeBonus = ISNULL(p.timeBonus, 0),
        c.programsCount = ISNULL(p.programsCount, 0),
        c.managerDiscount = ISNULL(p.managerDiscountCampaign, c.managerDiscount),
        c.discountReleaseID = p.discountReleaseID,
        -- у спонсорских, пакетных и кампаний без выпусков — NULL
        c.primePercent = pp.primePercent
    FROM dbo.Campaign c
    INNER JOIN #CampaignPhase1 p ON p.campaignID = c.campaignID
    LEFT JOIN #ActionPrime pp ON pp.campaignID = c.campaignID;

    /* =========================
       Phase 2. Recalculate action
       IMPORTANT: use live Campaign, because hlp_ActionDiscountCalculate
       depends on Campaign.price and issuesDuration
       ========================= */
    SELECT
        @tariffPrice = ISNULL(SUM(c.tariffPrice), 0),
        @startDate = dbo.ToShortDate(MIN(c.startDate)),
        @finishDate = dbo.ToShortDate(MAX(c.finishDate)),
        @campaignCount = COUNT(*)
    FROM dbo.Campaign c
    WHERE c.actionID = @actionID;

    IF @campaignCount > 1
        EXEC dbo.hlp_ActionDiscountCalculate
            @actionID = @actionID,
            @startDate = @startDate,
            @discountValue = @discountValue OUTPUT,
            @packageDiscountPriceListID = @packageDiscountPriceListID OUTPUT;
    ELSE
        SELECT @discountValue = 1, @packageDiscountPriceListID = NULL;

    UPDATE dbo.[Action]
    SET
        tariffPrice = @tariffPrice,
        discount = @discountValue,
        packageDiscountPriceListID = @packageDiscountPriceListID,
        startDate = @startDate,
        finishDate = @finishDate,
        modDate = GETDATE()
    WHERE actionId = @actionID;

    /* =========================
       Phase 3. Final campaign calculations
       IMPORTANT: use live Campaign, not snapshot
       ========================= */
    DECLARE cur_companies CURSOR LOCAL FAST_FORWARD
    FOR
    SELECT
        campaignID,
        massmediaID,
        campaignTypeID,
        startDate,
        finishDate,
        price,
        managerDiscount,
        finalPrice
    FROM dbo.Campaign
    WHERE actionID = @actionID;

    OPEN cur_companies;
    FETCH NEXT FROM cur_companies
    INTO @campaignID, @massmediaID, @campaignTypeID, @startDate, @finishDate,
         @price, @managerDiscountCampaign, @finalPrice;

    DECLARE @dayX DATETIME;

    IF @todayDate IS NULL
        SET @dayX = CONVERT(DATETIME, CONVERT(VARCHAR(6), GETDATE(), 112) + '01', 112);
    ELSE
        SET @dayX = CONVERT(DATETIME, CONVERT(VARCHAR(6), @todayDate, 112) + '01', 112);

    SET @theDate = DATEADD(DAY, 0, @dayX);
    SET @dayX = DATEADD(DAY, -1, @dayX);

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @campaignTypeID <> 4
            SET @estimatedPrice = @managerDiscountCampaign * @price * @discountValue;
        ELSE
            SET @estimatedPrice = @managerDiscountCampaign * @price;

        IF @startDate < @theDate
            EXEC dbo.GetPriceByPeriod @campaignID, @campaignTypeID, @startDate, @dayX, @fixedPrice OUT;
        ELSE
            SET @fixedPrice = 0;

        SET @finishDate = DATEADD(DAY, 1, @finishDate);

        EXEC dbo.GetIssuesPrice @campaignID, @campaignTypeID, @theDate, @finishDate, @issuesPrice OUT;

        IF @issuesPrice IS NOT NULL AND @issuesPrice > 0
        BEGIN
            SET @ratio = (CAST(@estimatedPrice AS DECIMAL(18,10)) - @fixedPrice) / @issuesPrice;

            IF @ratio < 0
            BEGIN
                CLOSE cur_companies;
                DEALLOCATE cur_companies;

                RAISERROR('CantChangeDiscount2', 16, 1);
                RETURN;
            END

            EXEC dbo.SetIssueRatio @campaignID, @campaignTypeID, @theDate, @finishDate, @ratio;
        END

        -- finalPrice хранит цену со ВСЕМИ скидками, включая пакетную,
        -- то есть ровно ту сумму, которая раскидывается по выпускам через @ratio.
        -- Домножать её на Action.discount при чтении больше не нужно.
        UPDATE dbo.Campaign
        SET
            finalPrice = @estimatedPrice,
            modTime = GETDATE(),
            modUser = ISNULL(@loggedUserID, modUser)
        WHERE campaignID = @campaignID;

        FETCH NEXT FROM cur_companies
        INTO @campaignID, @massmediaID, @campaignTypeID, @startDate, @finishDate,
             @price, @managerDiscountCampaign, @finalPrice;
    END

    CLOSE cur_companies;
    DEALLOCATE cur_companies;

    /* =========================
       Phase 4. Final action totals
       IMPORTANT: use live Campaign
       ========================= */
    DECLARE
        @priceSumByCampaigns DECIMAL(18,2),
        @sumPackModules DECIMAL(18,2),
        @sumOther DECIMAL(18,2);

    -- priceSumByCampaigns сохраняет прежний смысл: сумма кампаний со всеми
    -- скидками, КРОМЕ пакетной. Раньше это была просто SUM(finalPrice);
    -- теперь, когда finalPrice включает пакетную, формулу пишем явно.
    SELECT
        @priceSumByCampaigns = ISNULL(SUM(CAST(c.price * c.managerDiscount AS DECIMAL(18,2))), 0),
        @sumPackModules = ISNULL(SUM(CASE WHEN c.campaignTypeID = 4 THEN c.finalPrice ELSE 0 END), 0),
        @sumOther = ISNULL(SUM(CASE WHEN c.campaignTypeID = 4 THEN 0 ELSE c.finalPrice END), 0)
    FROM dbo.Campaign c
    WHERE c.actionID = @actionID;

    -- Set the OUTPUT parameter before updating the table
    SET @totalPrice = ISNULL(@sumOther + @sumPackModules, 0);

    UPDATE dbo.[Action]
    SET
        priceSumByCampaigns = ISNULL(@priceSumByCampaigns, 0),
        totalPrice = @totalPrice
    WHERE actionID = @actionID;

END
GO

CREATE OR ALTER PROC [dbo].[hlp_ActionDiscountCalculate]
(
@actionID int,
@startDate datetime,
@discountValue decimal(9,4) output,
@packageDiscountPriceListID int = NULL output -- какой прайс-лист дал скидку (NULL — ни один)
)
AS
SET NOCOUNT on

DECLARE @avgDuration float, @maxDuration float, @baseDuration float, @campaignsCount tinyint, @priceByCampaigns decimal(18,2)

Select @priceByCampaigns = Sum([price]) From Campaign where actionID = @actionID

-- ИСПРАВЛЕНО: COUNT(*) вместо COUNT(DISTINCT c.massmediaID)
-- Это гарантирует, что каждая кампания (включая тип 3) учитывается отдельно
-- ИСПРАВЛЕНО: пустые кампании (без единого размещения) в расчёте пакета не участвуют -
-- иначе они тянут вниз avgDuration и завышают @campaignsCount, срывая пакетную скидку.
-- Предикат тот же, что ActionRecalculate использует для oldTotalCount.
SELECT @avgDuration=AVG(CAST(c.issuesDuration AS float)), @maxDuration=MAX(CAST(c.issuesDuration AS float)), @campaignsCount=COUNT(*)
FROM Campaign c
WHERE c.actionID=@actionID and c.campaignTypeID < 4
	and (ISNULL(c.issuesCount, 0) + ISNULL(c.programsCount, 0)) > 0

SELECT @avgDuration=COALESCE(@avgDuration,0), @maxDuration=COALESCE(@maxDuration,0), @campaignsCount=COALESCE(@campaignsCount,0)

-- «Процент заполнения» прайс-листа (eachVolume) считается от средней длительности кампаний акции; для акций, начавшихся
-- с даты системной настройки «PackageDiscountMaxBasisFrom» (Протектор: «Настройки...»), — от максимальной. Дата не задана —
-- везде от средней. @startDate — начало акции (ActionRecalculate передаёт MIN(Campaign.startDate)).
DECLARE @maxBasisFrom datetime = dbo.f_SysParamsPackageDiscountMaxBasisFrom()
SELECT @baseDuration = CASE WHEN @maxBasisFrom IS NOT NULL AND @startDate >= @maxBasisFrom THEN @maxDuration ELSE @avgDuration END

-- Самый выгодный клиенту прайс-лист; при равных скидках — меньший ID, чтобы ссылка была однозначной
SELECT @discountValue = 1, @packageDiscountPriceListID = NULL

SELECT TOP 1 @discountValue=pl.discount, @packageDiscountPriceListID=pl.packageDiscountPriceListID FROM (
		SELECT m.packageDiscountPriceListID, count(DISTINCT c.massmediaID) AS campaignsCount
		FROM Campaign c
			JOIN (
				PackageDiscountMassmedia m 
					JOIN PackageDiscountPriceList p ON p.packageDiscountPriceListID=m.packageDiscountPriceListID
				) ON c.massmediaID = m.massmediaID
														AND (
																(c.campaignTypeID=1 AND m.isForType1=1)
																OR (c.campaignTypeID=2 AND m.isForType2=1)
																OR (c.campaignTypeID=3 AND m.isForType3=1)
																)
														AND CAST(c.issuesDuration as float) >= @baseDuration*p.eachVolume/100
														-- доля прайма линейной и модульной кампании не выше порога прайс-листа (включительно);
														-- спонсорские не проверяются (docs/tasks/package-discount-prime-limit.md)
														AND (c.campaignTypeID NOT IN (1, 3) OR ISNULL(c.primePercent, 0) <= p.maxPrimePercent)
		WHERE
			c.actionID=@actionID
			-- пустые кампании исключаем и здесь, чтобы HAVING сравнивал только реальные
			and (ISNULL(c.issuesCount, 0) + ISNULL(c.programsCount, 0)) > 0
		GROUP BY
			m.packageDiscountPriceListID
		-- ИСПРАВЛЕНО: count(c.massmediaID) вместо count(DISTINCT m.massmediaID)
		-- Считаем количество сопоставлённых кампаний, а не уникальных massmedia
		HAVING 
			count(c.massmediaID)=@campaignsCount
		) t
	JOIN PackageDiscountPriceList pl ON pl.packageDiscountPriceListID=t.packageDiscountPriceListID
	JOIN PackageDiscount d ON d.packageDiscountId=pl.packageDiscountID
WHERE 
	d.count = t.campaignsCount
	AND @startDate BETWEEN pl.startDate AND pl.finishDate
	and pl.value <= @priceByCampaigns
ORDER BY
	pl.discount, pl.packageDiscountPriceListID
GO

-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 01.02.2008
-- Description:	<Description,,>
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[PackageDiscountPriceListIUD]
(
	@packageDiscountPriceListId INT = NULL,
	@packageDiscountId INT = NULL,
	@startDate DATETIME = NULL,
	@finishDate datetime = null,
	@value decimal(18,2) = NULL,
	@discount decimal(9,4) = NULL,
	@eachVolume TINYINT = NULL,
	@maxPrimePercent TINYINT = NULL, -- максимальная доля прайма кампании, %; пусто: новый — 100, копия — как у исходного, правка — прежнее
	@sourcePackageDiscountPriceListId INT = NULL,
	@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
begin
SET NOCOUNT on
DECLARE 
	@Id int
IF @actionName = 'Clone'
	SELECT @packageDiscountId = packageDiscountID
	FROM PackageDiscountPriceList
	WHERE packageDiscountPriceListID = @sourcePackageDiscountPriceListId
IF @actionName IN('AddItem', 'UpdateItem', 'Clone') BEGIN
	IF @startDate > @finishDate BEGIN
		RAISERROR('StartFinishDateError', 16, 1)
		RETURN
	end
END
	-- При клонировании @packageDiscountPriceListId — исходный прайс-лист, из проверки его исключать нельзя
	if @actionName in ('AddItem', 'UpdateItem', 'Clone') 
		and exists(select * 
	          from PackageDiscountPriceList pdpl 
		           where pdpl.packageDiscountID = @packageDiscountId and 
				(@actionName = 'Clone' or @packageDiscountPriceListId is null or pdpl.packageDiscountPriceListID <> @packageDiscountPriceListId)
				and (pdpl.startDate <= @finishDate
				and pdpl.finishDate >= @startDate))
	begin
		raiserror('PackageDiscountsCross',16,1)
		return 
	end

	IF @actionName = 'AddItem' BEGIN
		INSERT INTO [PackageDiscountPriceList](packageDiscountId, startDate, finishDate, [value], discount, eachVolume, maxPrimePercent)
		VALUES(@packageDiscountId, @startDate, @finishDate, @value, @discount, @eachVolume, ISNULL(@maxPrimePercent, 100))

		if @@rowcount <> 1
		begin
			raiserror('InternalError', 16, 1)
			return 
		end 

		SET @packageDiscountPriceListId = SCOPE_IDENTITY()
		
		EXEC [PackageDiscountPriceLists] @packageDiscountPriceListId = @packageDiscountPriceListId
	END
	ELSE IF @actionName = 'Clone' BEGIN
		-- Копия прайс-листа пакетной скидки на новый период: те же радиостанции и типы кампаний,
		-- значения (сумма, скидка, процент заполнения, процент прайма) берутся из паспорта.
		IF @packageDiscountId IS NULL
		BEGIN
			raiserror('InternalError', 16, 1)
			return 
		END

		-- Прайс-лист и его радиостанции — одним целым
		SET XACT_ABORT ON
		BEGIN TRANSACTION

		INSERT INTO [PackageDiscountPriceList](packageDiscountId, startDate, finishDate, [value], discount, eachVolume, maxPrimePercent)
		SELECT @packageDiscountId, @startDate, @finishDate, @value, @discount, @eachVolume, ISNULL(@maxPrimePercent, src.maxPrimePercent)
		FROM PackageDiscountPriceList src
		WHERE src.packageDiscountPriceListID = @sourcePackageDiscountPriceListId

		SET @packageDiscountPriceListId = SCOPE_IDENTITY()

		INSERT INTO [PackageDiscountMassmedia](packageDiscountPriceListID, massmediaID, isForType1, isForType2, isForType3)
		SELECT @packageDiscountPriceListId, massmediaID, isForType1, isForType2, isForType3
		FROM PackageDiscountMassmedia
		WHERE packageDiscountPriceListID = @sourcePackageDiscountPriceListId

		COMMIT TRANSACTION

		EXEC [PackageDiscountPriceLists] @packageDiscountPriceListId = @packageDiscountPriceListId
	END
	ELSE IF @actionName = 'DeleteItem' BEGIN
		-- Прайс-лист, по которому уже посчитаны акции, удалять нельзя
		IF EXISTS(SELECT * FROM [Action] WHERE packageDiscountPriceListID = @packageDiscountPriceListId) BEGIN
			raiserror('PackageDiscountInUse', 16, 1)
			return
		END

		DELETE FROM PackageDiscountPriceList WHERE packageDiscountPriceListId = @packageDiscountPriceListId
	END
	ELSE IF @actionName = 'UpdateItem' BEGIN

		UPDATE	
			PackageDiscountPriceList
		SET			
			startDate = @startDate,
			finishDate = @finishDate, 
			[value] = @value,
			discount = @discount,
			eachVolume = @eachVolume,
			maxPrimePercent = ISNULL(@maxPrimePercent, maxPrimePercent) -- старый клиент без поля порог не сбрасывает
		WHERE		
			packageDiscountPriceListId = @packageDiscountPriceListId

		EXEC PackageDiscountPriceLists @packageDiscountPriceListId = @packageDiscountPriceListId
	END
END
GO

CREATE OR ALTER PROCEDURE [dbo].[DiscountChangeAffectedActions]
(
-- Кого заденет правка скидки: акции, в которых уже посчитана эта скидка (Campaign.discountReleaseID,
-- Action.packageDiscountPriceListID) и которым правка может её поменять при следующем пересчёте.
-- Только чтение. Параметры — те же, что у процедуры записи сущности (клиент передаёт параметры объекта).
@entityID int,                          -- 22 набор объёмной скидки, 23 порог набора, 189 пакет, 190 станция пакета, 191 прайс-лист пакета
@actionName varchar(32),                -- AddItem / UpdateItem / DeleteItem
@discountReleaseID int = NULL,
@discountValueID int = NULL,
@packageDiscountPriceListID int = NULL,
@packageDiscountID int = NULL,
@packageDiscountMassmediaID int = NULL,
@massmediaID smallint = NULL,
@startDate datetime = NULL,
@finishDate datetime = NULL,
@isForType1 bit = 0,
@isForType2 bit = 0,
@isForType3 bit = 0,
@summa decimal(18,2) = NULL,
@discount decimal(9,4) = NULL,
@value decimal(18,2) = NULL,
@eachVolume tinyint = NULL,              -- как в PackageDiscountPriceListIUD
@maxPrimePercent tinyint = NULL,         -- как в PackageDiscountPriceListIUD
@count tinyint = NULL,
@languageCode VARCHAR(10) = 'ru'        -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DECLARE @campaigns TABLE (campaignID int PRIMARY KEY)   -- объёмная скидка: по кампаниям
DECLARE @actions TABLE (actionID int PRIMARY KEY)       -- пакетная скидка: по акциям

DECLARE @tConfirmed NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Подтверждена');
DECLARE @tDraft NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Макет');

SET @startDate = CAST(@startDate AS date)
SET @finishDate = CAST(@finishDate AS date)

IF @entityID = 22 AND @actionName = 'UpdateItem'
	-- Даты: кампания выпадает из периода; флаги: снят тип этой кампании
	INSERT INTO @campaigns
	SELECT c.campaignID
	FROM Campaign c
	WHERE c.discountReleaseID = @discountReleaseID
		AND (c.startDate < @startDate OR c.startDate >= DATEADD(DAY, 1, @finishDate)
			OR (c.campaignTypeID = 1 AND @isForType1 = 0)
			OR (c.campaignTypeID = 2 AND @isForType2 = 0)
			OR (c.campaignTypeID = 3 AND @isForType3 = 0))

ELSE IF @entityID = 23 AND @actionName IN ('AddItem', 'UpdateItem', 'DeleteItem')
	-- Любая правка порогов набора; нажатие ОК без изменений не в счёт
	INSERT INTO @campaigns
	SELECT c.campaignID
	FROM Campaign c
	WHERE c.discountReleaseID IN (@discountReleaseID,
			(SELECT discountReleaseID FROM DiscountValue WHERE discountValueID = @discountValueID))
		AND NOT (@actionName = 'UpdateItem' AND EXISTS(
			SELECT * FROM DiscountValue
			WHERE discountValueID = @discountValueID AND discountReleaseID = @discountReleaseID
				AND summa = @summa AND discount = @discount))

ELSE IF @entityID = 191 AND @actionName = 'UpdateItem'
	-- Даты: акция выпадает из периода; значения: любое изменение скидки, порога суммы, процента заполнения;
	-- порог прайма — только понижение ниже доли прайма кампании акции
	INSERT INTO @actions
	SELECT a.actionID
	FROM [Action] a
		JOIN PackageDiscountPriceList pl ON pl.packageDiscountPriceListID = a.packageDiscountPriceListID
	WHERE pl.packageDiscountPriceListID = @packageDiscountPriceListID
		AND (a.startDate < @startDate OR a.startDate > @finishDate
			OR pl.discount <> @discount OR pl.value <> @value OR pl.eachVolume <> @eachVolume
			-- порог прайма понижен, и доля прайма линейной или модульной кампании акции выше нового порога
			OR (@maxPrimePercent < pl.maxPrimePercent AND EXISTS(
				SELECT * FROM Campaign c
				WHERE c.actionID = a.actionID AND c.campaignTypeID IN (1, 3) AND c.primePercent > @maxPrimePercent)))

ELSE IF @entityID = 190 AND @actionName IN ('UpdateItem', 'DeleteItem')
	-- Станция убрана или изменена: задеты акции, у которых есть кампания на этой станции.
	-- Добавление станции посчитанный пакет не ломает (все кампании акции уже совпали с прайс-листом).
	INSERT INTO @actions
	SELECT DISTINCT a.actionID
	FROM PackageDiscountMassmedia pm
		JOIN [Action] a ON a.packageDiscountPriceListID = pm.packageDiscountPriceListID
		JOIN Campaign c ON c.actionID = a.actionID AND c.massmediaID = pm.massmediaID
	WHERE pm.packageDiscountMassmediaID = @packageDiscountMassmediaID
		AND NOT (@actionName = 'UpdateItem'
			AND pm.massmediaID = @massmediaID AND pm.isForType1 = @isForType1
			AND pm.isForType2 = @isForType2 AND pm.isForType3 = @isForType3)

ELSE IF @entityID = 189 AND @actionName = 'UpdateItem'
	-- Число станций пакета — условие применения всех его прайс-листов; имя не в счёт
	INSERT INTO @actions
	SELECT a.actionID
	FROM PackageDiscount pd
		JOIN PackageDiscountPriceList pl ON pl.packageDiscountID = pd.packageDiscountId
		JOIN [Action] a ON a.packageDiscountPriceListID = pl.packageDiscountPriceListID
	WHERE pd.packageDiscountId = @packageDiscountID AND pd.[count] <> @count

SELECT
	ROW_NUMBER() OVER (ORDER BY x.startDate DESC, x.actionID, x.massmedia) AS rowID,
	x.*
FROM (
	SELECT a.actionID, c.campaignID, mm.name AS massmedia, f.name AS firm, u.userName AS manager,
		a.startDate, a.finishDate,
		CASE WHEN a.isConfirmed = 1 THEN @tConfirmed ELSE @tDraft END AS status,
		c.discount
	FROM @campaigns t
		JOIN Campaign c ON c.campaignID = t.campaignID
		JOIN [Action] a ON a.actionID = c.actionID
		LEFT JOIN MassMedia mm ON mm.massmediaID = c.massmediaID
		LEFT JOIN Firm f ON f.firmID = a.firmID
		LEFT JOIN [User] u ON u.userID = a.userID
	WHERE a.deleteDate IS NULL
	UNION ALL
	SELECT a.actionID, NULL, NULL, f.name, u.userName,
		a.startDate, a.finishDate,
		CASE WHEN a.isConfirmed = 1 THEN @tConfirmed ELSE @tDraft END,
		a.discount
	FROM @actions t
		JOIN [Action] a ON a.actionID = t.actionID
		LEFT JOIN Firm f ON f.firmID = a.firmID
		LEFT JOIN [User] u ON u.userID = a.userID
	WHERE a.deleteDate IS NULL
) x
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[DiscountChangeAffectedActions] TO PUBLIC
    AS [dbo];
GO

CREATE OR ALTER PROC [dbo].[Actions1]
(
@actionID int = NULL,
@firmID smallint = NULL,
@startOfInterval datetime = null,
@endOfInterval datetime = null,
@createDateBegin datetime = null, -- Новый параметр
@createDateEnd datetime = null,   -- Новый параметр
@paymentTypeId tinyint = null,
@campaignTypeId tinyint = null,
@campaignFinishDate datetime = null,
@firmId2 smallint = null,
@userID smallint = null,
@changeStartOfInterval datetime = null,
@changeEndOfInterval datetime = null,
@massmediaId smallint = null,
@agencyID smallint = null,
@issueDay datetime = null,
@issueDate datetime = null,
@rollerId smallint = null,
@isHideBlack bit = 0,
@isHideWhite bit = 0,
@paymentTypesIDString varchar(1024) = null,
@agenciesIDString varchar(1024) = NULL,
@withoutActionId INT = NULL,
@isShowActivate BIT = 0,
@isShowNotActivate BIT = 0,
@withoutActionsSince datetime = null,
@showBlack bit = 1,
@showWhite bit = 1,
@moduleID int = null,
@packModuleID int = null,
@loggedUserID smallint = null,
@managerDiscount float = null,
@massmediaGroupID smallint = null,
@showDeleted bit = 0,
@headCompanyID int = null,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT on
	DECLARE @tAction NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Акция №');
	-- Проблема
	-- a.createDate <= @createDateEnd — при @createDateEnd = '2025-05-01 00:00:00' любая акция, созданная 2025-05-01 11:42, 
	--отсекается, потому что 11:42 > 00:00.
	-- Решение — сдвиг границы, а не обрезка колонки
	SET @createDateEnd = DATEADD(DAY, 1, CAST(@createDateEnd AS date));

	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit,@isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id)
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

	declare @headCompaniesWithRecentAction table (headCompanyID int primary key)
	if @withoutActionsSince is not null
	begin
		insert into @headCompaniesWithRecentAction (headCompanyID)
		select distinct f1.headCompanyID
		from [Action] a1
			inner join [Firm] f1 on a1.firmID = f1.firmID
		where f1.headCompanyID is not null
			and a1.isConfirmed = 1
			and a1.finishDate >= @withoutActionsSince
			and (@startOfInterval is null or a1.startDate < @startOfInterval)
	end

	if @actionID is not null
	begin 
		select a.*, 
			us.userName as creator,
			@tAction + LTRIM(a.[actionID]) + ' (' + LTRIM(f.name) + ')'  as name,
			f.name as firmName,
			pkg.name as packageDiscountName,
			coalesce(x.iCount, 0) as iCount,
			coalesce(x.duration, '00:00') as duration,
			Cast(
				Case 
					When a.tariffPrice = 0 Then 1
					Else a.totalPrice/a.tariffPrice
			End  
			as decimal(5,2)) as finalRatio,
			a.startDate,
			a.finishDate,
			-- статистика веера: наибольшая доля прайма среди линейных и модульных кампаний и её станция
			-- (docs/tasks/package-discount-prime-limit.md)
			pm.primePercent as primePercentMax,
			pm.massmediaName as primePercentMaxMassmedia
		from [Action] a
			INNER JOIN [vUser] us ON us.userID = a.userID
			INNER JOIN [Firm] f ON f.firmID = a.firmID
			left join (PackageDiscountPriceList pkgpl
				inner join PackageDiscount pkg on pkg.packageDiscountId = pkgpl.packageDiscountID
				) on pkgpl.packageDiscountPriceListID = a.packageDiscountPriceListID
			left join
			(
				select c.actionID, count(distinct i.issueID) as iCount,
					dbo.fn_Int2Time(coalesce(sum(r.duration), 0)) as duration
				from dbo.Campaign c
					inner join Issue i on c.campaignID = i.campaignID
					inner join Roller r on i.rollerID = r.rollerID
				where c.actionID = @actionID
				group by c.actionID
			) x on a.actionID = x.actionID
			outer apply
			(
				select top 1 c.primePercent, mm.nameWithGroup as massmediaName
				from dbo.Campaign c
					inner join vMassMedia mm on mm.massmediaID = c.massmediaID
				where c.actionID = a.actionID and c.campaignTypeID in (1, 3) and c.primePercent is not null
				order by c.primePercent desc, mm.nameWithGroup
			) pm
		where 
			a.actionID = @actionID 
            -- Фильтр по дате создания
            AND (@createDateBegin IS NULL OR a.createDate >= @createDateBegin)
            AND (@createDateEnd IS NULL OR a.createDate < @createDateEnd)
			and (@headCompanyID is null or f.headCompanyID = @headCompanyID)
			-- Отбор по менеджеру из фильтра журнала: без него обычный менеджер находил
			-- чужую акцию по номеру. Загрузка карточки по номеру (Refresh) передаёт
			-- userID самой акции или не передаёт вовсе — её условие не отсекает.
			and (@userID is null or a.userID = @userID)
			AND (
				(a.[isConfirmed] = 0 AND @isShowNotActivate = 1 And a.deleteDate is null) 
				OR (a.[isConfirmed] = 1 AND @isShowActivate = 1 And a.deleteDate is null) 
				or (a.deleteDate is not null and @showDeleted = 1)
				OR (@isShowNotActivate = 0 And @isShowActivate = 0 And @showDeleted = 0)
				)
	end 
	else if @issueDay is not null or @rollerId is not null or @issueDate is not null or @moduleID is not null or @packModuleID is not null
	begin 
		declare @issues table (actionID int primary key )
		if @moduleID is not null or @packModuleID is not null
		begin
			-- Отбор по модулю / пакетному модулю: сначала выпуски этого модуля (индексы Issue.moduleIssueID /
			-- packModuleIssueID), потом окна и кампании только для них. Общий запрос ниже проходил по всем выпускам
			-- и окнам базы: прод 05.11.2026 — 14–22 с на вызов (agv, модуль 614), копия прода — 10–12 с.
			-- option (loop join): без подсказки оптимизатор ждёт сотни тысяч выпусков модуля и всё равно сканирует Issue
			-- (подсказка у соединения дала бы ту же скорость, но предупреждение «порядок соединения задан принудительно»).
			declare @moduleIssues table (issueID int primary key, actualWindowID int not null, campaignID int not null, rollerID int not null)
			if @moduleID is not null
				insert into @moduleIssues (issueID, actualWindowID, campaignID, rollerID)
				select i.issueID, i.actualWindowID, i.campaignID, i.rollerID
				from ModuleIssue mi
					inner join Issue i on i.moduleIssueID = mi.moduleIssueID
				where mi.moduleID = @moduleID
					and (@packModuleID is null or exists(select 1 from PackModuleIssue pmi
							inner join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
						where pmi.packModuleIssueID = i.packModuleIssueID and pmpl.packModuleID = @packModuleID))
				option (loop join)
			else
				insert into @moduleIssues (issueID, actualWindowID, campaignID, rollerID)
				select i.issueID, i.actualWindowID, i.campaignID, i.rollerID
				from PackModulePriceList pmpl
					inner join PackModuleIssue pmi on pmi.pricelistID = pmpl.priceListID
					inner join Issue i on i.packModuleIssueID = pmi.packModuleIssueID
				where pmpl.packModuleID = @packModuleID
				option (loop join)

			insert into @issues
			select distinct c.actionID
			from @moduleIssues i
				inner join TariffWindow tw on i.actualWindowID = tw.windowId
				inner join Campaign c on i.campaignID = c.campaignID
				Inner Join MassMedia mm On mm.massmediaID = tw.massmediaID
			where i.rollerID = coalesce(@rollerId, i.rollerID)
				and (@issueDate is null or ((datepart(hh, tw.windowDateActual) = datepart(hh, @issueDate)) and (datepart(minute, tw.windowDateActual) = datepart(minute, @issueDate))) )
				and (@issueDay is null or (@issueDay is not null and (tw.dayOriginal = @issueDay)) )
				and mm.massmediaGroupID = Coalesce(@massmediaGroupID, mm.massmediaGroupID)
				and tw.massmediaID = Coalesce(@massmediaId, tw.massmediaId)
		end
		else
		insert into @issues
		select distinct c.actionID 
		from Issue i 
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
			inner join Campaign c on i.campaignID = c.campaignID
			Inner Join MassMedia mm On mm.massmediaID = tw.massmediaID
		where i.rollerID = coalesce(@rollerId, i.rollerID)
			and (@issueDate is null or ((datepart(hh, tw.windowDateActual) = datepart(hh, @issueDate)) and (datepart(minute, tw.windowDateActual) = datepart(minute, @issueDate))) )
			and (@issueDay is null or (@issueDay is not null and (tw.dayOriginal = @issueDay)) )
			and mm.massmediaGroupID = Coalesce(@massmediaGroupID, mm.massmediaGroupID)
			and tw.massmediaID = Coalesce(@massmediaId, tw.massmediaId)
										
		SELECT distinct 
			a.*, 
			us.userName as creator,
			--'Акция №' + LTRIM(a.[actionID]) + ' (' + LTRIM(f.name) + ')'  as name,
			@tAction + LTRIM(a.[actionID]) as name,
			f.name as firmName,
			pkg.name as packageDiscountName,
			Cast(
			Case 
				When a.tariffPrice = 0 Then 1
				Else a.totalPrice/a.tariffPrice
			End  
			as decimal(5,2)) as finalRatio,
			a.startDate,
			a.finishDate
		FROM 
			[Action] a
			inner join @issues i on i.actionID = a.actionID
			Inner Join Campaign c ON c.actionId = a.actionId
			Inner Join PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
			INNER JOIN [Agency] ag ON c.[agencyID] = ag.[agencyID]
			INNER JOIN [vUser] us ON us.userID = a.userID
			INNER JOIN [Firm] f ON f.firmID = a.firmID
			left join (PackageDiscountPriceList pkgpl
				inner join PackageDiscount pkg on pkg.packageDiscountId = pkgpl.packageDiscountID
				) on pkgpl.packageDiscountPriceListID = a.packageDiscountPriceListID
			left join @massmedias umm on c.massmediaID = umm.massmediaID
			left join GroupMember gm on us.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id
		WHERE	
			(us.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
			a.isSpecial = 0 and	
			a.finishDate >= Coalesce(@startOfInterval, a.finishDate) And
			a.startDate <= Coalesce(@endOfInterval, a.startDate) And
            -- Фильтр по дате создания
            (@createDateBegin IS NULL OR a.createDate >= @createDateBegin) AND
            (@createDateEnd IS NULL OR a.createDate < @createDateEnd) AND
			c.paymentTypeId = Coalesce(@paymentTypeId, c.paymentTypeId) And
			c.campaignTypeId = Coalesce(@campaignTypeId, c.campaignTypeId) And
			c.finishDate = Coalesce(@campaignFinishDate, c.finishDate) And
			a.firmId = Coalesce(@firmId2, a.firmId) And
			a.userId = Coalesce(@userId, a.userId) And
			a.modDate >= Coalesce(@changeStartOfInterval, a.modDate) And
			a.modDate <= Coalesce(@changeEndOfInterval, a.modDate) And
			((c.[agencyID] IS NULL AND @agencyID IS NULL) OR c.agencyId = Coalesce(@agencyID, c.agencyId)) And
			a.actionId = Coalesce(@actionId, a.actionId) And
			(pt.isHidden = 0 or @isHideWhite = 0) And
			(pt.isHidden = 1 or @isHideBlack = 0) and
			((pt.IsHidden = 1 and @showBlack = 1)  or
			(pt.IsHidden = 0 and @showWhite = 1)) and
			a.[actionID] = COALESCE(@actionID, a.[actionID]) AND
			a.[firmID] = COALESCE(@firmID, a.[firmID]) 
			AND (
				(a.[isConfirmed] = 0 AND @isShowNotActivate = 1 And a.deleteDate is null) 
				OR (a.[isConfirmed] = 1 AND @isShowActivate = 1 And a.deleteDate is null) 
				or (a.deleteDate is not null and @showDeleted = 1)
				)
			AND (@withoutActionId IS NULL OR a.[actionID] <> @withoutActionId)
			and (@withoutActionsSince is null or not exists(select 1 from @headCompaniesWithRecentAction h where h.headCompanyID = f.headCompanyID))
			and (@managerDiscount is null or (c.managerDiscount - @managerDiscount) < -0.005)
			and (@headCompanyID is null or f.headCompanyID = @headCompanyID)
		order by a.actionID desc
	end
	else 
		Begin
		SELECT distinct 
			a.*, 
			us.userName as creator,
			--'Акция №' + LTRIM(a.[actionID]) + ' (' + LTRIM(f.name) + ')'  as name,
			@tAction + LTRIM(a.[actionID]) as name,
			f.name as firmName,
			pkg.name as packageDiscountName,
			Cast(
			Case 
				When a.tariffPrice = 0 Then 1
				Else a.totalPrice/a.tariffPrice
			End  
			as decimal(5,2)) as finalRatio,
			a.startDate,
			a.finishDate
		FROM 
			[Action] a
			Inner Join Campaign c ON c.actionId = a.actionId
			Inner Join PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
			INNER JOIN [User] us ON us.userID = a.userID
			INNER JOIN [Firm] f ON f.firmID = a.firmID
			left join (PackageDiscountPriceList pkgpl
				inner join PackageDiscount pkg on pkg.packageDiscountId = pkgpl.packageDiscountID
				) on pkgpl.packageDiscountPriceListID = a.packageDiscountPriceListID
			LEFT JOIN (
				PackModuleIssue i 
				JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
				JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
				JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
				) ON i.campaignID = c.campaignID
		where
			(a.userID = @loggedUserID 
						or @isRightToViewForeignActions = 1 
						or (
							@isRightToViewGroupActions = 1 
							AND EXISTS (
								SELECT 1 
								FROM GroupMember gm 
									JOIN fn_GetUserGroups(@loggedUserID) ug on gm.groupID = ug.id
								WHERE a.userID = gm.userID
								)
							)
						)
			and EXISTS (
					SELECT 1 
					FROM @massmedias umm 
					WHERE umm.massmediaID = CASE WHEN c.campaignTypeID=4 THEN m.massmediaID ELSE c.massmediaID END
							and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
					) 
			AND (@massmediaGroupID IS NULL 
					OR 
					EXISTS (
						SELECT 1
						FROM MassMedia mm
						WHERE mm.massmediaID = CASE WHEN c.campaignTypeID=4 THEN m.massmediaID ELSE c.massmediaID END
							AND mm.massmediaGroupID = @massmediaGroupID
						)
					)
			and	a.isSpecial = 0 and		
			(a.finishDate >= Coalesce(@startOfInterval, a.finishDate) Or (a.finishDate Is Null And @startOfInterval Is Null )) And
			(a.startDate <= Coalesce(@endOfInterval, a.startDate) Or (a.startDate Is Null And @endOfInterval Is Null ))  And
            -- Фильтр по дате создания
            (@createDateBegin IS NULL OR a.createDate >= @createDateBegin) AND
            (@createDateEnd IS NULL OR a.createDate < @createDateEnd) AND
			c.paymentTypeId = Coalesce(@paymentTypeId, c.paymentTypeId) And
			c.campaignTypeId = Coalesce(@campaignTypeId, c.campaignTypeId) And 
			c.finishDate = Coalesce(@campaignFinishDate, c.finishDate) And
			a.firmId = Coalesce(@firmId2, a.firmId) And
			a.userId = Coalesce(@userId, a.userId) And
			a.modDate >= Coalesce(@changeStartOfInterval, a.modDate) And
			a.modDate <= Coalesce(@changeEndOfInterval, a.modDate)
			and (c.massmediaID = Coalesce(@massmediaId, c.massmediaId) Or c.massmediaID Is Null)	
			and (m.massmediaID = Coalesce(@massmediaId, m.massmediaID) Or m.massmediaID Is Null)
			and ((c.[agencyID] IS NULL AND @agencyID IS NULL) OR c.agencyId = Coalesce(@agencyID, c.agencyId)) And
			a.actionId = Coalesce(@actionId, a.actionId) And
			(pt.isHidden = 0 or @isHideWhite = 0) And
			(pt.isHidden = 1 or @isHideBlack = 0) and
			((pt.IsHidden = 1 and @showBlack = 1)  or
			(pt.IsHidden = 0 and @showWhite = 1)) and
			a.[actionID] = COALESCE(@actionID, a.[actionID]) AND
			a.[firmID] = COALESCE(@firmID, a.[firmID])
			AND (
				(a.[isConfirmed] = 0 AND @isShowNotActivate = 1 And a.deleteDate is null) 
				OR (a.[isConfirmed] = 1 AND @isShowActivate = 1 And a.deleteDate is null) 
				or (a.deleteDate is not null and @showDeleted = 1)
				)
			AND (@withoutActionId IS NULL OR a.[actionID] <> @withoutActionId)
			and (@withoutActionsSince is null or not exists(select 1 from @headCompaniesWithRecentAction h where h.headCompanyID = f.headCompanyID))
			and (@managerDiscount is null or (c.managerDiscount - @managerDiscount) < -0.005)
			and (@headCompanyID is null or f.headCompanyID = @headCompanyID)
		order by a.actionID desc
		End
GO

-- =====================================================================
-- 5. Паспорт (iEntity 191): поле после «Процент заполнения (%):»; колонка списка; испанский
-- =====================================================================
DECLARE @after nvarchar(200) = N'name="eachVolume" min="0" max="100"/>';
DECLARE @name  nvarchar(100) = N'name="maxPrimePercent"';
DECLARE @p     nvarchar(max) = (SELECT CAST(passport AS nvarchar(max)) COLLATE Latin1_General_BIN2 FROM dbo.iEntity WHERE entityID = 191);
DECLARE @pos   int = CHARINDEX(@after COLLATE Latin1_General_BIN2, @p);

IF CHARINDEX(@name COLLATE Latin1_General_BIN2, ISNULL(@p, N'')) > 0
    PRINT N'Поле «Максимальный процент прайма (%):» в паспорте прайс-листа пакетной скидки уже есть.';
ELSE IF @p IS NULL OR @pos = 0
    RAISERROR(N'97: в паспорте прайс-листа пакетной скидки (iEntity 191) нет поля «Процент заполнения (%):» в ожидаемом виде. Паспорт не изменён.', 16, 1);
ELSE
BEGIN
    DECLARE @at int = @pos + LEN(@after);
    -- перевод строки после поля такой же, как в паспорте (CRLF или LF)
    DECLARE @eol nvarchar(2) = CASE WHEN SUBSTRING(@p, @at, 2) = NCHAR(13) + NCHAR(10) THEN NCHAR(13) + NCHAR(10) ELSE NCHAR(10) END;
    DECLARE @np nvarchar(max) = STUFF(@p, @at, 0,
        @eol + NCHAR(9) + NCHAR(9) + N'<field caption="Максимальный процент прайма (%):" name="maxPrimePercent" min="0" max="100"/>');

    IF TRY_CAST(@np AS xml) IS NULL
        RAISERROR(N'97: после вставки поля XML паспорта стал невалидным. Паспорт не изменён.', 16, 1);
    ELSE
    BEGIN
        UPDATE dbo.iEntity SET passport = @np WHERE entityID = 191;
        PRINT N'Поле «Максимальный процент прайма (%):» добавлено в паспорт прайс-листа пакетной скидки.';
    END
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 191 AND name = 'maxPrimePercent')
BEGIN
    INSERT INTO dbo.iEntityAttribute (entityID, alias, name, ordinal_position, selector, dataType)
    VALUES (191, N'Максимальный процент прайма', 'maxPrimePercent', 25, 0, NULL);
    PRINT N'Колонка «Максимальный процент прайма» добавлена в список прайс-листов пакетной скидки.';
END
GO

IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (VALUES
        ('es', '', N'Максимальный процент прайма (%):', N'Porcentaje máximo de prime (%):'),
        ('es', '', N'Максимальный процент прайма', N'Porcentaje máximo de prime'),
        ('es', '', N'Прайм', N'Prime'),
        ('es', '', N'Прайм, макс.', N'Prime, máx.')
    ) AS src (lang, context, [source], [text])
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);
GO

-- =====================================================================
-- 6. Доля прайма существующих линейных и модульных кампаний — порциями по 500 акций
-- =====================================================================
DECLARE @from int = 0, @to int, @done int = 0, @batches int = 0, @t datetime2 = SYSDATETIME();
CREATE TABLE #ActionIssue
(
    campaignID INT NOT NULL,
    campaignTypeID TINYINT NOT NULL,
    massmediaID SMALLINT NOT NULL,
    dayActual SMALLDATETIME NOT NULL,
    dayOriginal SMALLDATETIME NOT NULL,
    price DECIMAL(18,2) NOT NULL,
    tariffPrice DECIMAL(18,2) NOT NULL,
    duration INT NOT NULL
);
CREATE TABLE #ActionPrime (campaignID INT NOT NULL PRIMARY KEY, primePercent DECIMAL(4,1) NOT NULL);
WHILE 1 = 1
BEGIN
    SET @to = NULL;
    SELECT @to = MAX(t.actionID)
    FROM (SELECT DISTINCT TOP (500) c.actionID
          FROM dbo.Campaign c
          WHERE c.actionID > @from AND c.campaignTypeID IN (1, 3) AND c.primePercent IS NULL AND c.issuesCount > 0
          ORDER BY c.actionID) t;
    IF @to IS NULL BREAK;

    TRUNCATE TABLE #ActionIssue;
    TRUNCATE TABLE #ActionPrime;
    -- выпуски — как в ActionRecalculate (Phase 1.0), только по кампаниям без доли
    INSERT INTO #ActionIssue (campaignID, campaignTypeID, massmediaID, dayActual, dayOriginal, price, tariffPrice, duration)
    SELECT i.campaignID, c.campaignTypeID, tw.massmediaID, tw.dayActual, tw.dayOriginal, tw.price, i.tariffPrice, r.duration
    FROM dbo.Campaign c
        INNER JOIN dbo.Issue i ON i.campaignID = c.campaignID
        INNER JOIN dbo.TariffWindow tw ON tw.windowId = i.actualWindowID
        INNER JOIN dbo.Roller r ON r.rollerID = i.rollerID
    WHERE c.actionID > @from AND c.actionID <= @to AND c.campaignTypeID IN (1, 3) AND c.primePercent IS NULL;

    EXEC dbo.hlp_ActionCampaignPrimePercent;

    UPDATE c
    SET c.primePercent = p.primePercent
    FROM #ActionPrime p
        INNER JOIN dbo.Campaign c ON c.campaignID = p.campaignID
    WHERE c.primePercent IS NULL;

    SELECT @done += @@ROWCOUNT, @batches += 1, @from = @to;
END
DROP TABLE #ActionIssue;
DROP TABLE #ActionPrime;
PRINT CONCAT(N'Доля прайма заполнена у ', @done, N' кампаний (порций: ', @batches, N', ', DATEDIFF(SECOND, @t, SYSDATETIME()), N' с).');
GO

-- =====================================================================
-- Проверка
-- =====================================================================
IF COL_LENGTH('dbo.PackageDiscountPriceList', 'maxPrimePercent') IS NOT NULL
   AND COL_LENGTH('dbo.Campaign', 'primePercent') IS NOT NULL
   AND OBJECT_ID(N'dbo.hlp_ActionCampaignPrimePercent') IS NOT NULL
   AND OBJECT_ID(N'dbo.f_ActionCampaignPrimePercent') IS NULL
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionRecalculate')) LIKE N'%hlp_ActionCampaignPrimePercent%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_ActionDiscountCalculate')) LIKE N'%maxPrimePercent%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.PackageDiscountPriceListIUD')) LIKE N'%@maxPrimePercent%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.DiscountChangeAffectedActions')) LIKE N'%@maxPrimePercent%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.Actions1')) LIKE N'%primePercentMax%'
   AND (SELECT CAST(passport AS nvarchar(max)) FROM dbo.iEntity WHERE entityID = 191) LIKE N'%name="maxPrimePercent"%'
   AND EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 191 AND name = 'maxPrimePercent')
    PRINT N'ГОТОВО: максимальный процент прайма в пакетной скидке — колонки, процедуры, паспорт, колонка списка.';
ELSE
    RAISERROR(N'97: применено не полностью (колонки/процедуры/паспорт/колонка списка).', 16, 1);
GO
