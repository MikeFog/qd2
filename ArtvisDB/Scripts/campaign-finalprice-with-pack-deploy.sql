/*
    ПРОД-ДЕПЛОЙ: Campaign.finalPrice начинает хранить цену СО ВСЕМИ скидками,
                 включая пакетную (Action.discount).

    ПОВОД
      Исторически Campaign.finalPrice хранил цену со всеми скидками, КРОМЕ
      пакетной. Чтобы получить настоящую итоговую цену кампании, каждый
      потребитель обязан был домножить поле на Action.discount (и не домножать
      для пакетных модульных кампаний, campaignTypeID = 4). Это делали 14 разных
      процедур, каждая по-своему: часть с округлением до копеек, часть без.
      Отсюда расхождения в копейку и невозможность просто взять итоговую цену
      кампании из базы.

    ПРАВКА
      Пишущая сторона:
        ActionRecalculate     -- finalPrice = @estimatedPrice, то есть ровно та
                                сумма, которая тут же раскидывается по выпускам
                                через @ratio (для типа 4 пакетная не применяется,
                                @estimatedPrice её и не содержит);
                                priceSumByCampaigns сохраняет прежний смысл
                                (без пакетной) -- теперь считается явной формулой.
        CampaignSetFinalPrice -- не менялась: она и раньше писала в поле сумму
                                со всеми скидками, её просто затирал следующий
                                ActionRecalculate.
        SpecialActionIUD      -- не менялась: у спецакций Action.discount = 1.
        job_DeleteHistory     -- убрано домножение при свёртке в спецакцию.
      Читающая сторона: из 14 процедур убрано домножение на Action.discount.

    ПОБОЧНЫЙ ЭФФЕКТ (ожидаемый)
      В двух местах пакетная скидка применялась БЕЗ округления до копеек
      (ActionsForPaymentCommon -- условие HAVING, stat_Balance -- свёрнутые
      кампании). Теперь везде используется одно округлённое хранимое значение.
      Расхождение с прежним поведением -- не более 1 копейки на кампанию.
      Оценить заранее: campaign-finalprice-with-pack-check.sql, раздел 3.

    ЧЕГО НЕ КАСАЕТСЯ
      Клиент (C#) -- ни строки: свойство Campaign.FinalPrice не используется,
      живой код читает fullPrice/TotalPrice. Метаданные -- ни строки. Счета,
      договоры и медиапланы считаются от Issue.ratio * tariffPrice и к полю
      не обращаются.

    МИГРАЦИЯ ДАННЫХ -- ОДНОРАЗОВАЯ И НЕИДЕМПОТЕНТНАЯ
      Повторный прогон домножил бы цены на пакетную скидку второй раз.
      Защита: скрипт сам смотрит на тело dbo.ActionRecalculate ДО деплоя.
      Если там уже новая формула -- значит, миграция была, UPDATE пропускается.
      Маркер живёт в теле процедуры, его нельзя потерять при публикации DACPAC.

    ПОРЯДОК
      Всё -- процедуры и UPDATE -- в одной транзакции с XACT_ABORT ON.
      При ошибке sqlcmd -b обрывается, транзакция откатывается по разрыву
      соединения. Прерванный прогон без COMMIT ничего не оставляет.

    ПЕРЕД ЗАПУСКОМ
      1. BACKUP DATABASE.
      2. campaign-finalprice-with-pack-check.sql -- раздел "ДО".
      3. Желательно без работающих пользователей: UPDATE берёт ~40 тыс. строк
         Campaign и почти наверняка эскалирует блокировку до таблицы.

    ПОСЛЕ ЗАПУСКА
      campaign-finalprice-with-pack-check.sql -- раздел "ПОСЛЕ".

    ОТКАТ
      Процедуры: git show <коммит до правки> для каждой из 17.
      Данные: восстановить из бэкапа. Обратное деление на a.discount
              копейка-в-копейку не гарантировано.

    ПЕРЕСБОРКА
      Тела процедур в этом файле -- копии из ArtvisDB/dbo/Stored Procedures.
      Править их здесь нельзя. После правки исходника:
        python ArtvisDB/Scripts/campaign-finalprice-with-pack-gen.py

    ЗАПУСК
      sqlcmd -S <прод-сервер> -d Artvis -E -b -I -i campaign-finalprice-with-pack-deploy.sql
*/

-- USE [Artvis];
-- GO

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.ActionRecalculate') IS NULL
   OR OBJECT_ID('dbo.CampaignSetFinalPrice') IS NULL
   OR OBJECT_ID('dbo.Campaign') IS NULL
   OR OBJECT_ID('dbo.Action') IS NULL
BEGIN
    RAISERROR('НЕ ТА БАЗА: нет dbo.ActionRecalculate / dbo.CampaignSetFinalPrice / dbo.Campaign / dbo.Action. Деплой прерван.', 16, 1);
    SET NOEXEC ON;
END
GO

PRINT 'БД     : ' + DB_NAME();
PRINT 'Сервер : ' + CONVERT(sysname, SERVERPROPERTY('ServerName'));
GO

-- Снимок состояния ДО деплоя: переживает GO, живёт до конца сессии sqlcmd.
IF OBJECT_ID('tempdb..#deployState') IS NOT NULL DROP TABLE #deployState;
SELECT CAST(CASE
           WHEN CHARINDEX(N'finalPrice = @estimatedPrice', OBJECT_DEFINITION(OBJECT_ID('dbo.ActionRecalculate'))) > 0
           THEN 1 ELSE 0 END AS BIT) AS alreadyMigrated
INTO #deployState;

DECLARE @already BIT;
SELECT @already = alreadyMigrated FROM #deployState;
PRINT 'Состояние до деплоя: ' + CASE
    WHEN @already = 1
    THEN 'finalPrice УЖЕ со всеми скидками -- UPDATE данных будет пропущен'
    ELSE 'finalPrice пока без пакетной -- данные будут мигрированы' END;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

PRINT '--- процедуры ---';
GO
-- @@HEAD-END@@
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
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
        managerDiscountCampaign DECIMAL(18,10) NULL
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
            @discountValue = @campaignDiscount OUTPUT;

        IF (@oldTotalCount = 0 AND (@newIssuesCount + @newProgramsCount) > 0)
           OR (@oldTotalCount > 0 AND (@newIssuesCount + @newProgramsCount) = 0)
            SELECT @managerDiscountCampaign = dbo.fn_GetMaxUserDiscount(@loggedUserID, @startDate, @finishDate);
        ELSE
            SET @managerDiscountCampaign = NULL;

        UPDATE #CampaignPhase1
        SET
            campaignDiscount = @campaignDiscount,
            managerDiscountCampaign = @managerDiscountCampaign
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
        c.managerDiscount = ISNULL(p.managerDiscountCampaign, c.managerDiscount)
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
            @discountValue = @discountValue OUTPUT;
    ELSE
        SET @discountValue = 1;

    UPDATE dbo.[Action]
    SET
        tariffPrice = @tariffPrice,
        discount = @discountValue,
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
PRINT '  ok: dbo.ActionRecalculate';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER		Procedure [dbo].[CampaignSetFinalPrice]
(
@campaignId int, 
@finalPrice decimal(18,2),
@campaignTypeId TINYINT,
@loggedUserId INT,
@grantorUserId INT = NULL,
@todayDate datetime = null,
@managerDiscountReasonId smallint = null
)
WITH EXECUTE AS OWNER
As
Set NoCount On

Declare
	@fixedPrice decimal(18,2),
	@dayX datetime,
	@theDate datetime,
	@campaignStartDate datetime,
	@packDiscount decimal(9,4),
	@actionId int,
	@issuesPrice decimal(18,2),
	@campaignFinishDate datetime,
	@isAdmin bit

if @todayDate Is Null Set @todayDate = GETDATE()
set @theDate = [dbo].[ToShortDate](@todayDate)

Select	@dayX = convert(datetime, Convert(varchar(6), @todayDate, 112) + '01', 112) - 1
Set		@IsAdmin = dbo.f_IsAdmin(@loggedUserID)

Select
	@campaignStartDate = c.startDate,
	@packDiscount = a.discount,
	@actionId = a.actionId,
	@campaignFinishDate = c.finishDate
From
	Campaign c
	Inner Join [Action] a On a.actionId = c.actionId
Where
	c.campaignId = @campaignId

if /*@isAdmin = 0 And */ @campaignFinishDate <= @theDate
BEGIN
	Raiserror('CantChangeDiscount', 16, 1)
	Return
END

IF @campaignTypeId = 4
	SET @packDiscount = 1.0

Exec GetPriceByPeriod @campaignId, @campaignTypeId, @campaignStartDate,
	@dayX, @fixedPrice	out
	
set @fixedPrice = isnull(@fixedPrice, 0)

If /*@isAdmin = 0 And */ @fixedPrice > @finalPrice Begin
	Raiserror('FinalPriceIsTooLow', 16, 1)
	Return
END

DECLARE @managerDiscount decimal(18, 10) 
SELECT @managerDiscount = @finalPrice / ( price * @packDiscount)
FROM [Campaign]
WHERE [campaignID] = @campaignId

--select  @loggedUserId, @managerDiscount, @campaignStartDate, @campaignFinishDate

IF (@grantorUserId IS NULL OR dbo.[fn_IsAcceptRatioForUser](@grantorUserId, @managerDiscount, @campaignStartDate, @campaignFinishDate) = 0 ) 
	AND dbo.[fn_IsAcceptRatioForUser](@loggedUserId, @managerDiscount, @campaignStartDate, @campaignFinishDate) = 0
	BEGIN
		RAISERROR('MaxRatioExcess', 16, 1)
		RETURN
	END

-- @finalPrice приходит уже со всеми скидками, включая пакетную (см. расчёт
-- @managerDiscount выше) -- ровно в этом виде поле finalPrice и хранится.
Update
	Campaign
Set
	finalPrice = @finalPrice,
	managerDiscount = @managerDiscount
Where
	campaignID = @campaignId

MERGE [dbo].[ManagerDiscountHistory] AS target
USING (SELECT @campaignID AS campaignID) AS source
ON (target.[campaignID] = source.campaignID)

-- Если такая кампания уже есть в истории, обновляем данные
WHEN MATCHED THEN
    UPDATE SET 
        [userID] = IsNull(@grantorUserId, @loggedUserId),
        [managerDiscount] = @managerDiscount,
        [discountSetTime] = GETDATE(),
        [managerDiscountReasonId] = @managerDiscountReasonId

-- Если записи нет — вставляем новую
WHEN NOT MATCHED THEN
    INSERT ([campaignID], [userID], [managerDiscount], [discountSetTime], [managerDiscountReasonId])
    VALUES (@campaignID, IsNull(@grantorUserId, @loggedUserId), @managerDiscount, GETDATE(), @managerDiscountReasonId);
GO
PRINT '  ok: dbo.CampaignSetFinalPrice';
GO

SET QUOTED_IDENTIFIER OFF;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER PROC [dbo].[Campaigns]
(
@actionID int = null,
@campaignID int = null,
@massmediaID smallint = null,
@loggedUserID smallint = null
)
as
set nocount on

IF (@actionID IS NOT NULL OR @campaignID IS NOT NULL /* (@campaignID IS NOT NULL AND @massmediaID IS NULL AND @actionID IS NULL)*/)
begin
	SELECT
		cm.*,
		CASE cm.[campaignTypeID]
			WHEN 4 THEN 'Пакетная модульная кампания'
			ELSE mm.NAME + isnull(' (' + mg.name +')', '')
		END AS name	,
		mm.name as massmediaName,
		f.[name] AS firmName,
		ct.name as campaignTypeName,
		pt.name as paymentTypeName,
		ag.name as agencyName,
		dbo.fn_Int2Time(cm.issuesDuration) as issuesDurationString,
		u.lastName + ' ' + u.firstName as modUserName,
		CASE cm.[campaignTypeID]
			WHEN 1 THEN 91
			WHEN 2 THEN 93
			WHEN 3 THEN 92
			WHEN 4 THEN 171
		END AS entityId,
		CASE cm.[campaignTypeID]
			WHEN 4 THEN CAST(1 AS DECIMAL(9,4))
			ELSE a.[discount]
		END AS packDiscount,
		cm.[finalPrice] AS fullPrice,
		mg.name as groupName,
		a.deleteDate
	FROM
		[Campaign] cm WITH (NOLOCK)
		INNER JOIN [Action] a WITH (NOLOCK) ON cm.[actionID] = a.[actionID]
		INNER JOIN [Firm] f WITH (NOLOCK) ON a.[firmID] = f.[firmID]
		LEFT JOIN vMassMedia mm WITH (NOLOCK) ON mm.massmediaID = cm.massmediaID
		LEFT JOIN MassmediaGroup mg WITH (NOLOCK) on mg.massmediaGroupID = mm.massmediaGroupID
		INNER JOIN iCampaignType ct WITH (NOLOCK) ON ct.campaignTypeID = cm.campaignTypeID
		INNER JOIN PaymentType pt WITH (NOLOCK) ON pt.paymentTypeID = cm.paymentTypeID
		LEFT JOIN Agency ag WITH (NOLOCK) ON ag.agencyId = cm.agencyId
		LEFT OUTER JOIN [User] u WITH (NOLOCK) ON u.userId = cm.modUser
	WHERE
		cm.actionID = COALESCE(@actionID, cm.actionID) AND
		cm.campaignID = COALESCE(@campaignID, cm.campaignID)
	ORDER BY
		cm.campaignID
end
ELSE
begin
	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id)
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

	SELECT distinct
		cm.*,
		CASE cm.[campaignTypeID]
			WHEN 4 THEN 'Пакетная модульная кампания'
			ELSE mm.NAME + isnull(' (' + mg.name +')', '')
		END AS name	,
		mm.name as massmediaName,
		f.[name] AS firmName,
		ct.name as campaignTypeName,
		pt.name as paymentTypeName,
		ag.name as agencyName,
		dbo.fn_Int2Time(cm.issuesDuration) as issuesDurationString,
		u.lastName + ' ' + u.firstName as modUserName,
		CASE cm.[campaignTypeID]
			WHEN 1 THEN 91
			WHEN 2 THEN 93
			WHEN 3 THEN 92
			WHEN 4 THEN 171
		END AS entityId,
		NULL AS packmodulemassmediaID,
		a.[discount] AS packDiscount,
		cm.[finalPrice] AS fullPrice,
		mg.name as groupName,
		a.deleteDate
	FROM
		[Campaign] cm WITH (NOLOCK)
		INNER JOIN [Action] a WITH (NOLOCK) ON cm.[actionID] = a.[actionID]
		INNER JOIN [Firm] f WITH (NOLOCK) ON a.[firmID] = f.[firmID]
		INNER JOIN vMassMedia mm WITH (NOLOCK) ON mm.massmediaID = cm.massmediaID
			AND cm.massmediaID = COALESCE(@massmediaID, cm.massmediaID)
		LEFT JOIN MassmediaGroup mg WITH (NOLOCK) on mg.massmediaGroupID = mm.massmediaGroupID
		INNER JOIN iCampaignType ct WITH (NOLOCK) ON ct.campaignTypeID = cm.campaignTypeID
		INNER JOIN PaymentType pt WITH (NOLOCK) ON pt.paymentTypeID = cm.paymentTypeID
		LEFT JOIN Agency ag WITH (NOLOCK) ON ag.agencyId = cm.agencyId
		LEFT OUTER JOIN [User] u WITH (NOLOCK) ON u.userId = cm.modUser
		left join GroupMember gm WITH (NOLOCK) on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	where
		cm.actionID = COALESCE(@actionID, cm.actionID) AND
		cm.campaignID = COALESCE(@campaignID, cm.campaignID) AND
		a.isConfirmed = 1 AND cm.[campaignTypeID] <> 4
	union all
	(SELECT DISTINCT
		cm.*,
		'Пакетная модульня кампания' AS name,
		mm.name as massmediaName,
		f.[name] AS firmName,
		ct.name as campaignTypeName,
		pt.name as paymentTypeName,
		ag.name as agencyName,
		dbo.fn_Int2Time(cm.issuesDuration) as issuesDurationString,
		u.lastName + ' ' + u.firstName as modUserName,
		171 AS entityId,
		mm.massmediaID AS packmodulemassmediaID,
		CAST(1 AS DECIMAL(9,4)) AS packDiscount,
		CAST(cm.[finalPrice] AS DECIMAL(18,2)) AS fullPrice,
		mg.name as groupName,
		a.deleteDate
	FROM
		[Campaign] cm WITH (NOLOCK)
		INNER JOIN [Action] a WITH (NOLOCK) ON cm.[actionID] = a.[actionID]
		INNER JOIN [Firm] f WITH (NOLOCK) ON a.[firmID] = f.[firmID]
		INNER JOIN iCampaignType ct WITH (NOLOCK) ON ct.campaignTypeID = cm.campaignTypeID
			AND cm.[campaignTypeID] = 4
		INNER JOIN PaymentType pt WITH (NOLOCK) ON pt.paymentTypeID = cm.paymentTypeID
		LEFT JOIN Agency ag WITH (NOLOCK) ON ag.agencyId = cm.agencyId
		LEFT OUTER JOIN [User] u WITH (NOLOCK) ON u.userId = cm.modUser
		INNER JOIN [PackModuleIssue] pmi WITH (NOLOCK) ON pmi.campaignID = cm.campaignID
		INNER JOIN [PackModuleContent] pmc WITH (NOLOCK) ON pmc.pricelistID = pmi.pricelistID
		INNER JOIN Module m WITH (NOLOCK) ON m.moduleID = pmc.moduleID
		INNER JOIN vMassMedia mm WITH (NOLOCK) ON mm.massmediaID = m.massmediaID
			AND m.massmediaID = COALESCE(@massmediaID, m.massmediaID)
		LEFT JOIN MassmediaGroup mg WITH (NOLOCK) on mg.massmediaGroupID = mm.massmediaGroupID
		left join GroupMember gm WITH (NOLOCK) on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	where
		(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
		cm.actionID = COALESCE(@actionID, cm.actionID) AND
		cm.campaignID = COALESCE(@campaignID, cm.campaignID) AND
		a.isConfirmed = 1 AND cm.[campaignTypeID] = 4
	)
	ORDER BY
		cm.campaignID
end
GO
PRINT '  ok: dbo.Campaigns';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER    PROCEDURE [dbo].[ActionsForBalance]
(
@firmID smallint = NULL,
@startOfInterval datetime = null,
@endOfInterval datetime = null,
@userID smallint = null,
--@paymentTypesIDString varchar(1024) = null,
@agenciesIDString varchar(1024) = NULL,
@isHideWhite BIT = 0,
@isHideBlack BIT = 0,
@showBlack bit = 1,
@showWhite bit = 1,
@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;
	
	Select	@startOfInterval = Convert(datetime, Convert(varchar, @startOfInterval, 112), 112)
	Select	@endOfInterval = Convert(datetime, Convert(varchar, @endOfInterval, 112), 112)
			
	CREATE TABLE #Agency(agencyID smallint)

	-- Populate temporary tables with Agency and Payment types
	IF @agenciesIDString Is Null
		INSERT INTO #Agency 
		SELECT agencyID FROM Agency
	Else
		Exec dbo.hlp_PopulateTableFromCommaSeparatedString '#Agency', @agenciesIDString 
		
	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit,
		@isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)
		
	CREATE TABLE #tmp1(actionID int primary key,	summa decimal(18,2) NULL, tariffPrice decimal(18,2) null, [priceSumByCampaigns] decimal(18,2) null)
	INSERT INTO #tmp1 ([actionID], [summa], [tariffPrice], [priceSumByCampaigns]) 
SELECT distinct a.actionID, 0, 0, 0
FROM 
	[Action] a
		Inner Join Campaign c ON c.actionId = a.actionId
		Inner Join PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
		inner join [#Agency] ag on c.agencyID = ag.agencyID
		left join @massmedias umm on c.massmediaID = umm.massmediaID
		left join GroupMember gm on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
WHERE		
		(a.userID = @loggedUserID 
		 or @isRightToViewForeignActions = 1 
		 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and

		(a.isSpecial = 1 
		 or (c.campaignTypeID <> 4 
		     and umm.massmediaID is not null 
		     and ((a.userID = @loggedUserID and umm.myMassmedia = 1) 
		          or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1)))
		 or (c.campaignTypeID = 4 
		     and not exists(
				select *
				from PackModuleIssue pmi 
					inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
					inner join Module m on pmc.moduleID = m.moduleID
					left join @massmedias ummm on m.massmediaID = ummm.massmediaID
				where pmi.campaignID = c.campaignID 
				  and (
						ummm.massmediaID is null 
						or (a.userID = @loggedUserID and ummm.myMassmedia = 0)
						or (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0)
				  )
		     )
		)) and	
		a.finishDate >= Coalesce(@startOfInterval, a.finishDate) and
		a.startDate <= Coalesce(@endOfInterval, a.startDate) and
		a.userId = Coalesce(@userId, a.userId) and
		(pt.isHidden = 0 or @isHideWhite = 0) and
		(pt.isHidden = 1 or @isHideBlack = 0) and
		((pt.IsHidden = 1 and @showBlack = 1)  
		 or (pt.IsHidden = 0 and @showWhite = 1)) and
		a.[firmID] = COALESCE(@firmID, a.[firmID]) and
		a.[isConfirmed] = 1
		
	Declare cur_Companies Cursor local fast_forward
	For
	SELECT 	c.campaignID, c.campaignTypeID,
			c.startDate,
			a.[actionID],
			c.finalPrice,
			c.managerDiscount,
			c.discount,
			c.finishDate,
			ac.discount,
			c.tariffPrice
	From	campaign AS c join #tmp1 as a on c.actionID = a.actionID
			join paymenttype as pt on c.paymentTypeID = pt.paymenttypeID
			join agency as ag on ag.agencyID = c.agencyID
			inner join [Action] ac on ac.actionID = c.actionID
	Where	ag.agencyID IN (Select agencyID From #Agency)
			and (pt.isHidden = 0 or @isHideWhite = 0) And
				(pt.isHidden = 1 or @isHideBlack = 0) and
			((pt.IsHidden = 1 and @showBlack = 1)  or
			(pt.IsHidden = 0 and @showWhite = 1)) 
				
	Declare	@campaignID int, @TypeID int, @StartDate DATETIME,
			@Price decimal(18,2), @Action int,
			@FinalPrice decimal(18,2), @tariffPrice decimal(18,2), 
			@managerDiscount decimal(18,10), @discount decimal(9,4), 
			@finishDate datetime, @actiondiscount decimal(9,4)
		
	Open	cur_Companies
	Fetch	Next from cur_Companies
	Into 	@campaignID, @TypeID, @StartDate, @Action, @FinalPrice, @managerDiscount, @discount, @finishDate, @actiondiscount, @tariffPrice

	While	@@fetch_status = 0
	Begin
		If	(@startOfInterval is null and @endOfInterval IS null) OR (@endOfInterval > @finishDate and @startOfInterval < @StartDate)
		begin 
			set  @Price = @FinalPrice 
		end 
		else
		begin 
			exec GetPriceByPeriod @campaignID = @campaignID, @campaignTypeID = @TypeID, @startDate = @startOfInterval, @finishDate = @endOfInterval, @price = @price OUTPUT, @tariffPrice = @tariffPrice output
		end
		
		UPDATE [#tmp1] SET summa = summa + ISNULL(@Price, 0), [tariffPrice] = [tariffPrice] + ISNULL(@tariffPrice, 0), [priceSumByCampaigns] = [priceSumByCampaigns] + ISNULL(@tariffPrice * @managerDiscount * @discount, 0)
		WHERE [actionID] = @Action

		Fetch	Next from cur_Companies
		Into 	@campaignID, @TypeID, @StartDate, @Action, @FinalPrice, @managerDiscount, @discount, @finishDate, @actiondiscount, @tariffPrice

	end

	close		cur_Companies
	Deallocate	cur_Companies
		
	SELECT 
		ac.[actionID],
		ac.[firmID],
		ac.[startDate],
		ac.[finishDate],
		ac.[discount],
		ac.[userID],
		a.tariffPrice AS [tariffPrice],
		a.[priceSumByCampaigns] AS [priceSumByCampaigns],
		ac.[createDate],
		ac.[modDate],
		ac.[isSpecial],
		ac.[isConfirmed],
		a.summa AS totalPrice,
		us.firstName + Space(1) + us.lastName as creator,
		'Акция №' + LTRIM(ac.[actionID]) as name,
		f.name as firmName
	FROM 
		#tmp1 a
		INNER JOIN [Action] ac ON a.actionID = ac.actionID
		INNER JOIN [User] us ON us.userID = ac.userID
		INNER JOIN [Firm] f ON f.firmID = ac.firmID
	ORDER BY
		ac.[actionID] DESC		
END
GO
PRINT '  ok: dbo.ActionsForBalance';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER  PROC [dbo].[ActionsForPaymentCommon]
(
@paymentID int,
@loggedUserID smallint
)
AS
SET NOCOUNT ON

-- Проверка роли пользователя
DECLARE @isAdmin bit = 0
DECLARE @isBookKeeper bit = 0

SELECT 
	@isAdmin = IsAdmin,
	@isBookKeeper = IsBookKeeper
FROM [dbo].[user]
WHERE userID = @loggedUserID

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

declare @isRightToViewForeignActions bit,@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

DECLARE @paidUP TABLE(actionID INT, paidUp decimal(18,2), paymentIsHidden bit, agencyID smallint)

INSERT INTO @paidUP
SELECT distinct
	a.actionID,
	IsNull(sum(pa.summa), 0),
	pt.isHidden,
	p.agencyID
FROM 
	(
		select distinct a.actionID, a.firmID 
		from [Action] a 
			inner join Campaign c on a.actionID = c.actionID
			inner join 
			(
				select distinct u.userID 
				from [User] u
					left join [GroupMember] gm on u.userID = gm.userID
					left join @ugroups ug on gm.groupID = ug.id
				where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
			) as x on a.userID = x.userID
			left join @massmedias umm on c.massmediaID = umm.massmediaID
			where ((@isAdmin = 1 or @isBookKeeper = 1) and a.isSpecial = 1) or
		 		((c.campaignTypeID <> 4 and umm.massmediaID is not null and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1) )) 
					or (c.campaignTypeID = 4 and not exists(select * 
														from PackModuleIssue pmi 
															inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
															inner join Module m on pmc.moduleID = m.moduleID
															left join @massmedias ummm on m.massmediaID = ummm.massmediaID
														where pmi.campaignID = c.campaignID and (ummm.massmediaID is null or 
															(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
															 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0))))) 
	) a
	INNER JOIN Payment p ON p.firmID = a.firmID  
	LEFT JOIN PaymentAction pa ON pa.actionID = a.actionID 
		AND p.[paymentID] = pa.[paymentID] 
	LEFT JOIN [PaymentType] pt ON p.[paymentTypeID] = pt.[paymentTypeID]
GROUP BY 
	a.actionID, pt.isHidden,p.agencyID

SELECT 
	a.actionID,
	SUM(c.finalPrice) AS finalPrice,
	pu.paidUp as paidUp
FROM 
	[Action] a
	INNER JOIN @paidUP pu ON a.[actionID] = pu.[actionID]
	INNER JOIN [Campaign] c ON c.actionID = a.actionID
	INNER JOIN Payment p ON p.firmID = a.firmID
		And p.agencyID = c.[agencyID] and pu.agencyID = p.agencyID
	INNER JOIN [PaymentType] pt2 ON p.[paymentTypeID] = pt2.[paymentTypeID]
		AND pt2.[isHidden] = pu.paymentIsHidden
	INNER JOIN [PaymentType] pt ON c.[paymentTypeID] = pt.[paymentTypeID]
		AND pt.[isHidden] = pu.paymentIsHidden
WHERE
	p.paymentID = @paymentID AND a.[isConfirmed] = 1
GROUP BY 
	a.actionID, pu.paidUp
HAVING
	cast(SUM(c.finalPrice)*100 as int) - cast(pu.paidUp * 100 as int) > 0
GO
PRINT '  ok: dbo.ActionsForPaymentCommon';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER     PROC [dbo].[CampaignsForActJournalRetrieve]
(
@startDate DATETIME = null,
@finishDate DATETIME = null,
@agencyID int = null,
@firmId int = null,
@showBlack bit = 1,
@showWhite bit = 1,
@actionID INT = null,
@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
AS
Set Nocount On

If @agencyID Is Null Begin
	RaisError('AgencyShouldBeSelected', 16, 1)
	Return
END

IF @startDate IS NULL 
	SELECT @startDate = dbo.ToShortDate(MIN(c.[startDate])) FROM [Campaign] c
	
IF @finishDate IS NULL 
	SELECT @finishDate = dbo.ToShortDate(MAX(c.finishDate)) FROM [Campaign] c

IF @finishDate < @startDate
BEGIN
	RaisError('WrongDates', 16, 1)
	Return
end

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

Declare @res Table(
	currentdate DATETIME,
	campaignId int,
	typeId smallint,
	total decimal(18,2) NULL,
	massmediaID INT null,
	mistake decimal(18,2) default 0,
	issuesCount INT default 0,
	issuesDuration timeDuration NULL default 0,
	showByDuration BIT NULL default 1
)

DECLARE @tmpDate DATETIME 
SET @tmpDate = @startDate
WHILE @tmpDate <= @finishDate
begin
	Insert Into @res
	Select distinct
		dbo.ToShortDate(CASE 
			WHEN dbo.fn_LastDateOfMonth(@tmpDate) < a.[finishDate] 
				THEN dbo.fn_LastDateOfMonth(@tmpDate) 
				ELSE a.[finishDate]
		end),
		c.campaignID,
		c.campaignTypeID,
		0,
		c.[massmediaID],
		0,
		0,
		0,
		1
	from
		[Action] a 
		inner join Campaign c ON c.[actionID] = a.[actionID]
		inner join PaymentType pt On c.paymentTypeId = pt.paymentTypeId
		left join @massmedias umm on c.massmediaID = umm.massmediaID
		left join GroupMember gm on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	Where	
		(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) 
		and (
			a.isSpecial = 1 
			or c.campaignTypeID = 4 
			or umm.massmediaID is not null 
			and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
		) 
		and	a.isSpecial = 0	AND a.[actionID] = COALESCE(@actionID, a.[actionID])
		and a.firmID = isnull(@firmID, a.firmID)
		and c.agencyId = @agencyId AND
		(pt.isHidden = 0 or @showBlack = 1) And
		(pt.isHidden = 1 or @showWhite = 1) AND
		a.[isConfirmed] = 1 AND
		dbo.ToShortDate(a.[finishDate]) >= @tmpDate AND 
		(dbo.fn_LastDateOfMonth(@tmpDate) <= (@finishDate) OR dbo.ToShortDate(a.[finishDate]) <= @finishDate)
				
	SET @tmpDate = DATEADD(month, 1, dbo.fn_FirstDateOfMonth(@tmpDate))
END

Declare	
	@currentDate DATETIME,
	@typeId smallint,
	@total decimal(18,2),
	@campaignID INT,
	@massmediaID smallint,
	@campaignStartDate datetime,
	@campaignFinishDate datetime,
	@campaignFinalPrice decimal(18,2),
	@campaignAdiscount decimal(18,10),
	@mistake decimal(18,2),
	@userID smallint,
	@issuesCount INT,
	@issuesDuration timeDuration,
	@showByDuration BIT

Declare cur_comp2 Cursor local fast_forward
For
SELECT r.currentDate, r.campaignId, r.typeId, c.startDate, c.finishDate, c.finalPrice, a.discount, a.userID From @res r inner join Campaign c on r.campaignId = c.campaignId inner join [Action] a on c.actionID = a.actionID
Open cur_comp2

Fetch Next From cur_comp2 Into @currentDate, @campaignId, @typeId, @campaignStartDate,@campaignFinishDate,@campaignFinalPrice,@campaignAdiscount,@userID
While @@fetch_status = 0 BEGIN
	SET @startDate = dbo.fn_FirstDateOfMonth(@currentDate)

	IF @typeId = 4
	BEGIN
		DELETE FROM @res WHERE [campaignID] = @campaignID AND [currentdate] = @currentDate
		
		Exec GetPriceByPeriod 
			@campaignId, @typeId, @startDate, @currentDate, @total OUT
		
		CREATE TABLE #tmp(massmediaID SMALLINT, price decimal(18,2))
		INSERT INTO #tmp
		SELECT 
			m.[massmediaID], sum(mpl.[price])
		FROM [PackModuleIssue] i 
			INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
			INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
			INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
		WHERE 
			i.campaignID = @campaignID	and
			i.issueDate between @startDate and @currentDate 
		group by m.massmediaID
			
			
		declare @sumPrice decimal(18,2)
		SELECT @sumPrice = sum(t1.price) FROM [#tmp] AS t1
			
		INSERT INTO @res ([currentdate],[campaignId],[typeId],[total],[massmediaID])
		select @currentDate, @campaignId, @typeId, @total * sum(t1.price)/ @sumPrice, t1.massmediaID 
		from #tmp as t1
			inner join @massmedias mmu on t1.massmediaID = mmu.massmediaID 
				and ((@userID = @loggedUserID and mmu.myMassmedia = 1) or
					(@userID <> @loggedUserID and mmu.foreignMassmedia = 1))
		group by t1.massmediaID
		
		drop table #tmp 

		update r
		set 
			r.issuesCount = r.issuesCount + x.issuesCount,
			r.issuesDuration = r.issuesDuration + x.issuesDuration,
			r.showByDuration = x.showByDuration
		from 
			@res r
		inner join (
			select 	COUNT(*) as issuesCount, 
				SUM(rol.duration) as issuesDuration, 
				cast(case when SUM(tw.maxCapacity) > 0 then 0 else 1 end as bit) as showByDuration,
				m.massmediaID
			from Issue i 
				inner join TariffWindow tw on i.originalWindowID = tw.windowId
				INNER join Roller rol on rol.rollerID = i.rollerID
				INNER join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
				INNER JOIN [PackModuleContent] AS pmc ON pmi.[priceListID] = pmc.[pricelistID]
				INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
				INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
				left join @massmedias mmu on m.massmediaID = mmu.massmediaID 
					and ((@userID = @loggedUserID and mmu.myMassmedia = 1) or
						(@userID <> @loggedUserID and mmu.foreignMassmedia = 1))
			where i.campaignID = @campaignID and tw.massmediaID = m.massmediaID and pmi.issueDate between @startDate and @currentDate 
			group by m.massmediaID
		) as x on r.massmediaID = x.massmediaID
		where r.campaignId = @campaignID
	END
	ELSE
	begin
		Exec GetPriceByPeriod 
			@campaignId, @typeId, @startDate, @currentDate, @total OUT
		
		IF @typeId = 2
		BEGIN
			select 
				@issuesCount = COUNT(*), 
				@issuesDuration = SUM(st.duration), 
				@showByDuration = 0
			From ProgramIssue i 
				inner join SponsorTariff st on i.tariffID = st.tariffID
				inner join SponsorProgramPriceList pl on st.priceListID = pl.priceListID
			Where		
				i.campaignID = @campaignID and 
				Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate)), 112), 112) between dbo.ToShortDate(@startDate) and dbo.ToShortDate(@currentDate) 
		END
		ELSE
		BEGIN
			select 
				@issuesCount = COUNT(*), 
				@issuesDuration = SUM(rol.duration), 
				@showByDuration = case when SUM(tw.maxCapacity) > 0 then 0 else 1 end
			from Issue i 
				inner join Roller rol on rol.rollerID = i.rollerID
				inner join TariffWindow tw on i.originalWindowID = tw.windowId
			where i.campaignID = @campaignID and tw.dayOriginal between dbo.ToShortDate(@startDate) and dbo.ToShortDate(@currentDate) 
		END
		
		Update @res
		Set total = @total, showByDuration = @showByDuration, issuesCount = issuesCount + @issuesCount, issuesDuration = issuesDuration + @issuesDuration
		Where currentDate = @currentDate And campaignId = @campaignId
	END
	
	if @campaignFinishDate between @startDate and @currentDate
	begin 
		Exec GetPriceByPeriod @campaignId, @typeId, @campaignStartDate, @campaignFinishDate, @total out
		set @mistake = @campaignFinalPrice - @total
		update @res set mistake = @mistake where campaignId = @campaignID
	end 
	
	Fetch Next From cur_comp2 INTO @currentDate, @campaignId, @typeId, @campaignStartDate,@campaignFinishDate,@campaignFinalPrice,@campaignAdiscount,@userID
End

CLOSE cur_comp2
DEALLOCATE cur_comp2

Select 
	r.currentDate,
	r.currentDate AS currentDate2,
	c.campaignId,
	c.actionId,
	c.startDate,
	c.finishDate,
	cast(null as decimal(18,2)) as total,
	r.total as campaignTotal,
	f.name as firmName,
	f.firmId,
	m.nameWithGroup as massmediaName,
	m.massmediaId,
	pt.name as paymentTypeName,
	u.LastName + ' ' + u.FirstName as userName,
	r.mistake,
	case when r.showByDuration = 1 then dbo.fn_Int2Time(r.issuesDuration) + ' сек.' else cast(r.issuesCount as nvarchar(10)) + ' шт.' end as saleVolume
From 
	@res r
	Inner Join Campaign c On r.CampaignId = c.CampaignId
	Inner Join Action a On a.actionId = c.actionId
	Inner Join Firm f On f.firmId = a.firmId
	Inner Join vMassmedia m On m.massmediaId = r.massmediaID
	Inner Join PaymentType pt On pt.paymentTypeId = c.paymentTypeId
	Inner Join [User] u On u.userId = a.userId
WHERE 
	r.total IS NOT NULL AND r.total > 0
Order by
	r.currentDate asc,
	c.actionId desc
	

select top 1 1
from @res r
	inner join MassMedia mm on r.massmediaID = mm.massmediaID 
	inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID 
where mm.deadline < @finishDate
GO
PRINT '  ok: dbo.CampaignsForActJournalRetrieve';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER  Procedure [dbo].[job_DeleteHistory]
(
	@date datetime
)
WITH EXECUTE AS OWNER
As
set	nocount ON

declare @actionId int
declare @balance table (
			firmId smallint not null,
			agencyId smallint not null,
			paymentTypeId smallint not null,
			summa decimal(18,2) not null,
			managerId smallint not null,
			oldActionID int not null,
			newActionID int null
		)

declare @actions table (
			actionID int not null
		)


insert into @actions
select a.actionID
from [Action] a
where a.finishDate < @date and a.isConfirmed = 1 and a.isSpecial = 0
	and not exists(select * from Campaign c 
		inner join PaymentAction pa on c.actionID = pa.actionID
		inner join Payment p on p.paymentID = pa.paymentID 
		where c.actionID = a.actionID and (p.paymentTypeID <> c.paymentTypeID or p.agencyID <> c.agencyID or p.firmID <> a.firmID ))


insert into @balance (firmId, agencyId, paymentTypeId, summa, managerId, oldActionID)
select a.firmID, c.agencyID, c.paymentTypeID, 
	sum(c.finalPrice),
	a.userID, a.actionID
from [Action] a 
	inner join Campaign c on a.actionID = c.actionID
	inner join @actions a0 on a.actionID = a0.actionID
group by a.firmID, c.agencyID, c.paymentTypeID, a.userID, a.actionID



declare cur_balance cursor local fast_forward
for
select b.firmID, b.agencyID, b.paymentTypeID, sum(b.summa), b.managerID from @balance b
group by b.firmID, b.agencyID, b.paymentTypeID, b.managerID


declare @firmID smallint, @agencyID smallint, @paymentTypeID smallint, @summa decimal(18,2), @userID smallint

open cur_balance

fetch next from cur_balance into @firmID, @agencyID, @paymentTypeID, @summa, @userID

while @@fetch_status = 0
begin

	insert into [Action] (firmID,startDate,finishDate,discount,userID,tariffPrice,priceSumByCampaigns,createDate,modDate,isSpecial,isConfirmed,totalPrice,isAlerted) 
	values(@firmID,@date,@date,1, @userID,@summa,@summa,@date,@date,1,1,@summa,1) 
	
	SET @actionID = SCOPE_IDENTITY()
	
	update @balance set newActionID = @actionId where 
		firmId = @firmID and agencyId = @agencyID and paymentTypeId = @paymentTypeID and managerId = @userID
	
	insert into Campaign (actionID,startDate,finishDate,discount,tariffPrice,finalPrice,paymentTypeID,massmediaID,campaignTypeID,issuesCount,issuesDuration,modTime,modUser,agencyID,timeBonus,programsCount,billNo,billDate,managerDiscount,contractNo) 
	values (@actionID, @date, @date, 1, @summa, @summa, @paymentTypeID, null, 1, 0, 0, @date,@userID,@agencyID,0,0,null,null,1,null)


	fetch next from cur_balance into @firmID, @agencyID, @paymentTypeID, @summa, @userID
end

close cur_balance
deallocate cur_balance

insert into PaymentAction (paymentId, actionID, summa)
select pa.paymentID, x.newActionID, sum(pa.summa)
from PaymentAction pa
	inner join(
		select distinct b0.oldActionID, b0.newActionID from @balance b0 
	) x on pa.actionID = x.oldActionID
	inner join Payment p on pa.paymentID = p.paymentID 
group by pa.paymentID, x.newActionID


delete from pa
from PaymentAction pa
	inner join @actions a on pa.actionID = a.actionID


DROP INDEX [IX_TariffWindow_WindowID_DayOriginal] ON [dbo].[TariffWindow] WITH ( ONLINE = OFF )
DROP INDEX [UIX_TariffWindow_Massmedia_Date] ON [dbo].[TariffWindow] WITH ( ONLINE = OFF )
DROP INDEX [UIX_TariffWindow_Tariff_Date] ON [dbo].[TariffWindow] WITH ( ONLINE = OFF )
DROP INDEX [IX_Issue_ModuleIssueId] ON [dbo].[Issue] WITH ( ONLINE = OFF )
DROP INDEX [IX_Issue] ON [dbo].[Issue] WITH ( ONLINE = OFF )
DROP INDEX [IX_Issue_PackModuleIssueId] ON [dbo].[Issue] WITH ( ONLINE = OFF )
		
delete from tl from TransferLog tl 
		inner join @actions a0 on tl.actionID = a0.actionID
		
delete from di from LogDeletedIssue di 
		inner join @actions a0 on di.actionID = a0.actionID

delete from i from Issue i 
	inner join Campaign c on i.campaignID = c.campaignID
	inner join [Action] a on c.actionID = a.actionID
	inner join @actions a0 on a.actionID = a0.actionID
		
delete from i from ModuleIssue i 
	inner join Campaign c on i.campaignID = c.campaignID
	inner join [Action] a on c.actionID = a.actionID
	inner join @actions a0 on a.actionID = a0.actionID
		
delete from i from PackModuleIssue i 
	inner join Campaign c on i.campaignID = c.campaignID
	inner join [Action] a on c.actionID = a.actionID
	inner join @actions a0 on a.actionID = a0.actionID

delete from c from [Campaign] c
	inner join @actions a0 on c.actionID = a0.actionID

delete from a from [Action] a
	inner join @actions a0 on a.actionID = a0.actionID

delete from tw 
from TariffWindow tw 
	where tw.dayActual < @date and
	not exists(select * from Issue i where tw.windowId = i.actualWindowID or tw.windowId = i.originalWindowID)
	
	
CREATE UNIQUE NONCLUSTERED INDEX [IX_TariffWindow_WindowID_DayOriginal] ON [dbo].[TariffWindow] 
(
	[windowId] ASC,
	[dayOriginal] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

CREATE UNIQUE NONCLUSTERED INDEX [UIX_TariffWindow_Massmedia_Date] ON [dbo].[TariffWindow] 
(
	[windowDateOriginal] ASC,
	[massmediaID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

CREATE UNIQUE NONCLUSTERED INDEX [UIX_TariffWindow_Tariff_Date] ON [dbo].[TariffWindow] 
(
	[tariffId] ASC,
	[windowDateOriginal] ASC,
	[massmediaID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_Issue_PackModuleIssueId] ON [dbo].[Issue] 
(
	[packModuleIssueID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

CREATE UNIQUE NONCLUSTERED INDEX [IX_Issue] ON [dbo].[Issue] 
(
	[issueID] ASC,
	[actualWindowID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_Issue_ModuleIssueId] ON [dbo].[Issue] 
(
	[moduleIssueID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

exec job_DeleteEmptyActions
GO
PRINT '  ok: dbo.job_DeleteHistory';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 21.11.2008
-- Description:	Информация о скидках
-- =============================================
CREATE OR ALTER procedure [dbo].[Stat_AvgDiscount] 
(
@StartDay DATETIME = default,
@FinishDay DATETIME = default,
@FirmID int = default, 
@HeadCompanyID int = default, 
@MassmediaID int = default, 
@PaymentTypeID int = default,
@CampaignTypeID int = default,
@ManagerID int = default,
@AgencyID int = default,
@AdvertTypeID int = default,
@IsGroupByPaymentType bit = 0,
@IsGroupByCampaignType bit = 0,
@IsGroupByMassmedia bit = 0,
@IsGroupByFirm bit = 0,
@IsGroupByManager bit = 0,
@IsGroupByAgency bit = 0,
@IsGroupByMassmediaGroupType bit = 0,
@IsGroupByActionID bit = 0,
@massmediaGroupID int = NULL,
@ShowWhite bit = 1,
@ShowBlack bit = 1,
@Currency int = 1,
@loggedUserID smallint,
@actionID int = NULL
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;

	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit,
			@isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

	If	@StartDay Is Null Or @FinishDay Is Null
		Begin
		Raiserror('FilterStartFinishDays', 16, 1)
		Return
		End

	CREATE TABLE #res
	(
		campaignTariffPrice decimal(18,2),
		campaignPrice decimal(18,2),
		campaignVolumeDiscount decimal(9,4),
		campaignManagerDiscount decimal(9,4),
		campaignPackDiscount decimal(9,4),
		MassmediaID SMALLINT,
		PaymentTypeID SMALLINT,
		ActionID INT,
		campaignTypeID SMALLINT,
		Manager_ID SMALLINT,
		AgencyID SMALLINT,
		AdvertType_ID smallint,
		massmediaGroupID int
	)

	Set	@StartDay = dbo.ToShortDate(@StartDay)
	Set	@FinishDay = dbo.ToShortDate(@FinishDay)

	Declare cur_companies Cursor Local fast_forward
	For
	select  distinct  c.campaignID, c.ActionID, c.massmediaID, 
			c.PaymentTypeID, c.campaignTypeID, a.userID, c.AgencyID, 
			c.[startDate], mm.massmediaGroupID, a.discount, c.finalPrice, c.finishDate, c.managerDiscount, c.discount, c.tariffPrice
	From	
		Campaign c
		INNER Join [Action] a On c.ActionID = a.actionID AND a.[isConfirmed] = 1
		inner join Firm f on f.firmID = a.firmID
		inner join 
		(
			select distinct am.agencyID, max(cast(mm.foreignMassmedia as tinyint)) as foreignMassmedia from AgencyMassmedia am 
				inner join @massmedias mm on am.massmediaID = mm.massmediaID
			group by am.agencyID
		) xx on c.agencyID = xx.agencyID and (a.isSpecial = 0 or xx.foreignMassmedia = 1) 
		INNER JOIN PaymentType On c.PaymentTypeID = PaymentType.PaymentTypeID
		left join MassMedia mm on c.massmediaID = mm.massmediaID
		
		left JOIN [PackModuleIssue] pmi ON pmi.[campaignID] = c.[campaignID]
		left JOIN [PackModuleContent] pmc ON pmc.[pricelistID] = pmi.[pricelistID]
		left JOIN [Module] m ON pmc.[moduleID] = m.[moduleID]
		
		inner join @massmedias mmu on (mm.massmediaID = mmu.massmediaID 
							or m.massmediaID = mmu.massmediaID)
		inner join MassMedia mmfu on mmu.massmediaID = mmfu.massmediaID 
		inner join 
				(
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join @ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as x on a.userID = x.userID
	Where	c.StartDate <= @FinishDay and
			c.FinishDate >= @StartDay and
			c.AgencyID = IsNull(@AgencyID, c.AgencyID) and
			a.firmID = IsNull(@FirmID, a.firmID) and
			f.headCompanyID = IsNull(@headCompanyID, f.headCompanyID) and
			a.userID = IsNull(@ManagerID, a.userID) and
			c.PaymentTypeID = IsNull(@PaymentTypeID, c.PaymentTypeID) and
			c.campaignTypeID = IsNull(@CampaignTypeID, c.campaignTypeID) and
			(@ShowWhite <> 0 or PaymentType.isHidden <> 0) and  
			(@ShowBlack <> 0 or PaymentType.isHidden = 0)  
			and (@MassmediaID is null or mmfu.massmediaID = @MassmediaID)
			and (@massmediaGroupID is null or mmfu.massmediaGroupId = @massmediaGroupID)
			and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))
			and (@actionID is null or a.actionID = @actionID)

	Declare	@campaignID int, 
		@campaignPrice decimal(18,2),
		@SummaVar decimal(18,2),
		@CompStartDate datetime,
		@actionDiscount decimal(9,4),
		@sumPrice decimal(18,2),
		@finalPrice decimal(18,2),
		@cfinishDate datetime,
		@managerDiscount decimal(9,4),
		@volumeDiscount decimal(9,4),
		@campaignTariffPrice decimal(18,2)

	Open	cur_companies
	Fetch	next from cur_companies into 
		@campaignID, @ActionID, @MassmediaID, @PaymenttypeID, 
		@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @massmediaGroupID, @actionDiscount, @finalPrice, @cfinishDate,@managerDiscount,@volumeDiscount,@campaignTariffPrice

	While	@@fetch_status = 0
	begin
		if @FinishDay < @cfinishDate or @StartDay > @CompStartDate
			exec GetPriceByPeriod @campaignId, @CampaignTypeID, @StartDay, @FinishDay, @campaignPrice out, null,@campaignTariffPrice out
		else 
			set @campaignPrice = @finalPrice

		IF @CampaignTypeID = 4
		begin
			declare @tmp table (massmediaID smallint, price decimal(18,2), tariffPrice decimal(18,2))
					
			insert into @tmp(massmediaID, price)
			select
				m.[massmediaID], sum(mpl.[price])
			from [PackModuleIssue] i 
				INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
				INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
				INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
			where 
				i.campaignID = @campaignID	and
				i.issueDate between @StartDay and @FinishDay 
			group by m.massmediaID
				
			select @sumPrice = sum(t1.price) FROM @tmp AS t1
			
			insert into #res (campaignVolumeDiscount,campaignManagerDiscount,campaignPackDiscount,[campaignPrice],campaignTariffPrice, [MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],	[AgencyID],[AdvertType_ID],	massmediaGroupID) 
			select 1,@managerDiscount, 1,@campaignPrice * sum(t1.price)/ @sumPrice, @campaignTariffPrice * sum(t1.price)/ @sumPrice, t1.massmediaID, @PaymenttypeID, @ActionID, @CampaignTypeID,@ManagerID, @AgencyID, 0, mm.massmediaGroupID
			from @tmp as t1
				inner join MassMedia mm on t1.massmediaID = mm.massmediaID
				inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID 
			where t1.price > 0  
				and (@MassmediaID is null or mm.massmediaID = @MassmediaID)
				and (@massmediaGroupID is null or mm.massmediaGroupId = @massmediaGroupID)
				and ((@ManagerID = @loggedUserID and mmu.myMassmedia = 1) or (@ManagerID <> @loggedUserID and mmu.foreignMassmedia = 1))
			group by t1.massmediaID, mm.massmediaGroupID
		END
		ELSE
		begin
			if	@campaignPrice > 0 
				Insert	Into #res (campaignVolumeDiscount,campaignManagerDiscount,campaignPackDiscount,[campaignPrice],campaignTariffPrice, [MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],[AgencyID],[AdvertType_ID], massmediaGroupID) 
				Values( @volumeDiscount,@managerDiscount, @actionDiscount,@campaignPrice, @campaignTariffPrice, @MassmediaID, @PaymenttypeID, @ActionID, @CampaignTypeID, @ManagerID, @AgencyID, 0, @massmediaGroupID)
		end
			
		fetch next from cur_companies into 
				@campaignID, @ActionID, @MassmediaID, @PaymenttypeID,
				@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @massmediaGroupID, @actionDiscount, @finalPrice, @cfinishDate,@managerDiscount,@volumeDiscount,@campaignTariffPrice
	End	

	close cur_companies
	deallocate cur_companies

	-- output ---------------------------------------------------------
	Declare	@SQLString NVARCHAR(2500),
					@IsStarted int

	/* Build the SQL string once.*/
	Set	@SQLString = N'Select	row_number() over(order by coalesce(sum(r.campaignTariffPrice), 0)) as RowNum,'

	If	@IsGroupByPaymentType <> 0
		Set 	@SQLString = @SQLString + N'Paymenttype.Name as "payment_type",'
	If	@IsGroupByCampaignType <> 0
		Set 	@SQLString = @SQLString + N'iCampaignType.Name as "campaign_type",'
	If	@IsGroupByMassmedia <> 0
		Set 	@SQLString = @SQLString + N'vMassMedia.Name as "massmedia", vMassMedia.groupName as "massmedia_group",'
	If	@IsGroupByMassmediaGroupType <> 0
		Set 	@SQLString = @SQLString + N'MassmediaGroup.Name as "massmedia_group",'
	If	@IsGroupByFirm <> 0
		Set 	@SQLString = @SQLString + N'Firm.Name as "firm",'
	If	@IsGroupByManager <> 0
		Set 	@SQLString = @SQLString + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''') as "manager",'
	If	@IsGroupByAgency <> 0
		Set 	@SQLString = @SQLString + N'Agency.Name as "agency",'
	If	@IsGroupByActionID <> 0
		Set 	@SQLString = @SQLString + N'''Акция №'' + cast(r.actionID as varchar) as "actionID",'

	Set	@SQLString = @SQLString + N' coalesce(sum(r.campaignTariffPrice), 0) as tariffPrice
		, coalesce(avg(r.campaignVolumeDiscount), 0) as volumeDiscount
		, cast(coalesce(sum(r.campaignTariffPrice - r.campaignTariffPrice * r.campaignVolumeDiscount), 0) as decimal(18,2)) as volumeDicountPrice
		, coalesce(avg(r.campaignPackDiscount),0) as packDiscount
		, cast(coalesce(sum(r.campaignTariffPrice * r.campaignVolumeDiscount * (1 - r.campaignPackDiscount)) , 0) as decimal(18,2)) as discountPrice
		, coalesce(avg(r.campaignManagerDiscount), 0) as managerDiscount
		, cast(coalesce(sum(r.campaignTariffPrice * r.campaignVolumeDiscount * r.campaignPackDiscount * (1 - r.campaignManagerDiscount)), 0) as decimal(18,2)) as managerDicountPrice
		, coalesce(sum(r.campaignPrice), 0) as price	
	from #res as r '

	If	@IsGroupByMassmediaGroupType <> 0 Set @SQLString = @SQLString + N' inner join MassmediaGroup on r.massmediaGroupID = MassmediaGroup.massmediaGroupID '
	If	@IsGroupByPaymentType <> 0 Set @SQLString = @SQLString + N' inner join Paymenttype on r.PaymentTypeID = Paymenttype.PaymenttypeID'
	If	@IsGroupByCampaignType <> 0 Set @SQLString = @SQLString + N' inner join iCampaignType on r.campaignTypeID = iCampaignType.CampaignTypeID'
	If	@IsGroupByMassmedia <> 0 Set @SQLString = @SQLString + N' inner join vMassMedia on r.massmediaID = vMassMedia.massmediaID'
	If	@IsGroupByFirm <> 0 Set @SQLString = @SQLString + N' inner join Action on r.ActionID = Action.ActionID inner join Firm on Action.firmID = Firm.FirmID '
	If	@IsGroupByManager <> 0 Set @SQLString = @SQLString + N' inner join [User] on r.Manager_ID = [User].UserID'
	If	@IsGroupByAgency <> 0 Set @SQLString = @SQLString + N' inner join Agency on r.AgencyID = Agency.AgencyID'

	If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + 
		@IsGroupByMassmedia + @IsGroupByFirm + 
		@IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType + @IsGroupByActionID <> 0
		begin

		-- Group By part
		set	@IsStarted = 0
		Set 	@SQLString = @SQLString + N' Group by '

		if	@IsGroupByPaymentType <> 0 begin
			if	@IsStarted = 1 set @SQLString = @SQLString + N','	
			Set 	@SQLString = @SQLString + N'Paymenttype.Name'
			set	@IsStarted = 1
		end

		If	@IsGroupByCampaignType <> 0
			begin
			if	@IsStarted = 1 set @SQLString = @SQLString + N','	
			Set 	@SQLString = @SQLString + N'iCampaignType.Name'
			set	@IsStarted = 1
			end

		If	@IsGroupByMassmedia <> 0
			begin
			if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
			Set 	@SQLString = @SQLString + N'vMassMedia.Name, vMassMedia.groupName'
			set	@IsStarted = 1
			end

		If	@IsGroupByFirm <> 0
			begin
			if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
			Set 	@SQLString = @SQLString + N'Firm.Name'
			set	@IsStarted = 1
			end

		If	@IsGroupByManager <> 0
			begin

			if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
			Set 	@SQLString = @SQLString + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''')'
			set	@IsStarted = 1
			end

		If	@IsGroupByAgency <> 0
			begin
			if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
			Set 	@SQLString = @SQLString + N'Agency.Name'
			set	@IsStarted = 1
			end
				
		If	@IsGroupByMassmediaGroupType <> 0
			begin
			if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
			Set @SQLString = @SQLString + N'MassmediaGroup.Name'
			set	@IsStarted = 1
			end
		
		If	@IsGroupByActionID <> 0
			begin
			if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
			Set @SQLString = @SQLString + N'r.actionID'
			set	@IsStarted = 1
			end

		end

	EXECUTE sp_executesql @SQLString

	Drop table #res
end
GO
PRINT '  ok: dbo.Stat_AvgDiscount';
GO

SET QUOTED_IDENTIFIER OFF;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER   PROCEDURE [dbo].[stat_Balance]
(
@theDate datetime = null,
@FirmID int = default,
@HeadCompanyID int = default,
@AgencyID int = default,
@PaymentTypeID int = default,
@ShowBlack bit = 1,
@ShowWhite bit = 1,
@ManagerID int = default,
@EmptyFirmsOnly bit = 0,
@IsGroupByAgency bit = 0,
@agenciesIDString varchar(1024) = NULL,
@isHideWhite BIT = 0,
@isHideBlack BIT = 0,
@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
As
set	nocount ON

create	table #tmp1
(
[summa] 	decimal(18,2),
[firmID] 	int,
[agencyID] 	int
)

-- calculate payments till defined date -----------------------
CREATE TABLE #Agency(agencyID smallint primary key)

-- Populate temporary tables with Agency and Payment types
IF @agenciesIDString Is Null
	INSERT INTO #Agency 
	SELECT agencyID FROM Agency
Else
	Exec dbo.hlp_PopulateTableFromCommaSeparatedString '#Agency', @agenciesIDString 

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

If @EmptyFirmsOnly = 1
	Insert 	Into #tmp1
	Select	ISNULL(Sum(p.summa), 0) as summa,
			p.firmID,
			p.agencyID
	From	payment p
			inner join Firm f on p.firmID = f.firmID
			inner join paymenttype pt on p.paymentTypeID = pt.paymenttypeID
			inner join [#Agency] ag on p.agencyID = ag.agencyID
			left join [Action] a on a.firmID = p.firmID and a.[isConfirmed] = 1
	Where	a.actionID is null
			and (@theDate IS NULL OR p.paymentDate <= @theDate) and
			p.agencyID = IsNull(@AgencyID, p.agencyID) and
			p.paymentTypeID = IsNull(@PaymentTypeID, p.paymentTypeID) and
			p.firmID = IsNull(@FirmID, p.firmID) and
			f.headCompanyID = IsNull(@HeadCompanyID, f.headCompanyID) and
			((pt.IsHidden = 1 and @ShowBlack = 1)  or
			(pt.IsHidden = 0 and @ShowWhite = 1)) and
			(pt.isHidden = 0 or @isHideWhite = 0) And
			(pt.isHidden = 1 or @isHideBlack = 0)
	Group by p.firmID, p.agencyID
Else
	Begin
	If	@ManagerID is null 
	begin
		if @isRightToViewForeignActions = 0 and @isRightToViewGroupActions = 1
		begin 
			Insert 	Into #tmp1
			Select	ISNULL(Sum(pa.summa), 0) as summa,
					p.firmID,
					p.agencyID
			From	payment p
					inner join Firm f on f.firmID = p.firmID
					inner join paymenttype pt on p.paymentTypeID = pt.paymenttypeID
					inner join paymentAction pa on p.paymentID = pa.paymentID
					inner join [Action] a on a.actionID = pa.actionID
					inner join [#Agency] ag  on p.agencyID = ag.agencyID
					inner join (
						select distinct u.userID 
						from [User] u
							left join [GroupMember] gm on u.userID = gm.userID
							left join @ugroups ug on gm.groupID = ug.id
						where 
							u.userID = @loggedUserID 
							or @isRightToViewForeignActions = 1 
							or (@isRightToViewGroupActions = 1 and ug.id is not null)
					) as xu on a.userID = xu.userID
			Where (a.userID = @loggedUserID 
				   OR EXISTS (
					   SELECT 1 
					   FROM campaign c 
					   INNER JOIN @massmedias mm ON c.massmediaID = mm.massmediaID 
												 AND mm.foreignMassmedia = 1
					   WHERE c.actionID = a.actionID
							 AND c.campaignTypeID <> 4
				   )
				   OR EXISTS (
					   SELECT 1 
					   FROM campaign c 
					   INNER JOIN PackModuleIssue pmi ON c.campaignID = pmi.campaignID
					   INNER JOIN PackModuleContent pmc ON pmi.pricelistID = pmc.pricelistID
					   INNER JOIN Module m ON pmc.moduleID = m.moduleID
					   INNER JOIN @massmedias mm ON m.massmediaID = mm.massmediaID 
												 AND mm.foreignMassmedia = 1
					   WHERE c.actionID = a.actionID
							 AND c.campaignTypeID = 4
				   )) and
					(@theDate IS NULL OR p.paymentDate <= @theDate) and
					p.agencyID = IsNull(@AgencyID, p.agencyID) and
					p.paymentTypeID = IsNull(@PaymentTypeID, p.paymentTypeID) and
					p.firmID = IsNull(@FirmID, p.firmID) and
					f.headCompanyID = IsNull(@HeadCompanyID, f.headCompanyID) and
					((pt.IsHidden = 1 and @ShowBlack = 1)  or
					(pt.IsHidden = 0 and @ShowWhite = 1)) 
					AND (pt.isHidden = 0 or @isHideWhite = 0) And
					(pt.isHidden = 1 or @isHideBlack = 0) 
			Group by p.firmID, p.agencyID
		end
		else 
		begin 
			if @isRightToViewForeignActions = 1
				-- Пользователь с полными правами: берем ВСЕ платежи
				Insert 	Into #tmp1
				Select	ISNULL(Sum(p.summa), 0) as summa,
						p.firmID,
						p.agencyID
				From	payment p
						inner join Firm f on f.firmID = p.firmID
						inner join paymenttype pt on p.paymentTypeID = pt.paymenttypeID
						inner join [#Agency] ag on p.agencyID = ag.agencyID
				Where	(@theDate IS NULL OR p.paymentDate <= @theDate) and
						p.agencyID = IsNull(@AgencyID, p.agencyID) and
						p.paymentTypeID = IsNull(@PaymentTypeID, p.paymentTypeID) and
						p.firmID = IsNull(@FirmID, p.firmID) and
						f.headCompanyID = IsNull(@HeadCompanyID, f.headCompanyID) and
						((pt.IsHidden = 1 and @ShowBlack = 1)  or
						(pt.IsHidden = 0 and @ShowWhite = 1))
						AND (pt.isHidden = 0 or @isHideWhite = 0) And
							(pt.isHidden = 1 or @isHideBlack = 0) 
				Group by p.firmID, p.agencyID
			else
				-- Пользователь БЕЗ полных прав: берем только СВОИ платежи через Action
				Insert 	Into #tmp1
				Select	ISNULL(Sum(pa.summa), 0) as summa,
						p.firmID,
						p.agencyID
				From	payment p
						inner join Firm f On f.firmID = p.firmID
						inner join paymenttype pt on p.paymentTypeID = pt.paymenttypeID
						inner join paymentAction pa on p.paymentID = pa.paymentID
						inner join [Action] a on a.actionID = pa.actionID
						inner join [#Agency] ag  on p.agencyID = ag.agencyID
				Where	(a.userID = @loggedUserID) and
						(@theDate IS NULL OR p.paymentDate <= @theDate) and
						p.agencyID = IsNull(@AgencyID, p.agencyID) and
						p.paymentTypeID = IsNull(@PaymentTypeID, p.paymentTypeID) and
						p.firmID = IsNull(@FirmID, p.firmID) and
						f.headCompanyID = IsNull(@HeadCompanyID, f.headCompanyID) and
						((pt.IsHidden = 1 and @ShowBlack = 1)  or
						(pt.IsHidden = 0 and @ShowWhite = 1)) 
						AND (pt.isHidden = 0 or @isHideWhite = 0) And
							(pt.isHidden = 1 or @isHideBlack = 0) 
				Group by p.firmID, p.agencyID
		end 
	end 
	else 
	begin 
		Insert 	Into #tmp1
		Select	ISNULL(Sum(pa.summa), 0) as summa,
				p.firmID,
				p.agencyID
		From	payment p
				inner join Firm f on p.firmID = f.firmID
				inner join paymenttype pt on p.paymentTypeID = pt.paymenttypeID
				inner join paymentAction pa on p.paymentID = pa.paymentID
				inner join [Action] a on a.actionID = pa.actionID
				inner join [#Agency] ag  on p.agencyID = ag.agencyID
				inner join (
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join @ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as xu on a.userID = xu.userID
		Where	(@theDate IS NULL OR p.paymentDate <= @theDate) and
				p.agencyID = IsNull(@AgencyID, p.agencyID) and
				p.paymentTypeID = IsNull(@PaymentTypeID, p.paymentTypeID) and
				p.firmID = IsNull(@FirmID, p.firmID) and
				f.headCompanyID = IsNull(@HeadCompanyID, f.headCompanyID) and
				((pt.IsHidden = 1 and @ShowBlack = 1)  or
				(pt.IsHidden = 0 and @ShowWhite = 1)) and
				a.userID = coalesce(@ManagerID, a.userID)
				AND (pt.isHidden = 0 or @isHideWhite = 0) And
					(pt.isHidden = 1 or @isHideBlack = 0) 
		Group by p.firmID, p.agencyID
	end 

	-- calculate actions till defined date ------------------------
	select	c.campaignID, c.campaignTypeID,
		c.startDate, a.firmID,
		c.agencyID,
		c.finishDate,
		c.finalPrice,
		a.discount
	into	#campaigns
	From	campaign c
			inner join [Action] a on c.actionID = a.actionID
			inner join Firm f on a.firmID = f.firmID
			inner join paymenttype pt on c.paymentTypeID = pt.paymenttypeID
			inner join [#Agency] ag on c.agencyID = ag.agencyID
			left join @massmedias umm on c.massmediaID = umm.massmediaID
	Where
			(a.userID = @loggedUserID
			 or @isRightToViewForeignActions = 1
			 or (@isRightToViewGroupActions = 1
			     and exists(select 1
							from GroupMember gm
								inner join @ugroups ug on gm.groupID = ug.id
							where gm.userID = a.userID))) and
			(a.isSpecial = 1 
			 or (c.campaignTypeID <> 4 
			     and umm.massmediaID is not null 
			     and ((a.userID = @loggedUserID and umm.myMassmedia = 1) 
			          or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1)))
			 or (c.campaignTypeID = 4 
			     and not exists(select * 
							from PackModuleIssue pmi 
								inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
								inner join Module m on pmc.moduleID = m.moduleID
								left join @massmedias ummm on m.massmediaID = ummm.massmediaID
							where pmi.campaignID = c.campaignID 
							  and (ummm.massmediaID is null 
							       or (a.userID = @loggedUserID and ummm.myMassmedia = 0)
							       or (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0))))) and		
			(@theDate is NULL or (c.startDate <= @theDate)) and
			a.firmID = IsNull(@FirmID, a.firmID) and
			f.headCompanyID = IsNull(@HeadCompanyID, f.headCompanyID) and
			c.agencyID = IsNull(@AgencyID, c.agencyID) and
			c.paymentTypeID = IsNull(@PaymentTypeID, c.paymentTypeID) and
			((pt.IsHidden = 1 and @ShowBlack = 1) or (pt.IsHidden = 0 and @ShowWhite = 1)) and
			a.userID = IsNull(@ManagerID, a.userID) and
			a.[isConfirmed] = 1 and
			(pt.isHidden = 0 or @isHideWhite = 0) and
			(pt.isHidden = 1 or @isHideBlack = 0)

	Declare	@campaignID int, @TypeID int,
			@StartDay datetime,
			@Price decimal(18,2), @Agency int,
			@FinishDay datetime, @FinalPrice decimal(18,2), @actiondiscount decimal(9,4)

	-- кампании, завершившиеся до @theDate: цена берётся целиком, разбивка по периоду не нужна
	Insert	Into #tmp1(summa, firmID, agencyID)
	Select	-finalPrice,
			firmID, agencyID
	From	#campaigns
	Where	@theDate IS NULL OR @theDate > finishDate

	-- остальные (ещё идущие на @theDate) считаются по периоду, по одной
	Declare cur_Companies Cursor local fast_forward
	For
	select	campaignID, campaignTypeID, startDate, firmID, agencyID, finishDate, finalPrice, discount
	from	#campaigns
	where	@theDate IS NOT NULL and (finishDate IS NULL or @theDate <= finishDate)

	Open	cur_Companies

	Fetch	Next from cur_Companies
	Into 	@campaignID, @TypeID, @StartDay, @FirmID, @Agency, @FinishDay, @FinalPrice, @actiondiscount

	While	@@fetch_status = 0
		Begin
		EXEC GetPriceByPeriod @campaignID, @TypeID, @StartDay, @theDate, @Price output

		Insert	Into #tmp1(summa, firmID, agencyID)
		Values	(-@Price, @FirmID, @Agency)

		Fetch	Next from cur_Companies
		Into 	@campaignID, @TypeID, @StartDay, @FirmID, @Agency, @FinishDay, @FinalPrice, @actiondiscount

		End

	close		cur_Companies
	Deallocate	cur_Companies

	drop table #campaigns

	End

If	@IsGroupByAgency = 0
	Begin
		Select	
			firm.[firmID] AS firmID,
			firm.Name as name,
			hc.name as headCompanyName,
			case
				when sum(summa) > 0 then sum(summa)
				else 0
			end as summaPositive,
			case
				when sum(summa) < 0 then sum(summa)
				else 0
			end as summaNegative
		From	
			#tmp1 Join firm On #tmp1.firmID = firm.firmID
			inner join HeadCompany hc on hc.headCompanyID = firm.headCompanyID
		Group by firm.Name, firm.firmID, hc.name
		Having 	abs(sum(summa)) >= 0.005

	End
Else
	Begin

	Select	agencyID, firmID, sum(summa) as summa
	into		#tmp2
	From	#tmp1
	Group By agencyID, firmID
	
	drop table #tmp1

	-- ALTER  table with Agency ID ---------------------------
	Declare	@SQLString NVARCHAR(2500), @Desc NVARCHAR(64), @Where NVARCHAR(4000), @Select NVARCHAR(4000), @summa decimal(18,2), @col NVARCHAR(140)

	declare	cur_agency cursor local fast_forward for 
	Select	agency.agencyID, agency.Name, sum(summa) as summa
	From	#tmp2 join Agency on #tmp2.agencyID = agency.agencyID
	Group By agency.agencyID, agency.Name

	create table #rc (
		[RowNum] [int],
		[$Итого] decimal(18,2) default 0
	)
	insert 	#rc(RowNum, [$Итого])
	select	firmID, sum(summa)
	from	#tmp2
	group	by firmID

	declare @sql nvarchar(max), @addwhere bit
	set @sql = 'select 	firm.Name as "Фирма", hc.name as "Группа компаний",
						#rc.* 
				from 	#rc
						JOIN Firm ON Firm.firmID = #rc.RowNum
						Inner Join HeadCompany hc on hc.headCompanyID = firm.headCompanyID'
	set @addwhere = 0
	
	open	cur_agency
	while 1=1
	begin
		fetch next from cur_agency into @AgencyID, @Desc, @Summa
		if @@fetch_status <> 0	
			break

		if @addwhere = 0
		 set @sql = @sql + ' where (0 '
		 
		set @addwhere = 1
		
		set @col = QUOTENAME('$' + @Desc)

		set @sql = @sql + ' + abs(' + @col + ')'

		set @SQLString = N'ALTER TABLE #rc ADD ' + @col + N' decimal(18,2) default 0 with values;'
		exec sp_executeSQL @SQLString
		set @SQLString = N'UPDATE #rc set ' + @col + N' = summa from #rc join #tmp2 on #rc.RowNum = #tmp2.firmID and #tmp2.agencyID = @a'
		exec sp_executeSQL @SQLString, N'@a int', @a = @agencyID
		-- summary
		set @SQLString = N'UPDATE #rc set ' + @col + N' = @sum, [$Итого] = [$Итого] + @sum where RowNum = -1'
		exec sp_executeSQL @SQLString, N'@sum decimal(18,2)', @sum = @summa
	end

	close		cur_agency
	deallocate	cur_agency

	if @addwhere = 1
		set @sql = @sql + ') >= 0.005 '

	set @sql = @sql + ' order by firm.name '

	exec sp_executeSQL @sql
	 
	drop table #rc
	drop table #tmp2
	end
GO
PRINT '  ok: dbo.stat_Balance';
GO

SET QUOTED_IDENTIFIER OFF;
SET ANSI_NULLS OFF;
GO
/*
Modified by Denis Gladkikh (dgladkikh@fogsoft.ru) 19.09.2008 - Bad logic
*/
CREATE OR ALTER      PROCEDURE [dbo].[stat_BalanceAgency]
(
	@theDate datetime = null,
	@PaymentTypeID int = default,
	@ShowBlack bit = 1,
	@ShowWhite bit = 1,
	@loggedUserID smallint
) 
WITH EXECUTE AS OWNER
as
SET NOCOUNT ON
-- calculate payments till defined date -----------------------
create	table #tmp1
(
[summa] decimal(18,2),
[agencyID] int,
[firmID] int
)

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

Insert 	Into #tmp1
Select	sum(p.summa) as summa, 
		p.agencyID,
		p.firmID
From	payment p
		inner join paymentType pt on p.paymentTypeID = pt.paymentTypeID
		inner join 
			(
				select distinct am.agencyID from AgencyMassmedia am 
					inner join @massmedias mm on am.massmediaID = mm.massmediaID and mm.foreignMassmedia = 1
			) x on p.agencyID = x.agencyID
Where	(@theDate IS NULL OR p.paymentDate <= @theDate) and
		p.paymentTypeID = IsNull(@PaymentTypeID, p.paymentTypeID) and
		((pt.isHidden = 1 and @ShowBlack = 1)  or 
		(pt.isHidden = 0 and @ShowWhite = 1))
group by p.agencyID, p.firmID

-- calculate actions till defined date ------------------------
declare 		cur_Companies cursor local fast_forward for
select distinct		c.campaignID, c.campaignTypeID, 
			c.startDate, a.userid, 
			c.agencyID, c.paymentTypeID, 
			c.finishDate, c.FINALPRICE,
			a.firmID, a.discount
from		campaign c
			inner join [Action] a  on c.actionID = a.actionID
			inner join 
			(
				select distinct am.agencyID, max(cast(mm.foreignMassmedia as tinyint)) as foreignMassmedia from AgencyMassmedia am 
					inner join @massmedias mm on am.massmediaID = mm.massmediaID
				group by am.agencyID
			) xx on c.agencyID = xx.agencyID and (a.isSpecial = 0 or xx.foreignMassmedia = 1) 
			inner join paymentType pt on c.paymentTypeID = pt.paymentTypeID
			left join @massmedias umm on c.massmediaID = umm.massmediaID
			left join GroupMember gm on a.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id
where		(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
			(a.isSpecial = 1 or (c.campaignTypeID <> 4 and umm.massmediaID is not null and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1) )) 
				or (c.campaignTypeID = 4 and not exists(select * 
														from PackModuleIssue pmi 
															inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
															inner join Module m on pmc.moduleID = m.moduleID
															left join @massmedias ummm on m.massmediaID = ummm.massmediaID
														where pmi.campaignID = c.campaignID and (ummm.massmediaID is null or 
															(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
															 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0) )))) and	
			c.startDate <= @theDate and
			c.paymentTypeID = IsNull(@PaymentTypeID, c.paymentTypeID) and
			((pt.isHidden = 1 and @ShowBlack = 1)  or 
			(pt.isHidden = 0 and @ShowWhite = 1)) AND a.[isConfirmed] = 1

declare	@campaignID int, @TypeID int, @UserID int,
		@StartDay datetime, 
		@Price decimal(18,2), @AgencyID int, 
		@FinishDay datetime, @FinalPrice decimal(18,2), @firmID int, @actionDiscount decimal(9,4)

open	cur_Companies
fetch	next from cur_Companies 
into 	@campaignID, @TypeID, @StartDay, @UserID, @AgencyID, @PaymentTypeID, 
			@FinishDay, @FinalPrice, @firmID, @actionDiscount
	
while	@@fetch_status = 0
	begin 
	
	If	@theDate IS NULL OR @theDate > @FinishDay
		Set 	@Price = @FinalPrice 
	else
		EXEC GetPriceByPeriod @campaignID, @TypeID, @StartDay, @theDate, @Price output

	Insert	Into #tmp1(summa, agencyID, firmID)
	Values	(-@Price, @AgencyID, @firmID)

	fetch	next from cur_Companies 
	into 	@campaignID, @TypeID, @StartDay, @UserID, @AgencyID, @PaymentTypeID, 
				@FinishDay, @FinalPrice, @firmID, @actionDiscount
	end
	
close cur_Companies
deallocate	cur_Companies

select		agencyID, sum(summa) as sum_plus, cast(0 as decimal(18,2)) as sum_minus
into 		#tmp2
from		#tmp1
group by 	agencyID, firmID
having		sum(summa) > 0

insert into 	#tmp2
select		agencyID, cast(0 as decimal(18,2)) as sum_plus, sum(summa) as sum_minus
from		#tmp1
group by 	agencyID, firmID
having		sum(summa) < 0

select		row_number() over(order by agency.name) as RowNum,
			agency.name as 'Агенство',
			sum(sum_plus) as '$Сумма (+)',
			sum(sum_minus) as '$Сумма (-)'
from		#tmp2 JOIN agency ON #tmp2.agencyID = agency.agencyID
Group 	by 	agency.name
order by 	agency.name

drop table #tmp1
drop table #tmp2
GO
PRINT '  ok: dbo.stat_BalanceAgency';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER        PROCEDURE [dbo].[stat_BalanceManager]
(
	@theDate datetime = null,
	@ShowBlack bit = 1,
	@ShowWhite bit = 1,
	@loggedUserID smallint
) 
WITH EXECUTE AS OWNER
AS

set nocount on

-- calculate payments till defined date -----------------------
declare @tmp1 table
(
[summa] decimal(18,2),
[userID] int,
[agencyID] int,
[firmID] int
)

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

Insert 	Into @tmp1
Select	Sum(pa.summa) as summa, 
		a.userID,
		p.agencyID,
		p.firmID
From	payment p
		inner join paymentaction pa ON p.paymentID = pa.paymentID
		inner join [Action] a ON a.actionID = pa.actionID AND a.isConfirmed = 1
		inner join paymentType pt on p.paymentTypeID = pt.paymentTypeID
		left join 
				(
					select distinct am.agencyID from AgencyMassmedia am 
						inner join @massmedias mm on am.massmediaID = mm.massmediaID and mm.foreignMassmedia = 1
				) x on p.agencyID = x.agencyID
Where	(a.userID = @loggedUserID or x.agencyID is not null) and
		paymentDate <= coalesce(@theDate,paymentDate) And 
		((pt.isHidden = 1 and @ShowBlack = 1)  or 
		(pt.isHidden = 0 and @ShowWhite = 1))
Group 	by 
		p.agencyID,
		a.userID,
		p.firmID

-- calculate actions till defined date ------------------------
Declare 	cur_Companies cursor local fast_forward for
select distinct	c.campaignID, c.campaignTypeID, 
		c.startDate, a.userID,
		c.agencyID, c.finishDate, 
		c.finalPrice, a.firmID,a.discount
From	campaign  c
		inner join [Action] a on c.actionID = a.actionID AND a.isConfirmed = 1
		inner join 
		(
			select distinct am.agencyID, max(cast(mm.foreignMassmedia as tinyint)) as foreignMassmedia from AgencyMassmedia am 
				inner join @massmedias mm on am.massmediaID = mm.massmediaID
			group by am.agencyID
		) xx on c.agencyID = xx.agencyID and (a.isSpecial = 0 or xx.foreignMassmedia = 1) 
		inner join paymentType pt on c.paymentTypeID = pt.paymentTypeID
		left join @massmedias umm on c.massmediaID = umm.massmediaID
		left join GroupMember gm on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
where	(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
			(a.isSpecial = 1 or (c.campaignTypeID <> 4 and umm.massmediaID is not null and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1) )) 
				or (c.campaignTypeID = 4 and not exists(select * 
														from PackModuleIssue pmi 
															inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
															inner join Module m on pmc.moduleID = m.moduleID
															left join @massmedias ummm on m.massmediaID = ummm.massmediaID
														where pmi.campaignID = c.campaignID and (ummm.massmediaID is null or 
															(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
															 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0) )))) and	
		c.startDate <= coalesce(@theDate,c.startDate) and
		((pt.isHidden = 1 and @ShowBlack = 1)  or 
		(pt.isHidden = 0 and @ShowWhite = 1))

Declare	@campaignID int, @TypeID int, @UserID int,
		@StartDay datetime, 
		@Price decimal(18,2), @AgencyID int, 
		@FinishDay datetime, @FinalPrice decimal(18,2), @firmID int, @actiondiscount decimal(9,4)

Open	cur_Companies
Fetch	next from cur_Companies 
Into 	@campaignID, @TypeID, @StartDay, @UserID, @AgencyID, 
		@FinishDay, @FinalPrice, @firmID,@actiondiscount
	
While	@@fetch_status = 0
	Begin 
	
	If	coalesce(@theDate,@FinishDay) >= @FinishDay
		Set 	@Price = @FinalPrice 
	else
		Exec	GetPriceByPeriod @campaignID, @TypeID, @StartDay, @theDate, @Price output

	Insert	Into @tmp1(summa, userID, agencyID, firmID)
	Values	(-@Price, @UserID, @AgencyID, @firmID)

	Fetch	next from cur_Companies 
	Into 	@campaignID, @TypeID, @StartDay, @UserID, @AgencyID, 
			@FinishDay, @FinalPrice, @firmID,@actiondiscount
	End

close		cur_Companies	
Deallocate	cur_Companies

select t1.agencyID, sum(t1.summa) as summa, t1.userID
Into 	#tmp2
from 
	(Select	agencyID, sum(summa) as summa, userID, firmID
		From	@tmp1
		Group by agencyID, userID, firmID
		Having	sum(summa) < 0) as t1 
group by agencyID, userID
having sum(summa) < 0

Declare 	cur_H cursor local fast_forward for
Select	agency.agencyID, agency.Name, sum(summa) as summa
From	#tmp2 join Agency on #tmp2.agencyID = agency.agencyID
Group By agency.agencyID, agency.Name

Declare	@name nvarchar(64), @summa decimal(18,2), @sqlText nvarchar(4000)

create table #result 
(
	RowNum int,
	[$Итого] decimal(18,2) default 0
)
insert #result(RowNum) select distinct UserID from #tmp2

Open	cur_H
while 1=1
begin
	Fetch	next from cur_H
	Into 	@AgencyID, @name, @summa
	if @@fetch_status <> 0	
		break

	set @sqlText = N'ALTER TABLE #result ADD [$' + @name + '] decimal(18,2) default 0 with values;'
	exec sp_executeSQL @sqlText
	set @sqlText = N'UPDATE #result set [$' + @name + '] = summa, [$Итого] = [$Итого] + summa from #result join #tmp2 on #result.RowNum = #tmp2.UserID and #tmp2.agencyID =' + cast(@agencyID as varchar)
	exec sp_executeSQL @sqlText
	-- summary
	set @sqlText = N'UPDATE #result set [$' + @name + '] = @sum, [$Итого] = [$Итого] + @sum where RowNum = -1'
	exec sp_executeSQL @sqlText, N'@sum decimal(18,2)', @sum = @summa
end

close		cur_H	
Deallocate	cur_H

select 	isnull([User].lastname, '') + space(1) + isnull(left([User].firstname, 1), '') + '.' + isnull(left([User].secondname, 1), '') as "Менеджер",
		#result.* 
from 	#result
		JOIN [User] ON [User].userID = #result.RowNum

drop table #tmp2
drop table #result
GO
PRINT '  ok: dbo.stat_BalanceManager';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER PROC [dbo].[stat_Bonuses]
(
    @periodStartDate         DATETIME,
    @periodFinishDate        DATETIME,
    @userID                  SMALLINT = NULL,
    @massmediaGroupID        INT = NULL,
    @minBonusPercentage      DECIMAL(5, 2) = NULL,
    @showBlack               BIT = 1,
    @withZeroRegularCostOnly BIT = 0,
    @selectByCreateDate      BIT = 0,         -- если 1, отбор по Action.createDate, цена = finalPrice
    @isGroupByFirm           BIT = 1          -- 1 = группировка по фирме (как раньше); 0 = по головной организации (HeadCompany) — тогда firmID/firmName в рекордсете отсутствуют
)
AS
BEGIN
    SET NOCOUNT ON

    IF @periodStartDate IS NULL OR @periodFinishDate IS NULL
    BEGIN
        RAISERROR('Start date and finish date are required', 16, 1)
        RETURN
    END

    SET @periodStartDate  = CAST(CAST(@periodStartDate  AS DATE) AS DATETIME)
    SET @periodFinishDate = CAST(CAST(@periodFinishDate AS DATE) AS DATETIME)

    -- isSpecial-акции (ручная договорная цена, Campaign.price) в этот отчёт
    -- не входят ни в одном режиме — это отдельная категория записей, не
    -- связанная с обычным учётом оплаты/бонусов по выпускам.

    IF @selectByCreateDate = 1
    BEGIN
        IF @isGroupByFirm = 1
        BEGIN
            SELECT
                CONCAT(a.[firmID], '_', ISNULL(mg.[massmediaGroupID], 0), '_', ISNULL(a.[userID], 0)) AS UniqueKey,
                a.[firmID],
                f.[name]                         AS firmName,
                mg.[massmediaGroupID],
                mg.[name]                        AS massmediaGroupName,
                a.[userID],
                ISNULL(vu.[userName], 'Unknown') AS userName,
                COUNT(c.[campaignID])            AS CampaignCount,
                SUM(CASE WHEN c.[paymentTypeID] = 26
                         THEN c.[finalPrice]
                         ELSE 0 END)             AS TotalBonusCost,
                SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                         THEN c.[finalPrice]
                         ELSE 0 END)             AS RegularCost,
                SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 1
                         THEN c.[finalPrice]
                         ELSE 0 END)             AS HiddenCost,
                CASE
                    WHEN SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                  THEN c.[finalPrice]
                                  ELSE 0 END) = 0 THEN NULL
                    ELSE ROUND(
                        SUM(CASE WHEN c.[paymentTypeID] = 26
                                 THEN c.[finalPrice]
                                 ELSE 0 END)
                        / SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                   THEN c.[finalPrice]
                                   ELSE 0 END) * 100, 2)
                END                              AS BonusPercentage,
                @periodStartDate                 AS PeriodStartDate,
                @periodFinishDate                AS PeriodFinishDate,
                @selectByCreateDate              AS SelectByCreateDate
            FROM
                [dbo].[Campaign] c
                INNER JOIN [dbo].[Action] a         ON c.[actionID]         = a.[actionID]
                INNER JOIN [dbo].[Firm] f            ON a.[firmID]           = f.[firmID]
                INNER JOIN [dbo].[PaymentType] pt    ON pt.[PaymenttypeId]   = c.[paymenttypeId]
                LEFT  JOIN [dbo].[MassMedia] m       ON c.[massmediaID]      = m.[massmediaID]
                LEFT  JOIN [dbo].[MassmediaGroup] mg ON m.[massmediaGroupID] = mg.[massmediaGroupID]
                LEFT  JOIN [dbo].[vUser] vu          ON a.[userID]           = vu.[userID]
            WHERE
                a.[createDate] >= @periodStartDate
                AND a.[createDate] <  DATEADD(DAY, 1, @periodFinishDate)
                AND a.[isConfirmed] = 1
                AND a.[isSpecial] = 0
                AND (@userID IS NULL OR a.[userID] = @userID)
                AND (@massmediaGroupID IS NULL OR mg.[massmediaGroupID] = @massmediaGroupID)
                AND (c.[finalPrice] <> 0
                     OR (c.[campaignTypeID] = 1 AND EXISTS(SELECT 1 FROM [dbo].[Issue] i WHERE i.[campaignID] = c.[campaignID]))
                     OR (c.[campaignTypeID] = 2 AND EXISTS(SELECT 1 FROM [dbo].[ProgramIssue] pi WHERE pi.[campaignID] = c.[campaignID]))
                     OR (c.[campaignTypeID] = 3 AND EXISTS(SELECT 1 FROM [dbo].[ModuleIssue] mi WHERE mi.[campaignID] = c.[campaignID]))
                     OR (c.[campaignTypeID] = 4 AND EXISTS(SELECT 1 FROM [dbo].[PackModuleIssue] pmi WHERE pmi.[campaignID] = c.[campaignID])))
            GROUP BY
                a.[firmID], f.[name],
                mg.[massmediaGroupID], mg.[name],
                a.[userID], vu.[userName]
            HAVING
                (@minBonusPercentage IS NULL
                 OR ROUND(
                    SUM(CASE WHEN c.[paymentTypeID] = 26
                             THEN c.[finalPrice]
                             ELSE 0 END)
                    / NULLIF(SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                      THEN c.[finalPrice]
                                      ELSE 0 END), 0) * 100, 2
                    ) > @minBonusPercentage)
                AND (@withZeroRegularCostOnly = 0
                     OR SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                 THEN c.[finalPrice]
                                 ELSE 0 END) = 0)
            ORDER BY
                f.[name] ASC, mg.[name] ASC, vu.[userName] ASC, RegularCost DESC
        END
        ELSE
        BEGIN
            SELECT
                CONCAT('H', f.[headCompanyID], '_', ISNULL(mg.[massmediaGroupID], 0), '_', ISNULL(a.[userID], 0)) AS UniqueKey,
                f.[headCompanyID],
                hc.[name]                        AS headCompanyName,
                mg.[massmediaGroupID],
                mg.[name]                        AS massmediaGroupName,
                a.[userID],
                ISNULL(vu.[userName], 'Unknown') AS userName,
                COUNT(c.[campaignID])            AS CampaignCount,
                SUM(CASE WHEN c.[paymentTypeID] = 26
                         THEN c.[finalPrice]
                         ELSE 0 END)             AS TotalBonusCost,
                SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                         THEN c.[finalPrice]
                         ELSE 0 END)             AS RegularCost,
                SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 1
                         THEN c.[finalPrice]
                         ELSE 0 END)             AS HiddenCost,
                CASE
                    WHEN SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                  THEN c.[finalPrice]
                                  ELSE 0 END) = 0 THEN NULL
                    ELSE ROUND(
                        SUM(CASE WHEN c.[paymentTypeID] = 26
                                 THEN c.[finalPrice]
                                 ELSE 0 END)
                        / SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                   THEN c.[finalPrice]
                                   ELSE 0 END) * 100, 2)
                END                              AS BonusPercentage,
                @periodStartDate                 AS PeriodStartDate,
                @periodFinishDate                AS PeriodFinishDate,
                @selectByCreateDate              AS SelectByCreateDate
            FROM
                [dbo].[Campaign] c
                INNER JOIN [dbo].[Action] a         ON c.[actionID]         = a.[actionID]
                INNER JOIN [dbo].[Firm] f            ON a.[firmID]           = f.[firmID]
                LEFT  JOIN [dbo].[HeadCompany] hc    ON f.[headCompanyID]    = hc.[headCompanyID]
                INNER JOIN [dbo].[PaymentType] pt    ON pt.[PaymenttypeId]   = c.[paymenttypeId]
                LEFT  JOIN [dbo].[MassMedia] m       ON c.[massmediaID]      = m.[massmediaID]
                LEFT  JOIN [dbo].[MassmediaGroup] mg ON m.[massmediaGroupID] = mg.[massmediaGroupID]
                LEFT  JOIN [dbo].[vUser] vu          ON a.[userID]           = vu.[userID]
            WHERE
                a.[createDate] >= @periodStartDate
                AND a.[createDate] <  DATEADD(DAY, 1, @periodFinishDate)
                AND a.[isConfirmed] = 1
                AND a.[isSpecial] = 0
                AND (@userID IS NULL OR a.[userID] = @userID)
                AND (@massmediaGroupID IS NULL OR mg.[massmediaGroupID] = @massmediaGroupID)
                AND (c.[finalPrice] <> 0
                     OR (c.[campaignTypeID] = 1 AND EXISTS(SELECT 1 FROM [dbo].[Issue] i WHERE i.[campaignID] = c.[campaignID]))
                     OR (c.[campaignTypeID] = 2 AND EXISTS(SELECT 1 FROM [dbo].[ProgramIssue] pi WHERE pi.[campaignID] = c.[campaignID]))
                     OR (c.[campaignTypeID] = 3 AND EXISTS(SELECT 1 FROM [dbo].[ModuleIssue] mi WHERE mi.[campaignID] = c.[campaignID]))
                     OR (c.[campaignTypeID] = 4 AND EXISTS(SELECT 1 FROM [dbo].[PackModuleIssue] pmi WHERE pmi.[campaignID] = c.[campaignID])))
            GROUP BY
                f.[headCompanyID], hc.[name],
                mg.[massmediaGroupID], mg.[name],
                a.[userID], vu.[userName]
            HAVING
                (@minBonusPercentage IS NULL
                 OR ROUND(
                    SUM(CASE WHEN c.[paymentTypeID] = 26
                             THEN c.[finalPrice]
                             ELSE 0 END)
                    / NULLIF(SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                      THEN c.[finalPrice]
                                      ELSE 0 END), 0) * 100, 2
                    ) > @minBonusPercentage)
                AND (@withZeroRegularCostOnly = 0
                     OR SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                 THEN c.[finalPrice]
                                 ELSE 0 END) = 0)
            ORDER BY
                hc.[name] ASC, mg.[name] ASC, vu.[userName] ASC, RegularCost DESC
        END

        RETURN
    END

    -- =====================================================================
    -- Стандартный режим: цена по периоду вычисляется set-based напрямую
    -- (Issue/ProgramIssue/ModuleIssue/PackModuleIssue), без курсора и без
    -- построчных EXEC GetPriceByPeriod. Логика 4 типов кампаний воспроизводит
    -- GetPriceByPeriod максимально точно, включая то, что для campaignTypeID=4
    -- итог пропорционально делится между массмедиа. isSpecial-акции исключены
    -- из #CampaignBase целиком (см. фильтр ниже) — они не относятся к этому
    -- отчёту.
    --
    -- hasPlacementInPeriod: отдельно от @showBlack-гейта фиксирует, было ли у
    -- кампании хоть одно реальное размещение (Issue/ProgramIssue/ModuleIssue/
    -- PackModuleIssue) в периоде. Нужно, чтобы SUM() по costInPeriod не путал
    -- "кампания без выпусков в периоде" (NULL, суммой игнорируется) с
    -- "реальная стоимость равна нулю" — иначе @withZeroRegularCostOnly ловит
    -- фирмы без единого выпуска в периоде наравне с фирмами, которые реально
    -- потратили 0 обычных денег.
    -- =====================================================================

    CREATE TABLE #CampaignCosts
    (
        [campaignID]           INT,
        [firmID]               SMALLINT,
        [firmName]             NVARCHAR(MAX),
        [headCompanyID]        INT,
        [headCompanyName]      VARCHAR(256),
        [massmediaGroupID]     INT,
        [massmediaGroupName]   VARCHAR(250),
        [userID]               SMALLINT,
        [userName]             NVARCHAR(MAX),
        [paymentTypeID]        SMALLINT,
        [isHidden]             BIT,
        [costInPeriod]         DECIMAL(18, 2),
        [hasPlacementInPeriod] TINYINT
    )

    SELECT DISTINCT
        c.[campaignID], c.[campaignTypeID], c.[paymentTypeID],
        a.[firmID], a.[userID] AS actionUserID,
        f.[name] AS firmName,
        f.[headCompanyID], hc.[name] AS headCompanyName,
        pt.[isHidden],
        mg.[massmediaGroupID], mg.[name] AS massmediaGroupName,
        ISNULL(vu.[userName], 'Unknown') AS userName
    INTO #CampaignBase
    FROM [dbo].[Campaign] c
        INNER JOIN [dbo].[Action] a ON c.[actionID] = a.[actionID]
        INNER JOIN [dbo].[Firm] f ON a.[firmID] = f.[firmID]
        LEFT JOIN [dbo].[HeadCompany] hc ON f.[headCompanyID] = hc.[headCompanyID]
        INNER JOIN [dbo].[PaymentType] pt ON pt.[PaymenttypeId] = c.[paymenttypeId]
        LEFT JOIN [dbo].[MassMedia] m ON c.[massmediaID] = m.[massmediaID]
        LEFT JOIN [dbo].[MassmediaGroup] mg ON m.[massmediaGroupID] = mg.[massmediaGroupID]
        LEFT JOIN [dbo].[vUser] vu ON a.[userID] = vu.[userID]
    WHERE
        c.[startDate] <= @periodFinishDate
        AND c.[finishDate] >= @periodStartDate
        AND a.[isConfirmed] = 1
        AND a.[isSpecial] = 0
        AND (@userID IS NULL OR a.[userID] = @userID)
        AND (@massmediaGroupID IS NULL OR mg.[massmediaGroupID] = @massmediaGroupID OR c.[campaignTypeID] = 4)

    -- Тип 1 (линейные ролики). Одна строка на кампанию всегда (LEFT JOIN),
    -- независимо от showBlack — как в оригинале (INSERT после EXEC
    -- выполняется безусловно). costInPeriod = NULL, если showBlack блокирует
    -- платёжный тип (соответствует поведению GetPriceByPeriod: тот же самый
    -- (@showBlack=1 OR pt.isHidden=0) джойн проваливается что для
    -- обычного расчёта — то есть результат NULL).
    INSERT INTO #CampaignCosts
    SELECT cb.[campaignID], cb.[firmID], cb.[firmName], cb.[headCompanyID], cb.[headCompanyName],
           cb.[massmediaGroupID], cb.[massmediaGroupName],
           cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden],
           CASE
               WHEN NOT (@showBlack = 1 OR cb.[isHidden] = 0) THEN NULL
               ELSE SUM(CASE WHEN tw.[windowId] IS NOT NULL THEN i.[tariffPrice] * i.[ratio] END)
           END AS costInPeriod,
           MAX(CASE WHEN tw.[windowId] IS NOT NULL THEN 1 ELSE 0 END) AS hasPlacementInPeriod
    FROM #CampaignBase cb
        LEFT JOIN [dbo].[Issue] i ON i.[campaignID] = cb.[campaignID]
        LEFT JOIN [dbo].[TariffWindow] tw ON i.[originalWindowID] = tw.[windowId]
            AND tw.[dayOriginal] BETWEEN @periodStartDate AND @periodFinishDate
    WHERE cb.[campaignTypeID] = 1
    GROUP BY cb.[campaignID], cb.[firmID], cb.[firmName], cb.[headCompanyID], cb.[headCompanyName],
             cb.[massmediaGroupID], cb.[massmediaGroupName],
             cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden]

    -- Тип 2 (спонсорские программы)
    INSERT INTO #CampaignCosts
    SELECT cb.[campaignID], cb.[firmID], cb.[firmName], cb.[headCompanyID], cb.[headCompanyName],
           cb.[massmediaGroupID], cb.[massmediaGroupName],
           cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden],
           CASE
               WHEN NOT (@showBlack = 1 OR cb.[isHidden] = 0) THEN NULL
               ELSE SUM(CASE WHEN pl.[priceListID] IS NOT NULL THEN i.[tariffPrice] * i.[ratio] END)
           END AS costInPeriod,
           MAX(CASE WHEN pl.[priceListID] IS NOT NULL THEN 1 ELSE 0 END) AS hasPlacementInPeriod
    FROM #CampaignBase cb
        LEFT JOIN [dbo].[ProgramIssue] i ON i.[campaignID] = cb.[campaignID]
        LEFT JOIN [dbo].[SponsorTariff] st ON i.[tariffID] = st.[tariffID]
        LEFT JOIN [dbo].[SponsorProgramPriceList] pl ON st.[priceListID] = pl.[priceListID]
            AND CONVERT(DATETIME, CONVERT(VARCHAR(8),
                DATEADD(MINUTE, -DATEPART(MINUTE, pl.[broadcastStart]),
                    DATEADD(HOUR, -DATEPART(HOUR, pl.[broadcastStart]), i.[issueDate])), 112), 112)
                BETWEEN @periodStartDate AND @periodFinishDate
    WHERE cb.[campaignTypeID] = 2
    GROUP BY cb.[campaignID], cb.[firmID], cb.[firmName], cb.[headCompanyID], cb.[headCompanyName],
             cb.[massmediaGroupID], cb.[massmediaGroupName],
             cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden]

    -- Тип 3 (модульные)
    INSERT INTO #CampaignCosts
    SELECT cb.[campaignID], cb.[firmID], cb.[firmName], cb.[headCompanyID], cb.[headCompanyName],
           cb.[massmediaGroupID], cb.[massmediaGroupName],
           cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden],
           CASE
               WHEN NOT (@showBlack = 1 OR cb.[isHidden] = 0) THEN NULL
               ELSE SUM(i.[tariffPrice] * i.[ratio])
           END AS costInPeriod,
           MAX(CASE WHEN i.[moduleIssueID] IS NOT NULL THEN 1 ELSE 0 END) AS hasPlacementInPeriod
    FROM #CampaignBase cb
        LEFT JOIN [dbo].[ModuleIssue] i ON i.[campaignID] = cb.[campaignID]
            AND i.[issueDate] BETWEEN @periodStartDate AND @periodFinishDate
    WHERE cb.[campaignTypeID] = 3
    GROUP BY cb.[campaignID], cb.[firmID], cb.[firmName], cb.[headCompanyID], cb.[headCompanyName],
             cb.[massmediaGroupID], cb.[massmediaGroupName],
             cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden]

    -- Тип 4 (пакетные): состав пар campaignID×massmediaID определяется ТОЛЬКО
    -- датой выпуска (как massmedia_cursor в оригинале — без фильтра по
    -- showBlack), а сама стоимость — через CASE как выше. Для
    -- пропорционального деления вес считается отдельно (T4Weight), но набор
    -- строк (T4Set) не зависит от showBlack, чтобы не терять строки.
    -- T4Set уже строится через INNER JOIN PackModuleIssue с фильтром по
    -- периоду, поэтому любая строка, попавшая в эту вставку, по построению
    -- имеет реальное размещение в периоде — hasPlacementInPeriod = 1 всегда.
    ;WITH T4Set AS (
        SELECT DISTINCT cb.[campaignID], m.[massmediaID], mg2.[massmediaGroupID], mg2.[name] AS massmediaGroupName
        FROM #CampaignBase cb
            INNER JOIN [dbo].[PackModuleIssue] i ON i.[campaignID] = cb.[campaignID]
                AND i.[issueDate] BETWEEN @periodStartDate AND @periodFinishDate
            INNER JOIN [dbo].[PackModuleContent] pmc ON i.[priceListID] = pmc.[pricelistID]
            INNER JOIN [dbo].[ModulePriceList] mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
            INNER JOIN [dbo].[Module] m ON mpl.[moduleID] = m.[moduleID]
            LEFT JOIN [dbo].[MassMedia] mm2 ON m.[massmediaID] = mm2.[massmediaID]
            LEFT JOIN [dbo].[MassmediaGroup] mg2 ON mm2.[massmediaGroupID] = mg2.[massmediaGroupID]
        WHERE cb.[campaignTypeID] = 4
    ),
    T4Weight AS (
        SELECT cb.[campaignID], m.[massmediaID], SUM(mpl.[price]) AS mmPrice
        FROM #CampaignBase cb
            INNER JOIN [dbo].[PackModuleIssue] i ON i.[campaignID] = cb.[campaignID]
                AND i.[issueDate] BETWEEN @periodStartDate AND @periodFinishDate
            INNER JOIN [dbo].[PackModuleContent] pmc ON i.[priceListID] = pmc.[pricelistID]
            INNER JOIN [dbo].[ModulePriceList] mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
            INNER JOIN [dbo].[Module] m ON mpl.[moduleID] = m.[moduleID]
        WHERE cb.[campaignTypeID] = 4 AND (@showBlack = 1 OR cb.[isHidden] = 0)
        GROUP BY cb.[campaignID], m.[massmediaID]
    ),
    T4Total AS (
        SELECT cb.[campaignID], SUM(i.[tariffPrice] * i.[ratio]) AS packModulePrice
        FROM #CampaignBase cb
            INNER JOIN [dbo].[PackModuleIssue] i ON i.[campaignID] = cb.[campaignID]
                AND i.[issueDate] BETWEEN @periodStartDate AND @periodFinishDate
        WHERE cb.[campaignTypeID] = 4 AND (@showBlack = 1 OR cb.[isHidden] = 0)
        GROUP BY cb.[campaignID]
    ),
    T4Sum AS (
        SELECT [campaignID], SUM([mmPrice]) AS sumPrice
        FROM T4Weight
        GROUP BY [campaignID]
    )
    INSERT INTO #CampaignCosts
    SELECT cb.[campaignID], cb.[firmID], cb.[firmName], cb.[headCompanyID], cb.[headCompanyName],
           t4s.[massmediaGroupID], t4s.[massmediaGroupName],
           cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden],
           CASE
               WHEN NOT (@showBlack = 1 OR cb.[isHidden] = 0) THEN NULL
               ELSE tot.[packModulePrice] * w.[mmPrice] / s.[sumPrice]
           END AS costInPeriod,
           1 AS hasPlacementInPeriod
    FROM T4Set t4s
        INNER JOIN #CampaignBase cb ON cb.[campaignID] = t4s.[campaignID]
        LEFT JOIN T4Total tot ON tot.[campaignID] = t4s.[campaignID]
        LEFT JOIN T4Weight w ON w.[campaignID] = t4s.[campaignID] AND w.[massmediaID] = t4s.[massmediaID]
        LEFT JOIN T4Sum s ON s.[campaignID] = t4s.[campaignID]
    WHERE (@massmediaGroupID IS NULL OR t4s.[massmediaGroupID] = @massmediaGroupID)

    IF @isGroupByFirm = 1
    BEGIN
        SELECT
            CONCAT([firmID], '_', ISNULL([massmediaGroupID], 0), '_', ISNULL([userID], 0)) AS UniqueKey,
            [firmID],
            [firmName],
            [massmediaGroupID],
            [massmediaGroupName],
            [userID],
            [userName],
            COUNT([campaignID]) AS CampaignCount,
            SUM(CASE WHEN [paymentTypeID] = 26 THEN [costInPeriod] ELSE 0 END) AS TotalBonusCost,
            SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END) AS RegularCost,
            SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 1 THEN [costInPeriod] ELSE 0 END) AS HiddenCost,
            CASE
                WHEN SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END) = 0 THEN NULL
                ELSE ROUND(
                    (SUM(CASE WHEN [paymentTypeID] = 26 THEN [costInPeriod] ELSE 0 END)
                    / SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END)) * 100, 2)
            END AS BonusPercentage,
            @periodStartDate    AS PeriodStartDate,
            @periodFinishDate   AS PeriodFinishDate,
            @selectByCreateDate AS SelectByCreateDate
        FROM #CampaignCosts
        GROUP BY
            [firmID], [firmName],
            [massmediaGroupID], [massmediaGroupName],
            [userID], [userName]
        HAVING
            (@minBonusPercentage IS NULL
             OR ROUND(
                (SUM(CASE WHEN [paymentTypeID] = 26 THEN [costInPeriod] ELSE 0 END)
                / NULLIF(SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END), 0)) * 100, 2
                ) > @minBonusPercentage)
            AND (@withZeroRegularCostOnly = 0
                 OR (SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END) = 0
                     AND MAX([hasPlacementInPeriod]) = 1))
        ORDER BY
            [firmName] ASC, [massmediaGroupName] ASC, [userName] ASC, RegularCost DESC
    END
    ELSE
    BEGIN
        SELECT
            CONCAT('H', [headCompanyID], '_', ISNULL([massmediaGroupID], 0), '_', ISNULL([userID], 0)) AS UniqueKey,
            [headCompanyID],
            [headCompanyName],
            [massmediaGroupID],
            [massmediaGroupName],
            [userID],
            [userName],
            COUNT([campaignID]) AS CampaignCount,
            SUM(CASE WHEN [paymentTypeID] = 26 THEN [costInPeriod] ELSE 0 END) AS TotalBonusCost,
            SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END) AS RegularCost,
            SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 1 THEN [costInPeriod] ELSE 0 END) AS HiddenCost,
            CASE
                WHEN SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END) = 0 THEN NULL
                ELSE ROUND(
                    (SUM(CASE WHEN [paymentTypeID] = 26 THEN [costInPeriod] ELSE 0 END)
                    / SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END)) * 100, 2)
            END AS BonusPercentage,
            @periodStartDate    AS PeriodStartDate,
            @periodFinishDate   AS PeriodFinishDate,
            @selectByCreateDate AS SelectByCreateDate
        FROM #CampaignCosts
        GROUP BY
            [headCompanyID], [headCompanyName],
            [massmediaGroupID], [massmediaGroupName],
            [userID], [userName]
        HAVING
            (@minBonusPercentage IS NULL
             OR ROUND(
                (SUM(CASE WHEN [paymentTypeID] = 26 THEN [costInPeriod] ELSE 0 END)
                / NULLIF(SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END), 0)) * 100, 2
                ) > @minBonusPercentage)
            AND (@withZeroRegularCostOnly = 0
                 OR (SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END) = 0
                     AND MAX([hasPlacementInPeriod]) = 1))
        ORDER BY
            [headCompanyName] ASC, [massmediaGroupName] ASC, [userName] ASC, RegularCost DESC
    END

    DROP TABLE #CampaignCosts
    DROP TABLE #CampaignBase
END
GO
PRINT '  ok: dbo.stat_Bonuses';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 05.05.2009
-- Description:	Modified to include headCompanyID filtering and grouping
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[statFactorAnalysis] 
(
	@StartDay DATETIME = default,
	@FinishDay DATETIME = default,
	@ComparedStartDay datetime = default,
	@FirmID int = default, 
	@headCompanyID int = default,
	@MassmediaID int = default, 
	@PaymentTypeID int = default,
	@CampaignTypeID int = default,
	@ManagerID int = default,
	@AgencyID int = default,
	@IsGroupByPaymentType bit = 0,
	@IsGroupByCampaignType bit = 0,
	@IsGroupByMassmedia bit = 0,
	@IsGroupByFirm bit = 0,
	@IsGroupByHeadCompany bit = 0,
	@IsGroupByManager bit = 0,
	@IsGroupByAgency bit = 0,
	@IsGroupByMassmediaGroupType bit = 0,
	@massmediaGroupID int = null,
	@ShowWhite bit = 1,
	@ShowBlack bit = 1,
	@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit,
		@isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

	If	@StartDay is null or @FinishDay is null or @ComparedStartDay is null
	Begin
		Raiserror('StatFactorAnalysisStartFinishDays', 16, 1)
		return
	end
	

	Set	@StartDay = dbo.ToShortDate(@StartDay)
	Set	@FinishDay = dbo.ToShortDate(@FinishDay)

	declare @ComparedFinishDay datetime
	
	set @ComparedFinishDay = dateadd(day, datediff(day, @StartDay, @FinishDay) , @ComparedStartDay)
			
	create table #res 
	(
		cPrice decimal(18,2),
		price decimal(18,2),
		agencyID smallint,
		firmID int,
		massmediaID smallint,
		paymentTypeID smallint,
		userID smallint,
		massmediaGroupID smallint,
		campaignTypeID tinyint,
		duration decimal(18,2),
		cDuration decimal(18,2),
		headCompanyID int
	)
	
	insert into #res 
	select 0 as comparedPrice,
		x.price as price,
		x.agencyID,
		x.firmID,
		x.massmediaID,
		x.paymentTypeID,
		x.userID,
		x.massmediaGroupID,
		x.campaignTypeID,
		x.duration as duration,
		0 as cDuration,
		f.headCompanyID
	from 
	(
		select c.campaignID, 
			case 
				when 
					c.startDate between @startDay and @finishDay and 
					c.finishDate between @startDay and @finishDay and 
					i.cWithCapacity = 0
				then c.finalPrice
				else i.price
			end as price,
			c.agencyID,
			a.firmID,
			c.massmediaID,
			c.paymentTypeID,
			a.userID,
			mm.massmediaGroupID,
			a.actionID,
			c.campaignTypeID,
			pt.isHidden,
			i.duration
		from dbo.Campaign c 
			inner join dbo.[Action] a on c.actionID = a.actionID
			inner join dbo.MassMedia mm on c.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID
			inner join 
			(
				select	c.campaignID, 
					sum(case when tw.maxCapacity > 0 then 0 else i.[tariffPrice] * i.[ratio] end) as price, 
					sum(case when tw.maxCapacity > 0 then 1 else 0 end) as cWithCapacity, 
					sum(case when tw.maxCapacity > 0 then 0 else 1 end) as cWithoutCapacity, 
					sum(case when tw.maxCapacity > 0 then 0 else r.duration end) as duration
				from	
					Issue i
					inner JOIN Roller r on i.rollerID = r.rollerID
					inner join TariffWindow tw on i.originalWindowID = tw.windowId
					inner join Campaign c on i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
				where c.campaignTypeID = 1 and
					tw.dayOriginal between @StartDay and @FinishDay 
				group by c.campaignID 
			) i on i.campaignID = c.campaignID
			inner join dbo.PaymentType pt on c.paymentTypeID = pt.paymentTypeID
		where c.campaignTypeID = 1 and a.isSpecial = 0 and a.isConfirmed = 1
			and c.StartDate <= @FinishDay and c.FinishDate >= @StartDay
			and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))
		
		union all
		
		select c.campaignID, 
			case 
				when 
					c.startDate between @startDay and @finishDay and 
					c.finishDate between @startDay and @finishDay and 
					i.cWithCapacity = 0
				then c.finalPrice
				else i.price
			end as price,
			c.agencyID,
			a.firmID,
			c.massmediaID,
			c.paymentTypeID,
			a.userID,
			mm.massmediaGroupID,
			a.actionID,
			c.campaignTypeID,
			pt.isHidden,
			i.duration
		from dbo.Campaign c 
			inner join dbo.[Action] a on c.actionID = a.actionID
			inner join dbo.MassMedia mm on c.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID
			inner join 
			(
				select	c.campaignID, 
					sum(case when mpl.maxCapacity > 0 then 0 else i.[tariffPrice] * i.[ratio] end) as price, 
					sum(case when mpl.maxCapacity > 0 then 1 else 0 end) as cWithCapacity, 
					sum(case when mpl.maxCapacity > 0 then 0 else 1 end) as cWithoutCapacity, 
					sum(case when mpl.maxCapacity > 0 then 0 else r.duration end) as duration
				from	
					dbo.ModuleIssue i
					inner JOIN Roller r on i.rollerID = r.rollerID
					inner join Campaign c on i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join 
					(
						select mt.modulePriceListID, max(t.maxCapacity) as maxCapacity
						from dbo.ModuleTariff mt 
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by mt.modulePriceListID 
					) mpl on i.modulePricelistID = mpl.modulePriceListID
				where c.campaignTypeID = 3 and
					i.issueDate between @StartDay and @FinishDay 
				group by c.campaignID 
			) i on i.campaignID = c.campaignID
			inner join dbo.PaymentType pt on c.paymentTypeID = pt.paymentTypeID
		where c.campaignTypeID = 3 and a.isSpecial = 0 and a.isConfirmed = 1
			and c.StartDate <= @FinishDay and c.FinishDate >= @StartDay
			and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))

		union all			

		select c.campaignID, 
			(case 
				when 
					c.startDate between @startDay and @finishDay and 
					c.finishDate between @startDay and @finishDay and 
					i.cWithCapacity = 0
				then c.finalPrice 
				else i.price
			end) * (ii.price / iiSum.price) as price,
			c.agencyID,
			a.firmID,
			mm.massmediaID,
			c.paymentTypeID,
			a.userID,
			mm.massmediaGroupID,
			a.actionID,
			c.campaignTypeID,
			pt.isHidden,
			i.duration
		from dbo.Campaign c 
			inner join dbo.[Action] a on c.actionID = a.actionID
			inner join dbo.MassMedia mm on c.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID
			inner join 
			(
				select c.campaignID, 
					sum(case when pmpl.maxCapacity > 0 then 0 else i.[tariffPrice] * i.[ratio] end) as price, 
					sum(case when pmpl.maxCapacity > 0 then 1 else 0 end) as cWithCapacity, 
					sum(case when pmpl.maxCapacity > 0 then 0 else 1 end) as cWithoutCapacity, 
					sum(case when pmpl.maxCapacity > 0 then 0 else r.duration end) as duration
				from dbo.PackModuleIssue i
					inner JOIN Roller r on i.rollerID = r.rollerID
					inner join Campaign c on i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join (
						select pmc.pricelistID, max(t.maxCapacity) as maxCapacity
						from dbo.PackModuleContent pmc
							inner join dbo.ModuleTariff mt on mt.modulePriceListID = pmc.modulePriceListID
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by pmc.pricelistID
					) pmpl on i.pricelistID = pmpl.pricelistID
				where c.campaignTypeID = 4 and
					i.issueDate between @startDay and @finishDay 
				group by c.campaignID 
			) i on i.campaignID = c.campaignID
			inner join dbo.PaymentType pt on c.paymentTypeID = pt.paymentTypeID
			inner join 
			(
				select c.campaignID, m.massmediaID, sum(mpl.price) as price
				from dbo.PackModuleIssue i 
					inner JOIN dbo.PackModuleContent pmc ON i.priceListID = pmc.pricelistID
					inner JOIN dbo.ModulePriceList mpl ON pmc.modulePriceListID = mpl.modulePriceListID
					inner JOIN dbo.Module m ON mpl.moduleID = m.moduleID
					inner JOIN dbo.Campaign c ON i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join (
						select pmc.pricelistID, max(t.maxCapacity) as maxCapacity
						from dbo.PackModuleContent pmc
							inner join dbo.ModuleTariff mt on mt.modulePriceListID = pmc.modulePriceListID
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by pmc.pricelistID
					) pmpl on i.pricelistID = pmpl.pricelistID
				where i.issueDate between @startDay and @finishDay
				group by c.campaignID, m.massmediaID
			) ii on ii.campaignID = c.campaignID and mm.massmediaID = ii.massmediaID
			inner join 
			(
				select c.campaignID, sum(mpl.price) as price
				from dbo.PackModuleIssue i 
					inner JOIN dbo.PackModuleContent pmc ON i.priceListID = pmc.pricelistID
					inner JOIN dbo.ModulePriceList mpl ON pmc.modulePriceListID = mpl.modulePriceListID
					inner JOIN dbo.Module m ON mpl.moduleID = m.moduleID
					inner JOIN dbo.Campaign c ON i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join (
						select pmc.pricelistID, max(t.maxCapacity) as maxCapacity
						from dbo.PackModuleContent pmc
							inner join dbo.ModuleTariff mt on mt.modulePriceListID = pmc.modulePriceListID
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by pmc.pricelistID
					) pmpl on i.pricelistID = pmpl.pricelistID
				where i.issueDate between @startDay and @finishDay
				group by c.campaignID
			) iiSum on iiSum.campaignID = c.campaignID
		where c.campaignTypeID = 4 and a.isSpecial = 0 and a.isConfirmed = 1
			and c.StartDate <= @FinishDay and c.FinishDate >= @StartDay
			and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))
	) x
	inner join (
		select distinct u.userID 
		from [User] u
			left join [GroupMember] gm on u.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id
		where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
	) xu on x.userID = xu.userID
	inner join dbo.Firm f on x.firmID = f.firmID
	where 
		x.AgencyID = IsNull(@AgencyID, x.AgencyID) and
		x.firmID = IsNull(@FirmID, x.firmID) and
		f.headCompanyID = IsNull(@headCompanyID, f.headCompanyID) and
		x.userID = IsNull(@ManagerID, x.userID) and
		x.PaymentTypeID = IsNull(@PaymentTypeID, x.PaymentTypeID) and
		x.campaignTypeID = IsNull(@CampaignTypeID, x.campaignTypeID) and
		(@ShowWhite <> 0 or x.isHidden <> 0) and  
		(@ShowBlack <> 0 or x.isHidden = 0) and
		(@MassmediaID is null or x.massmediaID = @MassmediaID) and
		(@massmediaGroupID is null or x.massmediaGroupId = @massmediaGroupID)
	
	union all
	
	select x.price as comparedPrice,
		0 as price,
		x.agencyID,
		x.firmID,
		x.massmediaID,
		x.paymentTypeID,
		x.userID,
		x.massmediaGroupID,
		x.campaignTypeID,
		0 as duration,
		x.duration as cDuration,
		f.headCompanyID
	from 
	(
		select c.campaignID, 
			case 
				when 
					c.startDate between @ComparedStartDay and @ComparedFinishDay and 
					c.finishDate between @ComparedStartDay and @ComparedFinishDay and 
					i.cWithCapacity = 0
				then c.finalPrice
				else i.price
			end as price,
			c.agencyID,
			a.firmID,
			c.massmediaID,
			c.paymentTypeID,
			a.userID,
			mm.massmediaGroupID,
			a.actionID,
			c.campaignTypeID,
			pt.isHidden,
			i.duration
		from dbo.Campaign c 
			inner join dbo.[Action] a on c.actionID = a.actionID
			inner join dbo.MassMedia mm on c.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID
			inner join 
			(
				select c.campaignID, 
					sum(case when tw.maxCapacity > 0 then 0 else i.[tariffPrice] * i.[ratio] end) as price, 
					sum(case when tw.maxCapacity > 0 then 1 else 0 end) as cWithCapacity, 
					sum(case when tw.maxCapacity > 0 then 0 else 1 end) as cWithoutCapacity, 
					sum(case when tw.maxCapacity > 0 then 0 else r.duration end) as duration
				from Issue i
					inner JOIN Roller r on i.rollerID = r.rollerID
					inner join TariffWindow tw on i.originalWindowID = tw.windowId
					inner join Campaign c on i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
				where c.campaignTypeID = 1 and
					tw.dayOriginal between @ComparedStartDay and @ComparedFinishDay 
				group by c.campaignID 
			) i on i.campaignID = c.campaignID
			inner join dbo.PaymentType pt on c.paymentTypeID = pt.paymentTypeID
		where c.campaignTypeID = 1 and a.isSpecial = 0 and a.isConfirmed = 1
			and c.StartDate <= @ComparedFinishDay and c.FinishDate >= @ComparedStartDay
			and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))
		
		union all
		
		select c.campaignID, 
			case 
				when 
					c.startDate between @ComparedStartDay and @ComparedFinishDay and 
					c.finishDate between @ComparedStartDay and @ComparedFinishDay and 
					i.cWithCapacity = 0
				then c.finalPrice
				else i.price
			end as price,
			c.agencyID,
			a.firmID,
			c.massmediaID,
			c.paymentTypeID,
			a.userID,
			mm.massmediaGroupID,
			a.actionID,
			c.campaignTypeID,
			pt.isHidden,
			i.duration
		from dbo.Campaign c 
			inner join dbo.[Action] a on c.actionID = a.actionID
			inner join dbo.MassMedia mm on c.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID
			inner join 
			(
				select c.campaignID, 
					sum(case when mpl.maxCapacity > 0 then 0 else i.[tariffPrice] * i.[ratio] end) as price, 
					sum(case when mpl.maxCapacity > 0 then 1 else 0 end) as cWithCapacity, 
					sum(case when mpl.maxCapacity > 0 then 0 else 1 end) as cWithoutCapacity, 
					sum(case when mpl.maxCapacity > 0 then 0 else r.duration end) as duration
				from dbo.ModuleIssue i
					inner JOIN Roller r on i.rollerID = r.rollerID
					inner join Campaign c on i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join 
					(
						select mt.modulePriceListID, max(t.maxCapacity) as maxCapacity
						from dbo.ModuleTariff mt 
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by mt.modulePriceListID 
					) mpl on i.modulePricelistID = mpl.modulePriceListID
				where c.campaignTypeID = 3 and
					i.issueDate between @ComparedStartDay and @ComparedFinishDay 
				group by c.campaignID 
			) i on i.campaignID = c.campaignID
			inner join dbo.PaymentType pt on c.paymentTypeID = pt.paymentTypeID
		where c.campaignTypeID = 3 and a.isSpecial = 0 and a.isConfirmed = 1
			and c.StartDate <= @ComparedFinishDay and c.FinishDate >= @ComparedStartDay
			and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))

		union all			

		select c.campaignID, 
			(case 
				when 
					c.startDate between @ComparedStartDay and @ComparedFinishDay and 
					c.finishDate between @ComparedStartDay and @ComparedFinishDay and 
					i.cWithCapacity = 0
				then c.finalPrice 
				else i.price
			end) * (ii.price / iiSum.price) as price,
			c.agencyID,
			a.firmID,
			mm.massmediaID,
			c.paymentTypeID,
			a.userID,
			mm.massmediaGroupID,
			a.actionID,
			c.campaignTypeID,
			pt.isHidden,
			i.duration
		from dbo.Campaign c 
			inner join dbo.[Action] a on c.actionID = a.actionID
			inner join dbo.MassMedia mm on c.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID
			inner join 
			(
				select c.campaignID, 
					sum(case when pmpl.maxCapacity > 0 then 0 else i.[tariffPrice] * i.[ratio] end) as price, 
					sum(case when pmpl.maxCapacity > 0 then 1 else 0 end) as cWithCapacity, 
					sum(case when pmpl.maxCapacity > 0 then 0 else 1 end) as cWithoutCapacity, 
					sum(case when pmpl.maxCapacity > 0 then 0 else r.duration end) as duration
				from dbo.PackModuleIssue i
					inner JOIN Roller r on i.rollerID = r.rollerID
					inner join Campaign c on i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join (
						select pmc.pricelistID, max(t.maxCapacity) as maxCapacity
						from dbo.PackModuleContent pmc
							inner join dbo.ModuleTariff mt on mt.modulePriceListID = pmc.modulePriceListID
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by pmc.pricelistID
					) pmpl on i.pricelistID = pmpl.pricelistID
				where c.campaignTypeID = 4 and
					i.issueDate between @ComparedStartDay and @ComparedFinishDay 
				group by c.campaignID 
			) i on i.campaignID = c.campaignID
			inner join dbo.PaymentType pt on c.paymentTypeID = pt.paymentTypeID
			inner join 
			(
				select c.campaignID, m.massmediaID, sum(mpl.price) as price
				from dbo.PackModuleIssue i 
					inner JOIN dbo.PackModuleContent pmc ON i.priceListID = pmc.pricelistID
					inner JOIN dbo.ModulePriceList mpl ON pmc.modulePriceListID = mpl.modulePriceListID
					inner JOIN dbo.Module m ON mpl.moduleID = m.moduleID
					inner JOIN dbo.Campaign c ON i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join (
						select pmc.pricelistID, max(t.maxCapacity) as maxCapacity
						from dbo.PackModuleContent pmc
							inner join dbo.ModuleTariff mt on mt.modulePriceListID = pmc.modulePriceListID
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by pmc.pricelistID
					) pmpl on i.pricelistID = pmpl.pricelistID
				where i.issueDate between @ComparedStartDay and @ComparedFinishDay
				group by c.campaignID, m.massmediaID
			) ii on ii.campaignID = c.campaignID and mm.massmediaID = ii.massmediaID
			inner join 
			(
				select c.campaignID, sum(mpl.price) as price
				from dbo.PackModuleIssue i 
					inner JOIN dbo.PackModuleContent pmc ON i.priceListID = pmc.pricelistID
					inner JOIN dbo.ModulePriceList mpl ON pmc.modulePriceListID = mpl.modulePriceListID
					inner JOIN dbo.Module m ON mpl.moduleID = m.moduleID
					inner JOIN dbo.Campaign c ON i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join (
						select pmc.pricelistID, max(t.maxCapacity) as maxCapacity
						from dbo.PackModuleContent pmc
							inner join dbo.ModuleTariff mt on mt.modulePriceListID = pmc.modulePriceListID
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by pmc.pricelistID
					) pmpl on i.pricelistID = pmpl.pricelistID
				where i.issueDate between @ComparedStartDay and @ComparedFinishDay
				group by c.campaignID
			) iiSum on iiSum.campaignID = c.campaignID
		where c.campaignTypeID = 4 and a.isSpecial = 0 and a.isConfirmed = 1
			and c.StartDate <= @ComparedFinishDay and c.FinishDate >= @ComparedStartDay
			and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))
	) x
	inner join (
		select distinct u.userID 
		from [User] u
			left join [GroupMember] gm on u.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id
		where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
	) xu on x.userID = xu.userID
	inner join dbo.Firm f on x.firmID = f.firmID
	where 
		x.AgencyID = IsNull(@AgencyID, x.AgencyID) and
		x.firmID = IsNull(@FirmID, x.firmID) and
		f.headCompanyID = IsNull(@headCompanyID, f.headCompanyID) and
		x.userID = IsNull(@ManagerID, x.userID) and
		x.PaymentTypeID = IsNull(@PaymentTypeID, x.PaymentTypeID) and
		x.campaignTypeID = IsNull(@CampaignTypeID, x.campaignTypeID) and
		(@ShowWhite <> 0 or x.isHidden <> 0) and  
		(@ShowBlack <> 0 or x.isHidden = 0) and
		(@MassmediaID is null or x.massmediaID = @MassmediaID) and
		(@massmediaGroupID is null or x.massmediaGroupId = @massmediaGroupID)


	declare	@SQLString NVARCHAR(max),
				@IsStarted int
	
	set @SQLString = '
	select row_number() over(order by IsNull(Sum(price), 0)) as RowNum, '
	
	If	@IsGroupByPaymentType <> 0
		Set 	@SQLString = @SQLString + N'Paymenttype.Name as "payment_type",'
	If	@IsGroupByCampaignType <> 0
		Set 	@SQLString = @SQLString + N'iCampaignType.Name as "campaign_type",'
	If	@IsGroupByMassmedia <> 0
		Set 	@SQLString = @SQLString + N'vMassMedia.Name as "massmedia", vMassmedia.GroupName as "massmedia_group",'
	If	@IsGroupByMassmediaGroupType <> 0
		Set 	@SQLString = @SQLString + N'MassmediaGroup.Name as "massmedia_group",'
	If	@IsGroupByFirm <> 0
		Set 	@SQLString = @SQLString + N'Firm.Name as "firm",'
	If	@IsGroupByHeadCompany <> 0
		Set 	@SQLString = @SQLString + N'HeadCompany.Name as "head_company",'
	If	@IsGroupByManager <> 0
		Set 	@SQLString = @SQLString + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''') as "manager",'
	If	@IsGroupByAgency <> 0
		Set 	@SQLString = @SQLString + N'Agency.Name as "agency",'
	
	set @SQLString = @SQLString + '
	
		case when sum(r.duration) = 0 then 0 else sum(r.price)/sum(r.duration) end avgPrice,
		case when sum(r.cduration) = 0 then 0 else sum(r.cprice)/sum(r.cduration) end avgCPrice,
		dbo.fn_Int2Time(cast(sum(r.duration) as int)) duration,
		dbo.fn_Int2Time(cast(sum(r.cduration) as int)) cduration,
		sum(r.price) as price,
		sum(r.cprice) as cprice,
		1.0/2.0 * (sum(r.duration) - sum(r.cDuration)) * (case when sum(r.duration) = 0 then 0 else sum(r.price)/sum(r.duration) end + case when sum(r.cduration) = 0 then 0 else sum(r.cprice)/sum(r.cduration) end) as q1,
		1.0/2.0 * (sum(r.duration) + sum(r.cDuration)) * (case when sum(r.duration) = 0 then 0 else sum(r.price)/sum(r.duration) end - case when sum(r.cduration) = 0 then 0 else sum(r.cprice)/sum(r.cduration) end) as q2,
		1.0/2.0 * (sum(r.duration) - sum(r.cDuration)) * (case when sum(r.duration) = 0 then 0 else sum(r.price)/sum(r.duration) end + case when sum(r.cduration) = 0 then 0 else sum(r.cprice)/sum(r.cduration) end)
		+ 1.0/2.0 * (sum(r.duration) + sum(r.cDuration)) * (case when sum(r.duration) = 0 then 0 else sum(r.price)/sum(r.duration) end - case when sum(r.cduration) = 0 then 0 else sum(r.cprice)/sum(r.cduration) end) as q
	from #res r '
	
	If	@IsGroupByMassmediaGroupType <> 0 Set @SQLString = @SQLString + N' inner join MassmediaGroup on r.massmediaGroupID = MassmediaGroup.massmediaGroupID '
	If	@IsGroupByPaymentType <> 0 Set @SQLString = @SQLString + N' inner join Paymenttype on r.PaymentTypeID = Paymenttype.PaymenttypeID'
	If	@IsGroupByCampaignType <> 0 Set @SQLString = @SQLString + N' inner join iCampaignType on r.campaignTypeID = iCampaignType.CampaignTypeID'
	If	@IsGroupByMassmedia <> 0 Set @SQLString = @SQLString + N' inner join vMassMedia on r.massmediaID = vMassMedia.massmediaID'
	If	@IsGroupByFirm <> 0 Set @SQLString = @SQLString + N' inner join Firm on r.firmID = Firm.FirmID '
	If	@IsGroupByHeadCompany <> 0 Set @SQLString = @SQLString + N' inner join HeadCompany on r.headCompanyID = HeadCompany.headCompanyID '
	If	@IsGroupByManager <> 0 Set @SQLString = @SQLString + N' inner join [User] on r.userID = [User].UserID'
	If	@IsGroupByAgency <> 0 Set @SQLString = @SQLString + N' inner join Agency on r.AgencyID = Agency.AgencyID'
	
	If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + 
	@IsGroupByMassmedia + @IsGroupByFirm + 
	@IsGroupByHeadCompany + @IsGroupByManager + 
	@IsGroupByAgency + @IsGroupByMassmediaGroupType <> 0
	begin

	-- Group By part
	set	@IsStarted = 0
	Set 	@SQLString = @SQLString + N' Group by '

	if	@IsGroupByPaymentType <> 0 begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Paymenttype.Name'
		set	@IsStarted = 1
	end

	If	@IsGroupByCampaignType <> 0
		begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'iCampaignType.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByMassmedia <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'vMassMedia.Name, vMassMedia.GroupName'
		set	@IsStarted = 1
		end

	If	@IsGroupByFirm <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Firm.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByHeadCompany <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'HeadCompany.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByManager <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''')'
		set	@IsStarted = 1
		end

	If	@IsGroupByAgency <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Agency.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByMassmediaGroupType <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set @SQLString = @SQLString + N'MassmediaGroup.Name'
		set	@IsStarted = 1
		end

	end
	
	EXECUTE sp_executesql @SQLString
END
GO
PRINT '  ok: dbo.statFactorAnalysis';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER Procedure [dbo].[stat_VolumeOfRealization]
(
@StartDay DATETIME = default,
@FinishDay DATETIME = default,
@FirmID int = default, 
@MassmediaID int = default, 
@PaymentTypeID int = default,
@CampaignTypeID int = default,
@ManagerID int = default,
@AgencyID int = default,
@IsGroupByPaymentType bit = 0,
@IsGroupByCampaignType bit = 0,
@IsGroupByMassmedia bit = 0,
@IsGroupByFirm bit = 0,
@IsGroupByManager bit = 0,
@IsGroupByAgency bit = 0,
@IsGroupByMassmediaGroupType bit = 0,
@massmediaGroupID int = NULL,
@ShowWhite bit = 1,
@ShowBlack bit = 1,
@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
As

SET NOCOUNT ON

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

declare @isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

If	@StartDay Is Null Or @FinishDay Is Null
	Begin
	Raiserror('FilterStartFinishDays', 16, 1)
	Return
	End

CREATE TABLE #tmp1
(
CompanyPrice decimal(18,2),
MassmediaID SMALLINT,
PaymentTypeID SMALLINT,
ActionID INT,
campaignTypeID SMALLINT,
Manager_ID SMALLINT,
AgencyID SMALLINT,
massmediaGroupID int
)

Set	@StartDay = dbo.ToShortDate(@StartDay)
Set	@FinishDay = dbo.ToShortDate(@FinishDay)

-- select all companies, which has appropriated 
-- start and finish dates
Declare cur_companies Cursor Local fast_forward
For
select distinct  c.campaignID, c.ActionID, c.massmediaID, 
		c.PaymentTypeID, c.campaignTypeID, a.userID, c.AgencyID, 
		c.[startDate], max(coalesce(mm.massmediaGroupID,0)), a.discount, c.finalPrice, c.finishDate
From	
	Campaign c
	INNER Join [Action] a On c.ActionID = a.actionID
		AND a.[isConfirmed] = 1
	INNER JOIN PaymentType On c.PaymentTypeID = PaymentType.PaymentTypeID
	left join MassMedia mm on c.massmediaID = mm.massmediaID
	left JOIN [PackModuleIssue] pmi ON pmi.[campaignID] = c.[campaignID]
	left JOIN [PackModuleContent] pmc ON pmc.[pricelistID] = pmi.[pricelistID]
	left JOIN [Module] m ON pmc.[moduleID] = m.[moduleID]
	inner join @massmedias mmu on (mm.massmediaID = mmu.massmediaID 
						or m.massmediaID = mmu.massmediaID)
	inner join MassMedia mmfu on mmu.massmediaID = mmfu.massmediaID 
	left join GroupMember gm on a.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
Where	
	(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
	c.StartDate <= @FinishDay and
	c.FinishDate >= @StartDay and
	c.AgencyID = IsNull(@AgencyID, c.AgencyID) and
	a.firmID = IsNull(@FirmID, a.firmID) and
	a.userID = IsNull(@ManagerID, a.userID)  and
	c.PaymentTypeID = IsNull(@PaymentTypeID, c.PaymentTypeID) and
	c.campaignTypeID = IsNull(@CampaignTypeID, c.campaignTypeID) and
	(@ShowWhite <> 0 or PaymentType.isHidden <> 0) and  
	(@ShowBlack <> 0 or PaymentType.isHidden = 0)  
	and (@MassmediaID is null or mmfu.massmediaID = @MassmediaID)
	and (@massmediaGroupID is null or mmfu.massmediaGroupId = @massmediaGroupID)
	and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))

group by c.campaignID, c.ActionID, c.massmediaID, 
		c.PaymentTypeID, c.campaignTypeID, a.userID, c.AgencyID, 
		c.[startDate], a.discount, c.finalPrice, c.finishDate

-- select all companies, which have Issues inside interval
Declare	@campaignID int, 
	@ActionID int, 
	@campaignPrice decimal(18,2),
	@SummaVar decimal(18,2),
	@CompStartDate datetime,
	@actionDiscount float,
	@sumPrice decimal(18,2),
	@finalPrice decimal(18,2),
	@cfinishDate datetime,
	@campMassmediaGroupID int,
	@mmID smallint
	
declare @tmp table (massmediaID smallint, price decimal(18,2))

Open	cur_companies
Fetch	next from cur_companies into 
	@campaignID, @ActionID, @mmID, @PaymenttypeID, 
	@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @campMassmediaGroupID, @actionDiscount, @finalPrice, @cfinishDate

--Set	@FinishDay = Convert(datetime, Convert(varchar, @FinishDay, 112), 112) - 1
While	@@fetch_status = 0
begin
	if @FinishDay < @cfinishDate or @StartDay > @CompStartDate
		exec GetPriceByPeriod @campaignId, @CampaignTypeID, @StartDay, @FinishDay, @campaignPrice out
	else 
		set @campaignPrice = @finalPrice

	IF @CampaignTypeID = 4
	begin
		delete from @tmp
				
		insert into @tmp(massmediaID, price)
		select
			m.[massmediaID], sum(mpl.[price])
		from [PackModuleIssue] i 
			INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
			INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
			INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
		where 
			i.campaignID = @campaignID	and
			i.issueDate between @StartDay and @FinishDay 
		group by m.massmediaID
			
		select @sumPrice = sum(t1.price) FROM @tmp AS t1
		
		insert into #tmp1 ([CompanyPrice],	[MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],	[AgencyID],massmediaGroupID) 
		select @campaignPrice * sum(t1.price)/ @sumPrice,  t1.massmediaID, @PaymenttypeID, @ActionID, @CampaignTypeID,@ManagerID, @AgencyID, mm.massmediaGroupID
		from @tmp as t1
			inner join MassMedia mm on t1.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID 
		where t1.price > 0  
			and (@MassmediaID is null or mm.massmediaID = @MassmediaID)
			and (@massmediaGroupID is null or mm.massmediaGroupId = @massmediaGroupID)
		group by t1.massmediaID, mm.massmediaGroupID
	END
	ELSE
	begin
		if	@campaignPrice > 0 
			Insert	Into #tmp1 ([CompanyPrice],[MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],[AgencyID],massmediaGroupID) 
			Values(@campaignPrice,  @mmID, @PaymenttypeID, @ActionID, @CampaignTypeID, @ManagerID, @AgencyID, @campMassmediaGroupID)
	end
		
	fetch next from cur_companies into 
			@campaignID, @ActionID, @mmID, @PaymenttypeID,
			@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @campMassmediaGroupID, @actionDiscount, @finalPrice, @cfinishDate
End	

close cur_companies
deallocate cur_companies

Select	@SummaVar = IsNull(sum(CompanyPrice), 0) From	#tmp1

-- output ---------------------------------------------------------
Declare	@SQLString NVARCHAR(2500),
				@IsStarted int

/* Build the SQL string once.*/
Set	@SQLString = N'Select	row_number() over(order by IsNull(Sum(CompanyPrice), 0)) as RowNum,'
Set	@SQLString = @SQLString + N' IsNull(Sum(CompanyPrice), 0) as  sum1'

Set @SQLString = @SQLString + N',  '

/*
If	@IsGroupByCommissionaire <> 0
	Set 	@SQLString = @SQLString + N'Dic_Commissionaire.Description as "Комиссионер",'
*/
If	@IsGroupByPaymentType <> 0
	Set 	@SQLString = @SQLString + N'Paymenttype.Name as "payment_type",'
If	@IsGroupByCampaignType <> 0
	Set 	@SQLString = @SQLString + N'iCampaignType.Name as "campaign_type",'
If	@IsGroupByMassmedia <> 0
	Set 	@SQLString = @SQLString + N'vMassMedia.NameWithGroup as "massmedia", vMassMedia.massmediaID,'
If	@IsGroupByMassmediaGroupType <> 0
	Set 	@SQLString = @SQLString + N'MassmediaGroup.Name as "massmedia_group",'
If	@IsGroupByFirm <> 0
	Set 	@SQLString = @SQLString + N'Firm.Name as "firm",'
If	@IsGroupByManager <> 0
	Set 	@SQLString = @SQLString + N'[User].userName as "manager",'
If	@IsGroupByAgency <> 0
	Set 	@SQLString = @SQLString + N'Agency.Name as "agency",'

If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + 
	@IsGroupByMassmedia + @IsGroupByFirm + 
	@IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType  = 0
	set		@SQLString = @SQLString + N'max(''Все'') as "all",'

Set 	@SQLString = @SQLString + 
		N'case @Summa
			when	0 then 0
			else	Cast((IsNull(Sum(CompanyPrice), 0) * 100.0 / @Summa) as decimal(12,2))
		End as "percent"	
From	#tmp1'

If	@IsGroupByMassmediaGroupType <> 0 Set @SQLString = @SQLString + N' inner join MassmediaGroup on #tmp1.massmediaGroupID = MassmediaGroup.massmediaGroupID '
If	@IsGroupByPaymentType <> 0 Set @SQLString = @SQLString + N' inner join Paymenttype on #tmp1.PaymentTypeID = Paymenttype.PaymenttypeID'
If	@IsGroupByCampaignType <> 0 Set @SQLString = @SQLString + N' inner join iCampaignType on #tmp1.campaignTypeID = iCampaignType.CampaignTypeID'
If	@IsGroupByMassmedia <> 0 Set @SQLString = @SQLString + N' inner join vMassMedia on #tmp1.massmediaID = vMassMedia.massmediaID'
If	@IsGroupByFirm <> 0 Set @SQLString = @SQLString + N' inner join Action on #tmp1.ActionID = Action.ActionID inner join Firm on Action.firmID = Firm.FirmID '
If	@IsGroupByManager <> 0 Set @SQLString = @SQLString + N' inner join [User] on #tmp1.Manager_ID = [User].UserID'
If	@IsGroupByAgency <> 0 Set @SQLString = @SQLString + N' inner join Agency on #tmp1.AgencyID = Agency.AgencyID'

Set 	@SQLString = @SQLString + N' Where CompanyPrice <> 0 '

If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + 
	@IsGroupByMassmedia + @IsGroupByFirm + 
	@IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType <> 0
	begin

	-- Group By part
	set	@IsStarted = 0
	Set 	@SQLString = @SQLString + N' Group by '

/*
	if	@IsGroupByCommissionaire <> 0 begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Dic_Commissionaire.Description'
		set	@IsStarted = 1
	end
*/
	if	@IsGroupByPaymentType <> 0 begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Paymenttype.Name'
		set	@IsStarted = 1
	end

	If	@IsGroupByCampaignType <> 0
		begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'iCampaignType.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByMassmedia <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'vMassMedia.NameWithGroup, vMassMedia.massmediaID'
		set	@IsStarted = 1
		end

	If	@IsGroupByFirm <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Firm.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByManager <> 0
		begin

		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'[User].userName'
		set	@IsStarted = 1
		end

	If	@IsGroupByAgency <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Agency.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByMassmediaGroupType <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set @SQLString = @SQLString + N'MassmediaGroup.Name'
		set	@IsStarted = 1
		end

	end

EXECUTE sp_executesql @SQLString,
	N'@Summa decimal(18,2)',
	@Summa = @SummaVar		

Drop		table #tmp1
GO
PRINT '  ok: dbo.stat_VolumeOfRealization';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER  Procedure [dbo].[stat_VolumeOfRealizationByMonth]
(
@StartDay DATETIME = null,
@FinishDay DATETIME = null,
@FirmID int = null, 
@ManagerID int = null,
@agencyID int = null,
@massmediaID smallint =NULL,
@IsGroupByMassmedia bit = 0,
@IsGroupByFirm bit = 0,
@IsGroupByManager bit = 0,
@IsGroupByAgency bit = 0,
@ShowWhite bit = 1,
@ShowBlack bit = 1,
@loggedUserId smallint 
)
WITH EXECUTE AS OWNER
As

SET NOCOUNT ON

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUserMassmedia(@loggedUserID, @massmediaID)

	declare @isRightToViewForeignActions bit,
			@isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

	If	@StartDay Is Null Or @FinishDay Is Null
	Begin
		Raiserror('FilterStartFinishDays', 16, 1)
		Return
	End

	Set	@StartDay = dbo.ToShortDate(@StartDay)
	Set	@FinishDay = dbo.ToShortDate(@FinishDay)

	declare @month tinyint, @year smallint, @finishYear smallint, @finishMonth tinyint 
	
	set @month = datepart(month, @StartDay)
	set @year = datepart(year, @StartDay)
	set @finishMonth = datepart(month, @FinishDay)
	set @finishYear = datepart(year, @FinishDay)

	create table #res (m tinyint, y smallint, massmediaID smallint, actionID int, managerID smallint, agencyID smallint, price money)
	
	declare @start datetime, @end datetime
	
	declare	@campaignID int, @actionID int, @mmID smallint, @campaignTypeID tinyint, @userID smallint, @campaignPrice money, @cStart datetime, @cEnd datetime, @aDiscount float, @cAgencyID int
	
	while @year < @finishYear or (@year = @finishYear and @month <= @finishMonth)
	begin 
		set @start = convert(datetime,'01.' + cast(@month as varchar) + '.' + cast(@year as varchar), 104)
		set @end = dateadd(day, -1, dateadd(month, 1, convert(datetime,'01.' + cast(@month as varchar) + '.' + cast(@year as varchar), 104)))

		if @@error <> 0
		begin 
			raiserror('InternalError', 16, 1)
			return
		end 

		if @StartDay > @start
			set @start = @StartDay
		
		if @FinishDay < @end
			set @end = @FinishDay

		declare cur_companies cursor local fast_forward
		for
		select c.campaignID, c.actionID, c.massmediaID, c.campaignTypeID, a.userID, c.agencyID, c.startDate, c.finishDate, a.discount, c.finalPrice
		from	
			Campaign c
			inner join [Action] a On c.ActionID = a.actionID
				and a.[isConfirmed] = 1 and a.isSpecial = 0
			inner join PaymentType On c.PaymentTypeID = PaymentType.PaymentTypeID
			inner join @massmedias umm on c.massmediaID = umm.massmediaID
				and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
			inner join 
				(
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join @ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as x on a.userID = x.userID
		where c.campaignTypeID <> 4 and c.StartDate <= @end and
			c.FinishDate >= @start and
			c.AgencyID = coalesce(@AgencyID, c.AgencyID) and
			a.firmID = coalesce(@FirmID, a.firmID) and
			a.userID = coalesce(@ManagerID, a.userID) and
			(@ShowWhite <> 0 or PaymentType.isHidden <> 0) and  
			(@ShowBlack <> 0 or PaymentType.isHidden = 0) 
		union 
		select distinct	c.campaignID, c.actionID, m.massmediaID, c.campaignTypeID, a.userID, c.agencyID, c.startDate, c.finishDate, a.discount, c.finalPrice
		From	
			Campaign c
			INNER Join [Action] a On c.ActionID = a.actionID
				AND a.[isConfirmed] = 1 and a.isSpecial = 0
			INNER JOIN PaymentType On c.PaymentTypeID = PaymentType.PaymentTypeID
			INNER JOIN [PackModuleIssue] pmi ON pmi.[campaignID] = c.[campaignID]
			INNER JOIN [PackModuleContent] pmc ON pmc.[pricelistID] = pmi.[pricelistID]
			INNER JOIN [Module] m ON pmc.[moduleID] = m.[moduleID]
			inner join @massmedias umm on m.massmediaID = umm.massmediaID
				and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
			inner join 
				(
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join @ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as x on a.userID = x.userID
		where c.[campaignTypeID] = 4 and
				c.StartDate <= @end and
				c.FinishDate >= @start and
				c.AgencyID = coalesce(@AgencyID, c.AgencyID) and
				a.firmID = coalesce(@FirmID, a.firmID) and
				a.userID = coalesce(@ManagerID, a.userID) and
				(@ShowWhite <> 0 or PaymentType.isHidden <> 0) and  
				(@ShowBlack <> 0 or PaymentType.isHidden = 0)
			
		open cur_companies	
		fetch next from cur_companies into @campaignID, @actionID, @mmID, @campaignTypeID, @userID, @cAgencyID, @cStart, @cEnd, @aDiscount, @campaignPrice
		
		while	@@fetch_status = 0
		begin 
			-- Кампания целиком внутри периода: @campaignPrice = c.finalPrice, а он
			-- теперь уже со всеми скидками -- брать как есть. Иначе считаем по периоду.
			-- Условие -- отрицание прежнего if, с явной проверкой NULL: у between
			-- с NULL результат unknown, и not(unknown) ветку бы не открыл.
			if @campaignTypeID = 4
				or @cStart is null or @cEnd is null
				or @cStart not between @start and @end
				or @cEnd not between @start and @end
				exec GetPriceByPeriod @campaignID, @campaignTypeID, @start, @end, @campaignPrice OUTPUT, @mmID
			
			if @campaignPrice > 0
			begin
				insert into #res (m,y,massmediaID,actionID,managerID,agencyID,price) 
				values (@month,@year,@mmID,@actionID,@userID,@cAgencyID,@campaignPrice ) 
			end
			
			fetch next from cur_companies into @campaignID, @actionID, @mmID, @campaignTypeID, @userID, @cAgencyID, @cStart, @cEnd, @aDiscount, @campaignPrice
		end 
		
		close cur_companies	
		deallocate cur_companies
	
		set @month = @month + 1
		if (@month > 12)
		begin
			set @month = 1
			set @year = @year + 1
		end 
	end 
	
	-------------------------------------------------------------------------
	declare	@sql nvarchar(max)
	set @sql = 'select row_number() over(order by #res.[y], #res.[m]) as RowNum,
				iMonthName.name + space(1) + cast(#res.[y] as varchar) + '' г.'' as [period],'
			
	if @IsGroupByMassmedia <> 0
		set @sql = @sql + 'vMassMedia.name as mmName, vMassMedia.groupName, '
	if @IsGroupByFirm <> 0
		set @sql = @sql + 'Firm.Name as firmName,'
	if @IsGroupByManager <> 0
		set @sql = @sql + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''') as manager,'
	if @IsGroupByAgency <> 0
		set @sql = @sql + N'Agency.Name as agencyName,'

	set @sql = @sql + ' sum(#res.price) as price from #res inner join [Action] on #res.actionID = [Action].actionID inner join iMonthName on #res.m = iMonthName.number '
	
	if @IsGroupByMassmedia <> 0
		set @sql = @sql + ' inner join vMassMedia on #res.massmediaID = vMassMedia.massmediaID '
	if @IsGroupByFirm <> 0
		set @sql = @sql + ' inner join Firm on [Action].firmID = Firm.firmID '
	if @IsGroupByManager <> 0
		set @sql = @sql + ' inner join [User] on [Action].userID = [User].userID '
	if @IsGroupByAgency <> 0
		set @sql = @sql + ' inner join Agency on #res.agencyID = Agency.agencyID '

	set @sql = @sql + ' group by #res.[y], #res.[m], iMonthName.name '

	if @IsGroupByMassmedia <> 0
		set @sql = @sql + ',vMassMedia.name, vMassMedia.groupName '
	if @IsGroupByFirm <> 0
		set @sql = @sql + ',Firm.name '
	if @IsGroupByManager <> 0
		set @sql = @sql + ',coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''') '
	if @IsGroupByAgency <> 0
		set @sql = @sql + ',Agency.name '
	
	set @sql = @sql + ' order by #res.[y], #res.[m] '
	--select @sql
	EXECUTE sp_executesql @sql
	
	drop table #res
GO
PRINT '  ok: dbo.stat_VolumeOfRealizationByMonth';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER Procedure [dbo].[stat_VolumeOfRealizationNew]
(
@StartDay DATETIME = default,
@FinishDay DATETIME = default,
@FirmID int = default, 
@MassmediaID int = default, 
@PaymentTypeID int = default,
@CampaignTypeID int = default,
@ManagerID int = default,
@AgencyID int = default,
@AdvertTypeID int = default,
@IsGroupByPaymentType bit = 0,
@IsGroupByCampaignType bit = 0,
@IsGroupByMassmedia bit = 0,
@IsGroupByFirm bit = 0,
@IsGroupByManager bit = 0,
@IsGroupByAgency bit = 0,
@IsGroupByMassmediaGroupType bit = 0,
@IsGroupByAdvertType bit = 0,
@massmediaGroupID int = NULL,
@ShowWhite bit = 1,
@ShowBlack bit = 1,
@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
As

SET NOCOUNT ON

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

declare @isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

If	@StartDay Is Null Or @FinishDay Is Null
	Begin
	Raiserror('FilterStartFinishDays', 16, 1)
	Return
	End

CREATE TABLE #tmp1
(
CompanyPrice decimal(18,2),
MassmediaID SMALLINT,
PaymentTypeID SMALLINT,
ActionID INT,
campaignTypeID SMALLINT,
Manager_ID SMALLINT,
AgencyID SMALLINT,
massmediaGroupID int,
advertTypeID smallint,
issuePrice decimal(18,2)
)

Set	@StartDay = dbo.ToShortDate(@StartDay)
Set	@FinishDay = dbo.ToShortDate(@FinishDay)

-- select all companies, which has appropriated 
-- start and finish dates
Declare cur_companies Cursor Local fast_forward
For
select distinct  
	c.campaignID, c.ActionID, c.massmediaID, 
	c.PaymentTypeID, c.campaignTypeID, a.userID, c.AgencyID, 
	--c.[startDate], max(coalesce(mm.roltypeID,0)),  max(coalesce(mm.massmediaGroupID,0)), 
	c.[startDate], mm.massmediaGroupID,  --max(coalesce(mm.massmediaGroupID,0)), 
	a.discount, c.finalPrice, c.finishDate, r.advertTypeID, i.ratio * i.tariffPrice
From	
	Campaign c
	INNER Join [Action] a On c.ActionID = a.actionID AND a.[isConfirmed] = 1
	INNER JOIN Issue i On i.campaignID = c.campaignID
	INNER Join Roller r On r.rollerID = i.rollerID
	INNER JOIN PaymentType On c.PaymentTypeID = PaymentType.PaymentTypeID
	left join MassMedia mm on c.massmediaID = mm.massmediaID	
	left JOIN [PackModuleIssue] pmi ON pmi.[campaignID] = c.[campaignID]
	left JOIN [PackModuleContent] pmc ON pmc.[pricelistID] = pmi.[pricelistID]
	left JOIN [Module] m ON pmc.[moduleID] = m.[moduleID]	
	inner join @massmedias mmu on (mm.massmediaID = mmu.massmediaID 
						or m.massmediaID = mmu.massmediaID)
	inner join MassMedia mmfu on mmu.massmediaID = mmfu.massmediaID 	
	left join GroupMember gm on a.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
Where	
	(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
	c.StartDate <= @FinishDay and
	c.FinishDate >= @StartDay and
	c.AgencyID = IsNull(@AgencyID, c.AgencyID) and
	a.firmID = IsNull(@FirmID, a.firmID) and
	a.userID = IsNull(@ManagerID, a.userID) and
	c.PaymentTypeID = IsNull(@PaymentTypeID, c.PaymentTypeID) and
	c.campaignTypeID = IsNull(@CampaignTypeID, c.campaignTypeID) and
	(@ShowWhite <> 0 or PaymentType.isHidden <> 0) and  
	(@ShowBlack <> 0 or PaymentType.isHidden = 0)  
	and (@MassmediaID is null or mmfu.massmediaID = @MassmediaID)
	and (@massmediaGroupID is null or mmfu.massmediaGroupId = @massmediaGroupID)
	and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))
	and r.advertTypeID = IsNull(@AdvertTypeID, r.advertTypeID)
/*
group by 
	c.campaignID, c.ActionID, c.massmediaID, 
	c.PaymentTypeID, c.campaignTypeID, a.userID, c.AgencyID, 
	c.[startDate], a.discount, c.finalPrice, c.finishDate,
	mm.roltypeID
*/

-- select all companies, which have Issues inside interval
Declare	
	@campaignID int, 
	@ActionID int, 
	@campaignPrice decimal(18,2),
	@SummaVar decimal(18,2),
	@CompStartDate datetime,
	@actionDiscount decimal(9,4),
	@sumPrice decimal(18,2),
	@finalPrice decimal(18,2),
	@cfinishDate datetime,
	@campMassmediaGroupID int,
	@mmID smallint,
	@advTypeID smallint,
	@issuePrice decimal(18,2)
	
declare @tmp table (massmediaID smallint, price decimal(18,2))

Open	cur_companies
Fetch	next from cur_companies into 
	@campaignID, @ActionID, @mmID, @PaymenttypeID, 
	@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @campMassmediaGroupID, @actionDiscount, 
	@finalPrice, @cfinishDate, @advTypeID, @issuePrice

--Set	@FinishDay = Convert(datetime, Convert(varchar, @FinishDay, 112), 112) - 1
While	@@fetch_status = 0
begin
	if @FinishDay < @cfinishDate or @StartDay > @CompStartDate
		exec GetPriceByPeriod @campaignId, @CampaignTypeID, @StartDay, @FinishDay, @campaignPrice out
	else 
		set @campaignPrice = @finalPrice

	IF @CampaignTypeID = 4
	begin
		delete from @tmp
				
		insert into @tmp(massmediaID, price)
		select
			m.[massmediaID], sum(mpl.[price])
		from [PackModuleIssue] i 
			INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
			INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
			INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
		where 
			i.campaignID = @campaignID	and
			i.issueDate between @StartDay and @FinishDay 
		group by m.massmediaID
			
		select @sumPrice = sum(t1.price) FROM @tmp AS t1
		
		insert into #tmp1 ([CompanyPrice],	[MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],	[AgencyID], massmediaGroupID) 
		select @campaignPrice * sum(t1.price)/ @sumPrice,  t1.massmediaID, @PaymenttypeID, @ActionID, @CampaignTypeID,@ManagerID, @AgencyID, mm.massmediaGroupID
		from @tmp as t1
			inner join MassMedia mm on t1.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID 
		where t1.price > 0  
			and (@MassmediaID is null or mm.massmediaID = @MassmediaID)
			and (@massmediaGroupID is null or mm.massmediaGroupId = @massmediaGroupID)
		group by t1.massmediaID, mm.massmediaGroupID
	END
	ELSE
	begin
		if	@campaignPrice > 0 
			Insert	Into #tmp1 ([CompanyPrice],[MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],[AgencyID], massmediaGroupID, advertTypeID) 
			Values(@campaignPrice,  @mmID, @PaymenttypeID, @ActionID, @CampaignTypeID, @ManagerID, @AgencyID, @campMassmediaGroupID, @advTypeID)
	end
		
	fetch next from cur_companies into 
			@campaignID, @ActionID, @mmID, @PaymenttypeID,
			@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @campMassmediaGroupID, @actionDiscount, 
			@finalPrice, @cfinishDate, @advTypeID, @issuePrice
End	

close cur_companies
deallocate cur_companies

Select	@SummaVar = IsNull(sum(CompanyPrice), 0) From	#tmp1

-- output ---------------------------------------------------------
Declare	@SQLString NVARCHAR(2500),
				@IsStarted int

/* Build the SQL string once.*/
Set	@SQLString = N'Select	row_number() over(order by IsNull(Sum(CompanyPrice), 0)) as RowNum,'
Set	@SQLString = @SQLString + N' IsNull(Sum(CompanyPrice), 0) as  sum1'

Set @SQLString = @SQLString + N',  '

If	@IsGroupByAdvertType <> 0
	Set 	@SQLString = @SQLString + N'AdvertType.Name as "advert_type",'
If	@IsGroupByPaymentType <> 0
	Set 	@SQLString = @SQLString + N'Paymenttype.Name as "payment_type",'
If	@IsGroupByCampaignType <> 0
	Set 	@SQLString = @SQLString + N'iCampaignType.Name as "campaign_type",'
If	@IsGroupByMassmedia <> 0
	Set 	@SQLString = @SQLString + N'vMassMedia.Name as "massmedia", vMassMedia.groupName as "massmedia_group",'
If	@IsGroupByMassmediaGroupType <> 0
	Set 	@SQLString = @SQLString + N'MassmediaGroup.Name as "massmedia_group",'
If	@IsGroupByFirm <> 0
	Set 	@SQLString = @SQLString + N'Firm.Name as "firm",'
If	@IsGroupByManager <> 0
	Set 	@SQLString = @SQLString + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''') as "manager",'
If	@IsGroupByAgency <> 0
	Set 	@SQLString = @SQLString + N'Agency.Name as "agency",'
If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + 
	@IsGroupByMassmedia + @IsGroupByFirm + @IsGroupByAdvertType +
	@IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType /*+ @IsGroupByCommissionaire*/ = 0
	set		@SQLString = @SQLString + N'max(''Все'') as "all",'

Set 	@SQLString = @SQLString + 
		N'case @Summa
			when	0 then 0
			else	Cast((IsNull(Sum(CompanyPrice), 0) * 100.0 / @Summa) as decimal(18,2))
		End as "percent"	
From	#tmp1'

If	@IsGroupByMassmediaGroupType <> 0 Set @SQLString = @SQLString + N' inner join MassmediaGroup on #tmp1.massmediaGroupID = MassmediaGroup.massmediaGroupID '
If	@IsGroupByPaymentType <> 0 Set @SQLString = @SQLString + N' inner join Paymenttype on #tmp1.PaymentTypeID = Paymenttype.PaymenttypeID'
If	@IsGroupByCampaignType <> 0 Set @SQLString = @SQLString + N' inner join iCampaignType on #tmp1.campaignTypeID = iCampaignType.CampaignTypeID'
If	@IsGroupByMassmedia <> 0 Set @SQLString = @SQLString + N' inner join vMassMedia on #tmp1.massmediaID = vMassMedia.massmediaID'
If	@IsGroupByAdvertType <> 0 Set @SQLString = @SQLString + N' inner join AdvertType on #tmp1.advertTypeID = AdvertType.advertTypeID'
If	@IsGroupByFirm <> 0 Set @SQLString = @SQLString + N' inner join Action on #tmp1.ActionID = Action.ActionID inner join Firm on Action.firmID = Firm.FirmID '
If	@IsGroupByManager <> 0 Set @SQLString = @SQLString + N' inner join [User] on #tmp1.Manager_ID = [User].UserID'
If	@IsGroupByAgency <> 0 Set @SQLString = @SQLString + N' inner join Agency on #tmp1.AgencyID = Agency.AgencyID'


Set 	@SQLString = @SQLString + N' Where CompanyPrice <> 0 '

If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + 
	@IsGroupByMassmedia + @IsGroupByFirm + @IsGroupByAdvertType +
	@IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType /*+ @IsGroupByCommissionaire*/ <> 0
	begin

	-- Group By part
	set	@IsStarted = 0
	Set 	@SQLString = @SQLString + N' Group by '

/*
	if	@IsGroupByCommissionaire <> 0 begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Dic_Commissionaire.Description'
		set	@IsStarted = 1
	end
*/
	if	@IsGroupByPaymentType <> 0 begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Paymenttype.Name'
		set	@IsStarted = 1
	end

	If	@IsGroupByCampaignType <> 0
		begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'iCampaignType.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByAdvertType <> 0
		begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'AdvertType.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByMassmedia <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'vMassMedia.Name, vMassMedia.groupName'
		set	@IsStarted = 1
		end

	If	@IsGroupByFirm <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Firm.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByManager <> 0
		begin

		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''')'
		set	@IsStarted = 1
		end

	If	@IsGroupByAgency <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Agency.Name'
		set	@IsStarted = 1
		end
	
	If	@IsGroupByMassmediaGroupType <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set @SQLString = @SQLString + N'MassmediaGroup.Name'
		set	@IsStarted = 1
		end

	end

EXECUTE sp_executesql @SQLString,
	N'@Summa decimal(18,2)',
	@Summa = @SummaVar		

Drop		table #tmp1
GO
PRINT '  ok: dbo.stat_VolumeOfRealizationNew';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 26.01.2009
-- Description:	
-- =============================================
CREATE OR ALTER procedure [dbo].[stat_VolumesByPaymentTypes] 
(
	@managerID smallint = NULL, 
	@firmID int = null,
	@startDate datetime = null,
	@finishDate datetime = null,
	@agencyID smallint = null,
	@groupByPaymentType bit = 0,
	@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;

    declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)
	
	create table #res(actionID int, userID smallint, firmID int, price decimal(18,2), paymentTypeID smallint, isHidden bit)
	
--	insert into [#res] (
--		actionID,
--		userID,
--		firmID,
--		price,
--		paymentTypeID,
--		isHidden
--	) 
	declare cur_companies cursor local fast_forward
	for
	select distinct a.actionID, 
		a.userID, 
		a.firmID,
		c.finalPrice,
		pt.paymentTypeID,
		pt.isHidden,
		c.startDate,
		c.finishDate,
		c.campaignID,
		c.campaignTypeID
	from Campaign c
		inner join [Action] a  on c.actionID = a.actionID
		inner join paymentType pt on c.paymentTypeID = pt.paymentTypeID
		left join @massmedias umm on c.massmediaID = umm.massmediaID
		left join GroupMember gm on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	where (a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
		(a.isSpecial = 1 or (c.campaignTypeID <> 4 and umm.massmediaID is not null and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1) )) 
				or (c.campaignTypeID = 4 and not exists(select * 
															from PackModuleIssue pmi 
																inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
																inner join Module m on pmc.moduleID = m.moduleID
																left join @massmedias ummm on m.massmediaID = ummm.massmediaID
															where pmi.campaignID = c.campaignID and (ummm.massmediaID is null or 
																(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
																 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0) )))) and	
		(@finishDate is null or c.startDate <= @finishDate)
		and (@startDate is null or c.finishDate >= @startDate)
		and a.[isConfirmed] = 1
		and c.agencyID = coalesce(@agencyID, c.agencyID)
		and a.userID = coalesce(@managerID, a.userID)
		and a.firmID = coalesce(@firmID, a.firmID)
	
	declare @actionID int, @userID smallint, 
		@campaignPrice decimal(18,2), @paymentTypeID smallint, @isHidden bit,
		@campaignstartdate datetime, @campaignfinishDate datetime, @campaignID int,
		@campaignTypeID  tinyint
	
	open cur_companies
	fetch next from cur_companies into 
		@actionID, @userID, @firmID, @campaignPrice, @paymentTypeID, @isHidden, @campaignstartdate,@campaignfinishDate,  @campaignID, @campaignTypeID
		
	while @@fetch_status = 0
	begin
		if @finishDate < @campaignfinishDate or @startDate > @campaignstartdate
			exec GetPriceByPeriod @campaignId, @CampaignTypeID, @startDate, @finishDate, @campaignPrice out

		if	@campaignPrice > 0 
			insert into [#res] (actionID,userID,firmID,price,paymentTypeID,isHidden) 
			values(@actionID, @userID, @firmID, @campaignPrice, @paymentTypeID, @isHidden)	
	
		fetch next from cur_companies into 
			@actionID, @userID, @firmID, @campaignPrice, @paymentTypeID, @isHidden, @campaignstartdate,@campaignfinishDate,  @campaignID, @campaignTypeID
	end
	
	if @groupByPaymentType = 0
	begin 
		select r.actionID, -- primary key
			r.actionID as 'Акция',
			coalesce(u.lastName,space(0)) + space(1) + coalesce(u.firstName,space(0)) + space(1) + coalesce(u.secondName,space(0)) as 'Менеджер',
			f.name as 'Фирма',
			Cast(r.not_hidden as decimal(8,2)) as 'С оплатой',
			Cast(r.hidden as decimal(8,2)) 'Без оплаты'
		from (
				select r.actionID,
					r.firmID,
					r.userID,
					sum(case when r.isHidden = 1 then r.price else 0 end) as hidden,
					sum(case when r.isHidden = 0 then r.price else 0 end) as not_hidden
				from #res r
				group by r.actionID,r.firmID,r.userID
			) r
			inner join [User] u on r.userID = u.userID
			inner join Firm f on r.firmID = f.firmID
		order by r.actionID
	end 
	else 
	begin 
		declare @sql nvarchar(max)
		declare @sqlselect nvarchar(max)
		declare @sqlsubselect nvarchar(max)
	
		declare cur_paymentType cursor fast_forward local
		for 
		select pt.paymentTypeID, pt.name
		from PaymentType pt
		where pt.isActive = 1
		order by pt.name
		
		declare @name nvarchar(64), @index smallint
		
		select @index = 1, @sqlsubselect = '', @sqlselect = ''
		
		open cur_paymentType
		
		fetch next from cur_paymentType into @paymentTypeID, @name
		
		while @@fetch_status = 0
		begin 
			set @sqlsubselect = @sqlsubselect + ', sum(case when r.paymentTypeID = ' + cast(@paymentTypeID as varchar) + ' then r.price else 0 end) as pt' + cast(@index as varchar)
			set @sqlselect = @sqlselect + ', cast(r.pt' + cast(@index as varchar) + ' as decimal(10, 2))  as ''' + replace(@name, '''', '''''') + ''''

			set @index = @index + 1		
			fetch next from cur_paymentType into @paymentTypeID, @name
		end 
		
		close cur_paymentType
		deallocate cur_paymentType
			
		set @sql = 'select r.actionID, r.actionID as ''Акция'',
					coalesce(u.lastName,space(0)) + space(1) + coalesce(u.firstName,space(0)) + space(1) + coalesce(u.secondName,space(0)) as ''Менеджер'',
					f.name as ''Фирма'' '
		set @sql = @sql + @sqlselect		
		set @sql = @sql + '
			from (
					select r.actionID,
						r.firmID,
						r.userID'
		set @sql = @sql + @sqlsubselect		
		set @sql = @sql + '
					from #res r
					group by r.actionID,r.firmID,r.userID
				) r
				inner join [User] u on r.userID = u.userID
				inner join Firm f on r.firmID = f.firmID
			order by r.actionID	'
		
		exec sp_executeSQL @sql
	end
	
	drop table #res
end
GO
PRINT '  ok: dbo.stat_VolumesByPaymentTypes';
GO
-- @@TAIL-BEGIN@@
PRINT '--- данные ---';
GO

IF (SELECT alreadyMigrated FROM #deployState) = 1
BEGIN
    PRINT '  миграция данных пропущена: finalPrice уже содержит пакетную скидку';
END
ELSE
BEGIN
    DECLARE @rows INT;

    -- campaignTypeID = 4 (пакетные модульные) пакетной скидке не подлежат;
    -- discount = 1 -- умножение на единицу, строку трогать незачем.
    UPDATE c
    SET    c.finalPrice = CAST(c.finalPrice * a.discount AS DECIMAL(18,2))
    FROM   dbo.Campaign c
           INNER JOIN dbo.[Action] a ON a.actionID = c.actionID
    WHERE  c.campaignTypeID <> 4
       AND a.discount <> 1;

    SET @rows = @@ROWCOUNT;
    PRINT '  мигрировано кампаний: ' + CONVERT(varchar(12), @rows);
END
GO

PRINT '--- проверка ---';
GO

-- CHARINDEX, а не LIKE: в шаблонах есть квадратные скобки, для LIKE это
-- символьный класс, и проверка молча врёт.
PRINT 'ActionRecalculate: ' + CASE
    WHEN CHARINDEX(N'finalPrice = @estimatedPrice', OBJECT_DEFINITION(OBJECT_ID('dbo.ActionRecalculate'))) > 0
    THEN 'OK -- пишет цену со всеми скидками' ELSE 'ОШИБКА -- тело не обновилось' END;

PRINT 'Campaigns: ' + CASE
    WHEN CHARINDEX(N'cm.[finalPrice] AS fullPrice', OBJECT_DEFINITION(OBJECT_ID('dbo.Campaigns'))) > 0
    THEN 'OK -- fullPrice берётся напрямую' ELSE 'ОШИБКА -- тело не обновилось' END;
GO

-- Ни одна процедура не должна больше домножать finalPrice на пакетную скидку.
DECLARE @stale int;
SELECT @stale = COUNT(*)
FROM   sys.sql_modules m
       JOIN sys.objects o ON o.object_id = m.object_id
WHERE  CHARINDEX(N'finalPrice', m.definition) > 0
   AND (CHARINDEX(N'finalPrice] * a.[discount]', m.definition) > 0
     OR CHARINDEX(N'finalPrice * a.discount',    m.definition) > 0
     OR CHARINDEX(N'FinalPrice * @actiondiscount', m.definition) > 0
     OR CHARINDEX(N'actionDiscount * @finalPrice', m.definition) > 0
     OR CHARINDEX(N'finalPrice * discount',      m.definition) > 0
     OR CHARINDEX(N'campaignPrice * @aDiscount', m.definition) > 0
     OR CHARINDEX(N'campaignFinalPrice * @campaignAdiscount', m.definition) > 0);
PRINT 'Процедур, всё ещё домножающих на пакетную скидку: ' + CONVERT(varchar(12), @stale)
      + CASE WHEN @stale = 0 THEN ' -- OK' ELSE ' -- ОШИБКА' END;
GO

-- Сходимость: Action.totalPrice должен совпасть с суммой finalPrice по кампаниям.
DECLARE @bad int;
SELECT @bad = COUNT(*)
FROM   dbo.[Action] a
       CROSS APPLY (SELECT ISNULL(SUM(c.finalPrice), 0) AS s
                    FROM dbo.Campaign c WHERE c.actionID = a.actionID) x
WHERE  a.isSpecial = 0
   AND ABS(a.totalPrice - x.s) > 0.01;
PRINT 'Акций, где totalPrice расходится с SUM(finalPrice) больше чем на копейку: ' + CONVERT(varchar(12), @bad);
PRINT '  (ненулевое значение само по себе не ошибка: totalPrice мог устареть ещё до деплоя;';
PRINT '   сравнивать надо с тем же числом, снятым скриптом ..-check.sql в разделе "ДО")';
GO

COMMIT TRANSACTION;
PRINT 'ЗАФИКСИРОВАНО.';
GO

DROP TABLE #deployState;
GO
SET NOEXEC OFF;
GO
