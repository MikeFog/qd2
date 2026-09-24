/*
    ПРОД-ДЕПЛОЙ: запоминать применённую скидку и предупреждать о затронутых акциях при её правке.
    Задача: docs/tasks/discount-applied-pricelist-id.md.

    ЧТО ДЕЛАЕТ
      1. Схема: Campaign.discountReleaseID (набор объёмной скидки) и Action.packageDiscountPriceListID
         (прайс-лист пакетной скидки) — NULL, FK без каскада, индексы. Старые строки НЕ заполняются:
         ID появится у акции при её следующем пересчёте.
      2. hlp_CompanyDiscountCalculate / hlp_ActionDiscountCalculate — дополнительно возвращают ID
         найденной скидки (значение скидки прежнее; порог объёмной — явный ORDER BY summa DESC,
         ничья пакетных — меньший ID).
      3. ActionRecalculate — пишет эти ID теми же UPDATE, что и скидку.
      4. Удаление использованной скидки — жёсткий отказ: DiscountReleaseIUD (DiscountReleaseInUse),
         PackageDiscountPriceListIUD и PackageDiscountIUD (PackageDiscountInUse). Из
         PackageDiscountPriceListIUD DeleteItem убран хвост старой схемы (подгонка даты соседа).
      5. DiscountChangeAffectedActions — список акций, которые задевает правка скидки (для окна в qd2).
      6. Метаданные: сообщения iMessage; сущности 23 (порог) и 190 (станция пакета) переключены на
         классы Merlin.Classes.DiscountValue / PackageDiscountMassmedia.

    ПРЕДУСЛОВИЕ     накачен discount-release-finish-date-deploy.sql (DiscountRelease.finishDate NOT NULL).
    КЛИЕНТ          нужен новый Merlin.exe; после наката клиенты qd2 перезапустить (iMessage и классы
                    сущностей читаются при старте). Старый клиент работает, но без окна-предупреждения.
    КОГДА           лучше вне рабочего времени: ALTER TABLE Action/Campaign берёт кратковременную
                    блокировку схемы, индексы строятся офлайн (Express).
    ИДЕМПОТЕНТНОСТЬ повторный запуск безопасен.
    ОТКАТ           процедуры — из предыдущего коммита; DROP INDEX / FK / колонок; iEntity 23 и 190:
                    className = 'FogSoft.WinForm.Classes.PresentationObject' (assemblyName 23 — NULL,
                    190 — 'FogSoft.WinForm').
*/

-- USE [Artvis];
-- GO

SET NOCOUNT ON;
-- sqlcmd по умолчанию создаёт процедуры с QUOTED_IDENTIFIER OFF — задаём явно, как у процедур на проде
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID('dbo.ActionRecalculate') IS NULL OR OBJECT_ID('dbo.PackageDiscountPriceList') IS NULL
   OR OBJECT_ID('dbo.DiscountRelease') IS NULL
BEGIN
    RAISERROR('НЕ ТА БАЗА: нет ActionRecalculate / PackageDiscountPriceList / DiscountRelease. Деплой прерван.', 16, 1);
    SET NOEXEC ON;
END
ELSE IF COLUMNPROPERTY(OBJECT_ID('dbo.DiscountRelease'), 'finishDate', 'AllowsNull') = 1
BEGIN
    RAISERROR('Сначала накатите discount-release-finish-date-deploy.sql (явная дата окончания скидок). Деплой прерван.', 16, 1);
    SET NOEXEC ON;
END
GO
-- 1. Схема
IF COL_LENGTH('dbo.Action', 'packageDiscountPriceListID') IS NULL
    ALTER TABLE dbo.[Action] ADD packageDiscountPriceListID INT NULL;
GO
IF OBJECT_ID('dbo.FK_Action_PackageDiscountPriceList') IS NULL
    ALTER TABLE dbo.[Action] ADD CONSTRAINT FK_Action_PackageDiscountPriceList
        FOREIGN KEY (packageDiscountPriceListID) REFERENCES dbo.PackageDiscountPriceList (packageDiscountPriceListID);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Action') AND name = 'IX_Action_packageDiscountPriceListID')
    CREATE NONCLUSTERED INDEX IX_Action_packageDiscountPriceListID ON dbo.[Action] (packageDiscountPriceListID);
GO
IF COL_LENGTH('dbo.Campaign', 'discountReleaseID') IS NULL
    ALTER TABLE dbo.Campaign ADD discountReleaseID SMALLINT NULL;
GO
IF OBJECT_ID('dbo.FK_Campaign_DiscountRelease') IS NULL
    ALTER TABLE dbo.Campaign ADD CONSTRAINT FK_Campaign_DiscountRelease
        FOREIGN KEY (discountReleaseID) REFERENCES dbo.DiscountRelease (discountReleaseID);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Campaign') AND name = 'IX_Campaign_discountReleaseID')
    CREATE NONCLUSTERED INDEX IX_Campaign_discountReleaseID ON dbo.Campaign (discountReleaseID);
GO
-- 2–5. Процедуры
CREATE OR ALTER PROCEDURE [dbo].[hlp_CompanyDiscountCalculate]
(
@massMediaID smallint,
@campaignTypeID tinyint,
@startDate datetime,
@tariffPrice decimal(18,2),
@discountValue decimal(9,4) output,
@discountReleaseID smallint = NULL output -- какой набор скидок дал скидку (NULL — ни один порог не пройден)
)
as
SET NOCOUNT on
select @discountValue = NULL, @discountReleaseID = NULL

-- Порог — наибольшая сумма, до которой дотягивает кампания
Select TOP 1
	@discountValue = dv.discount,
	@discountReleaseID = dr.discountReleaseID
From		
	DiscountRelease dr 
	Inner Join DiscountValue dv On dv.discountReleaseId = dr.discountReleaseId
Where	
	dr.[massmediaID] = @massMediaID and
	@startDate >= dr.startDate AND 
	@startDate < DATEADD(DAY, 1, dr.finishDate) and
	dv.summa <= @tariffPrice 
	AND 
	(
	(dr.[isForType1] = 1 And @campaignTypeID = 1)
	Or 	(dr.[isForType2] = 1 And @campaignTypeID = 2)
	Or 	(dr.[isForType3] = 1 And @campaignTypeID = 3)
	)
Order By
	dv.summa DESC
Set	@DiscountValue = IsNull(@DiscountValue, 1)
GO

CREATE OR ALTER PROCEDURE [dbo].[hlp_ActionDiscountCalculate]
(
@actionID int,
@startDate datetime,
@discountValue decimal(9,4) output,
@packageDiscountPriceListID int = NULL output -- какой прайс-лист дал скидку (NULL — ни один)
)
AS
SET NOCOUNT on

DECLARE @avgDuration float, @campaignsCount tinyint, @priceByCampaigns decimal(18,2)

Select @priceByCampaigns = Sum([price]) From Campaign where actionID = @actionID

-- ИСПРАВЛЕНО: COUNT(*) вместо COUNT(DISTINCT c.massmediaID)
-- Это гарантирует, что каждая кампания (включая тип 3) учитывается отдельно
-- ИСПРАВЛЕНО: пустые кампании (без единого размещения) в расчёте пакета не участвуют -
-- иначе они тянут вниз avgDuration и завышают @campaignsCount, срывая пакетную скидку.
-- Предикат тот же, что ActionRecalculate использует для oldTotalCount.
SELECT @avgDuration=AVG(CAST(c.issuesDuration AS float)), @campaignsCount=COUNT(*)
FROM Campaign c
WHERE c.actionID=@actionID and c.campaignTypeID < 4
	and (ISNULL(c.issuesCount, 0) + ISNULL(c.programsCount, 0)) > 0

SELECT @avgDuration=COALESCE(@avgDuration,0), @campaignsCount=COALESCE(@campaignsCount,0)

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
														AND CAST(c.issuesDuration as float) >= @avgDuration*p.eachVolume/100
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

CREATE OR ALTER PROCEDURE [dbo].[ActionRecalculate]
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
       Phase 1A. Type 1
       ========================= */
    ;WITH Type1Agg AS
    (
        SELECT
            i.campaignID,
            tariffPrice   = SUM(i.tariffPrice),
            issuesDuration = SUM(r.duration),
            issuesCount   = COUNT(*),
            startDate     = MIN(tw.dayOriginal),
            finishDate    = MAX(tw.dayOriginal)
        FROM dbo.Issue i
        INNER JOIN dbo.TariffWindow tw ON tw.windowId = i.originalWindowID
        INNER JOIN dbo.Roller r ON r.rollerID = i.rollerID
        INNER JOIN #CampaignPhase1 p ON p.campaignID = i.campaignID AND p.campaignTypeID = 1
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
            startDate    = MIN(DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate))),
            finishDate   = MAX(DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate))),
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
            issueStartDate = MIN(tw.dayOriginal),
            issueFinishDate = MAX(tw.dayOriginal)
        FROM dbo.Issue i
        INNER JOIN dbo.TariffWindow tw ON tw.windowId = i.originalWindowID
        INNER JOIN dbo.Tariff t ON t.tariffID = tw.tariffId
        INNER JOIN dbo.Pricelist pl ON pl.pricelistID = t.pricelistID
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
            issuesDuration = SUM(r.duration),
            issuesCount = COUNT(*)
        FROM dbo.Issue i
        INNER JOIN dbo.Roller r ON r.rollerID = i.rollerID
        INNER JOIN #CampaignPhase1 p ON p.campaignID = i.campaignID AND p.campaignTypeID = 3
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
        c.discountReleaseID = p.discountReleaseID
    FROM dbo.Campaign c
    INNER JOIN #CampaignPhase1 p ON p.campaignID = c.campaignID;

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

CREATE OR ALTER PROCEDURE [dbo].[DiscountReleaseIUD]
(
@discountReleaseID smallint = NULL,
@massmediaID smallint = NULL,
@startDate datetime = NULL,
@finishDate datetime = NULL,
@isForType1 bit = 0,
@isForType2 bit = 0,
@isForType3 bit = 0,
@sourceDiscountReleaseID smallint = NULL,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
as
set nocount on

-- Набор скидок действует с startDate по finishDate включительно, обе даты задаются явно
-- (как у прайс-листов). Соседние наборы не подгоняются, периоды одной радиостанции
-- пересекаться не могут.
IF @actionName = 'Clone'
	SELECT @massmediaID = massmediaID FROM DiscountRelease WHERE discountReleaseID = @sourceDiscountReleaseID
ELSE IF @actionName = 'UpdateItem'
	SELECT @massmediaID = massmediaID FROM DiscountRelease WHERE discountReleaseID = @discountReleaseID

IF @actionName IN ('AddItem', 'UpdateItem', 'Clone') BEGIN
	IF @massmediaID IS NULL OR @startDate IS NULL OR @finishDate IS NULL BEGIN
		raiserror('InternalError', 16, 1)
		return
	END

	SET @startDate = CAST(@startDate AS date)
	SET @finishDate = CAST(@finishDate AS date)

	IF @startDate > @finishDate BEGIN
		raiserror('StartFinishDateError', 16, 1)
		return
	END

	IF EXISTS(
		SELECT * FROM DiscountRelease
		WHERE
			massmediaID = @massmediaID AND
			startDate <= @finishDate AND
			finishDate >= @startDate AND
			(@actionName <> 'UpdateItem' OR discountReleaseID <> @discountReleaseID)
		) BEGIN
		raiserror('PLPeriodIntersection', 16, 1)
		return
	END
END

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [DiscountRelease](massmediaID, startDate, finishDate, isForType1, isForType2, isForType3)
	VALUES(@massmediaID, @startDate, @finishDate, @isForType1, @isForType2, @isForType3)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return
	end

	SET @DiscountReleaseID = SCOPE_IDENTITY()

	EXEC DiscountReleases @discountReleaseID = @discountReleaseID
END
ELSE IF @actionName = 'Clone' BEGIN
	-- Копия набора скидок радиостанции на новый период:
	-- те же суммы и проценты (DiscountValue), даты и флаги типов кампаний берутся из паспорта.
	SET XACT_ABORT ON
	BEGIN TRANSACTION

	INSERT INTO [DiscountRelease](massmediaID, startDate, finishDate, isForType1, isForType2, isForType3)
	VALUES(@massmediaID, @startDate, @finishDate, @isForType1, @isForType2, @isForType3)

	SET @DiscountReleaseID = SCOPE_IDENTITY()

	INSERT INTO [DiscountValue](discountReleaseID, summa, discount)
	SELECT @DiscountReleaseID, summa, discount
	FROM DiscountValue
	WHERE discountReleaseID = @sourceDiscountReleaseID

	COMMIT TRANSACTION

	EXEC DiscountReleases @discountReleaseID = @discountReleaseID
END
ELSE IF @actionName = 'DeleteItem' BEGIN
	-- Набор, по которому уже посчитаны кампании, удалять нельзя
	IF EXISTS(SELECT * FROM Campaign WHERE discountReleaseID = @discountReleaseID) BEGIN
		raiserror('DiscountReleaseInUse', 16, 1)
		return
	END

	DELETE FROM [DiscountRelease] WHERE DiscountReleaseID = @DiscountReleaseID
END
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE
		[DiscountRelease]
	SET
		startDate = @startDate,
		finishDate = @finishDate,
		isForType1 = @isForType1,
		isForType2 = @isForType2,
		isForType3 = @isForType3
	WHERE
		discountReleaseID = @discountReleaseID

	EXEC DiscountReleases @discountReleaseID = @discountReleaseID

END
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
		INSERT INTO [PackageDiscountPriceList](packageDiscountId, startDate, finishDate, [value], discount, eachVolume)
		VALUES(@packageDiscountId, @startDate, @finishDate, @value, @discount, @eachVolume)

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
		-- значения (сумма, скидка, процент заполнения) берутся из паспорта.
		IF @packageDiscountId IS NULL
		BEGIN
			raiserror('InternalError', 16, 1)
			return 
		END

		-- Прайс-лист и его радиостанции — одним целым
		SET XACT_ABORT ON
		BEGIN TRANSACTION

		INSERT INTO [PackageDiscountPriceList](packageDiscountId, startDate, finishDate, [value], discount, eachVolume)
		VALUES(@packageDiscountId, @startDate, @finishDate, @value, @discount, @eachVolume)

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
			eachVolume = @eachVolume
		WHERE		
			packageDiscountPriceListId = @packageDiscountPriceListId

		EXEC PackageDiscountPriceLists @packageDiscountPriceListId = @packageDiscountPriceListId
	END
END
GO

-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 31.01.2008
-- Description:	<Description,,>
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[PackageDiscountIUD]
(
	@packageDiscountId INT = NULL,
	@name NVARCHAR(32) = NULL,
	@count TINYINT = null,
	@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

    IF @actionName = 'AddItem'
    BEGIN
		INSERT INTO PackageDiscount([NAME], [count]) 
			VALUES(@name, @count)
		
		if @@rowcount <> 1
		begin
			raiserror('InternalError', 16, 1)
			return 
		end 

		SET @packageDiscountId = SCOPE_IDENTITY()
		
		EXEC PackageDiscounts @packageDiscountId
	END
	ELSE IF @actionName = 'UpdateItem'
	BEGIN
		UPDATE PackageDiscount
		SET [NAME] = @name, [count] = @count
		WHERE packageDiscountID = @packageDiscountId
		
		EXEC PackageDiscounts @packageDiscountId
	END
	ELSE IF @actionName = 'DeleteItem'
	BEGIN
		-- Пакет удаляется вместе с прайс-листами; если по любому из них посчитаны акции — нельзя
		IF EXISTS(SELECT * FROM [Action] a
		          JOIN PackageDiscountPriceList pl ON pl.packageDiscountPriceListID = a.packageDiscountPriceListID
		          WHERE pl.packageDiscountID = @packageDiscountId)
		BEGIN
			raiserror('PackageDiscountInUse', 16, 1)
			return
		END

		DELETE FROM PackageDiscount WHERE packageDiscountID = @packageDiscountId
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
@count tinyint = NULL
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DECLARE @campaigns TABLE (campaignID int PRIMARY KEY)   -- объёмная скидка: по кампаниям
DECLARE @actions TABLE (actionID int PRIMARY KEY)       -- пакетная скидка: по акциям

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
	-- Даты: акция выпадает из периода; значения: любое изменение скидки, порога суммы, процента заполнения
	INSERT INTO @actions
	SELECT a.actionID
	FROM [Action] a
		JOIN PackageDiscountPriceList pl ON pl.packageDiscountPriceListID = a.packageDiscountPriceListID
	WHERE pl.packageDiscountPriceListID = @packageDiscountPriceListID
		AND (a.startDate < @startDate OR a.startDate > @finishDate
			OR pl.discount <> @discount OR pl.value <> @value OR pl.eachVolume <> @eachVolume)

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
		CASE WHEN a.isConfirmed = 1 THEN N'Подтверждена' ELSE N'Макет' END AS status,
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
		CASE WHEN a.isConfirmed = 1 THEN N'Подтверждена' ELSE N'Макет' END,
		a.discount
	FROM @actions t
		JOIN [Action] a ON a.actionID = t.actionID
		LEFT JOIN Firm f ON f.firmID = a.firmID
		LEFT JOIN [User] u ON u.userID = a.userID
	WHERE a.deleteDate IS NULL
) x
GO
GRANT EXECUTE ON OBJECT::[dbo].[DiscountChangeAffectedActions] TO PUBLIC AS [dbo];
GO
-- 6. Метаданные
IF NOT EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'DiscountReleaseInUse')
    INSERT INTO dbo.iMessage (name, message)
    VALUES ('DiscountReleaseInUse', N'По этому набору скидок уже посчитаны кампании — удалить его нельзя. Если нужна другая скидка, создайте копию набора и измените её. Операция прервана.');
IF NOT EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'PackageDiscountInUse')
    INSERT INTO dbo.iMessage (name, message)
    VALUES ('PackageDiscountInUse', N'По этой пакетной скидке уже посчитаны акции — удалить её нельзя. Если нужна другая скидка, создайте копию прайс-листа и измените её. Операция прервана.');

UPDATE dbo.iEntity SET className = 'Merlin.Classes.DiscountValue', assemblyName = 'Merlin'
WHERE entityID = 23 AND tableName = 'DiscountValue'
  AND (className <> 'Merlin.Classes.DiscountValue' OR ISNULL(assemblyName, '') <> 'Merlin');
UPDATE dbo.iEntity SET className = 'Merlin.Classes.PackageDiscountMassmedia', assemblyName = 'Merlin'
WHERE entityID = 190 AND tableName = 'PackageDiscountMassmedia'
  AND (className <> 'Merlin.Classes.PackageDiscountMassmedia' OR ISNULL(assemblyName, '') <> 'Merlin');
GO
SET NOEXEC OFF;
GO
-- Контроль: всё = ожидаемому
SELECT 'Action.packageDiscountPriceListID' AS item, COUNT(*) AS actual, '1' AS expected FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Action') AND name = 'packageDiscountPriceListID'
UNION ALL SELECT 'Campaign.discountReleaseID', COUNT(*), '1' FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Campaign') AND name = 'discountReleaseID'
UNION ALL SELECT 'FK', COUNT(*), '2' FROM sys.foreign_keys WHERE name IN ('FK_Action_PackageDiscountPriceList', 'FK_Campaign_DiscountRelease')
UNION ALL SELECT 'DiscountChangeAffectedActions', COUNT(*), '1' FROM sys.procedures WHERE name = 'DiscountChangeAffectedActions'
UNION ALL SELECT 'iMessage', COUNT(*), '2' FROM dbo.iMessage WHERE name IN ('DiscountReleaseInUse', 'PackageDiscountInUse')
UNION ALL SELECT 'iEntity 23/190 className', COUNT(*), '2' FROM dbo.iEntity
    WHERE (entityID = 23 AND className = 'Merlin.Classes.DiscountValue') OR (entityID = 190 AND className = 'Merlin.Classes.PackageDiscountMassmedia');
GO
