-- Фактическое окно выпуска, этап 4 «Деньги» (docs/tasks/window-actual-switch.md §5.1, §5.5 п. 3; решения Р-3, Р-10, Р-11
-- и замер — docs/tasks/window-actual-open-questions.md §9, §11). Деньги, даты и отчёты — по дню окна выхода выпуска.
--
-- Пакет нельзя разрывать (иначе двойной счёт или недосчёт в распределении по месяцам): ActionRecalculate, GetIssuesPrice,
-- SetIssueRatio, GetPriceByPeriod, rpt_GenericBill — одним скриптом.
-- 1. Даты кампании при пересчёте (ActionRecalculate): линейные и спонсорские — по дню окна выхода. Наценки спонсора
--    (расход бонусного времени) — по прайс-листу окна выхода, у окна без тарифа — окна постановки (Р-3 «Уточнено»);
--    так же в проверке бонусного времени при активации (ActionActivate). Модульные и пакетные — по дню модуля/пакета.
-- 2. Распределение по месяцам и суммы за период: GetIssuesPrice, SetIssueRatio, GetPriceByPeriod (через неё — счёт, акт,
--    договор, итоги медиаплана, балансы, «Объём реализации» старых видов), rpt_GenericBill.
-- 3. Журнал актов (CampaignsForActJournalRetrieve): количество и длительность линейных — по дню выхода, модульных — по дню
--    модуля (как их деньги). Бонусы (stat_Bonuses), «Объём реализации» (stat_GetPrice_proc), «По месяцам»
--    (stat_GetPriceByMonth_proc), «в секундах» (Stat_VolumeOfRealizationSec — и модульные по дню выхода), факторный анализ
--    (statFactorAnalysis), «Разделить акцию» по дате (MoveIssues2NewCampaign).
-- 4. Перенос трафиком (IssueTransfer, Р-10): после переноса даты кампании и акции пересчитываются по дням выхода — только
--    даты, скидки, цена и ratio не трогаются (Р-3).
--
-- На ArtvisDev и проде переносов через границу месяца 0 (контрольный запрос Р-11 10.10.2026): месячные суммы, счета и акты
-- не меняются; меняются только суммы за произвольный период по историческим выпускам, перенесённым на другой день.
-- Ставить после 27, 29 и 32: несёт их тексты (IssueTransfer из 27, шаг 3 broadcastStart из 29) и опирается на индекс
-- IX_Issue_campaignID_covering + actualWindowID из 32 — без них скрипт ничего не меняет. Только через sqlcmd -b (пакет
-- нельзя разрывать), в нерабочее время. Клиент не нужен. Идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 33_actual-window-money.sql

SET NOCOUNT ON;
GO
IF EXISTS (SELECT 1 FROM dbo.Pricelist WHERE DATEPART(hh, broadcastStart) * 60 + DATEPART(mi, broadcastStart) <> 0)
   OR EXISTS (SELECT 1 FROM dbo.SponsorProgramPricelist WHERE broadcastStart <> '19000101')
   OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetIssuesPrice')) LIKE N'%DATEPART(%broadcastStart%'
   OR NOT EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'TransferOtherMassmedia')
   OR NOT EXISTS (
        SELECT 1
        FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = OBJECT_ID(N'dbo.Issue') AND i.name = N'IX_Issue_campaignID_covering'
          AND c.name = N'actualWindowID' AND ic.is_included_column = 1)
BEGIN
    RAISERROR(N'33: сначала Deploy/27, 29 (с шагом 1 broadcastStart) и 32. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE OR ALTER Procedure [dbo].[GetIssuesPrice]
(
@campaignID int, 
@campaignTypeID int,								
@startDate datetime, 
@finishDate DATETIME,
@price decimal(18,2) = 0 out
)
as
SET NOCOUNT on
SET @startDate = dbo.ToShortDate(@startDate)
SET @finishDate = dbo.ToShortDate(@finishDate)
	
If @campaignTypeID = 1	Begin
	-- CROSS APPLY + TOP 1 вместо inner join: у кампании десятки-сотни выпусков,
	-- но прямой join оптимизатор строит как Hash Match и в build-фазу вычитывает
	-- весь срез TariffWindow за период по ВСЕМ СМИ (~165 тыс. строк на месяц),
	-- чтобы сматчить их с выпусками одной кампании. 30 мс вместо 0.2 мс на вызов,
	-- а ActionRecalculate зовёт это в курсоре по каждой кампании акции.
	-- windowId — PK TariffWindow, совпадение не более одного: семантика та же.
	Select	
		@price = Sum(i.[tariffPrice])
	From		
		Issue i
		Cross Apply
		(
			Select Top 1 1 As matched
			From TariffWindow tw
			Where tw.windowId = i.actualWindowID and
				tw.dayOriginal between @startDate and @finishDate
		) w
	Where	
		i.campaignID = @campaignID
End

Else If @campaignTypeID = 2 Begin
	Select	
		@price = Sum(i.[tariffPrice])
	From		
		ProgramIssue i 
		inner join SponsorTariff st on i.tariffID = st.tariffID
		inner join SponsorProgramPriceList pl on pl.priceListID = st.priceListID
	Where	
		i.campaignID = @campaignID	and
		i.issueDate between @startDate 
			and dateadd(day, 1, @finishDate) 
End				

Else If @campaignTypeID = 3 Begin
	Select	
		@price = Sum(i.[tariffPrice])
	From		
		ModuleIssue i 
	Where	
		i.campaignID = @campaignID	and
		i.issueDate between @startDate and @finishDate
End				

Else If @campaignTypeID = 4 Begin
	Select	
		@price = Sum(i.[tariffPrice])
	From		
		[PackModuleIssue] i 
	Where	
		i.campaignID = @campaignID	and
		i.issueDate between @startDate and @finishDate
End
GO

CREATE OR ALTER PROC [dbo].[SetIssueRatio]
(
    @campaignID int,
    @campaignTypeID int,
    @startDate datetime,
    @finishDate datetime,
    @ratio float
)
AS
BEGIN
    SET NOCOUNT ON;

    CREATE TABLE #issue
    (
        issueID int NOT NULL PRIMARY KEY
    );

    SET @startDate  = CONVERT(datetime, CONVERT(varchar(8), @startDate, 112), 112);
    SET @finishDate = CONVERT(datetime, CONVERT(varchar(8), @finishDate, 112), 112);

    -- CROSS APPLY + TOP 1 вместо INNER JOIN — см. комментарий в GetIssuesPrice:
    -- прямой join читает весь срез TariffWindow за период по всем СМИ.
    INSERT INTO #issue (issueID)
    SELECT i.issueID
    FROM Issue i
        CROSS APPLY
        (
            SELECT TOP 1 1 AS matched
            FROM TariffWindow tw
            WHERE tw.windowId = i.actualWindowID
              AND tw.dayOriginal BETWEEN @startDate AND @finishDate
        ) w
    WHERE
        i.campaignId = @campaignID;

    UPDATE i WITH (ROWLOCK)
    SET i.ratio = @ratio
    FROM Issue i
        INNER JOIN #issue x ON x.issueID = i.issueID
    WHERE i.ratio <> @ratio;

    IF @campaignTypeID = 2
        UPDATE i
        SET i.Ratio = @ratio
        FROM ProgramIssue i
            INNER JOIN Campaign c
                ON i.campaignId = @campaignID
               AND c.campaignID = i.campaignID
            INNER JOIN SponsorTariff st
                ON i.tariffID = st.tariffID
            INNER JOIN SponsorProgramPriceList pl
                ON st.pricelistID = pl.pricelistID
        WHERE
            i.issueDate BETWEEN
                @startDate
                AND
                @finishDate
            AND i.Ratio <> @ratio;

    IF @campaignTypeID = 3
        UPDATE ModuleIssue
        SET ratio = @ratio
        WHERE
            campaignId = @campaignID
            AND issueDate BETWEEN @startDate AND @finishDate
            AND ratio <> @ratio;

    IF @campaignTypeID = 4
        UPDATE [PackModuleIssue]
        SET [ratio] = @ratio
        WHERE
            [campaignID] = @campaignID
            AND [issueDate] BETWEEN @startDate AND @finishDate
            AND [ratio] <> @ratio;
END
GO

/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE OR ALTER PROC [dbo].[GetPriceByPeriod]
(
@campaignID int, 
@campaignTypeID int,
@startDate datetime, 
@finishDate datetime, 
@price decimal(18,2) OUT,
@massmediaID INT = NULL,
@tariffPrice decimal(18,2) = NULL out,
@showBlack bit = 1,
@taxPrice decimal(18,2) = null out,
@withTax bit = 0,
@rollerIDString VARCHAR(8000) = null
)
As
SET NOCOUNT ON

if EXISTS(SELECT * 
	FROM [Campaign] c 
		INNER JOIN [Action] a ON c.[actionID] = a.[actionID] 
		inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
			and (@showBlack = 1 or pt.IsHidden = 0)
		WHERE c.[campaignID] = @campaignID AND a.isSpecial = 1)
	BEGIN
	SELECT @price = price, @tariffPrice = tariffPrice 
	FROM [Campaign] WHERE [campaignID] = @campaignID
		
	RETURN
	end
	
IF (@startDate IS NULL AND @finishDate IS NULL)
begin 
	SELECT @price = 0
	return 
end 

SET @startDate = dbo.ToShortDate(@startDate)
SET @finishDate = dbo.ToShortDate(@finishDate) 
Set	@price = 0

If	@campaignTypeID = 1 
	begin
	
	if @withTax = 0
		Select	@price = sum(i.[tariffPrice] * i.[ratio]), @tariffPrice = SUM(i.[tariffPrice])
		From	
			Issue i
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
			inner join Campaign c on i.campaignID = c.campaignID
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		Where	
			i.campaignID = @campaignID	and
			tw.dayOriginal between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)
	else 
		Select	@price = sum(i.[tariffPrice] * i.[ratio]), 
			@tariffPrice = SUM(i.[tariffPrice]),
			@taxPrice = sum(case when at.divisor is null then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		From	
			Issue i
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
			inner join Campaign c on i.campaignID = c.campaignID
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and tw.dayOriginal between at.startDate and at.finishDate
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		Where	
			i.campaignID = @campaignID	and
			tw.dayOriginal between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)

	End
Else If	@campaignTypeID = 2	
	begin
	
	if @withTax = 0
		Select	
			@price = isnull(Sum(i.[tariffPrice] * i.[ratio]), 0), @tariffPrice = isnull(SUM(i.[tariffPrice]), 0)
		From		
			ProgramIssue i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			inner join SponsorTariff st on i.tariffID = st.tariffID
			inner join SponsorProgramPriceList pl on st.priceListID = pl.priceListID
		Where		
			i.campaignID = @campaignID and 
			Convert(datetime, Convert(varchar(8), i.issueDate, 112), 112) between @startDate and @finishDate
	else 
		Select	
			@price = isnull(Sum(i.[tariffPrice] * i.[ratio]), 0), @tariffPrice = isnull(SUM(i.[tariffPrice]), 0),
			@taxPrice = sum(case when at.divisor is null then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		From		
			ProgramIssue i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			inner join SponsorTariff st on i.tariffID = st.tariffID
			inner join SponsorProgramPriceList pl on st.priceListID = pl.priceListID
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and Convert(datetime, Convert(varchar(8), i.issueDate, 112), 112) between at.startDate and at.finishDate
		Where		
			i.campaignID = @campaignID and 
			Convert(datetime, Convert(varchar(8), i.issueDate, 112), 112) between @startDate and @finishDate
	End
Else If	@campaignTypeID = 3 begin
	if @withTax = 0
		SELECT @price = 	Sum(i.[tariffPrice] * i.[ratio]), @tariffPrice = SUM(i.[tariffPrice])
		From		
			ModuleIssue i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		Where		
			i.campaignID = @campaignID	and
			i.issueDate between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)
	else 
		SELECT @price = 	Sum(i.[tariffPrice] * i.[ratio]), @tariffPrice = SUM(i.[tariffPrice]),
			@taxPrice = sum(case when at.divisor is null then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		From		
			ModuleIssue i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and i.issueDate between at.startDate and at.finishDate
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		Where		
			i.campaignID = @campaignID	and
			i.issueDate between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)
end
			
ELSE IF @campaignTypeID = 4
BEGIN

	DECLARE @packModulePrice decimal(18,2) 
	DECLARE @tariffPricePM decimal(18,2)

	if @withTax = 0
		SELECT 
			@packModulePrice = SUM(i.[tariffPrice] * i.[ratio]), @tariffPricePM = SUM(i.[tariffPrice])
		FROM [PackModuleIssue] i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		WHERE 
			i.campaignID = @campaignID	and
			i.issueDate between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)
	else  
		SELECT 
			@packModulePrice = SUM(i.[tariffPrice] * i.[ratio]), @tariffPricePM = SUM(i.[tariffPrice]),
			@taxPrice = sum(case when coalesce(at.divisor, 0) < 0.0000001 then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		FROM [PackModuleIssue] i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and i.issueDate between at.startDate and at.finishDate
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		WHERE 
			i.campaignID = @campaignID	and
			i.issueDate between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)
		
	IF @massmediaID IS NULL
		BEGIN
			SET	@price = @packModulePrice
			SET @tariffPrice = @tariffPricePM
			RETURN
		END

	CREATE TABLE #tmp(massmediaID SMALLINT, price decimal(18,2))
	INSERT INTO #tmp
	SELECT 
		m.[massmediaID], sum(mpl.[price])
	FROM [PackModuleIssue] i 
		INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
		INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
		INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
		INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
		inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
			and (@showBlack = 1 or pt.IsHidden = 0)
		left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
	WHERE 
		i.campaignID = @campaignID	and
		i.issueDate between @startDate and @finishDate  and
		(@rollerIDString is null or rr.ID is not null)
	group by m.massmediaID

	declare @sumPrice decimal(18,2)
	SELECT @sumPrice = sum(t1.price) FROM [#tmp] AS t1

	DECLARE @sumPriceMM decimal(18,2)
	SELECT @sumPriceMM = sum(t1.price) FROM [#tmp] AS t1 WHERE t1.[massmediaID] = @massmediaID

	SET @price = @packModulePrice * @sumPriceMM / @sumPrice
	SET @tariffPrice = @tariffPricePM * @sumPriceMM / @sumPrice
	drop table #tmp
END
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
        INNER JOIN dbo.TariffWindow tw ON tw.windowId = i.actualWindowID -- день выхода (docs/tasks/window-actual-switch.md, этап 4)
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

CREATE OR ALTER PROC [dbo].[ActionActivate]
(
@actionID int,
@loggedUserID smallint,
@isTestActivate bit,
@tryTransferFailedIssues bit = 0,
@allowDifferentWindowPrice bit = 0,
@transferAttemptCount int = 0,
@avoidFirmRollerWindows bit = 1
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON	

DECLARE @isAdmin bit, @rightForMinus bit, @rightToGoBack bit, @IsTrafficManager bit
DECLARE @blockActivation bit = 0;
	
EXEC hlp_GetMainUserCredentials
	@loggedUserId = @loggedUserID, @rightToGoBack = @rightToGoBack out, @isAdmin = @isAdmin out, @rightForMinus = @rightForMinus out,  @IsTrafficManager = @IsTrafficManager out

declare @issue table (
	name nvarchar(64) not null,
	advertTypeName nvarchar(256) null,
	statusDescription nvarchar(64) not null,
	duration int not null,
	positionID int not null,
	issueDate datetime not null,
	issueID int primary key,
	rollerID int not null,
	moduleIssueID int,
	packModuleIssueID int,
	campaignID int not null,
	campaignTypeID tinyint not null,
	massmediaID int not null, 
	windowId int not null,
	oldWindowId int not null,
	oldIssueDate datetime not null,
	oldWindowPrice decimal(18, 2) not null,
	grantorID smallint null,
	isTransferred bit not null default 0,
	transferStatus nvarchar(64) null
)

declare @fatalErrors table (name nvarchar(64) not null)
declare @plannedTransfers table (
	issueID int primary key,
	oldWindowID int not null,
	newWindowID int not null,
	oldIssueDate datetime not null,
	newIssueDate datetime not null
)

declare @tomorrow datetime
select @tomorrow = dateadd(day, 1, CONVERT(date, getdate()))

declare @firmID smallint
select @firmID = firmID from [Action] where actionID = @actionID

DECLARE cur_issues CURSOR FOR
Select i.issueId From Issue i inner join Campaign c on c.campaignID = i.campaignID  where c.actionID = @actionID and i.isConfirmed = 0
declare @issueId int

OPEN cur_issues
FETCH NEXT FROM cur_issues INTO @issueId

WHILE @@FETCH_STATUS = 0
BEGIN
	insert into @issue([name], advertTypeName, statusDescription,duration,positionID,issueDate,issueID,rollerID,moduleIssueID,packModuleIssueID,campaignID,campaignTypeID,massmediaID, windowId, oldWindowId, oldIssueDate, oldWindowPrice, grantorID)
	select 
		r.[name],
		r.advertTypeName,
		case 
			when c.finishDate < @tomorrow and c.campaignTypeID <> 2 and @rightToGoBack <> 1 then 'CampaignAlreadyFinished' 
			when (tw.isDisabled = 1) then 'DisabledInsertRoller' 
			when (r.rolActionTypeID in (1, 8, 9) and tw.maxCapacity > 0) then 'DisabledInsertSimpleRoller' 
			when ((tw.isFirstPositionOccupied = 1 And i.positionId = -20) 
					or (tw.isSecondPositionOccupied = 1	And i.positionId = -10)
					or (tw.isLastPositionOccupied = 1	And i.positionId = 10)) then 'FirstLastIssueError' 
			when (tw.dayActual < @tomorrow) and @rightToGoBack <> 1 And @IsTrafficManager = 0 then 'IncorrectIssueDate' 
			when (@rightForMinus = 0 
				and (tw.[timeInUseConfirmed] + r.duration + (Select IsNull(sum(it2.duration), 0) From @issue it2 Where it2.windowId = tw.windowId) > tw.duration) 
				and (i.grantorID is null or (dbo.fn_IsRightForMinus(i.grantorID) = 0)))
				then 'WindowOverflow' 
			when (@rightForMinus = 0 and (tw.[maxCapacity] > 0 
				AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + 1 + (Select count(*) From @issue it2 Where it2.windowId = tw.windowId))) < 0) 
				and (i.grantorID is null or (dbo.fn_IsRightForMinus(i.grantorID) = 0))) 
				then 'WindowMaxCapacityOverflow' 
			when r.advertTypeID Is Null then 'RollerWithoutActionType' 
			when tw.dayOriginal <= mm.deadLine And @isAdmin = 0 And @IsTrafficManager = 0  then 'DeadLineViolation'
			else 'OK'
		end,
		r.duration, i.positionId, tw.windowDateActual, i.issueID, i.rollerID, i.moduleIssueID, i.packModuleIssueID, i.campaignID, c.campaignTypeID, mm.massmediaID, tw.windowId, tw.windowId, tw.windowDateActual, tw.price, i.grantorID
	from Issue i 
		inner join vRoller r on i.rollerID = r.rollerID
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
		inner join Campaign c on c.campaignID = i.campaignID 
		inner join MassMedia mm on mm.massmediaID = tw.massmediaID
	where i.issueID = @issueId

	FETCH NEXT FROM cur_issues INTO @issueId
END

CLOSE cur_issues
DEALLOCATE cur_issues

declare @programmissue table (
	[name] nvarchar(64) not null,
	statusDescription nvarchar(64) not null,
	duration int not null,
	issueID int primary key,
	campaignID int not null,
	issueDate datetime not null,
	advertTypeName nvarchar(256)
)

insert into @programmissue (
	[name],
	statusDescription,
	duration,
	issueDate,
	issueID,
	campaignID,
	advertTypeName
) 
select 
	COALESCE(NULLIF(LTRIM(RTRIM(st.comment)), ''), sp.[name]),
case 
    when i.advertTypeID Is Null then 'RollerWithoutActionType'
    when (i.issueDate < @tomorrow) and @rightToGoBack <> 1 and @IsTrafficManager = 0 then 'IncorrectIssueDate'
    when i2.issueID is null then 'OK' 
    else 'AlreadySponsered' 
end,
	st.duration,
	i.issueDate,
	i.issueID,
	i.campaignID,
	adv.name
from ProgramIssue i 
	inner join Campaign c on i.campaignID = c.campaignID
	inner join SponsorProgram sp on i.programID = sp.sponsorProgramID
	inner join SponsorTariff st on i.tariffID = st.tariffID
	left join AdvertType adv on adv.advertTypeID = i.advertTypeID
	left join ProgramIssue i2 on i.issueID <> i2.issueID and i.programID = i2.programID 
		and i.tariffID = i2.tariffID and i.issueDate = i2.issueDate and i2.isConfirmed = 1 
where c.actionID = @actionID and i.isConfirmed = 0

update i2 set i2.statusDescription = 'PartOfModule' 
from @issue i INNER JOIN @issue i2 ON i.moduleIssueID = i2.moduleIssueID
	where i.statusDescription <> 'OK' and i2.statusDescription like 'OK' 

update i2 set i2.statusDescription = 'PartOfPackModule' 
from @issue i INNER JOIN @issue i2 ON i.packModuleIssueID = i2.packModuleIssueID
	where i.statusDescription <> 'OK' and i2.statusDescription like 'OK' 

if exists(
		select * from 
		(
			select c.campaignID, SUM(dbo.f_GetSponsorDuration(r.[duration], i.positionId, pl.extraChargeFirstRoller, pl.extraChargeSecondRoller, pl.extraChargeLastRoller)) as sumDuration 
			from [Issue] i 
				inner join @issue i2 on i.issueID = i2.issueID and i2.statusDescription like 'OK'
				inner join Campaign c on i.campaignID = c.campaignID
				inner join TariffWindow tw on i.actualWindowID = tw.windowId
				inner join TariffWindow twO on i.originalWindowID = twO.windowId
				-- наценки — по прайс-листу окна выхода, у окна без тарифа — окна постановки (как в ActionRecalculate)
				cross apply (select top 1 pl.extraChargeFirstRoller, pl.extraChargeSecondRoller, pl.extraChargeLastRoller
					from (values (1, tw.tariffId), (2, twO.tariffId)) v (ord, tariffId)
						inner join Tariff t on t.tariffID = v.tariffId
						inner join Pricelist pl on pl.pricelistID = t.pricelistID
					order by v.ord) pl
				inner join Roller r on i.rollerID = r.rollerID
				inner join MassMedia mm on tw.massmediaID = mm.massmediaID
			where 
				c.actionID = @actionID and c.campaignTypeID = 2
			group by 
				c.campaignID
		) as x
		left join (
			select c.campaignID, sum(pl.bonus) as sumBonus
			from [ProgramIssue] i 
				inner join @programmissue i2 on i.issueID = i2.issueID and i2.statusDescription like 'OK'
				inner join Campaign c on c.campaignID = i.campaignID
				inner join SponsorTariff st on i.tariffID = st.tariffID
				inner join [SponsorProgramPricelist] pl ON st.[pricelistID] = pl.[pricelistID]
			where c.actionID = @actionID and c.campaignTypeID = 2
			group by c.campaignID
		) as y on x.campaignID = y.campaignID 
		where y.sumBonus is null or y.sumBonus < x.sumDuration 
	)
begin 
    UPDATE it
      SET it.statusDescription = 'TimeBonusExceed'
    FROM @issue it
    INNER JOIN Campaign c ON c.campaignID = it.campaignID
    WHERE c.actionID = @actionID
      AND c.campaignTypeID = 2
      AND it.statusDescription = 'OK';

    SET @blockActivation = 1;
	Insert Into @fatalErrors(name) values('CannotPerfomActivationSponsor')
end

-- Политическая агитация: у станций всех активируемых роликов типа 6 должны быть
-- заполнены три ролика обвязки в карточке, иначе обвязку не из чего создать
if exists (
	select 1
	from @issue it
		inner join Roller r on r.rollerID = it.rollerID
		inner join MassMedia mm on mm.massmediaID = it.massmediaID
	where r.rolActionTypeID = 6
		and (mm.agitationLocalRollerID is null
			or mm.agitationAnnounceRollerID is null
			or mm.agitationFederalRollerID is null))
begin
	update it set it.statusDescription = 'AgitationStationRollersNotSet'
	from @issue it
		inner join Roller r on r.rollerID = it.rollerID
		inner join MassMedia mm on mm.massmediaID = it.massmediaID
	where r.rolActionTypeID = 6
		and (mm.agitationLocalRollerID is null
			or mm.agitationAnnounceRollerID is null
			or mm.agitationFederalRollerID is null)

	SET @blockActivation = 1;
	Insert Into @fatalErrors(name) values('AgitationStationRollersNotSet')
end

IF @isTestActivate = 0
	AND @blockActivation = 0
	AND @tryTransferFailedIssues = 1
	AND @transferAttemptCount > 0
BEGIN
	DECLARE
		@transferIssueID int,
		@transferSourceWindowID int,
		@transferSourceDate datetime,
		@transferSourcePrice decimal(18, 2),
		@transferPositionID int,
		@transferDuration int,
		@transferGrantorID smallint,
		@candidateWindowID int,
		@candidateIssueDate datetime

	DECLARE cur_transfer CURSOR LOCAL FOR
		SELECT issueID, oldWindowId, oldIssueDate, oldWindowPrice, positionID, duration, grantorID
		FROM @issue
		WHERE statusDescription <> 'OK'
			AND campaignTypeID = 1
			AND moduleIssueID IS NULL
			AND packModuleIssueID IS NULL

	OPEN cur_transfer
	FETCH NEXT FROM cur_transfer INTO @transferIssueID, @transferSourceWindowID, @transferSourceDate,
		@transferSourcePrice, @transferPositionID, @transferDuration, @transferGrantorID

	WHILE @@FETCH_STATUS = 0
	BEGIN
		SET @candidateWindowID = NULL
		SET @candidateIssueDate = NULL

		;WITH rankedCandidates AS
		(
			SELECT
				tw.windowId,
				tw.windowDateActual,
				attemptNo = ROW_NUMBER() OVER (ORDER BY tw.windowDateActual DESC)
			FROM TariffWindow tw
			WHERE tw.windowDateActual < @transferSourceDate
				AND tw.massmediaID = (SELECT massmediaID FROM @issue WHERE issueID = @transferIssueID)
				AND tw.dayActual = (SELECT dayActual FROM TariffWindow WHERE windowId = @transferSourceWindowID)
				AND tw.windowId <> @transferSourceWindowID
				AND tw.isDisabled = 0
				AND NOT EXISTS (SELECT 1 FROM Tariff t WHERE t.tariffID = tw.tariffId AND t.isForModuleOnly = 1)

			UNION ALL

			SELECT
				tw.windowId,
				tw.windowDateActual,
				attemptNo = ROW_NUMBER() OVER (ORDER BY tw.windowDateActual ASC)
			FROM TariffWindow tw
			WHERE tw.windowDateActual > @transferSourceDate
				AND tw.massmediaID = (SELECT massmediaID FROM @issue WHERE issueID = @transferIssueID)
				AND tw.dayActual = (SELECT dayActual FROM TariffWindow WHERE windowId = @transferSourceWindowID)
				AND tw.windowId <> @transferSourceWindowID
				AND tw.isDisabled = 0
				AND NOT EXISTS (SELECT 1 FROM Tariff t WHERE t.tariffID = tw.tariffId AND t.isForModuleOnly = 1)
		),
		validCandidates AS
		(
			SELECT
				rc.windowId,
				rc.windowDateActual,
				rc.attemptNo,
				freeTime = tw.duration - tw.timeInUseConfirmed
					- (Select IsNull(sum(it2.duration), 0) From @issue it2 Where it2.windowId = tw.windowId And it2.statusDescription = 'OK')
			FROM rankedCandidates rc
				INNER JOIN TariffWindow tw ON tw.windowId = rc.windowId
				INNER JOIN Issue i ON i.issueID = @transferIssueID
				INNER JOIN Roller r ON r.rollerID = i.rollerID
				INNER JOIN Campaign c ON c.campaignID = i.campaignID
				INNER JOIN MassMedia mm ON mm.massmediaID = tw.massmediaID
			WHERE rc.attemptNo <= @transferAttemptCount
				AND (@allowDifferentWindowPrice = 1 OR tw.price = @transferSourcePrice)
				AND (@avoidFirmRollerWindows = 0
					OR (NOT EXISTS (
							SELECT 1
							FROM Issue fi
								INNER JOIN Campaign fc ON fc.campaignID = fi.campaignID
								INNER JOIN [Action] fa ON fa.actionID = fc.actionID
							WHERE fi.actualWindowID = tw.windowId
								AND fa.firmID = @firmID
								AND fa.deleteDate IS NULL
								AND fi.isConfirmed = 1)
						AND NOT EXISTS (
							SELECT 1 FROM @issue it2
							WHERE it2.windowId = tw.windowId
								AND it2.statusDescription = 'OK')))
				AND NOT (c.finishDate < @tomorrow and c.campaignTypeID <> 2 and @rightToGoBack <> 1)
				AND NOT (r.rolActionTypeID in (1, 8, 9) and tw.maxCapacity > 0)
				AND NOT ((tw.isFirstPositionOccupied = 1 And @transferPositionID = -20)
					or (tw.isSecondPositionOccupied = 1 And @transferPositionID = -10)
					or (tw.isLastPositionOccupied = 1 And @transferPositionID = 10))
				AND NOT EXISTS (
					SELECT 1
					FROM @issue it2
					WHERE it2.windowId = tw.windowId
						AND it2.statusDescription = 'OK'
						AND it2.positionID = @transferPositionID
						AND @transferPositionID IN (-20, -10, 10)
				)
				AND NOT ((tw.dayActual < @tomorrow) and @rightToGoBack <> 1 And @IsTrafficManager = 0)
				AND NOT (@rightForMinus = 0
					and (tw.[timeInUseConfirmed] + @transferDuration
						+ (Select IsNull(sum(it2.duration), 0) From @issue it2 Where it2.windowId = tw.windowId And it2.statusDescription = 'OK') > tw.duration)
					and (@transferGrantorID is null or (dbo.fn_IsRightForMinus(@transferGrantorID) = 0)))
				AND NOT (@rightForMinus = 0
					and (tw.[maxCapacity] > 0
						AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + 1
							+ (Select count(*) From @issue it2 Where it2.windowId = tw.windowId And it2.statusDescription = 'OK'))) < 0)
					and (@transferGrantorID is null or (dbo.fn_IsRightForMinus(@transferGrantorID) = 0)))
				AND r.advertTypeID Is Not Null
				AND NOT (tw.dayOriginal <= mm.deadLine And @isAdmin = 0 And @IsTrafficManager = 0)
		)
		SELECT TOP 1
			@candidateWindowID = windowId,
			@candidateIssueDate = windowDateActual
		FROM validCandidates
		ORDER BY attemptNo, freeTime DESC, windowDateActual

		IF @candidateWindowID IS NOT NULL
		BEGIN
			UPDATE @issue
			SET
				statusDescription = 'OK',
				windowId = @candidateWindowID,
				issueDate = @candidateIssueDate,
				isTransferred = 1,
				transferStatus = 'Transferred'
			WHERE issueID = @transferIssueID

			INSERT INTO @plannedTransfers(issueID, oldWindowID, newWindowID, oldIssueDate, newIssueDate)
			VALUES(@transferIssueID, @transferSourceWindowID, @candidateWindowID, @transferSourceDate, @candidateIssueDate)
		END

		FETCH NEXT FROM cur_transfer INTO @transferIssueID, @transferSourceWindowID, @transferSourceDate,
			@transferSourcePrice, @transferPositionID, @transferDuration, @transferGrantorID
	END

	CLOSE cur_transfer
	DEALLOCATE cur_transfer
END

SELECT 
	'i_' + cast(i2.issueID as varchar) as issueID,
	am.message as statusDescription, 
	i2.[name], 
	i2.advertTypeName,
	dbo.fn_Int2Time(i2.duration) as duration,
	ip.[description] as issuePosition,
	i2.issueDate,
	m.name as radiostationName,
	m.groupName
FROM
	@ISSUE i2
	INNER JOIN iIssuePosition ip ON ip.positionID = i2.positionID
	Inner Join vMassmedia m On m.massmediaID = i2.massmediaID
	LEFT JOIN iMessageToActivate am ON i2.statusDescription COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT
WHERE i2.statusDescription like 'OK'
	and i2.isTransferred = 0
union all
select 
	'pi_' + cast(i.issueID as  varchar) as issueID,
	am.message as statusDescription, 
	i.[name], 
	i.advertTypeName,
	dbo.fn_Int2Time(i.duration) as duration,
	null as issuePosition,
	i.issueDate,
	m.name as radiostationName,
	m.groupName
from @programmissue i
	Inner Join Campaign c on c.campaignID = i.campaignID
	Inner Join vMassmedia m On m.massmediaID = c.massmediaID
	LEFT JOIN iMessageToActivate am ON i.statusDescription COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT
where i.statusDescription like 'OK'
ORDER BY 
	issueDate 

select 
	'i_' + cast(i2.issueID as varchar) as issueID,
	am.message as statusDescription,	
	i2.[name],
	i2.advertTypeName,
	dbo.fn_Int2Time(i2.duration) as duration,
	ip.[description] as issuePosition,
	i2.issueDate,
	m.name as radiostationName,
	m.groupName
from
	@ISSUE i2
	Inner Join vMassmedia m On m.massmediaID = i2.massmediaID
	INNER JOIN iIssuePosition ip ON ip.positionID = i2.positionID
	LEFT JOIN iMessageToActivate am ON i2.statusDescription COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT
where i2.statusDescription <> 'OK'
union all
select 
	'pi_' + cast(i.issueID as  varchar),
	am.message as statusDescription, 
	i.[name], 
	i.advertTypeName,
	dbo.fn_Int2Time(i.duration) as duration,
	null as issuePosition,
	i.issueDate as issueDate,
	m.name as radiostationName,
	m.groupName
from 
	@programmissue i
	Inner Join Campaign c on c.campaignID = i.campaignID
	Inner Join vMassmedia m On m.massmediaID = c.massmediaID
	LEFT JOIN iMessageToActivate am ON i.statusDescription COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT
where i.statusDescription <> 'OK'
order by  
	issueDate 

Select am.message as errorMessage from @fatalErrors fe LEFT JOIN iMessageToActivate am ON fe.name COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT

SELECT
	'i_' + cast(i2.issueID as varchar) as issueID,
	am.message as statusDescription,
	i2.[name],
	i2.advertTypeName,
	dbo.fn_Int2Time(i2.duration) as duration,
	ip.[description] as issuePosition,
	i2.oldIssueDate,
	i2.issueDate,
	m.name as radiostationName,
	m.groupName
FROM
	@ISSUE i2
	INNER JOIN iIssuePosition ip ON ip.positionID = i2.positionID
	Inner Join vMassmedia m On m.massmediaID = i2.massmediaID
	LEFT JOIN iMessageToActivate am ON i2.statusDescription COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT
WHERE i2.statusDescription like 'OK'
	and i2.isTransferred = 1
ORDER BY
	i2.issueDate

IF @isTestActivate = 0 AND @blockActivation = 0
begin
--	INSERT INTO [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
--	select @loggedUserID, @actionID, i.rollerID, it.issueDate, tw.massmediaID 
--	from @issue it inner join Issue i on it.issueID = i.issueID inner join TariffWindow tw on i.originalWindowID = tw.windowId where it.statusDescription <> 'OK'
/*
	delete from i from @issue it inner join Issue i on it.issueID = i.issueID where it.statusDescription <> 'OK'
	delete from mi from @issue it inner join ModuleIssue mi on it.moduleIssueID = mi.moduleIssueID where it.statusDescription <> 'OK'
	delete from pmi from @issue it inner join PackModuleIssue pmi on it.packModuleIssueID = pmi.packModuleIssueID where it.statusDescription <> 'OK'
	delete from i from @programmissue it inner join ProgramIssue i on it.issueID = i.issueID where it.statusDescription <> 'OK'
*/

	Update
		TariffWindow
	Set
		timeInUseUnconfirmed =
			Case
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed - r.duration
				Else timeInUseUnconfirmed
			End,
		capacityInUseUnconfirmed =
			Case
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed - 1
				Else capacityInUseUnconfirmed
			End,
		firstPositionsUnconfirmed =
			Case
				When i.positionId = -20 Then firstPositionsUnconfirmed - 1
				Else firstPositionsUnconfirmed
			End,
		secondPositionsUnconfirmed =
			Case
				When i.positionId = -10 Then secondPositionsUnconfirmed - 1
				Else secondPositionsUnconfirmed
			End,
		lastPositionsUnconfirmed =
			Case
				When i.positionId = 10 Then lastPositionsUnconfirmed - 1
				Else lastPositionsUnconfirmed
			End
	From
		@plannedTransfers pt
		Inner Join Issue i On i.issueID = pt.issueID
		Inner Join Roller r On r.rollerID = i.rollerID
	Where
		TariffWindow.windowId = pt.oldWindowID

	Update i
	Set
		originalWindowID = pt.newWindowID,
		actualWindowID = pt.newWindowID,
		tariffPrice = dbo.fn_GetIssuePrice(
			r.duration,
			twNew.price,
			1,
			i.positionId,
			IsNull(pl.extraChargeFirstRoller, 0),
			IsNull(pl.extraChargeSecondRoller, 0),
			IsNull(pl.extraChargeLastRoller, 0))
	From
		Issue i
		Inner Join @plannedTransfers pt On pt.issueID = i.issueID
		Inner Join Roller r On r.rollerID = i.rollerID
		Inner Join TariffWindow twNew On twNew.windowId = pt.newWindowID
		Left Join Tariff t On t.tariffID = twNew.tariffId
		Left Join Pricelist pl On pl.pricelistID = t.pricelistID

	Update
		TariffWindow
	Set
		timeInUseUnconfirmed =
			Case
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed + r.duration
				Else timeInUseUnconfirmed
			End,
		capacityInUseUnconfirmed =
			Case
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed + 1
				Else capacityInUseUnconfirmed
			End,
		firstPositionsUnconfirmed =
			Case
				When i.positionId = -20 Then firstPositionsUnconfirmed + 1
				Else firstPositionsUnconfirmed
			End,
		secondPositionsUnconfirmed =
			Case
				When i.positionId = -10 Then secondPositionsUnconfirmed + 1
				Else secondPositionsUnconfirmed
			End,
		lastPositionsUnconfirmed =
			Case
				When i.positionId = 10 Then lastPositionsUnconfirmed + 1
				Else lastPositionsUnconfirmed
			End
	From
		@plannedTransfers pt
		Inner Join Issue i On i.issueID = pt.issueID
		Inner Join Roller r On r.rollerID = i.rollerID
	Where
		TariffWindow.windowId = pt.newWindowID

	INSERT INTO [TransferLog]([userID], [oldDate], [newDate], [actionID], [issueID])
	SELECT @loggedUserID, oldIssueDate, newIssueDate, @actionID, issueID
	FROM @plannedTransfers

-- 1. Удаление обычных выпусков (Issue)
	DECLARE @delIssueID int;
	DECLARE cur_del_issue CURSOR LOCAL FOR
		SELECT issueID FROM @issue WHERE statusDescription <> 'OK';
	
	OPEN cur_del_issue;
	FETCH NEXT FROM cur_del_issue INTO @delIssueID;
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC [dbo].[IssueIUD]
			@issueID = @delIssueID,
			@actionName = 'DeleteItem',
			@loggedUserId = @loggedUserID;

		FETCH NEXT FROM cur_del_issue INTO @delIssueID;
	END
	CLOSE cur_del_issue;
	DEALLOCATE cur_del_issue;


	-- 2. Удаление ProgramIssue (через соответствующую процедуру)
	DECLARE @delProgIssueID int;
	DECLARE cur_del_prog CURSOR LOCAL FOR
		SELECT issueID FROM @programmissue WHERE statusDescription <> 'OK';
	
	OPEN cur_del_prog;
	FETCH NEXT FROM cur_del_prog INTO @delProgIssueID;
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC [dbo].[ProgramIssueIUD] -- Укажи правильное имя процедуры!
			@issueID = @delProgIssueID,
			@actionName = 'DeleteItem',
			@loggedUserId = @loggedUserID;

		FETCH NEXT FROM cur_del_prog INTO @delProgIssueID;
	END
	CLOSE cur_del_prog;
	DEALLOCATE cur_del_prog;


	-- 3. Удаление ModuleIssue (уникальные ID, чтобы не дергать процу дважды для одного модуля)
	DECLARE @delModuleID int;
	DECLARE cur_del_mod CURSOR LOCAL FOR
		SELECT DISTINCT moduleIssueID FROM @issue WHERE statusDescription <> 'OK' AND moduleIssueID IS NOT NULL;
	
	OPEN cur_del_mod;
	FETCH NEXT FROM cur_del_mod INTO @delModuleID;
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC [dbo].[ModuleIssueIUD] -- Укажи правильное имя процедуры!
			@moduleIssueID = @delModuleID, -- или @issueID, в зависимости от того, как проца принимает параметр
			@actionName = 'DeleteItem',
			@loggedUserId = @loggedUserID;

		FETCH NEXT FROM cur_del_mod INTO @delModuleID;
	END
	CLOSE cur_del_mod;
	DEALLOCATE cur_del_mod;


	-- 4. Удаление PackModuleIssue 
	DECLARE @delPackID int;
	DECLARE cur_del_pack CURSOR LOCAL FOR
		SELECT DISTINCT packModuleIssueID FROM @issue WHERE statusDescription <> 'OK' AND packModuleIssueID IS NOT NULL;
	
	OPEN cur_del_pack;
	FETCH NEXT FROM cur_del_pack INTO @delPackID;
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC [dbo].[PackModuleIssueID] -- Укажи правильное имя процедуры!
			@packModuleIssueID = @delPackID, -- параметр твоей процы
			@actionName = 'DeleteItem',
			@loggedUserId = @loggedUserID;

		FETCH NEXT FROM cur_del_pack INTO @delPackID;
	END
	CLOSE cur_del_pack;
	DEALLOCATE cur_del_pack;

	update i set isConfirmed = 1, activationDate = getdate()
	from Issue i inner join @Issue it on i.issueID = it.issueID and it.statusDescription = 'OK' 

	update i set i.isConfirmed = 1 from ProgramIssue i inner join Campaign c on i.campaignID = c.campaignID inner join @programmissue ii on i.issueID = ii.issueID where c.actionID = @actionID 

	update mi set mi.isConfirmed = 1 from ModuleIssue mi 
		inner join @Issue i on mi.moduleIssueID = i.moduleIssueID and i.statusDescription = 'OK' 
		
	update pmi set pmi.isConfirmed = 1 from PackModuleIssue pmi 
		inner join @Issue i on pmi.packModuleIssueID = i.packModuleIssueID and i.statusDescription = 'OK'  
		
	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When [maxCapacity] = 0 
					Then timeInUseConfirmed + t1.duration
				Else timeInUseConfirmed
			End,
		timeInUseUnconfirmed = 
			Case 
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed - t1.duration
				Else timeInUseUnconfirmed
			End,
		capacityInUseConfirmed = 
			Case 
				When ([maxCapacity] > 0) 
					Then capacityInUseConfirmed + t1.countIssues
				Else capacityInUseConfirmed
			End,
		capacityInUseUnconfirmed = 
			Case  
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed - t1.countIssues
				Else capacityInUseUnconfirmed
			end,
		isFirstPositionOccupied = 
			Case 
				When firstCount > 0 Then 1
				Else isFirstPositionOccupied
			End,
		isSecondPositionOccupied = 
			Case 
				When secondCount > 0 Then 1
				Else isSecondPositionOccupied
			End,
		isLastPositionOccupied = 
			Case 
				When lastCount > 0 Then 1
				Else isLastPositionOccupied
			End,
		firstPositionsUnconfirmed = firstPositionsUnconfirmed - firstCount,
		secondPositionsUnconfirmed = secondPositionsUnconfirmed - secondCount,
		lastPositionsUnconfirmed = lastPositionsUnconfirmed - lastCount
	from
		(select i.actualWindowID as windowID, 
			sum(r.duration) as duration, 
			count(i.issueID) as countIssues,
			sum(coalesce(case when i.positionId = -20 then 1 else 0 end, 0)) as firstCount,
			sum(coalesce(case when i.positionId = -10 then 1 else 0 end, 0)) as secondCount,
			sum(coalesce(case when i.positionId = 10 then 1 else 0 end, 0)) as lastCount
		 from 
			@issue i0 
			inner join Issue i on i0.issueID = i.issueID
			Inner Join Roller r On r.rollerId = i.rollerId
		where i0.statusDescription = 'OK'
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID

	-- Политическая агитация: авто-вставка обвязки (44/7/55) в окна с подтверждённым
	-- типом 6; выпуски создаются в служебной акции, минус по времени окна допустим
	if exists (
		select 1 from @issue it
			inner join Roller r on r.rollerID = it.rollerID
		where it.statusDescription = 'OK' and r.rolActionTypeID = 6)
	begin
		exec AgitationFraming
			@actionName = 'InsertForAction',
			@actionID = @actionID,
			@loggedUserID = @loggedUserID
	end

	UPDATE [Action] Set isConfirmed = 1 WHERE actionID = @actionID

	exec ActionRecalculate
		@actionID = @actionID
END
GO

CREATE OR ALTER PROC [dbo].[rpt_GenericBill]
(
@actionId int,
@agencyId smallint,
@beginDate datetime = null,
@endDate datetime = null
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

declare @invoiceTableText nvarchar(1024), @invoiceTableTextSponsor nvarchar(1024)
select @invoiceTableText = reportText from [dbo].[ReportPartText] where codeName='invoice1'
select @invoiceTableTextSponsor = reportText from [dbo].[ReportPartText] where codeName='InvoiceSponsor'

declare @isByMonth bit
select @isByMonth = case when @beginDate is not null and @endDate is not null then 1 else 0 end

Declare @billDate datetime
if (@isByMonth = 1)
	Select @billDate = @endDate
else
	Select @billDate = billDate
	From [Bill]
	Where	actionID = @actionId And agencyID = @agencyId

declare @res Table (
	[name] NVARCHAR(255),
	[price] decimal(18,2) NOT null,
	[taxPrice] decimal(18,2) not null
)

declare cur_campaigns cursor local fast_forward
for 
select c.campaignID, c.campaignTypeID, c.startDate, c.finishDate
from Campaign c 
	Inner Join Paymenttype pt On pt.PaymenttypeId = c.PaymenttypeId
		and pt.IsHidden = 0
where 
	c.Actionid = @ActionId and
	c.AgencyId = @AgencyId and
	((@beginDate is null and @endDate is null) or 
	(c.startDate <= @endDate and c.finishDate >=  @beginDate))

declare @campaignID int, @campaignTypeID tinyint, @price decimal(18,2), @startDate datetime, @finishDate datetime,@taxPrice decimal(18,2)
open cur_campaigns

fetch next from cur_campaigns into @campaignID, @campaignTypeID, @startDate, @finishDate

while @@fetch_status = 0
begin
	if (@isByMonth = 0)
	begin
		select @beginDate = @startDate, @endDate = @finishDate
	end 

	-- Кампания без единого выпуска в периоде счёта в счёт не попадает (решение заказчика
	-- от 15.09.2026). Раньше такая кампания давала либо ошибку 515 (GetPriceByPeriod на
	-- раннем выходе не присваивает @taxPrice, в @res.taxPrice уходил NULL), либо строку с
	-- нулевой ценой и НДС соседней кампании — переменные цикла между итерациями не сбрасывались.
	-- Проверяем факт выпусков, а не даты кампании: у части кампаний startDate/finishDate пусты
	-- при живых выпусках (акция 185835), по датам такие кампании молча исчезли бы из счёта.
	-- Пустой период (@beginDate is null) здесь означает «вся кампания».
	-- Типы 2 и 4 не проверяем: там вставка идёт запросом с GROUP BY, при отсутствии выпусков
	-- он сам не даёт ни одной строки.
	if @campaignTypeID in (1,3)
		and not exists (
			select 1
			from Issue i
				inner join TariffWindow tw on i.actualWindowID = tw.windowID
			where @campaignTypeID = 1 and i.campaignID = @campaignID
				and (@beginDate is null or tw.dayOriginal between @beginDate and @endDate))
		and not exists (
			select 1
			from ModuleIssue mi
			where @campaignTypeID = 3 and mi.campaignID = @campaignID
				and (@beginDate is null or mi.issueDate between @beginDate and @endDate))
	begin
		fetch next from cur_campaigns into @campaignID, @campaignTypeID, @startDate, @finishDate
		continue
	end

	-- Сбрасываем на каждой кампании: GetPriceByPeriod на ранних выходах присваивает не все
	-- out-параметры, и без сброса в счёт уходит цена или НДС предыдущей кампании.
	select @price = null, @taxPrice = null

	if (@campaignTypeID in (1,3))
	begin 
		exec GetPriceByPeriod
			@campaignID = @campaignID,
			@campaignTypeID = @campaignTypeID, 
			@startDate = @beginDate,
			@finishDate = @endDate,
			@price = @price OUT,
			@showBlack = 0,
			@withTax = 1,
			@taxPrice = @taxPrice out
		
		insert into @res 
		select replace(replace(@invoiceTableText, '{строка для счёта/договора}', IsNull(mm.reportString, '{строка для счёта/договора}')), '{группа радиостанций}', 
			IsNull(mm.groupName, '{группа радиостанций}')) as name,
			@price as price, @taxPrice
		from Campaign c 
			inner join vMassmedia mm on c.massmediaID = mm.massmediaID
		where c.campaignID = @campaignID
	end 
	else if (@campaignTypeID in (2))
	begin 
		insert into @res
		Select	
			--'Реклама в программе ''' + p.[name] + '''' AS NAME,
			replace(replace(replace(@invoiceTableTextSponsor, '{строка для счёта/договора}', IsNull(mm.reportString, '{строка для счёта/договора}')), 
			'{группа радиостанций}', IsNull(mm.groupName, '{группа радиостанций}')),  
			'{программа}', p.[name]) as name,
			Sum(i.[tariffPrice] * i.[ratio]) as price,
			sum(case when coalesce(at.divisor, 0) < 0.0000001 then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		From		
			ProgramIssue i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join SponsorProgram p on i.programID = p.sponsorProgramID 
			inner join SponsorTariff st on i.tariffID = st.tariffID
			inner join SponsorProgramPricelist pl on st.priceListID = pl.pricelistID
			inner join vMassmedia mm on c.massmediaID = mm.massmediaID
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and Convert(datetime, Convert(varchar(8), i.issueDate, 112), 112) between at.startDate and at.finishDate
		Where		
			i.campaignID = @campaignID and 
			i.issueDate between @beginDate and dateadd(ss, -1, dateadd(day, 1, @endDate))
		GROUP BY 
			p.[name], mm.reportString, mm.groupName
	end 
	else if (@campaignTypeID in (4))
	begin 
		Exec GetPriceByPeriod 
			@campaignId, @campaignTypeID, @beginDate, @endDate, @price out, @withTax = 1, @taxPrice = @taxPrice out
		
		CREATE TABLE #tmp(massmediaID SMALLINT, price decimal(18,2), taxPrice decimal(18,2))--, tariffPrice decimal(18,2))
		INSERT INTO #tmp
		SELECT 
			m.[massmediaID], sum(mpl.[price]),--, i.[tariffPrice]
			sum(case when at.divisor is null then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		FROM [PackModuleIssue] i 
			inner join dbo.Campaign c on i.campaignID = c.campaignID
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (pt.IsHidden = 0)
			INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
			INNER JOIN [ModulePriceList] AS mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
			INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and i.issueDate between at.startDate and at.finishDate
		WHERE 
			i.campaignID = @campaignID
			and i.issueDate between @beginDate and @finishDate
		group by m.massmediaID

		declare @sumPrice decimal(18,2)
		SELECT @sumPrice = sum(t1.price) FROM [#tmp] AS t1
		
		INSERT INTO @res 
		select replace(replace(@invoiceTableText, '{строка для счёта/договора}', IsNull(mm.reportString, '{строка для счёта/договора}')), '{группа радиостанций}', IsNull(mm.groupName, '{группа радиостанций}')) as name,
				(@price * sum(t1.price)/ @sumPrice) as price,
				(@taxPrice * sum(t1.price)/ @sumPrice) as taxPrice
		from #tmp as t1
			inner join vMassmedia mm on t1.massmediaID = mm.massmediaID
		group by t1.massmediaID, mm.groupName, mm.reportString
		
		drop table #tmp 
		
	end 		
	
	fetch next from cur_campaigns into @campaignID, @campaignTypeID, @startDate, @finishDate
end

close cur_campaigns
deallocate cur_campaigns

declare @res2 Table (
	[name] NVARCHAR(255),
	[quantity] int NOT null,
	[tax] decimal(18,2) NOT null,
	[price] decimal(18,2) not null,
	dirPainting image,
	qrCode image
)

-- Result
Insert into @res2 ([name], [quantity], [tax], [price])
SELECT 
	[name],
	COUNT(*) AS [quantity],
	sum([taxPrice])	as tax,	
	SUM([price]) AS [price]
FROM 	   
	@res
GROUP BY 
	[name]

Update @res2 Set dirPainting = painting
From Agency Where agencyID = @agencyId

select * from @res2
GO

CREATE OR ALTER PROC [dbo].[CampaignsForActJournalRetrieve]
(
@startDate DATETIME = null,
@finishDate DATETIME = null,
@agencyID int = null,
@firmId int = null,
@showBlack bit = 1,
@showWhite bit = 1,
@actionID INT = null,
@loggedUserID smallint,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
Set Nocount On
DECLARE @tSec NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' сек.');
DECLARE @tPcs NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' шт.');

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
				Convert(datetime, Convert(varchar(8), i.issueDate, 112), 112) between dbo.ToShortDate(@startDate) and dbo.ToShortDate(@currentDate) 
		END
		ELSE
		BEGIN
			select 
				@issuesCount = COUNT(*), 
				@issuesDuration = SUM(rol.duration), 
				@showByDuration = case when SUM(tw.maxCapacity) > 0 then 0 else 1 end
			from Issue i 
				inner join Roller rol on rol.rollerID = i.rollerID
				inner join TariffWindow tw on i.actualWindowID = tw.windowId
				left join ModuleIssue mi on mi.moduleIssueID = i.moduleIssueID
			where i.campaignID = @campaignID and coalesce(mi.issueDate, tw.dayOriginal) between dbo.ToShortDate(@startDate) and dbo.ToShortDate(@currentDate) 
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
	case when r.showByDuration = 1 then dbo.fn_Int2Time(r.issuesDuration) + @tSec else cast(r.issuesCount as nvarchar(10)) + @tPcs end as saleVolume
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
        [headCompanyName]      NVARCHAR(256),
        [massmediaGroupID]     INT,
        [massmediaGroupName]   NVARCHAR(250),
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
        LEFT JOIN [dbo].[TariffWindow] tw ON i.[actualWindowID] = tw.[windowId]
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
                i.[issueDate], 112), 112)
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

CREATE OR ALTER PROC [dbo].[stat_GetPrice_proc]
(
    @startDate datetime,
    @finishDate datetime,
    @loggedUserID smallint
)
AS
BEGIN
    SET NOCOUNT ON;

    ------------------------------------------------------------
    -- права доступа — считаем ОДИН раз
    ------------------------------------------------------------
    DECLARE @canForeign bit = dbo.fn_IsRightToViewForeignActions(@loggedUserID);
    DECLARE @canGroup   bit = dbo.fn_IsRightToViewGroupActions(@loggedUserID);

    ------------------------------------------------------------
    -- temp tables вместо table variables
    ------------------------------------------------------------
    CREATE TABLE #moduleMassmediaPrice
    (
        campaignID int NOT NULL,
        massmediaID smallint NOT NULL,
        moduleMassmediaPrice decimal(18,2) NOT NULL,
        PRIMARY KEY (campaignID, massmediaID)
    );

    CREATE TABLE #modulePrice
    (
        campaignID int NOT NULL PRIMARY KEY,
        modulePrice decimal(18,2) NOT NULL
    );

    CREATE TABLE #Result
    (
        campaignID int NOT NULL,
        advertTypeID smallint NULL,
        actionID int NOT NULL,
        massmediaID smallint NOT NULL,
        paymentTypeID smallint NOT NULL,
        campaignTypeID tinyint NOT NULL,
        agencyID smallint NULL,
        startDate datetime NOT NULL,
        finishDate datetime NOT NULL,
        finalPrice decimal(18,2) NULL,
        userID smallint NOT NULL,
        firmID smallint NOT NULL,
        discount decimal(18,10) NULL,
        massmediaGroupID int NULL,
        price decimal(18,2) NOT NULL
    );

    CREATE CLUSTERED INDEX IX_Result
        ON #Result (campaignID, massmediaID, advertTypeID);

    ------------------------------------------------------------
    -- 1. Пакетные цены (campaignTypeID = 4)
    ------------------------------------------------------------
    INSERT INTO #moduleMassmediaPrice
    SELECT
        i.campaignID,
        m.massmediaID,
        SUM(mpl.price)
    FROM PackModuleIssue i
        JOIN Campaign c ON c.campaignID = i.campaignID AND c.campaignTypeID = 4
        JOIN PackModuleContent pmc ON i.priceListID = pmc.pricelistID
        JOIN ModulePriceList mpl ON pmc.modulePriceListID = mpl.modulePriceListID
        JOIN Module m ON mpl.moduleID = m.moduleID
    WHERE i.issueDate BETWEEN @startDate AND @finishDate
    GROUP BY i.campaignID, m.massmediaID;

    INSERT INTO #modulePrice
    SELECT campaignID, SUM(moduleMassmediaPrice)
    FROM #moduleMassmediaPrice
    GROUP BY campaignID;

    ------------------------------------------------------------
    -- 2. Специальные акции
    ------------------------------------------------------------
    INSERT INTO #Result
    SELECT
        c.campaignID,
        NULL,
        c.actionID,
        c.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID,
        c.price
    FROM Campaign c
        JOIN Action a ON a.actionID = c.actionID AND a.isConfirmed = 1
        JOIN MassMedia m ON m.massmediaID = c.massmediaID
    WHERE a.isSpecial = 1
      AND c.startDate <= @finishDate
      AND c.finishDate >= @startDate;

    ------------------------------------------------------------
    -- 3. Линейная
    ------------------------------------------------------------
    INSERT INTO #Result
    SELECT
        c.campaignID,
        r.advertTypeID,
        c.actionID,
        c.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID,
        CAST(ROUND(SUM(i.tariffPrice * i.ratio),2) AS decimal(18,2))
    FROM Campaign c
        JOIN Action a ON a.actionID = c.actionID AND a.isConfirmed = 1
        JOIN Issue i ON i.campaignID = c.campaignID
        JOIN TariffWindow tw ON tw.windowId = i.actualWindowID
        JOIN Roller r ON r.rollerID = i.rollerID
        JOIN MassMedia m ON m.massmediaID = c.massmediaID
    WHERE c.campaignTypeID = 1
      AND a.isSpecial = 0
      AND tw.dayOriginal BETWEEN @startDate AND @finishDate
    GROUP BY
        c.campaignID,
        r.advertTypeID,
        c.actionID,
        c.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID;

    ------------------------------------------------------------
    -- 4. Модульная
    ------------------------------------------------------------
    INSERT INTO #Result
    SELECT
        c.campaignID,
        r.advertTypeID,
        c.actionID,
        c.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID,
        CAST(ROUND(SUM(i.tariffPrice * i.ratio),2) AS decimal(18,2))
    FROM Campaign c
        JOIN Action a ON a.actionID = c.actionID AND a.isConfirmed = 1
        JOIN ModuleIssue i ON i.campaignID = c.campaignID
        JOIN Roller r ON r.rollerID = i.rollerID
        JOIN MassMedia m ON m.massmediaID = c.massmediaID
    WHERE c.campaignTypeID = 3
      AND a.isSpecial = 0
      AND i.issueDate BETWEEN @startDate AND @finishDate
    GROUP BY
        c.campaignID,
        r.advertTypeID,
        c.actionID,
        c.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID;

    ------------------------------------------------------------
    -- 5. Пакетная (campaignTypeID = 4)
    ------------------------------------------------------------
    INSERT INTO #Result
    SELECT
        c.campaignID,
        r.advertTypeID,
        c.actionID,
        mmp.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID,
        CAST(ROUND(SUM(i.tariffPrice * i.ratio * mmp.moduleMassmediaPrice / mp.modulePrice),2) AS decimal(18,2))
    FROM Campaign c
        JOIN Action a ON a.actionID = c.actionID AND a.isConfirmed = 1
        JOIN PackModuleIssue i ON i.campaignID = c.campaignID
        JOIN Roller r ON r.rollerID = i.rollerID
        JOIN #moduleMassmediaPrice mmp ON mmp.campaignID = c.campaignID
        JOIN #modulePrice mp ON mp.campaignID = c.campaignID
        JOIN MassMedia m ON m.massmediaID = mmp.massmediaID
    WHERE c.campaignTypeID = 4
      AND a.isSpecial = 0
      AND i.issueDate BETWEEN @startDate AND @finishDate
    GROUP BY
        c.campaignID,
        r.advertTypeID,
        c.actionID,
        mmp.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID;

------------------------------------------------------------
-- 3.5 Спонсорская (campaignTypeID = 2)  [ДОБАВЛЕНО]
-- Важно: у спонсорских может не быть записей в Issue.
-- Источник фактов: ProgramIssue + SponsorTariff/SponsorProgramPriceList
------------------------------------------------------------
INSERT INTO #Result
SELECT
    c.campaignID,
    pi.advertTypeID,
    c.actionID,
    c.massmediaID,
    c.paymentTypeID,
    c.campaignTypeID,
    c.agencyID,
    c.startDate,
    c.finishDate,
    c.finalPrice,
    a.userID,
    a.firmID,
    a.discount,
    m.massmediaGroupID,
    CAST(ROUND(SUM(pi.tariffPrice * pi.ratio), 2) AS decimal(18,2)) AS price
FROM Campaign c
    JOIN Action a ON a.actionID = c.actionID AND a.isConfirmed = 1
    JOIN ProgramIssue pi ON pi.campaignID = c.campaignID
    JOIN MassMedia m ON m.massmediaID = c.massmediaID
WHERE c.campaignTypeID = 2
  AND a.isSpecial = 0
  AND c.startDate <= @finishDate
  AND c.finishDate >= @startDate
  AND EXISTS
  (
      SELECT 1
      FROM SponsorTariff st
          JOIN SponsorProgramPriceList pl ON st.priceListID = pl.priceListID
      WHERE st.tariffID = pi.tariffID
        AND
        (
            -- день выпуска (сдвиг на начало эфирного дня broadcastStart снят 10.10.2026: он везде 00:00, docs/broadcast-start.md §7)
            CONVERT(datetime,
                CONVERT(varchar(8),
                    pi.issueDate,
                112),
            112)
        ) BETWEEN @startDate AND @finishDate
  )
GROUP BY
    c.campaignID,
    pi.advertTypeID,
    c.actionID,
    c.massmediaID,
    c.paymentTypeID,
    c.campaignTypeID,
    c.agencyID,
    c.startDate,
    c.finishDate,
    c.finalPrice,
    a.userID,
    a.firmID,
    a.discount,
    m.massmediaGroupID;


    ------------------------------------------------------------
    -- 6. Права доступа + вывод
    ------------------------------------------------------------
    SELECT r.*
    FROM #Result r
    WHERE EXISTS (
        SELECT 1
        FROM fn_GetMassmediasForUser(@loggedUserID) mm
        WHERE mm.massmediaID = r.massmediaID
          AND (
                (r.userID = @loggedUserID AND mm.myMassmedia = 1)
             OR (r.userID <> @loggedUserID AND mm.foreignMassmedia = 1)
          )
    )
    AND (
            r.userID = @loggedUserID
         OR @canForeign = 1
         OR (
                @canGroup = 1
            AND EXISTS (
                SELECT 1
                FROM GroupMember gm
                JOIN fn_GetUserGroups(@loggedUserID) ug ON ug.id = gm.groupID
                WHERE gm.userID = r.userID
            )
         )
    );
END
GO

CREATE OR ALTER PROC [dbo].[stat_GetPriceByMonth_proc]
(
    @startDate datetime,
    @finishDate datetime,
    @loggedUserID smallint
)
AS
BEGIN
    SET NOCOUNT ON;

    ------------------------------------------------------------
    -- права доступа — считаем ОДИН раз
    ------------------------------------------------------------
    DECLARE @canForeign bit = dbo.fn_IsRightToViewForeignActions(@loggedUserID);
    DECLARE @canGroup   bit = dbo.fn_IsRightToViewGroupActions(@loggedUserID);

    ------------------------------------------------------------
    -- Пакетные цены по месяцам (campaignTypeID = 4)
    ------------------------------------------------------------
    CREATE TABLE #moduleMassmediaPrice
    (
        y smallint NOT NULL,
        m tinyint NOT NULL,
        campaignID int NOT NULL,
        massmediaID smallint NOT NULL,
        moduleMassmediaPrice decimal(18,2) NOT NULL,
        PRIMARY KEY (y, m, campaignID, massmediaID)
    );

    CREATE TABLE #modulePrice
    (
        y smallint NOT NULL,
        m tinyint NOT NULL,
        campaignID int NOT NULL,
        modulePrice decimal(18,2) NOT NULL,
        PRIMARY KEY (y, m, campaignID)
    );

    CREATE TABLE #Result
    (
        y smallint NOT NULL,
        m tinyint NOT NULL,
        campaignID int NOT NULL,
        advertTypeID smallint NULL,
        actionID int NOT NULL,
        massmediaID smallint NOT NULL,
        paymentTypeID smallint NOT NULL,
        campaignTypeID tinyint NOT NULL,
        agencyID smallint NULL,
        startDate datetime NOT NULL,
        finishDate datetime NOT NULL,
        finalPrice decimal(18,2) NULL,
        userID smallint NOT NULL,
        firmID smallint NOT NULL,
        discount decimal(18,10) NULL,
        massmediaGroupID int NULL,
        price decimal(18,2) NOT NULL
    );

    CREATE UNIQUE CLUSTERED INDEX IX_Result
        ON #Result (y, m, campaignID, massmediaID, advertTypeID);

    ------------------------------------------------------------
    -- moduleMassmediaPrice (type 4) по mn
    ------------------------------------------------------------
    INSERT INTO #moduleMassmediaPrice (y, m, campaignID, massmediaID, moduleMassmediaPrice)
    SELECT
        mn.y,
        mn.m,
        i.campaignID,
        m.massmediaID,
        SUM(mpl.price) AS moduleMassmediaPrice
    FROM PackModuleIssue i
        JOIN Campaign c ON c.campaignID = i.campaignID AND c.campaignTypeID = 4
        JOIN PackModuleContent pmc ON i.priceListID = pmc.pricelistID
        JOIN ModulePriceList mpl ON pmc.modulePriceListID = mpl.modulePriceListID
        JOIN Module m ON mpl.moduleID = m.moduleID
        JOIN dbo.f_months(@startDate, @finishDate) mn
            ON i.issueDate BETWEEN mn.startDate AND mn.finishDate
    WHERE i.issueDate BETWEEN @startDate AND @finishDate
    GROUP BY mn.y, mn.m, i.campaignID, m.massmediaID;

    INSERT INTO #modulePrice (y, m, campaignID, modulePrice)
    SELECT y, m, campaignID, SUM(moduleMassmediaPrice)
    FROM #moduleMassmediaPrice
    GROUP BY y, m, campaignID;

    ------------------------------------------------------------
    -- 1) Линейная (campaignTypeID=1) — 1:1 как в fn_statGetPriceByMonth
    ------------------------------------------------------------
    INSERT INTO #Result
    (
        y, m,
        campaignID, advertTypeID, actionID, massmediaID, paymentTypeID, campaignTypeID, agencyID,
        startDate, finishDate, finalPrice, userID, firmID, discount, massmediaGroupID, price
    )
    SELECT
        mn.y,
        mn.m,
        c.campaignID,
        r.advertTypeID,
        c.actionID,
        c.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID,
        CAST(ROUND(SUM(i.tariffPrice * i.ratio), 2) AS decimal(18,2)) AS price
    FROM dbo.f_months(@startDate, @finishDate) mn
        JOIN Campaign c
            ON c.campaignTypeID = 1
           AND c.startDate <= @finishDate
           AND c.finishDate >= @startDate
        JOIN Action a
            ON a.actionID = c.actionID
           AND a.isConfirmed = 1
           AND a.isSpecial = 0
        JOIN MassMedia m ON m.massmediaID = c.massmediaID
        JOIN Issue i ON i.campaignID = c.campaignID
        JOIN Roller r ON r.rollerID = i.rollerID
    WHERE EXISTS
    (
        SELECT 1
        FROM TariffWindow tw
        WHERE tw.windowId = i.actualWindowID
          AND tw.dayOriginal BETWEEN mn.startDate AND mn.finishDate
    )
    GROUP BY
        mn.y, mn.m,
        c.campaignID,
        r.advertTypeID,
        c.actionID, c.massmediaID, c.paymentTypeID, c.campaignTypeID, c.agencyID,
        c.startDate, c.finishDate, c.finalPrice,
        a.userID, a.firmID, a.discount,
        m.massmediaGroupID;

    ------------------------------------------------------------
    -- 2) Спонсорская (campaignTypeID=2) — по дню выпуска (сдвиг broadcastStart снят 10.10.2026, docs/broadcast-start.md §7)
    ------------------------------------------------------------
    INSERT INTO #Result
    (
        y, m,
        campaignID, advertTypeID, actionID, massmediaID, paymentTypeID, campaignTypeID, agencyID,
        startDate, finishDate, finalPrice, userID, firmID, discount, massmediaGroupID, price
    )
    SELECT
        mn.y,
        mn.m,
        c.campaignID,
        pi.advertTypeID,
        c.actionID,
        c.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        mm.massmediaGroupID,
        CAST(ROUND(SUM(pi.tariffPrice * pi.ratio), 2) AS decimal(18,2)) AS price
    FROM Campaign c
        JOIN Action a
            ON a.actionID = c.actionID
           AND a.isConfirmed = 1
           AND a.isSpecial = 0
        JOIN MassMedia mm ON mm.massmediaID = c.massmediaID
        JOIN ProgramIssue pi ON pi.campaignID = c.campaignID
        JOIN SponsorTariff st ON pi.tariffID = st.tariffID
        JOIN SponsorProgramPriceList pl ON st.priceListID = pl.priceListID
        JOIN dbo.f_months(@startDate, @finishDate) mn
            ON CONVERT(datetime,
                    CONVERT(varchar(8),
                        pi.issueDate,
                        112
                    ),
                    112
               ) BETWEEN mn.startDate AND mn.finishDate
    WHERE c.campaignTypeID = 2
      AND c.startDate <= @finishDate
      AND c.finishDate >= @startDate
    GROUP BY
        mn.y, mn.m,
        c.campaignID,
        pi.advertTypeID,
        c.actionID, c.massmediaID, c.paymentTypeID, c.campaignTypeID, c.agencyID,
        c.startDate, c.finishDate, c.finalPrice,
        a.userID, a.firmID, a.discount,
        mm.massmediaGroupID;

    ------------------------------------------------------------
    -- 3) Модульная (campaignTypeID=3) — 1:1 как fn_statGetPriceByMonth
    ------------------------------------------------------------
    INSERT INTO #Result
    (
        y, m,
        campaignID, advertTypeID, actionID, massmediaID, paymentTypeID, campaignTypeID, agencyID,
        startDate, finishDate, finalPrice, userID, firmID, discount, massmediaGroupID, price
    )
    SELECT
        mn.y,
        mn.m,
        c.campaignID,
        r.advertTypeID,
        c.actionID,
        c.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID,
        CAST(ROUND(SUM(mi.tariffPrice * mi.ratio), 2) AS decimal(18,2)) AS price
    FROM Campaign c
        JOIN Action a
            ON a.actionID = c.actionID
           AND a.isConfirmed = 1
           AND a.isSpecial = 0
        JOIN MassMedia m ON m.massmediaID = c.massmediaID
        JOIN ModuleIssue mi ON mi.campaignID = c.campaignID
        JOIN dbo.f_months(@startDate, @finishDate) mn
            ON mi.issueDate BETWEEN mn.startDate AND mn.finishDate
        JOIN Roller r ON r.rollerID = mi.rollerID
    WHERE c.campaignTypeID = 3
      AND c.startDate <= @finishDate
      AND c.finishDate >= @startDate
    GROUP BY
        mn.y, mn.m,
        c.campaignID,
        r.advertTypeID,
        c.actionID, c.massmediaID, c.paymentTypeID, c.campaignTypeID, c.agencyID,
        c.startDate, c.finishDate, c.finalPrice,
        a.userID, a.firmID, a.discount,
        m.massmediaGroupID;

    ------------------------------------------------------------
    -- 4) Пакетная (campaignTypeID=4) — 1:1 как fn_statGetPriceByMonth
    ------------------------------------------------------------
    INSERT INTO #Result
    (
        y, m,
        campaignID, advertTypeID, actionID, massmediaID, paymentTypeID, campaignTypeID, agencyID,
        startDate, finishDate, finalPrice, userID, firmID, discount, massmediaGroupID, price
    )
    SELECT
        mn.y,
        mn.m,
        c.campaignID,
        r.advertTypeID,
        c.actionID,
        mmp.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        mm.massmediaGroupID,
        CAST(ROUND(SUM(pmi.tariffPrice * pmi.ratio * mmp.moduleMassmediaPrice / mp.modulePrice), 2) AS decimal(18,2)) AS price
    FROM Campaign c
        JOIN Action a
            ON a.actionID = c.actionID
           AND a.isConfirmed = 1
           AND a.isSpecial = 0
        JOIN PackModuleIssue pmi ON pmi.campaignID = c.campaignID
        JOIN dbo.f_months(@startDate, @finishDate) mn
            ON pmi.issueDate BETWEEN mn.startDate AND mn.finishDate
        JOIN Roller r ON r.rollerID = pmi.rollerID
        JOIN #moduleMassmediaPrice mmp
            ON mmp.y = mn.y AND mmp.m = mn.m AND mmp.campaignID = c.campaignID
        JOIN #modulePrice mp
            ON mp.y = mn.y AND mp.m = mn.m AND mp.campaignID = c.campaignID
        JOIN MassMedia mm
            ON mm.massmediaID = mmp.massmediaID
    WHERE c.campaignTypeID = 4
      AND c.startDate <= @finishDate
      AND c.finishDate >= @startDate
    GROUP BY
        mn.y, mn.m,
        c.campaignID,
        mmp.massmediaID,
        r.advertTypeID,
        c.actionID, c.paymentTypeID, c.campaignTypeID, c.agencyID,
        c.startDate, c.finishDate, c.finalPrice,
        a.userID, a.firmID, a.discount,
        mm.massmediaGroupID;

    ------------------------------------------------------------
    -- Права доступа + вывод (как в твоей исходной fn_* версии)
    ------------------------------------------------------------
    SELECT r.*
    FROM #Result r
    WHERE EXISTS
    (
        SELECT 1
        FROM fn_GetMassmediasForUser(@loggedUserID) mmu
        WHERE mmu.massmediaID = r.massmediaID
          AND (
                (r.userID = @loggedUserID AND mmu.myMassmedia = 1)
             OR (r.userID <> @loggedUserID AND mmu.foreignMassmedia = 1)
          )
    )
    AND
    (
           r.userID = @loggedUserID
        OR @canForeign = 1
        OR (
                @canGroup = 1
            AND EXISTS
            (
                SELECT 1
                FROM GroupMember gm
                JOIN fn_GetUserGroups(@loggedUserID) ug ON ug.id = gm.groupID
                WHERE gm.userID = r.userID
            )
        )
    );
END
GO

-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 24.11.2008
-- Description:	Объем реализации в секундах
-- =============================================
CREATE OR ALTER procedure [dbo].[Stat_VolumeOfRealizationSec] 
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
	@loggedUserID smallint,
	@isShowNotActivated bit = 0
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;
	
	create table #massmedias (massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into #massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit,
			@isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	create table #ugroups (id int)
	insert into #ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

	If	@StartDay Is Null Or @FinishDay Is Null
	Begin
		Raiserror('FilterStartFinishDays', 16, 1)
		Return
	end
	
	declare @sql nvarchar(max),	@IsStarted int
	
	Set	@sql = N'select row_number() over(order by coalesce(sum(r.duration), 0)) as RowNum,'
	
	If	@IsGroupByPaymentType <> 0
		Set 	@sql = @sql + N'pt.Name as "payment_type",'
	If	@IsGroupByCampaignType <> 0
		Set 	@sql = @sql + N'ct.Name as "campaign_type",'
	If	@IsGroupByMassmedia <> 0
		Set 	@sql = @sql + N'mm.Name as "massmedia", mm.groupName as "massmedia_group",'
	If	@IsGroupByMassmediaGroupType <> 0
		Set 	@sql = @sql + N'mmg.Name as "massmedia_group",'
	If	@IsGroupByFirm <> 0
		Set 	@sql = @sql + N'f.Name as "firm",'
	If	@IsGroupByManager <> 0
		Set 	@sql = @sql + N'coalesce(u.LastName, '''') + coalesce(space(1) + u.FirstName, '''') as "manager",'
	If	@IsGroupByAgency <> 0
		Set 	@sql = @sql + N'ag.Name as "agency",'


	Set 	@sql = @sql + 
			N'dbo.fn_Int2Time(sum(r.duration)) as duration 	
	 from Issue i 
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
		inner join Roller r on i.rollerID = r.rollerID
		inner join Campaign c on i.campaignID = c.campaignID
		inner join [Action] a on a.actionID = c.actionID
		inner join PaymentType pt on c.paymentTypeID = pt.paymentTypeID
		inner join #massmedias mmu on tw.massmediaID = mmu.massmediaID
		inner join vMassMedia mm on mmu.massmediaID = mm.massmediaID
		inner join 
				(
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join #ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as x on a.userID = x.userID '

	If	@IsGroupByMassmediaGroupType <> 0 Set @sql = @sql + N' inner join MassmediaGroup as mmg on mm.massmediaGroupID = mmg.massmediaGroupID '
	If	@IsGroupByCampaignType <> 0 Set @sql = @sql + N' inner join iCampaignType as ct on c.campaignTypeID = ct.CampaignTypeID'
	If	@IsGroupByFirm <> 0 Set @sql = @sql + N' inner join Firm as f on a.firmID = f.FirmID '
	If	@IsGroupByManager <> 0 Set @sql = @sql + N' inner join [User] as u on a.userID = u.UserID'
	If	@IsGroupByAgency <> 0 Set @sql = @sql + N' inner join Agency as ag on c.AgencyID = ag.AgencyID'

	set @sql = @sql + N' where'

	if @isShowNotActivated = 0
	begin 
		set @sql = @sql + N' i.isConfirmed = 1 and'
	end

	set @sql = @sql + N' tw.dayOriginal between @StartDay and @FinishDay and
		(@FirmID is null or a.firmID = @FirmID) and
		(@ManagerID is null or a.userID = @ManagerID) and
		(@CampaignTypeID is null or c.campaignTypeID = @CampaignTypeID) and
		(@AgencyID is null or c.AgencyID = @AgencyID) and
		(@PaymentTypeID is null or c.PaymentTypeID = @PaymentTypeID) and
		(@ShowWhite <> 0 or pt.isHidden <> 0) and  
		(@ShowBlack <> 0 or pt.isHidden = 0)  
		and (@MassmediaID is null or mmu.massmediaID = @MassmediaID)
		and (@massmediaGroupID is null or mm.massmediaGroupId = @massmediaGroupID) '

	If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + @IsGroupByMassmedia + @IsGroupByFirm + @IsGroupByManager 
		+ @IsGroupByAgency + @IsGroupByMassmediaGroupType <> 0
	begin
		set	@IsStarted = 0
		Set 	@sql = @sql + N' Group by '
	
		if	@IsGroupByPaymentType <> 0 begin
			if	@IsStarted = 1 set @sql = @sql + N','	
			Set 	@sql = @sql + N'pt.Name'
			set	@IsStarted = 1
		end

		If	@IsGroupByCampaignType <> 0
			begin
			if	@IsStarted = 1 set @sql = @sql + N','	
			Set 	@sql = @sql + N'ct.Name'
			set	@IsStarted = 1
			end

		If	@IsGroupByMassmedia <> 0
			begin
			if	@IsStarted = 1 set  @sql = @sql + N','	
			Set 	@sql = @sql + N'mm.Name, mm.groupName'
			set	@IsStarted = 1
			end

		If	@IsGroupByFirm <> 0
			begin
			if	@IsStarted = 1 set  @sql = @sql + N','	
			Set 	@sql = @sql + N'f.Name'
			set	@IsStarted = 1
			end

		If	@IsGroupByManager <> 0
			begin

			if	@IsStarted = 1 set  @sql = @sql + N','	
			Set 	@sql = @sql + N'coalesce(u.LastName, '''') + coalesce(space(1) + u.FirstName, '''')'
			set	@IsStarted = 1
			end

		If	@IsGroupByAgency <> 0
			begin
			if	@IsStarted = 1 set  @sql = @sql + N','	
			Set 	@sql = @sql + N'ag.Name'
			set	@IsStarted = 1
			end
		
		If	@IsGroupByMassmediaGroupType <> 0
			begin
			if	@IsStarted = 1 set  @sql = @sql + N','	
			Set @sql = @sql + N'mmg.Name'
			set	@IsStarted = 1
			end

	end
	
	EXECUTE sp_executesql @sql,
		N'@StartDay DATETIME, @FinishDay DATETIME,@FirmID int, @MassmediaID int, @PaymentTypeID int,@CampaignTypeID int,@ManagerID int,@AgencyID int, @massmediaGroupID int,@ShowWhite bit,@ShowBlack bit,@loggedUserID smallint,@isRightToViewForeignActions bit,@isRightToViewGroupActions bit',
			@StartDay = @StartDay,
			@FinishDay = @FinishDay,
			@FirmID = @FirmID, 
			@MassmediaID = @MassmediaID, 
			@PaymentTypeID = @PaymentTypeID,
			@CampaignTypeID = @CampaignTypeID,
			@ManagerID = @ManagerID,
			@AgencyID = @AgencyID,
			@massmediaGroupID = @massmediaGroupID,
			@ShowWhite = @ShowWhite,
			@ShowBlack = @ShowBlack,
			@loggedUserID = @loggedUserID,
			@isRightToViewForeignActions = @isRightToViewForeignActions,
			@isRightToViewGroupActions = @isRightToViewGroupActions

	drop table [#massmedias]
end
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
					inner join TariffWindow tw on i.actualWindowID = tw.windowId
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
					inner join TariffWindow tw on i.actualWindowID = tw.windowId
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

CREATE OR ALTER Proc [dbo].[MoveIssues2NewCampaign]
(
@oldCampaignId int,
@newCampaignId int,
@rollerId int = null,
@splitDate datetime = null,
@splitType int
)
AS

If @splitType = 1 -- split by period
	Update 
		Issue
	Set 
		campaignID = @newCampaignId
	From 
		Issue i
		Inner Join TariffWindow tw on i.actualWindowID = tw.windowId
	Where 
		campaignID = @oldCampaignId
		and tw.dayOriginal >= @splitDate
Else -- split by rollers
	Update 
		Issue
	Set 
		campaignID = @newCampaignId
	Where 
		campaignID = @oldCampaignId
		and rollerID = @rollerId
GO

CREATE OR ALTER PROC [dbo].[IssueTransfer]
(
@issueID int,
@campaignID int,
@newWindowId int,
@newDate datetime,
@newPosition smallint = null,
@loggedUserID smallint,
@massmediaID smallint,
@isConfirmed bit
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

declare 
	@isAdmin bit,	
	@IsTrafficManager bit,	
	@rightForMinus bit, 
	@rightToGoBack bit

-- Р-12 (09.10.2026): окно-приёмник — той же станции, что и окно, где выпуск стоит сейчас (у пакетной
-- кампании Campaign.massmediaID пуст: пакет на нескольких станциях). Интерфейс переносит только внутри
-- сетки одной станции; здесь — страховка от ошибки вызывающего кода.
if not exists (select 1
		from Issue i
			inner join TariffWindow twCur on twCur.windowId = i.actualWindowID
			inner join TariffWindow tw on tw.windowId = @newWindowId and tw.massmediaID = twCur.massmediaID
		where i.issueID = @issueID)
begin
	raiserror('TransferOtherMassmedia', 16, 1)
	return
end

-- Р-12: в окне один «Локальное СМИ»/«Локальное СМИ (агитация)», один «Федеральное СМИ»/«Федеральное
-- СМИ (агитация)» и одна «Отбивка политической агитации» — как при постановке (hlp_IssueVerify),
-- по окну выхода. Раньше перенос трафиком это не проверял.
declare @transferRolType tinyint, @transferTypeError varchar(50)
select @transferRolType = r.rolActionTypeID
from Issue i inner join Roller r on r.rollerID = i.rollerID
where i.issueID = @issueID

if @transferRolType in (4, 44, 5, 55, 7) and exists (select 1
		from Issue i2
			inner join Roller r2 on r2.rollerID = i2.rollerID
		where i2.actualWindowID = @newWindowId
			and i2.issueID <> @issueID
			and ((@transferRolType in (4, 44) and r2.rolActionTypeID in (4, 44))
				or (@transferRolType in (5, 55) and r2.rolActionTypeID in (5, 55))
				or (@transferRolType = 7 and r2.rolActionTypeID = 7)))
begin
	set @transferTypeError = case when @transferRolType in (4, 44) then 'RolType4AlreadyExistInWindow'
		when @transferRolType in (5, 55) then 'RolType5AlreadyExistInWindow'
		else 'RolType7AlreadyExistInWindow' end
	raiserror(@transferTypeError, 16, 1)
	return
end

-- Проверка на возможность размещения
if exists(select * 
		from DisabledWindow dw 
			inner join TariffWindow tw on tw.massmediaID = dw.massmediaID
				and tw.windowDateActual between dw.startDate And dw.finishDate
			where tw.windowID = @newWindowId)
begin
	raiserror('DisabledWindowTransfer',16,1)
	return
end 

declare @msgError varchar(50)
EXEC hlp_GetMainUserCredentials

	@loggedUserId, @rightToGoBack out, @isAdmin out, @IsTrafficManager out, @rightForMinus OUT

select top 1 @msgError = 
	case
		when mm.deadLine is not null and tw.dayActual <= mm.deadLine and @isAdmin = 0 and @IsTrafficManager = 0 then 'DeadLineViolationTransfer' 
		when tw.isDisabled = 1 then 'DisabledInsertRoller'
		when r.rolActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
		when (( tw.isFirstPositionOccupied = 1 And @newPosition = -20) 
				or (tw.isSecondPositionOccupied = 1	And @newPosition = -10)
				or (tw.isLastPositionOccupied = 1	And @newPosition = 10)) then 'FirstLastIssueErrorTransfer'
		when @rightForMinus = 0 AND i.isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + 1)) < 0) then 'WindowMaxCapacityOverflowTransfer'
		when @rightForMinus = 0 AND i.isConfirmed = 1 and tw.[timeInUseConfirmed] + r.duration > tw.duration then 'WindowOverflowTransfer'
		else null 
	end 
from 
	Issue i 
	inner join Roller r on i.rollerID = r.rollerID
	inner join [TariffWindow] tw on tw.windowId = @newWindowId
	inner join MassMedia mm on tw.massmediaID = mm.massmediaID
where i.issueID = @issueID 
order by 1 desc

if @msgError is not null 
begin 
	raiserror(@msgError,16,1)
	return
end 

-- Политическая агитация: запоминаем окно и тип ролика до переноса, чтобы после
-- переноса снять обвязку со старого окна и создать в новом (см. хвост процедуры)
Declare @agitOldWindowID int, @agitRolActionTypeID tinyint, @agitIsConfirmed bit
Select
	@agitOldWindowID = i.actualWindowID,
	@agitRolActionTypeID = r.rolActionTypeID,
	@agitIsConfirmed = i.isConfirmed
From Issue i Inner Join Roller r On r.rollerID = i.rollerID
Where i.issueID = @issueID

Declare
	@oldDate datetime

Select
	@oldDate = tw.windowDateActual
From
	Issue i 
	inner join TariffWindow tw on i.actualWindowID = tw.windowID
Where 
	i.issueId = @issueId

Update 
	TariffWindow
Set
	timeInUseConfirmed = 
		Case 
			When i.isConfirmed = 1 AND [maxCapacity] = 0 
				Then timeInUseConfirmed - r.duration
			Else timeInUseConfirmed
		End,
	timeInUseUnconfirmed = 
		Case 
			When i.isConfirmed = 0 AND [maxCapacity] = 0
				Then timeInUseUnconfirmed - r.duration
			Else timeInUseUnconfirmed
		End,
	capacityInUseConfirmed = 
		Case 
			When (i.isConfirmed = 1 AND [maxCapacity] > 0) 
				Then capacityInUseConfirmed - 1
			Else capacityInUseConfirmed
		End,
	capacityInUseUnconfirmed = 
		Case  
			When (i.isConfirmed = 0 AND [maxCapacity] > 0)
				Then capacityInUseUnconfirmed - 1
			Else capacityInUseUnconfirmed
		End,
	isFirstPositionOccupied = 
		Case 
			When i.isConfirmed = 1 And i.positionId = -20 Then 0
			Else isFirstPositionOccupied
		End,
	isSecondPositionOccupied = 
		Case 
			When i.isConfirmed = 1 And i.positionId = -10 Then 0
			Else isSecondPositionOccupied
		End,
	isLastPositionOccupied = 
		Case 
			When i.isConfirmed = 1 And i.positionId = 10 Then 0
			Else isLastPositionOccupied
		End,
	firstPositionsUnconfirmed = 
		Case  
			When i.isConfirmed = 0 And i.positionId = -20 Then firstPositionsUnconfirmed - 1
			Else firstPositionsUnconfirmed
		End,
	secondPositionsUnconfirmed = 
		Case 
			When i.isConfirmed = 0 And i.positionId = -10 Then secondPositionsUnconfirmed - 1
			Else secondPositionsUnconfirmed
		End,
	lastPositionsUnconfirmed = 
		Case	
			When i.isConfirmed = 0 And i.positionId = 10 Then lastPositionsUnconfirmed - 1
			Else	lastPositionsUnconfirmed
		End
From
	Issue i 
	Inner Join Roller r On r.rollerId = i.rollerId
Where
	TariffWindow.windowId = i.actualWindowId
	and i.issueID = @issueID

UPDATE Issue
SET	actualWindowID = @newWindowId,
	positionId = @newPosition
WHERE issueID = @issueID

Update 
	TariffWindow
Set
	timeInUseConfirmed = 
		Case 
			When i.isConfirmed = 1 AND [maxCapacity] = 0 
				Then timeInUseConfirmed + r.duration
			Else timeInUseConfirmed
		End,
	timeInUseUnconfirmed = 
		Case 
			When i.isConfirmed = 0 AND [maxCapacity] = 0 
				Then timeInUseUnconfirmed + r.duration
			Else timeInUseUnconfirmed
		End,
	capacityInUseConfirmed = 
		Case 
			When (i.isConfirmed = 1 AND [maxCapacity] > 0) 
				Then capacityInUseConfirmed + 1
			Else capacityInUseConfirmed
		End,
	capacityInUseUnconfirmed = 
		Case  
			When (i.isConfirmed = 0 AND [maxCapacity] > 0)
				Then capacityInUseUnconfirmed + 1
			Else capacityInUseUnconfirmed
		End,
	isFirstPositionOccupied = 
		Case 
			When i.isConfirmed = 1 And i.positionId = -20 Then 1
			Else isFirstPositionOccupied
		End,
	isSecondPositionOccupied = 
		Case 
			When i.isConfirmed = 1 And i.positionId = -10 Then 1
			Else isSecondPositionOccupied
		End,
	isLastPositionOccupied = 
		Case 
			When i.isConfirmed = 1 And i.positionId = 10 Then 1
			Else isLastPositionOccupied
		End,
	firstPositionsUnconfirmed = 
		Case  
			When i.isConfirmed = 0 And i.positionId = -20 Then firstPositionsUnconfirmed + 1
			Else firstPositionsUnconfirmed
		End,
	secondPositionsUnconfirmed = 
		Case 
			When i.isConfirmed = 0 And i.positionId = -10 Then secondPositionsUnconfirmed + 1
			Else secondPositionsUnconfirmed
		End,
	lastPositionsUnconfirmed = 
		Case	
			When i.isConfirmed = 0 And i.positionId = 10 Then lastPositionsUnconfirmed + 1
			Else	lastPositionsUnconfirmed
		End
From
	Issue i
	Inner Join Roller r On r.rollerId = i.rollerId
Where
	TariffWindow.windowId = i.actualWindowId
	and i.issueID = @issueID

-- Р-10 (10.10.2026): после переноса даты кампании и акции — по дням выхода, по тем же правилам, что
-- ActionRecalculate (фазы 1A, 1B и 2). Только даты: скидки, цена и ratio не трогаются (Р-3). Без этого
-- выпуск, перенесённый за первый или последний день кампании, выпадает из акта, бонусов и «По месяцам».
-- Модульные и пакетные кампании датируются днём модуля/пакета — перенос их не меняет.
declare @datesCampaignID int, @datesCampaignTypeID tinyint, @datesActionID int,
	@datesStart datetime, @datesFinish datetime, @programStart datetime, @programFinish datetime
select @datesCampaignID = c.campaignID, @datesCampaignTypeID = c.campaignTypeID, @datesActionID = c.actionID
from Issue i inner join Campaign c on c.campaignID = i.campaignID
where i.issueID = @issueID

if @datesCampaignTypeID = 1
	select @datesStart = min(tw.dayOriginal), @datesFinish = max(tw.dayOriginal)
	from Issue i inner join TariffWindow tw on tw.windowId = i.actualWindowID
	where i.campaignID = @datesCampaignID
else if @datesCampaignTypeID = 2
begin
	select @datesStart = min(twA.dayOriginal), @datesFinish = max(twA.dayOriginal)
	from Issue i
		inner join TariffWindow twA on twA.windowId = i.actualWindowID
		inner join TariffWindow tw on tw.windowId = i.originalWindowID
		cross apply (select top 1 1 as hasPricelist
			from (values (twA.tariffId), (tw.tariffId)) v (tariffId)
				inner join Tariff t on t.tariffID = v.tariffId
				inner join Pricelist pl on pl.pricelistID = t.pricelistID) p
	where i.campaignID = @datesCampaignID

	select @programStart = min(pi.issueDate), @programFinish = max(pi.issueDate)
	from ProgramIssue pi
		inner join SponsorTariff st on st.tariffID = pi.tariffID
		inner join SponsorProgramPricelist spl on spl.pricelistID = st.pricelistID
	where pi.campaignID = @datesCampaignID

	set @datesStart = case when @programStart is null then @datesStart when @datesStart is null then @programStart
		when @programStart < @datesStart then @programStart else @datesStart end
	set @datesFinish = case when @programFinish is null then @datesFinish when @datesFinish is null then @programFinish
		when @programFinish > @datesFinish then @programFinish else @datesFinish end
end

if @datesCampaignTypeID in (1, 2)
begin
	set @datesStart = dbo.ToShortDate(@datesStart)
	set @datesFinish = dbo.ToShortDate(@datesFinish)

	update Campaign
	set startDate = @datesStart, finishDate = @datesFinish
	where campaignID = @datesCampaignID
		and (startDate is null or finishDate is null or startDate <> @datesStart or finishDate <> @datesFinish)

	update a
	set startDate = x.startDate, finishDate = x.finishDate
	from [Action] a
		cross apply (select startDate = dbo.ToShortDate(min(c.startDate)), finishDate = dbo.ToShortDate(max(c.finishDate))
			from Campaign c where c.actionID = a.actionID) x
	where a.actionID = @datesActionID
		and (a.startDate is null or a.finishDate is null or a.startDate <> x.startDate or a.finishDate <> x.finishDate)
end

declare @actionID int
select  @actionID = c.actionID from Campaign c where c.campaignID = @campaignID

-- Insert record to transfer log journal
-- Write Log
INSERT INTO [TransferLog]([userID], [oldDate], [newDate], [actionID], [issueID])
Values(@loggedUserID, @oldDate, @newDate, @actionID, @issueID)

-- Политическая агитация: перенос подтверждённого ролика типа 6 переносит и обвязку -
-- снимаем её со старого окна (если там больше нет агитации) и создаём в новом
If @agitRolActionTypeID = 6 And @agitIsConfirmed = 1
Begin
	Exec AgitationFraming
		@actionName = 'CleanupWindow',
		@windowID = @agitOldWindowID,
		@loggedUserID = @loggedUserID

	Exec AgitationFraming
		@actionName = 'InsertForWindow',
		@windowID = @newWindowId,
		@loggedUserID = @loggedUserID
End
GO

-- Проверка: новые версии применены.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetIssuesPrice')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.SetIssueRatio')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetPriceByPeriod')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.rpt_GenericBill')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_Bonuses')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_GetPrice_proc')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_GetPriceByMonth_proc')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.Stat_VolumeOfRealizationSec')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.statFactorAnalysis')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.MoveIssues2NewCampaign')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionRecalculate')) LIKE N'%twA.windowId = i.actualWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionRecalculate')) LIKE N'%tw.windowId = i.actualWindowID -- день выхода%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignsForActJournalRetrieve')) LIKE N'%coalesce(mi.issueDate, tw.dayOriginal)%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.IssueTransfer')) LIKE N'%@datesCampaignTypeID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionRecalculate')) LIKE N'%(VALUES (1, twA.tariffId), (2, tw.tariffId))%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionActivate')) LIKE N'%(values (1, tw.tariffId), (2, twO.tariffId))%'
    PRINT N'ГОТОВО: деньги, даты и отчёты — по дню окна выхода (14 процедур).';
ELSE
    RAISERROR(N'33: новая версия применена не ко всем процедурам.', 16, 1);
GO

-- Снять режим «не выполнять», если его включила защита в начале (иначе окно SSMS остаётся в NOEXEC).
SET NOEXEC OFF;
GO
