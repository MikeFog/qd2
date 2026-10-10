-- broadcastStart, шаг 3 (docs/broadcast-start.md §7): тождественная правка слоя 1, 19 процедур.
-- «Начало эфирного дня» во всех прайс-листах 00:00 (шаг 1 на проде с 22.09.2026), поэтому сдвиги на него
-- ничего не меняют. Убрано:
--   * сдвиг DATEADD(mi, ±DATEPART(mi, broadcastStart), DATEADD(hh, ±DATEPART(hh, broadcastStart), x)) -> x;
--   * вычитание issueDate - broadcastStart -> issueDate;
--   * недостижимая вторая ветка дня недели «время < начала дня» (ProgramIssueIUD, stat_SponsorBusiness);
--   * fn_GetTimeString(broadcastStart, x) -> CONVERT(varchar(5), x, 108) (то же «ЧЧ:ММ»).
-- Сравнения границ (BETWEEN, <, >=) не переписаны. В stat_SponsorBusiness (режим «свободные и занятые»)
-- прайс-лист присоединён LEFT JOIN: строка без прайс-листа и раньше отсекалась сдвигом (NULL) — теперь явно.
-- Колонка broadcastStart, её выдача наружу и запись (Pricelists, ModulePriceLists, SponsorPricelistByDate,
-- PricelistIUD, sponsorPLIUD) и MediaPlanRetrieve_v2 не тронуты.
--
-- Проверка — контрольный замер ArtvisDB/Scripts/broadcast-start-snapshot.sql «до/после» на ArtvisDev.
-- Порядок: после шага 1 (broadcast-start-neutralize-deploy.sql) — на базе, где SponsorProgramPricelist ещё
-- 03:00 (Tumen), сначала шаг 1. Клиент не нужен. Идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 29_broadcast-start-layer1-collapse.sql

SET NOCOUNT ON;
GO
-- Шаг 1 обязателен: иначе спонсорские выпуски до 03:00 поменяют день. У спонсорских прайс-листов проверяется всё
-- значение (ProgramIssueIUD, ProgramIssuesDays, SponsorCampaignPrograms, stat_SponsorBusiness зависели и от даты),
-- у обычных — часы и минуты (в этих процедурах только DATEPART).
IF EXISTS (SELECT 1 FROM dbo.Pricelist WHERE DATEPART(hh, broadcastStart) * 60 + DATEPART(mi, broadcastStart) <> 0)
   OR EXISTS (SELECT 1 FROM dbo.SponsorProgramPricelist WHERE broadcastStart <> '19000101')
BEGIN
    RAISERROR(N'29: есть прайс-листы с началом эфирного дня не 00:00 — сначала шаг 1 (broadcast-start-neutralize-deploy.sql). Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [dbo].[TariffWindowIUD]
(
@windowId int = NULL,
@windowDateActual datetime = NULL,
@windowDateOriginal datetime = NULL,
@duration int = NULL,
@duration_total int = NULL,
@price decimal(18,2) = NULL,
@massmediaID INT = NULL,
@isDisabled bit = null,
@windowPrevId int = null,
@windowNextId int = null,
@actionName varchar(32)
)
as
SET NOCOUNT ON

if @actionName in ('UpdateItem', 'AddItem')
begin
	-- Продолжительность не может быть больше полной; нулевая полная продолжительность означает «не задана»
	if @duration_total > 0 and @duration > @duration_total
	begin
		raiserror('DurationExceedsTotal', 16,1)
		return
	end

	if (not exists (select * from Pricelist pl 
					where pl.massmediaID = @massmediaID 
						and @windowDateActual >= pl.startDate and @windowDateActual < finishDate + 1)
		or
		not exists (select * from Pricelist pl 
					where pl.massmediaID = @massmediaID 
						and @windowDateOriginal >= pl.startDate and @windowDateOriginal < pl.finishDate + 1))
	begin 
		raiserror('BadTariffWindowDay', 16,1)
		return 
	end
	
	if exists(select * 
		from DisabledWindow dw 
		where dw.massmediaID = @massmediaID and 
			((@windowDateActual between dw.startDate and dw.finishDate) or 
				(@windowDateOriginal between dw.startDate and dw.finishDate)))
	begin 
		raiserror('CannotAddWindow_Disabled', 16,1)
		return 
	end
end 

IF @actionName = 'DeleteItem'
begin 
	if exists(select * 
			from Issue 
			where actualWindowID = @windowId or originalWindowID = @windowId)
	begin 
		raiserror('FK_Issue_TariffWindow', 16,1)
		return 
	end 

	if exists(select * From [TariffWindow] WHERE windowId = @windowId And tariffId Is Not Null)
	begin 
		raiserror('TariffWindowDeleteAttempt', 16,1)
		return 
	end 
	
	DELETE FROM [TariffWindow] WHERE windowId = @windowId
end
ELSE IF @actionName = 'UpdateItem'
begin
	-- Объединённое окно (windowPrevId/windowNextId): перенос времени выхода не
	-- должен ломать порядок окон в эфире (окно-«хвост» не может выйти раньше
	-- окна-«головы»). См. docs/window-merging.md §3 (#1). Проверяем, только если
	-- реально меняется время выхода ИЛИ впервые ставится связь (объединение);
	-- правки isDisabled / price / продолжительности сюда не попадают.
	if (@windowPrevId is not null or @windowNextId is not null)
	begin
		declare @oldActual datetime, @oldPrevId int, @oldNextId int
		select @oldActual = windowDateActual, @oldPrevId = windowPrevId, @oldNextId = windowNextId
		from [TariffWindow] where windowId = @windowId

		if @windowDateActual <> @oldActual
			or isnull(@windowPrevId, 0) <> isnull(@oldPrevId, 0)
			or isnull(@windowNextId, 0) <> isnull(@oldNextId, 0)
		begin
			if @windowPrevId is not null
				and exists (select 1 from [TariffWindow]
					where windowId = @windowPrevId and windowDateActual >= @windowDateActual)
			begin
				raiserror('LinkedWindowsWrongOrder', 16, 1)
				return
			end

			if @windowNextId is not null
				and exists (select 1 from [TariffWindow]
					where windowId = @windowNextId and windowDateActual <= @windowDateActual)
			begin
				raiserror('LinkedWindowsWrongOrder', 16, 1)
				return
			end
		end
	end

	-- Окно объединённого тарифа (TariffUnion): окно тарифа, с которым объединён этот,
	-- в тот же день должно выйти раньше, окно тарифа-продолжения — позже.
	declare @tariffId int, @dayOriginal datetime, @actualBefore datetime
	select @tariffId = tariffId, @dayOriginal = dayOriginal, @actualBefore = windowDateActual
	from [TariffWindow] where windowId = @windowId

	if @tariffId is not null and @windowDateActual <> @actualBefore
		and (exists (select 1
				from TariffUnion tu
					inner join [TariffWindow] p on p.tariffId = tu.tariffID and p.dayOriginal = @dayOriginal
				where tu.tariffUnionID = @tariffId and p.windowDateActual >= @windowDateActual)
			or exists (select 1
				from TariffUnion tu
					inner join [TariffWindow] n on n.tariffId = tu.tariffUnionID and n.dayOriginal = @dayOriginal
				where tu.tariffID = @tariffId and n.windowDateActual <= @windowDateActual))
	begin
		raiserror('UnitedTariffWindowsWrongOrder', 16, 1)
		return
	end

	UPDATE	
		tw
	SET			
		tw.windowDateActual = @windowDateActual, 
		tw.duration = @duration, 
		tw.duration_total = @duration_total,
		tw.price = @price,
		tw.windowPrevId = @windowPrevId,
		tw.windowNextId = @windowNextId,
		tw.isDisabled = coalesce(@isDisabled, 0),
		tw.dayActual = Convert(datetime, Convert(varchar(8), @windowDateActual, 112), 112)
	from [TariffWindow] tw
		inner join Pricelist pl on tw.massmediaID = pl.massmediaID and @windowDateActual >= pl.startDate and @windowDateActual < pl.finishDate + 1
	WHERE		
		tw.windowId = @windowId
		
	SELECT * FROM [TariffWindow] WHERE [windowId] = @windowId
END
ELSE IF @actionName = 'AddItem'
BEGIN
	declare @isInsideChain bit
	set @isInsideChain = dbo.f_CheckLinkedTariffWindows(@windowDateOriginal, @massmediaID)
	
	IF @isInsideChain = 1
	begin
		raiserror('InsideLinkedWindowError', 16, 1)
		return 
	end 
	
	INSERT 
		INTO [TariffWindow] ([windowDateOriginal], [windowDateActual], [duration], [price], [massmediaID], 
		isDisabled, dayActual, dayOriginal, duration_total) 
	select @windowDateOriginal, @windowDateActual, @duration, @price, @massmediaID, coalesce(@isDisabled, 0)
		,Convert(datetime, Convert(varchar(8), @windowDateActual, 112), 112)
		,Convert(datetime, Convert(varchar(8), @windowDateOriginal, 112), 112)
		, @duration_total
	from 
		Pricelist pl 
	where 
		@massmediaID = pl.massmediaID 
		and @windowDateActual between pl.startDate and pl.finishDate 
	
	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @windowId = SCOPE_IDENTITY()

	SELECT * FROM [TariffWindow] WHERE [windowId] = @windowId
END
GO

CREATE OR ALTER PROCEDURE [dbo].[ProgramIssueIUD]
(
@issueID int = NULL OUT,
@campaignID int = NULL,
@programID smallint = NULL,
@tariffID int = NULL,
@issueDate datetime = NULL,
@tariffPrice decimal(18,2) = NULL,
@bonus smallint = NULL,
@loggedUserID smallint,
@isConfirmed bit = null,
@advertTypeID smallint = null,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
set datefirst 1
DECLARE	
	@massmediaID smallint,
	@RightToGoBack bit,
	@IsAdmin bit,
	@IsTrafficManager bit,
	@RightForMinus bit, 
	@finishDate datetime,
	@timeBonus int,
	@issuesDuration int

SELECT 
	@massmediaID = massmediaID,
	@finishDate = finishDate,
	@timeBonus = timeBonus,
	@issuesDuration = issuesDuration	
FROM 
	Campaign 
WHERE 
	campaignID = @campaignID

EXEC hlp_GetMainUserCredentials
	@loggedUserId, @rightToGoBack out, @isAdmin out, @IsTrafficManager out, @rightForMinus out

IF @actionName = 'AddItem' 
	BEGIN
	-- Verify data against few rules
	-- 1. Disabled window
	IF dbo.fn_IsDisabledWindow(@massmediaID, @issueDate) = 1 BEGIN
		RAISERROR('DisabledWindowInsertProgram', 16, 1)
		RETURN
		END

	-- 2. impossible to add issues with date less than today 
	If	dbo.ToShortDate(@issueDate) <= dbo.ToShortDate(getdate()) And @IsAdmin <> 1 And @IsTrafficManager <> 1  BEGIN
		RAISERROR('IncorrectProgramIssueDate', 16, 1)
		RETURN
		END

	-- 3. if company has already finished, refuse changes ------
	if	@finishDate < dbo.ToShortDate(getdate()) And @IsAdmin <> 1 And @IsTrafficManager <> 1  BEGIN
		RAISERROR('CampaignAlreadyFinished', 16, 1)
		RETURN
		END

	declare @datepart tinyint 
	set @datepart = datepart(dw, @issueDate)

	if not exists(select * 
		from SponsorTariff t 
			inner join SponsorProgramPricelist sppl on t.pricelistID = sppl.pricelistID
		where t.tariffID = @tariffID and sppl.sponsorProgramID = @programID
			and ((t.monday = 1 and @datepart = 1)
				or (t.tuesday = 1 and @datepart = 2)
				or (t.wednesday = 1 and @datepart = 3)
				or (t.thursday = 1 and @datepart = 4)
				or (t.friday = 1 and @datepart = 5)
				or (t.saturday = 1 and @datepart = 6)
				or (t.sunday = 1 and @datepart = 7)))
	begin 
		raiserror('ProgramNotExists',16,1)
		return
	end 

	if exists(select * from ProgramIssue where programID = @programID and tariffID = @tariffID and @issueDate = issueDate and isConfirmed = 1)
	begin 
		raiserror('UIX_ProgramIssue_program_issueDate',16,1)
		return
	end 

	INSERT INTO [ProgramIssue](campaignID, programID, tariffID, issueDate, [tariffPrice], isConfirmed, advertTypeID)
	select @campaignID, @programID, @tariffID, @issueDate, @tariffPrice, @isConfirmed, @advertTypeID
	
	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @issueID = SCOPE_IDENTITY()
		
	EXEC dbo.[ProgramIssues] @issueID= @issueID
END
ELSE IF @actionName = 'DeleteItem' BEGIN
	-- Only admin is allowed to delete issue which is in the past already
	If	@issueDate < GetDate() And @IsAdmin <> 1 BEGIN
		RAISERROR('PastIssue', 16, 1)
		RETURN
	END	
	
	if @bonus is null 
		select @bonus = pl.bonus
			FROM ProgramIssue i 
				inner join SponsorTariff st on i.tariffID = st.tariffID
				INNER JOIN [SponsorProgramPricelist] pl ON st.[pricelistID] = pl.[pricelistID]
			where i.issueID = @issueID

	-- Should be balance between time bonus and roller issues duration
	If	@issuesDuration > @timeBonus - @bonus BEGIN
		RAISERROR('ProgramIssueDeleteBonusError', 16, 1)
		RETURN
	END		
	
	DELETE FROM [ProgramIssue] WHERE issueID = @issueID
	END
ELSE IF @actionName = 'UpdateItem'
	Begin
	UPDATE	
		[ProgramIssue]
	SET			 
		issueDate = @issueDate,
		advertTypeID = @advertTypeID
	WHERE		
		issueID = @issueID

	EXEC dbo.[ProgramIssues] @issueID= @issueID
	End
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
			Where tw.windowId = i.originalWindowID and
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
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
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
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
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
            WHERE tw.windowId = i.originalWindowID
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
        JOIN TariffWindow tw ON tw.windowId = i.originalWindowID
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
        WHERE tw.windowId = i.originalWindowID
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
				inner join TariffWindow tw on i.originalWindowID = tw.windowID
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

/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE OR ALTER PROC [dbo].[SponsorCampaignPrograms]
(
@campaignID int,
@issueDate datetime = NULL
)
as
SET NOCOUNT ON
SELECT DISTINCT
	pi.[campaignID], 
	pi.[programID],
	@issueDate as issueDate,
	sp.NAME,
	spp.[bonus],
	a.deleteDate
FROM 
	[ProgramIssue] pi
	INNER JOIN SponsorProgram sp ON sp.sponsorProgramID = pi.programID
	INNER JOIN [SponsorProgramPricelist] spp ON sp.[sponsorProgramID] = spp.[sponsorProgramID]
		and dbo.ToShortDate(pi.issueDate) between spp.startDate and spp.finishDate
	INNER JOIN Campaign c On pi.campaignID = c.campaignID
	INNER JOIN Action a On a.actionID = c.actionID
WHERE
	pi.campaignID = @campaignID AND
	(@issueDate is null or pi.issueDate between @issueDate AND dateadd(ss, -1, dateadd(day, 1, @issueDate)))
--group by pi.[campaignID], pi.[programID],issueDate, sp.NAME,spp.[bonus]
GO

/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE OR ALTER PROCEDURE [dbo].[SponsorCampaignProgramDelete]
(
	@campaignID AS INT,
	@programID AS SMALLINT = NULL,
	@loggedUserID AS SMALLINT,
	@actionName AS VARCHAR(32),
	@issueDate AS DATETIME = NULL
)
AS
begin
	SET NOCOUNT ON
	if @actionName <> 'DeleteItem' 
		return 
		
	if @issueDate is not null 
		set @issueDate = dbo.ToShortDate(@issueDate)		
		
	declare @issues table (issueID int primary key)
	insert into @issues 
	select i.issueID
		from ProgramIssue i
			inner join SponsorTariff st on i.tariffID = st.tariffID
			inner join SponsorProgramPricelist pl on st.pricelistID = pl.pricelistID
		where i.campaignID = @campaignID 
			and i.programID = coalesce(@programID, i.programID)
			and (@issueDate is null or dbo.ToShortDate(i.issueDate) = @issueDate)
	
	if dbo.f_IsAdmin(@loggedUserID) <> 1 
		and exists(select * from @issues it 
					inner join ProgramIssue i on it.issueID = i.issueID 
					inner join SponsorTariff st on i.tariffID = st.tariffID
					inner join SponsorProgramPricelist pl on st.pricelistID = pl.pricelistID
					where dbo.ToShortDate(i.issueDate) < Convert(datetime, Convert(varchar(8), dateadd(day, 1, getdate()), 112), 112))
	begin 
		raiserror('PastIssue', 16, 1)
		return
	end
	
	if exists(select c.campaignID, sum(pl.bonus) from @issues it
				inner join ProgramIssue i on it.issueID = i.issueID 
				inner join Campaign c on i.campaignID = c.campaignID
				inner join SponsorTariff st on i.tariffID = st.tariffID
				inner join [SponsorProgramPricelist] pl ON st.[pricelistID] = pl.[pricelistID]
			  group by c.campaignID, c.issuesDuration, c.timeBonus
			  having c.issuesDuration > c.timeBonus - sum(pl.bonus))
	begin 
		raiserror('ProgramIssueDeleteBonusError', 16, 1)
		return 
	end 
	
	delete from i from ProgramIssue i inner join @issues it on i.issueID = it.issueID
		
end 
GO

/*
Modified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008
*/
CREATE OR ALTER PROC [dbo].[ProgramIssues]
(
@issueID INT = NULL,
@campaignID int = null,
@issueDate datetime = null,
@windowDate datetime = null,
@programID smallint = null,
@massmediaID smallint = null,
@firmID smallint = null,
@userID smallint = null,
@startDate datetime = null,
@finishDate datetime = null,
@showUncorfirmed bit = true,
@showDeleted bit = 0
)
AS

SET NOCOUNT ON

set @issueDate = dbo.ToShortDate(@issueDate) -- to work object refresh

SELECT 
	pi.*,
	sp.name,
	f.name as firmName,
	u.userName,
	a.deleteDate,
	adv.name as advertTypeName
FROM 
	[ProgramIssue] pi
	INNER JOIN Campaign c ON c.campaignID = pi.campaignID
	INNER JOIN [Action] a ON a.actionID = c.actionID
	INNER JOIN Firm f ON f.firmID = a.firmID
	INNER JOIN [User] u ON a.userID = u.userID
	INNER JOIN SponsorProgram sp ON sp.sponsorProgramID = pi.programID
	inner join SponsorTariff st on pi.tariffID = st.tariffID
	inner join SponsorProgramPricelist pl on st.pricelistID = pl.priceListID
	left join AdvertType adv On adv.advertTypeID = pi.advertTypeID
WHERE
	pi.campaignID = COALESCE(@campaignID, pi.campaignID) and
	pi.issueDate = coalesce(@windowDate, pi.issueDate) and
	(@issueDate is null or pi.issueDate >= @issueDate) And
	(@issueDate is null or pi.issueDate < dateadd(day, 1, @issueDate)) And
	pi.programID = COALESCE(@programID, pi.programID) AND
	sp.massmediaID = COALESCE(@massmediaID, sp.massmediaID) And
	f.firmID = Coalesce(@firmID, f.firmID) And
	u.userID = Coalesce(@userID, u.userID) AND
	pi.[issueID] = COALESCE(@issueID, pi.[issueID]) and 
	(@startDate is null or @finishDate is null or (  pi.issueDate between  @startDate and dateadd(ss, -1, dateadd(day, 1, @finishDate))     ))
	and (@showUncorfirmed = 1 or pi.isConfirmed = 1)
	and (a.deleteDate Is Null or @showDeleted = 1)
ORDER BY
	pi.issueDate DESC
GO

/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE OR ALTER PROC [dbo].[ProgramIssuesDays]
(
@campaignID INT,
@programID AS SMALLINT = NULL
)
as
SET NOCOUNT ON
SELECT DISTINCT	
	pi.[campaignID], 
	CONVERT(varchar(10), pi.[issueDate], 104) as [name],
	CONVERT(datetime, CONVERT(varchar(10), pi.[issueDate], 104), 104) as issueDate,
	@programID AS programID,
	spp.[bonus],
	a.deleteDate
FROM 
	[ProgramIssue] pi
	INNER JOIN SponsorProgram sp ON sp.sponsorProgramID = pi.programID
	INNER JOIN [SponsorProgramPricelist] spp ON sp.[sponsorProgramID] = spp.[sponsorProgramID]
		and dbo.ToShortDate(pi.issueDate) between spp.startDate and spp.finishDate
	INNER JOIN Campaign c On pi.campaignID = c.campaignID
	INNER JOIN Action a On a.actionID = c.actionID
WHERE
	pi.[campaignID] = @campaignID 
	AND (@programID IS NULL OR @programID = [programID])
ORDER BY
	CONVERT(datetime, CONVERT(varchar(10), pi.[issueDate], 104), 104)
GO

/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE OR ALTER Procedure [dbo].[stat_SponsorBusiness] (
@StartDay datetime = null, 
@FinishDay datetime = null,
@ProgramID int = null,
@FirmID int = null,
@UserID int = null,
@ShowBusyOnly bit = 1,
@actionID int = null,
@massmediaID int = null ,
@loggedUserID smallint,
@headCompanyID smallint = NULL
)
As
set nocount on

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

SET DATEFIRST 1

if @StartDay is not null
	set @StartDay = dbo.ToShortDate(@StartDay)
	
if @FinishDay is not null
	set @FinishDay = dateadd(ss, -1, dateadd(day, 1, dbo.ToShortDate(@FinishDay)))
	
if @UserID is not null 
	set	@ShowBusyOnly = 1

if @ShowBusyOnly = 0
begin
	if @StartDay is null or @FinishDay is null 
		select @StartDay = case when @StartDay is null then dbo.ToShortDate(min(pl.startDate)) else @StartDay end,
			@finishDay = case when @finishDay is null then dbo.ToShortDate(max(pl.finishDate)) else @finishDay end
		from SponsorProgramPricelist pl 

	declare @date datetime, @dateweek tinyint
	set @date = @StartDay
	
	declare @res table(issueDate datetime, price decimal(18,2), programID int, tariffID int)
	
	while (@date <= @FinishDay)
	begin 
		set @dateweek = datepart(dw, @date)
	
		insert into @res (issueDate,price,programID,tariffID) 
		select @date + st.[time], st.price, pl.sponsorProgramID, st.tariffID
		from 
			SponsorTariff st
			inner join SponsorProgramPricelist pl on st.pricelistID = pl.pricelistID
			inner join SponsorProgram sp on sp.sponsorProgramID = pl.sponsorProgramID
			inner join @massmedias umm on sp.massmediaID = umm.massmediaID
		where sp.massmediaID = coalesce(@massmediaID, sp.massmediaID) 
		 and pl.startDate <= @FinishDay and pl.finishDate >= @StartDay
		 and ((st.monday = 1 and @dateweek = 1)
					or (st.tuesday = 1 and @dateweek = 2)
					or (st.wednesday = 1 and @dateweek = 3)
					or (st.thursday = 1 and @dateweek = 4)
					or (st.friday = 1 and @dateweek = 5)
					or (st.saturday = 1 and @dateweek = 6)
					or (st.sunday = 1 and @dateweek = 7))
		
		set @date = dateadd(day, 1, @date)
	end 
	
	select 
		row_number() over(order by i.issueDate) as RowNum,
		mm.name as mmname,
		mm.groupName,
		sp.[name] as programName, 
		r.issueDate as issueDate, 
		r.price as price,
		f.name as firmName,
		hc.name as headCompanyName,
		case when u.userID is null then '' else isnull(u.lastname, '') + space(1) + isnull(left(u.firstname, 1), '') + '.' + isnull(left(u.secondname, 1), '') end as manager,
		a.actionID 
	from @res r
		inner join SponsorProgram sp on r.programID = sp.sponsorProgramID
		left join ProgramIssue i on r.tariffID = i.tariffID 
			and i.programID = r.programID
			and i.issueDate = r.issueDate
		left join SponsorTariff st on i.tariffID = st.tariffID
		left join SponsorProgramPricelist pl on pl.sponsorProgramID = sp.sponsorProgramID and r.issueDate between pl.startDate and pl.finishDate
		left join Campaign c on i.campaignID = c.campaignID
		left join [Action] a on c.actionID = a.actionID
		left join [User] u on a.userID = u.userID
		left join Firm f on a.firmID = f.firmID
		LEFT JOIN HeadCompany hc on hc.headCompanyID = f.headCompanyID
		inner join vMassmedia mm on sp.massmediaID = mm.massmediaID
		inner join @massmedias umm on mm.massmediaID = umm.massmediaID
		inner join 
				(
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join @ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as x on a.userID = x.userID
	where sp.massmediaID = coalesce(@massmediaID, sp.massmediaID) 
		and (c.actionID is null or c.actionID = coalesce(@actionID, c.actionID))
		and (a.firmID is null or a.firmID = coalesce(@firmID, a.firmID))
		and (f.headCompanyID is null or f.headCompanyID = coalesce(@headCompanyID, f.headCompanyID))
		and (a.userID is null or a.userID = coalesce(@userID, a.userID))
		and r.programID = coalesce(@programID, r.programID)
		and (@StartDay is null or (pl.pricelistID is not null and @StartDay <= r.issueDate))
		and (@finishDay is null or (pl.pricelistID is not null and @FinishDay >= r.issueDate))
		and (i.isConfirmed is null or i.isConfirmed = 1)
		and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
	order by r.issueDate
end 
else 
begin
	select 
		row_number() over(order by i.issueDate) as RowNum,
		mm.name as mmname,
		mm.groupName,
		sp.[name] as programName, 
		i.issueDate as issueDate, 
		st.price as price,
		f.name as firmName,
		hc.name as headCompanyName,
		isnull(u.lastname, '') + space(1) + isnull(left(u.firstname, 1), '') + '.' + isnull(left(u.secondname, 1), '') as manager,
		a.actionID
	from 
		ProgramIssue i 
		inner join SponsorTariff st on i.tariffID = st.tariffID
		inner join SponsorProgramPricelist pl on st.pricelistID = pl.pricelistID
		inner join Campaign c on i.campaignID = c.campaignID
		inner join [Action] a on c.actionID = a.actionID
		inner join SponsorProgram sp on i.programID = sp.sponsorProgramID
		inner join [User] u on a.userID = u.userID
		inner join Firm f on a.firmID = f.firmID
		inner join HeadCompany hc on hc.headCompanyID = f.headCompanyID
		inner join vMassmedia mm on sp.massmediaID = mm.massmediaID
		inner join @massmedias umm on mm.massmediaID = umm.massmediaID
		inner join 
				(
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join @ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as x on a.userID = x.userID
	where sp.massmediaID = coalesce(@massmediaID, sp.massmediaID) 
		and c.actionID = coalesce(@actionID, c.actionID)
		and a.firmID = coalesce(@firmID, a.firmID)
		and f.headCompanyID = coalesce(@headCompanyID, f.headCompanyID)
		and a.userID = coalesce(@userID, a.userID)
		and i.programID = coalesce(@programID, i.programID)
		and (@StartDay is null or @StartDay <= i.issueDate)
		and (@finishDay is null or @FinishDay > i.issueDate)
		and i.isConfirmed = 1
		and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
	order by i.issueDate
END
GO

CREATE OR ALTER PROCEDURE [dbo].[TariffPassport]
(
	@pricelistID smallint = null,
	@tariffId int = null 
)
AS
BEGIN
	SET NOCOUNT ON;



Declare @nextTariffId int
Set @nextTariffId = dbo.fn_FindTariffIDForChain(@tariffId, @pricelistID)

select 
	(CONVERT(varchar(5), t.[time], 108) 
	+ case t.monday when 1 then ',пн' else '' end 
	+ case t.wednesday when 1 then ',вт' else '' end 
	+ case t.tuesday when 1 then ',ср' else '' end 
	+ case t.thursday when 1 then ',чт' else '' end 
	+ case t.friday when 1 then ',пт' else '' end 
	+ case t.saturday when 1 then ',сб' else '' end 
	+ case t.sunday when 1 then ',вс' else '' end 
	+ case when t.pricelistID = @pricelistID then '' else ' (' + mm.[name] + ')' end) as name, 
	t.tariffID as id 
from Tariff t	
	inner join Pricelist pl on t.pricelistID = pl.pricelistID
	inner join vMassmedia mm on pl.massmediaID = mm.massmediaID
where t.pricelistID = @pricelistID
	And t.tariffID = @nextTariffId 
		

Select blockTypeId as Id, [name] + ' (' + code + ')' as name, blockTypeId From BlockType
END
GO

/*
Получает данные для показа дней и выпусков в виде дерева
*/
CREATE OR ALTER PROC [dbo].[CampaignDaysTreePassport]
(
@campaignID int,
@campaignTypeID tinyint,
@objectID int = NULL, 
@positionID int = NULL
)
AS
SET NOCOUNT ON

-- It must be original day
declare @days table (id varchar(20), parentID varchar(20), issueDate datetime, [image] varchar(50), [name] nvarchar(128))

if (@campaignTypeID in (1,2))
	begin 
	insert into @days(id, issueDate,[image], [name])
	select distinct
		convert(varchar, tw.dayActual, 104),
		tw.dayActual,
		'Day.png',
		convert(varchar, tw.dayActual, 104)
	from
		Issue i
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
	where
		i.[campaignID] = @campaignID
		And i.rollerID = IsNull(@objectID, i.rollerID)
		And i.positionId = IsNull(@positionID, i.positionId)
	order by
		tw.dayActual

	insert into @days(id, parentID, [image], [name], issueDate)
	select
		i.issueID,
		convert(varchar, tw.dayActual, 104),
		'Issue.png',
		CONVERT(varchar(5), tw.windowDateActual, 108) + ' ' + r.name +
		Case i.positionId
			When  -20 Then ' (F)'
			When  -15 Then ' (F)'
			When  -10 Then ' (S)'
			When  -5 Then ' (S)'
			When  10 Then ' (L)'
			When  5 Then ' (L)'
			Else ''
		End,
		tw.dayActual
	from
		Issue i
		Inner Join Roller r On i.rollerID = r.rollerID
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
		inner join Tariff t on tw.tariffId = t.tariffID
		inner join Pricelist pl on t.pricelistID = pl.pricelistID
	where
		i.[campaignID] = @campaignID
		And i.rollerID = IsNull(@objectID, i.rollerID)
		And i.positionId = IsNull(@positionID, i.positionId)
	order by
		tw.windowDateActual
end 
else if @campaignTypeID = 3
	Begin
	insert into @days(id, issueDate,[image], [name])
	select distinct
		convert(varchar, mi.issueDate, 104),
		mi.issueDate,
		'Day.png',
		convert(varchar, mi.issueDate, 104)
	from
		[ModuleIssue] mi
	where
		mi.[campaignID] = @campaignID
		And mi.moduleID = IsNull(@objectID, mi.moduleID)
		And mi.positionId = IsNull(@positionID, mi.positionId)
	order by
		mi.issueDate

	insert into @days(id, parentID, [image], [name], issueDate)
	select
		mi.moduleIssueID,
		convert(varchar, mi.issueDate, 104),
		'Module.png',
		m.name + ' - ' + r.name +
		Case mi.positionId
			When  -20 Then ' (F)'
			When  -15 Then ' (F)'
			When  -10 Then ' (S)'
			When  -5 Then ' (S)'
			When  10 Then ' (L)'
			When  5 Then ' (L)'
			Else ''
		End,
		mi.issueDate
	FROM 
		[ModuleIssue] mi
		INNER JOIN Module m ON m.moduleID = mi.moduleID
		Inner Join Roller r on r.rollerId = mi.rollerId
	where 
		mi.[campaignID] = @campaignID
		And mi.moduleID = IsNull(@objectID, mi.moduleID)
		And mi.positionId = IsNull(@positionID, mi.positionId)
	End
else if @campaignTypeID = 4
	Begin
	insert into @days(id, issueDate,[image], [name])
	select distinct
		convert(varchar, pmi.issueDate, 104),
		pmi.issueDate,
		'Day.png',
		convert(varchar, pmi.issueDate, 104)
	from
		[PackModuleIssue] pmi
		INNER JOIN [PackModulePriceList] pl ON pmi.[pricelistID] = pl.[priceListID]
	where
		pmi.[campaignID] = @campaignID
		And pl.packModuleID = IsNull(@objectID, pl.packModuleID)
		And pmi.positionId = IsNull(@positionID, pmi.positionId)
	order by
		pmi.issueDate

	insert into @days(id, parentID, [image], [name], issueDate)
	select
		pmi.[packModuleIssueID],
		convert(varchar, pmi.issueDate, 104),
		'PackModule.png',
		pm.name + ' - ' + r.name+
		Case pmi.positionId
			When  -20 Then ' (F)'
			When  -15 Then ' (F)'
			When  -10 Then ' (S)'
			When  -5 Then ' (S)'
			When  10 Then ' (L)'
			When  5 Then ' (L)'
			Else ''
		End,
		pmi.issueDate
	FROM 
		[PackModuleIssue] pmi
		INNER JOIN [PackModulePriceList] pl ON pmi.[pricelistID] = pl.[priceListID]
		INNER JOIN [PackModule] pm ON pl.[packModuleID] = pm.[packModuleID]
		INNER JOIN [Roller] r ON pmi.[rollerID] = r.[rollerID]
	where 
		pmi.[campaignID] = @campaignID
		And pl.packModuleID = IsNull(@objectID, pl.packModuleID)
		And pmi.positionId = IsNull(@positionID, pmi.positionId)
	End
else if @campaignTypeID = 100 -- такого типа нет, это для выпусков программ спонсорской кампании
	Begin
	insert into @days(id, issueDate,[image], [name])
	select distinct
		convert(varchar, mi.issueDate, 104),
		mi.issueDate,
		'Day.png',
		convert(varchar, mi.issueDate, 104)
	from
		[ProgramIssue] mi
	where
		mi.[campaignID] = @campaignID
		And mi.programID = IsNull(@objectID, mi.programID)
	order by
		2

	insert into @days(id, parentID, [image], [name], issueDate)
	SELECT
		pi.[issueID],
		Convert(varchar, pi.issueDate, 104),
		'SponsorProgram.png',
		sp.name + COALESCE(' - ' + adv.name, ''),
		pi.[issueDate]
	FROM 
		[ProgramIssue] pi
		INNER JOIN SponsorProgram sp ON sp.sponsorProgramID = pi.programID
		LEFT JOIN AdvertType adv On adv.advertTypeID = pi.advertTypeID
	where 
		pi.[campaignID] = @campaignID
		And pi.programID = IsNull(@objectID, pi.programID)
	End

select * from @days order by issueDate

select positionId as Id, description as name from iIssuePosition Where positionId In(-20, -10, 0, 10)
GO

/*
Modified: Denis Gladkikh (dgladkikh@fogsoft.ru) 18.09.2008 - replace @moduleIssueID and @packModuleIssueID on @moduleID and @packModuleID
*/
CREATE OR ALTER PROC [dbo].[RollerSubstitutionPassport]
(
@campaignID int,
@campaignTypeID int,
@rollerID int,
@moduleID int = null,
@packModuleID int = null 
)
AS
SET NOCOUNT ON

SELECT COUNT(*) as issues FROM Issue i
	left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
	left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
	left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
WHERE i.campaignID = @campaignID AND i.rollerID = @rollerID 
	and (@moduleID is null or mi.moduleID = @moduleID) 
	and (@packModuleID is null or pmpl.packModuleID = @packModuleID)

-- Rollers
DECLARE @massmediaID smallint

SELECT DISTINCT 
	r.rollerID as [id],
	r.name
FROM 
	Roller r 
	inner join Roller ro on ro.rollerID = @rollerID and r.rollerID <> @rollerID
	INNER JOIN [Action] a ON r.firmID = a.firmID
	INNER JOIN Campaign c ON c.actionID = a.actionID and c.campaignID = @campaignID
where
	r.isEnabled = 1	AND 
	r.isMute = 0 And
	r.parentID Is Null
ORDER BY 
	r.[name]
	
-- It must be original day
declare @days table (id varchar(20), parentID varchar(20), windowID int, timeString char(5), issueDate datetime, [image] varchar(50), [name] varchar(20))

insert into @days(id, issueDate,[image], [name])
select distinct	
	convert(varchar, tw.dayOriginal, 104),
	tw.dayOriginal,
	'Day.png',
	convert(varchar, tw.dayOriginal, 104)
from 
	Issue i 
	inner join TariffWindow tw on i.originalWindowID = tw.windowId
	left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
	left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
	left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
where 
	i.[campaignID] = @campaignID AND
	i.rollerID = @rollerID  
	and (@moduleID is null or mi.moduleID = @moduleID) 
	and (@packModuleID is null or pmpl.packModuleID = @packModuleID)
order by
	tw.dayOriginal

if (@campaignTypeID in (1,2))
begin 
	insert into @days(id, parentID, windowID,[image], [name],issueDate)
	select distinct
		convert(varchar, tw.dayOriginal, 104) + CONVERT(varchar(5), tw.windowDateOriginal, 108),
		convert(varchar, tw.dayOriginal, 104),
		tw.windowId,
		'Issue.png',
		CONVERT(varchar(5), tw.windowDateOriginal, 108),
		tw.dayOriginal
	from 
		Issue i 
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
		inner join Tariff t on tw.tariffId = t.tariffID
		inner join Pricelist pl on t.pricelistID = pl.pricelistID
		left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
		left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
		left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
	where 
		i.[campaignID] = @campaignID AND
		i.rollerID = @rollerID  
		and (@moduleID is null or mi.moduleID = @moduleID) 
		and (@packModuleID is null or pmpl.packModuleID = @packModuleID)
	order by CONVERT(varchar(5), tw.windowDateOriginal, 108)
end 

select * from @days order by issueDate
GO

-- Проверка: новые версии применены (в коде процедур нет сдвигов на broadcastStart и fn_GetTimeString).
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowIUD')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowIUD')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ProgramIssueIUD')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ProgramIssueIUD')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionRecalculate')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionRecalculate')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetIssuesPrice')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetIssuesPrice')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetPriceByPeriod')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetPriceByPeriod')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.SetIssueRatio')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.SetIssueRatio')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_GetPrice_proc')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_GetPrice_proc')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_GetPriceByMonth_proc')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_GetPriceByMonth_proc')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_Bonuses')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_Bonuses')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.rpt_GenericBill')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.rpt_GenericBill')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignsForActJournalRetrieve')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignsForActJournalRetrieve')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.SponsorCampaignPrograms')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.SponsorCampaignPrograms')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.SponsorCampaignProgramDelete')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.SponsorCampaignProgramDelete')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ProgramIssues')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ProgramIssues')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ProgramIssuesDays')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ProgramIssuesDays')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_SponsorBusiness')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_SponsorBusiness')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffPassport')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffPassport')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignDaysTreePassport')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignDaysTreePassport')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.RollerSubstitutionPassport')) NOT LIKE N'%DATEPART(%broadcastStart%' AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.RollerSubstitutionPassport')) NOT LIKE N'%fn_GetTimeString%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ProgramIssueIUD')) NOT LIKE N'%sppl.broadcastStart%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_SponsorBusiness')) NOT LIKE N'%pl.broadcastStart%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.SponsorCampaignPrograms')) NOT LIKE N'%spp.broadcastStart%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ProgramIssuesDays')) NOT LIKE N'%spp.broadcastStart%'
    PRINT N'ГОТОВО: broadcastStart, шаг 3 — сдвиги слоя 1 сняты (19 процедур).';
ELSE
    RAISERROR(N'29: новая версия применена не ко всем процедурам.', 16, 1);
GO
