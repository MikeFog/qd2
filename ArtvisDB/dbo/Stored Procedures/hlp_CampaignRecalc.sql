CREATE PROC [dbo].[hlp_CampaignRecalc]
(
    @campaignID INT,
    @loggedUserID INT = NULL
)
WITH EXECUTE AS OWNER
AS
BEGIN
    /*
        Пересчёт ОДНОЙ кампании — зеркало фаз 1A–1F процедуры ActionRecalculate,
        вырезанное для одной кампании. Обновляет только строку Campaign:
            tariffPrice, issuesCount, issuesDuration, startDate, finishDate,
            discount (объёмная скидка), timeBonus, programsCount, managerDiscount.
        Campaign.price — вычисляемый столбец (tariffPrice * discount) — пересчитается сам.

        НЕ трогает: Action.*, Campaign.finalPrice, Issue.ratio, платежи.
        Полный пересчёт (тотал акции, finalPrice, ratio, CorrectPaymentAction)
        остаётся за ActionRecalculate, который зовётся на закрытии формы / активации.

        Вызывается из IssueIUD / ModuleIssueIUD после Add/Delete/Update выпуска
        (кроме массовых путей — см. @skipCampaignRecalc там).
    */
    SET NOCOUNT ON;

    DECLARE
        @campaignTypeID TINYINT,
        @massmediaID    SMALLINT,
        @oldTotalCount  INT,
        @tariffPrice    DECIMAL(18,2),
        @issuesDuration INT,
        @issuesCount    INT,
        @programsCount  INT,
        @startDate      DATETIME,
        @finishDate     DATETIME,
        @timeBonus      INT,
        @campaignDiscount        DECIMAL(9,4),
        @managerDiscountCampaign DECIMAL(18,10);

    -- Текущее состояние: oldTotalCount считаем так же, как INSERT в #CampaignPhase1
    -- (ActionRecalculate.sql, Phase 1 INSERT: issuesCount + programsCount ДО пересчёта).
    SELECT
        @campaignTypeID = c.campaignTypeID,
        @massmediaID    = c.massmediaID,
        @oldTotalCount  = ISNULL(c.issuesCount, 0) + ISNULL(c.programsCount, 0)
    FROM dbo.Campaign c
    WHERE c.campaignID = @campaignID;

    IF @campaignTypeID IS NULL
        RETURN;   -- нет такой кампании

    -- Значения по умолчанию — кампания без выпусков
    SELECT
        @tariffPrice = 0, @issuesDuration = 0, @issuesCount = 0,
        @programsCount = 0, @timeBonus = 0,
        @startDate = NULL, @finishDate = NULL;

    -------------------------------------------------------------------------
    -- Агрегаты по типу кампании (зеркало Phase 1A–1D)
    -------------------------------------------------------------------------
    IF @campaignTypeID = 1                                   -- линейная (Phase 1A)
    BEGIN
        SELECT
            @tariffPrice    = ISNULL(SUM(i.tariffPrice), 0),
            @issuesDuration = ISNULL(SUM(r.duration), 0),
            @issuesCount    = COUNT(*),
            @startDate      = dbo.ToShortDate(MIN(tw.dayOriginal)),
            @finishDate     = dbo.ToShortDate(MAX(tw.dayOriginal))
        FROM dbo.Issue i
        INNER JOIN dbo.TariffWindow tw ON tw.windowId = i.originalWindowID
        INNER JOIN dbo.Roller r        ON r.rollerID  = i.rollerID
        WHERE i.campaignID = @campaignID;
    END
    ELSE IF @campaignTypeID = 2                              -- спонсорская (Phase 1B)
    BEGIN
        DECLARE
            @paTariffPrice DECIMAL(18,2), @paStart DATETIME, @paFinish DATETIME,
            @paBonus INT, @paCount INT,
            @iaDuration INT, @iaCount INT, @iaStart DATETIME, @iaFinish DATETIME;

        -- ProgramAgg: программные выходы
        SELECT
            @paTariffPrice = SUM(pi.tariffPrice),
            @paStart  = MIN(DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), pi.issueDate))),
            @paFinish = MAX(DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), pi.issueDate))),
            @paBonus  = SUM(pl.bonus),
            @paCount  = COUNT(*)
        FROM dbo.ProgramIssue pi
        INNER JOIN dbo.SponsorTariff st           ON st.tariffID   = pi.tariffID
        INNER JOIN dbo.SponsorProgramPricelist pl ON pl.pricelistID = st.pricelistID
        WHERE pi.campaignID = @campaignID;

        -- IssueAgg: ролики
        SELECT
            @iaDuration = SUM(dbo.f_GetSponsorDuration(r.duration, i.positionId, pl.extraChargeFirstRoller, pl.extraChargeSecondRoller, pl.extraChargeLastRoller)),
            @iaCount    = COUNT(*),
            @iaStart    = MIN(tw.dayOriginal),
            @iaFinish   = MAX(tw.dayOriginal)
        FROM dbo.Issue i
        INNER JOIN dbo.TariffWindow tw ON tw.windowId  = i.originalWindowID
        INNER JOIN dbo.Tariff t        ON t.tariffID   = tw.tariffId
        INNER JOIN dbo.Pricelist pl    ON pl.pricelistID = t.pricelistID
        INNER JOIN dbo.Roller r        ON r.rollerID    = i.rollerID
        WHERE i.campaignID = @campaignID;

        SELECT
            @tariffPrice    = ISNULL(@paTariffPrice, 0),
            @issuesDuration = ISNULL(@iaDuration, 0),
            @issuesCount    = ISNULL(@iaCount, 0),
            @programsCount  = ISNULL(@paCount, 0),
            @timeBonus      = ISNULL(@paBonus, 0),
            @startDate = dbo.ToShortDate(
                CASE
                    WHEN @paStart IS NULL THEN @iaStart
                    WHEN @iaStart IS NULL THEN @paStart
                    WHEN @paStart < @iaStart THEN @paStart
                    ELSE @iaStart
                END),
            @finishDate = dbo.ToShortDate(
                CASE
                    WHEN @paFinish IS NULL THEN @iaFinish
                    WHEN @iaFinish IS NULL THEN @paFinish
                    WHEN @paFinish > @iaFinish THEN @paFinish
                    ELSE @iaFinish
                END);
    END
    ELSE IF @campaignTypeID = 3                              -- модульная (Phase 1C)
    BEGIN
        SELECT
            @issuesDuration = ISNULL(SUM(r.duration), 0),
            @issuesCount    = COUNT(*)
        FROM dbo.Issue i
        INNER JOIN dbo.Roller r ON r.rollerID = i.rollerID
        WHERE i.campaignID = @campaignID;

        SELECT
            @tariffPrice = ISNULL(SUM(mi.tariffPrice), 0),
            @startDate   = dbo.ToShortDate(MIN(mi.issueDate)),
            @finishDate  = dbo.ToShortDate(MAX(mi.issueDate))
        FROM dbo.ModuleIssue mi
        WHERE mi.campaignID = @campaignID;
    END
    ELSE IF @campaignTypeID = 4                              -- пакетная модульная (Phase 1D)
    BEGIN
        SELECT
            @issuesDuration = ISNULL(SUM(r.duration), 0),
            @issuesCount    = COUNT(*)
        FROM dbo.Issue i
        INNER JOIN dbo.Roller r ON r.rollerID = i.rollerID
        WHERE i.campaignID = @campaignID;

        SELECT
            @tariffPrice = ISNULL(SUM(pmi.tariffPrice), 0),
            @startDate   = dbo.ToShortDate(MIN(pmi.issueDate)),
            @finishDate  = dbo.ToShortDate(MAX(pmi.issueDate))
        FROM dbo.PackModuleIssue pmi
        WHERE pmi.campaignID = @campaignID;
    END

    -------------------------------------------------------------------------
    -- Объёмная скидка + менеджерская скидка (зеркало Phase 1E)
    -------------------------------------------------------------------------
    EXEC dbo.hlp_CompanyDiscountCalculate
        @massMediaID    = @massmediaID,
        @campaignTypeID = @campaignTypeID,
        @startDate      = @startDate,
        @tariffPrice    = @tariffPrice,
        @discountValue  = @campaignDiscount OUTPUT;

    -- Менеджерскую скидку пересчитываем ТОЛЬКО на переходе 0↔N выпусков,
    -- иначе NULL = «оставить как есть» (Phase 1F: ISNULL(@md, c.managerDiscount)).
    DECLARE @newTotalCount INT = ISNULL(@issuesCount, 0) + ISNULL(@programsCount, 0);

    IF (@oldTotalCount = 0 AND @newTotalCount > 0)
       OR (@oldTotalCount > 0 AND @newTotalCount = 0)
        SET @managerDiscountCampaign = dbo.fn_GetMaxUserDiscount(@loggedUserID, @startDate, @finishDate);
    ELSE
        SET @managerDiscountCampaign = NULL;

    -------------------------------------------------------------------------
    -- Запись (зеркало Phase 1F)
    -------------------------------------------------------------------------
    UPDATE dbo.Campaign
    SET
        tariffPrice     = ISNULL(@tariffPrice, 0),
        issuesDuration  = ISNULL(@issuesDuration, 0),
        issuesCount     = ISNULL(@issuesCount, 0),
        startDate       = @startDate,
        finishDate      = @finishDate,
        discount        = @campaignDiscount,
        timeBonus       = ISNULL(@timeBonus, 0),
        programsCount   = ISNULL(@programsCount, 0),
        managerDiscount = ISNULL(@managerDiscountCampaign, managerDiscount)
    WHERE campaignID = @campaignID;
END
