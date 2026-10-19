-- Дни недели в двух процедурах (docs/tasks/side-defects.md №4). Обе общие для десктопа и веба; параметры и выдача
-- прежние.
--
-- 1. stat_FillPercentage — отчёт «% заполнения, объем рекламы» (сущность 163, «Статистика», десктоп и веб): галочки
--    «Понедельник»…«Воскресенье» сравнивались с DATEPART(WEEKDAY) так, будто 1 = воскресенье (DATEFIRST 7), а SET
--    DATEFIRST в процедуре нет. У логина AdvertAgUser с русским языком DATEFIRST = 1, и каждая галочка управляла
--    следующим днём: «Понедельник» — вторником, …, «Воскресенье» — понедельником. Пока отмечены все дни, отчёт верный;
--    со снятыми днями — нет: по прод-логу так считали «будни» (Artvis, 29.09.2026, 12 станций группы 5; у станции 209
--    ошибка заполняемости окна до ~16 п. п.) и «без вторника» (Tumen, 07.10.2026). Теперь отмеченные дни — битовая маска, день окна —
--    DATEDIFF от 01.01.1900 (понедельник) % 7, не зависит от DATEFIRST и языка сессии. NULL в галочке — день не отмечен,
--    как раньше.
-- 2. TariffPassport — название тарифа в списке «Блок:» карточки тарифа: подписи «вт» и «ср» стояли под колонками друг
--    друга (так с первой версии). Пока незаметно: тарифов, где вторник отмечен не так, как среда, в базах нет.
--
-- Проверено 19.10.2026 на ArtvisDev: эталон — старая stat_FillPercentage при SET DATEFIRST 7; новая (в транзакции с
-- откатом) при DATEFIRST 1 и 7 побайтно совпала с эталоном в 26 случаях — наборы дней (только пн, пн–пт, без вт,
-- только вс, все, вт = NULL) × разбивка по дням и окнам (станция 236, 05–11.10.2026), станция 209 за 01.01.2025–
-- 18.10.2026 по окнам 06:00–23:00 с пн–пт, группа 5 за сентябрь 2026 по дням без вт. Старая при DATEFIRST 1 (как у
-- логина) расходится с эталоном везде, где снят хотя бы один день; со всеми днями — совпадает.
--
-- Порядок: после Deploy/29 (несёт TariffPassport) и ArtvisDB/Scripts/local-promo-roller-types-deploy.sql (несёт
-- stat_FillPercentage) — без них скрипт отказывается: «Ничего не изменено». Клиент qd2 и перезапуск веба не нужны.
-- Не перезапускать после 59: 29 и local-promo-roller-types-deploy.sql (несут старые тексты); если перезапустили —
-- повторить 59. Идемпотентен (CREATE OR ALTER). Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 59_weekdays-fill-percentage-tariff-name.sql

SET NOCOUNT ON;
GO
IF CHARINDEX(N'fn_GetTimeString', OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffPassport'))) > 0
   OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffPassport')) IS NULL
   OR CHARINDEX(N'rolActionTypeID in (1, 8, 9)', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_FillPercentage')), N'')) = 0
BEGIN
    RAISERROR(N'59: сначала Deploy/29 и ArtvisDB/Scripts/local-promo-roller-types-deploy.sql. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. stat_FillPercentage
-- ЧАСТЬ 2: Обновлённая процедура

CREATE OR ALTER PROCEDURE [dbo].[stat_FillPercentage]
(
    @StartDay datetime = null, 
    @FinishDay datetime = null,
    @StartTime datetime = NULL,
    @FinishTime datetime = NULL,
    @MassMediaID smallint = default,
    @massmediaGroupID int = default,
    @PaymentTypeID smallint = default,
    @campaignTypeID tinyint = default,
    @IsGroupByPaymentType bit = 0,
    @IsGroupByCampaignType bit = 0,
    @IsGroupByDay bit = 0,
    @IsGroupByTariffWindow bit = 0,
    @IncludeEmptyBlocks bit = 1,
    @loggedUserID smallint,
    @Monday bit = 1,
    @Tuesday bit = 1,
    @Wednesday bit = 1,
    @Thursday bit = 1,
    @Friday bit = 1,
    @Saturday bit = 1,
    @Sunday bit = 1
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    -- Отмеченные дни недели битами: бит 0 - понедельник ... бит 6 - воскресенье, как dayMask в fn_AgitationExcludeIntervals.
    -- День окна - DATEDIFF от 01.01.1900 (понедельник) % 7: не зависит от DATEFIRST сессии (у логина с русским языком
    -- DATEFIRST = 1, а прежнее сравнение номера дня недели считало 1 воскресеньем и сдвигало каждую галочку на день). NULL - день не отмечен.
    DECLARE @dayMask int = ISNULL(@Monday, 0) * 1 + ISNULL(@Tuesday, 0) * 2 + ISNULL(@Wednesday, 0) * 4 + ISNULL(@Thursday, 0) * 8
        + ISNULL(@Friday, 0) * 16 + ISNULL(@Saturday, 0) * 32 + ISNULL(@Sunday, 0) * 64;

    -- НЕ ТРОГАЕМ!
    DECLARE @MinDate datetime = CAST('19000101' AS datetime);
    DECLARE @MaxDate datetime = CAST('22001231' AS datetime);
    DECLARE @MinTime time(7) = CAST('00:00:00.0000000' AS time(7));
    DECLARE @MaxTime time(7) = CAST('23:59:59.9999999' AS time(7));

    -- Предварительно вычисляем для оптимизации
    DECLARE @StartTimeCoalesced time(0) = CAST(COALESCE(CAST(@StartTime AS time), @MinTime) AS time(0));
    DECLARE @FinishTimeCoalesced time(0) = CAST(COALESCE(CAST(@FinishTime AS time), @MaxTime) AS time(0));

    DECLARE @sql nvarchar(4000);

    CREATE TABLE #massmedias
    (
        massmediaID smallint PRIMARY KEY,
        myMassmedia bit,
        foreignMassmedia bit
    );

    CREATE TABLE #available
    (
        mmid smallint,
        fulltime int,
        PRIMARY KEY (mmid)
    );

    CREATE TABLE #availableByDays
    (
        mmid smallint,
        [date] datetime,
        fulltime int,
        PRIMARY KEY (mmid, [date])
    );

    CREATE TABLE #issues
    (
        mmid smallint,
        campaigntypeid tinyint,
        paymenttypeid tinyint,
        [date] datetime,
        tariffTime time(0) not null,
        duration int,
        fulltime int,
        PRIMARY KEY (mmid, [date], tariffTime, campaigntypeid, paymenttypeid)
    );

    CREATE TABLE #availableResult
    (
        mmid smallint,
        [date] datetime,
        tariffTime time(0),
        fulltime int,
        PRIMARY KEY (mmid, [date], tariffTime)
    );

    CREATE TABLE #sold
    (
        mmid smallint,
        [date] datetime,
        tariffTime time(0),
        campaigntypeid tinyint,
        paymenttypeid tinyint,
        duration int,
        PRIMARY KEY (mmid, [date], tariffTime, campaigntypeid, paymenttypeid)
    );

    if (@StartDay is not null)
        set @StartDay = dbo.ToShortDate(@StartDay);

    if (@FinishDay is not null)
        set @FinishDay = dbo.ToShortDate(@FinishDay);

    INSERT INTO #massmedias (massmediaID, myMassmedia, foreignMassmedia)
    SELECT *
    FROM dbo.fn_GetMassmediasForUser(@loggedUserID);

    IF @IsGroupByDay = 0
    BEGIN
        IF @IsGroupByTariffWindow = 0
        BEGIN
            insert into #available
            select
                tw.massmediaID as mmid,
                sum(tw.duration) as fulltime
            from TariffWindow tw
                inner join MassMedia mm On mm.massmediaID = tw.massmediaID
            where tw.tariffId is not null
                and tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                and tw.massmediaID IN (select massmediaID from #massmedias)
                AND (@IncludeEmptyBlocks = 1 OR tw.timeInUseConfirmed > 0)
                and (mm.massmediaGroupID = coalesce(@massmediaGroupID, mm.massmediaGroupID) or (mm.massmediaGroupID is null and @massmediaGroupID is null))
                AND (@dayMask & POWER(2, DATEDIFF(DAY, '19000101', tw.dayActual) % 7)) <> 0
            group by tw.massmediaID;

            insert into #issues (mmid, campaigntypeid, paymenttypeid, [date], tariffTime, duration, fulltime)
            select
                tw.massmediaID as mmid,
                c.campaignTypeID as campaigntypeid,
                c.paymentTypeID as paymenttypeid,
                tw.dayActual as [date],
                cast('00:00:00' as time(0)) as tariffTime,
                sum((r.duration / (case when tw.maxCapacity > 0 and tw.capacityInUseConfirmed > 0 then tw.capacityInUseConfirmed else 1 end))) as duration,
                MAX(r1.fulltime)
            from TariffWindow tw
                inner join Issue i on i.actualWindowID = tw.windowId
                inner join Campaign c on i.campaignID = c.campaignID
                inner join [Action] a on c.actionID = a.actionID
                inner join Roller r on i.rollerID = r.rollerID and r.rolActionTypeID in (1, 8, 9)
                inner join #available r1 ON r1.mmid = tw.massmediaID
            where tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and i.isConfirmed = 1
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                AND (@dayMask & POWER(2, DATEDIFF(DAY, '19000101', tw.dayActual) % 7)) <> 0
                and EXISTS
                (
                    select 1
                    from #massmedias umm
                    where umm.massmediaID = tw.massmediaID
                        and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
                )
                AND (a.userID = @loggedUserID
                        OR dbo.fn_IsRightToViewForeignActions(@loggedUserID) = 1
                        OR
                        (
                            dbo.fn_IsRightToViewGroupActions(@loggedUserID) = 1
                            AND EXISTS
                            (
                                SELECT 1
                                FROM GroupMember gm
                                    JOIN fn_GetUserGroups(@loggedUserID) ug on gm.groupID = ug.id
                                WHERE a.userID = gm.userID
                            )
                        )
                    )
            group by tw.massmediaID, tw.dayActual, c.campaignTypeID, c.paymentTypeID;
        END
        ELSE
        BEGIN
            insert into #availableResult
            select
                tw.massmediaID as mmid,
                @MinDate as [date],
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0)) as tariffTime,
                sum(tw.duration) as fulltime
            from TariffWindow tw
                inner join MassMedia mm On mm.massmediaID = tw.massmediaID
            where tw.tariffId is not null
                and tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                and tw.massmediaID IN (select massmediaID from #massmedias)
                AND (@IncludeEmptyBlocks = 1 OR tw.timeInUseConfirmed > 0)
                and (mm.massmediaGroupID = coalesce(@massmediaGroupID, mm.massmediaGroupID) or (mm.massmediaGroupID is null and @massmediaGroupID is null))
                AND (@dayMask & POWER(2, DATEDIFF(DAY, '19000101', tw.dayActual) % 7)) <> 0
            group by
                tw.massmediaID,
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0));

            insert into #sold (mmid, [date], tariffTime, campaigntypeid, paymenttypeid, duration)
            select
                tw.massmediaID as mmid,
                @MinDate as [date],
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0)) as tariffTime,
                c.campaignTypeID as campaigntypeid,
                c.paymentTypeID as paymenttypeid,
                sum(
                    r.duration /
                    (case
                        when tw.maxCapacity > 0 and tw.capacityInUseConfirmed > 0
                            then tw.capacityInUseConfirmed
                        else 1
                    end)
                ) as duration
            from TariffWindow tw
                inner join Issue i on i.actualWindowID = tw.windowId
                inner join Campaign c on i.campaignID = c.campaignID
                inner join [Action] a on c.actionID = a.actionID
                inner join Roller r on i.rollerID = r.rollerID and r.rolActionTypeID in (1, 8, 9)
            where tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and i.isConfirmed = 1
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                AND (@dayMask & POWER(2, DATEDIFF(DAY, '19000101', tw.dayActual) % 7)) <> 0
                and EXISTS
                (
                    select 1
                    from #massmedias umm
                    where umm.massmediaID = tw.massmediaID
                        and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
                )
                AND
                (
                    a.userID = @loggedUserID
                    OR dbo.fn_IsRightToViewForeignActions(@loggedUserID) = 1
                    OR
                    (
                        dbo.fn_IsRightToViewGroupActions(@loggedUserID) = 1
                        AND EXISTS
                        (
                            SELECT 1
                            FROM GroupMember gm
                                JOIN fn_GetUserGroups(@loggedUserID) ug on gm.groupID = ug.id
                            WHERE a.userID = gm.userID
                        )
                    )
                )
            group by
                tw.massmediaID,
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0)),
                c.campaignTypeID,
                c.paymentTypeID;
        END
    END
    ELSE
    BEGIN
        IF @IsGroupByTariffWindow = 0
        BEGIN
            insert into #availableByDays
            select
                tw.massmediaID as mmid,
                tw.dayActual as [date],
                sum(tw.duration) as fulltime
            from TariffWindow tw
            where tw.tariffId is not null
                and tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                and tw.massmediaID IN (select massmediaID from #massmedias)
                AND (@IncludeEmptyBlocks = 1 OR tw.timeInUseConfirmed > 0)
                AND (@dayMask & POWER(2, DATEDIFF(DAY, '19000101', tw.dayActual) % 7)) <> 0
            group by tw.massmediaID, tw.dayActual;

            insert into #issues (mmid, campaigntypeid, paymenttypeid, [date], tariffTime, duration, fulltime)
            select
                tw.massmediaID as mmid,
                c.campaignTypeID as campaigntypeid,
                c.paymentTypeID as paymenttypeid,
                tw.dayActual as [date],
                cast('00:00:00' as time(0)) as tariffTime,
                sum((r.duration / (case when tw.maxCapacity > 0 and tw.capacityInUseConfirmed > 0 then tw.capacityInUseConfirmed else 1 end))) as duration,
                MAX(r1.fulltime)
            from TariffWindow tw
                inner join Issue i on i.actualWindowID = tw.windowId
                inner join Campaign c on i.campaignID = c.campaignID
                inner join [Action] a on c.actionID = a.actionID
                inner join Roller r on i.rollerID = r.rollerID and r.rolActionTypeID in (1, 8, 9)
                inner join #availableByDays r1 ON r1.mmid = tw.massmediaID and r1.[date] = tw.dayActual
            where tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and i.isConfirmed = 1
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                AND (@dayMask & POWER(2, DATEDIFF(DAY, '19000101', tw.dayActual) % 7)) <> 0
                and EXISTS
                (
                    select 1
                    from #massmedias umm
                    where umm.massmediaID = tw.massmediaID
                        and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
                )
                AND (a.userID = @loggedUserID
                        OR dbo.fn_IsRightToViewForeignActions(@loggedUserID) = 1
                        OR
                        (
                            dbo.fn_IsRightToViewGroupActions(@loggedUserID) = 1
                            AND EXISTS
                            (
                                SELECT 1
                                FROM GroupMember gm
                                    JOIN fn_GetUserGroups(@loggedUserID) ug on gm.groupID = ug.id
                                WHERE a.userID = gm.userID
                            )
                        )
                    )
            group by tw.massmediaID, tw.dayActual, c.campaignTypeID, c.paymentTypeID;
        END
        ELSE
        BEGIN
            insert into #availableResult
            select
                tw.massmediaID as mmid,
                tw.dayActual as [date],
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0)) as tariffTime,
                sum(tw.duration) as fulltime
            from TariffWindow tw
            where tw.tariffId is not null
                and tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                and tw.massmediaID IN (select massmediaID from #massmedias)
                AND (@IncludeEmptyBlocks = 1 OR tw.timeInUseConfirmed > 0)
                AND (@dayMask & POWER(2, DATEDIFF(DAY, '19000101', tw.dayActual) % 7)) <> 0
            group by
                tw.massmediaID,
                tw.dayActual,
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0));

            insert into #sold (mmid, [date], tariffTime, campaigntypeid, paymenttypeid, duration)
            select
                tw.massmediaID as mmid,
                tw.dayActual as [date],
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0)) as tariffTime,
                c.campaignTypeID as campaigntypeid,
                c.paymentTypeID as paymenttypeid,
                sum(
                    r.duration /
                    (case
                        when tw.maxCapacity > 0 and tw.capacityInUseConfirmed > 0
                            then tw.capacityInUseConfirmed
                        else 1
                    end)
                ) as duration
            from TariffWindow tw
                inner join Issue i on i.actualWindowID = tw.windowId
                inner join Campaign c on i.campaignID = c.campaignID
                inner join [Action] a on c.actionID = a.actionID
                inner join Roller r on i.rollerID = r.rollerID and r.rolActionTypeID in (1, 8, 9)
            where tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and i.isConfirmed = 1
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                AND (@dayMask & POWER(2, DATEDIFF(DAY, '19000101', tw.dayActual) % 7)) <> 0
                and EXISTS
                (
                    select 1
                    from #massmedias umm
                    where umm.massmediaID = tw.massmediaID
                        and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
                )
                AND
                (
                    a.userID = @loggedUserID
                    OR dbo.fn_IsRightToViewForeignActions(@loggedUserID) = 1
                    OR
                    (
                        dbo.fn_IsRightToViewGroupActions(@loggedUserID) = 1
                        AND EXISTS
                        (
                            SELECT 1
                            FROM GroupMember gm
                                JOIN fn_GetUserGroups(@loggedUserID) ug on gm.groupID = ug.id
                            WHERE a.userID = gm.userID
                        )
                    )
                )
            group by
                tw.massmediaID,
                tw.dayActual,
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0)),
                c.campaignTypeID,
                c.paymentTypeID;
        END
    END

    IF @IsGroupByTariffWindow = 1
    BEGIN
        set @sql = 'select row_number() over(order by ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + ' b.date, ';

        set @sql = @sql + ' b.tariffTime, mm.name) as RowNum, ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + 'b.date as [date], ';

        set @sql = @sql + ' convert(char(5), b.tariffTime, 108) as [tariffTime], ';
        set @sql = @sql + ' mm.[name] as [massmedia], mm.groupName, ';

        if (@IsGroupByPaymentType = 1)
            set @sql = @sql + ' pt.[name] as [paymentType], ';

        if (@IsGroupByCampaignType = 1)
            set @sql = @sql + ' ct.[name] as [campaignType], ';

        set @sql = @sql + ' dbo.fn_Int2Time(sum(isnull(s.duration, 0))) as [realTime], 
            cast(sum(cast(isnull(s.duration, 0) as float)) / max(cast(b.fulltime as float)) * 100 as decimal(5,2)) as [fill]
        from #availableResult b
            inner join vMassmedia mm on b.mmid = mm.massmediaID
            left join #sold s on s.mmid = b.mmid
                             and s.[date] = b.[date]
                             and s.tariffTime = b.tariffTime ';

        if (@IsGroupByPaymentType = 1)
            set @sql = @sql + ' left join PaymentType pt on s.paymenttypeid = pt.paymentTypeID ';

        if (@IsGroupByCampaignType = 1)
            set @sql = @sql + ' left join iCampaignType ct on s.campaigntypeid = ct.campaignTypeID ';

        set @sql = @sql + ' WHERE 1 = 1 ';

        if (@PaymentTypeID IS NOT NULL)
            set @sql = @sql + ' AND (s.paymentTypeID = ' + CAST(@PaymentTypeID AS varchar) + ' OR s.paymentTypeID IS NULL) ';

        if (@campaignTypeID IS NOT NULL)
            set @sql = @sql + ' AND (s.campaignTypeID = ' + CAST(@campaignTypeID AS varchar) + ' OR s.campaignTypeID IS NULL) ';

        set @sql = @sql + ' group by ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + ' b.date, ';

        set @sql = @sql + ' b.tariffTime, mm.massmediaID, mm.[name], mm.groupName ';

        if (@IsGroupByPaymentType = 1)
            set @sql = @sql + ' , pt.paymentTypeID, pt.[name] ';

        if (@IsGroupByCampaignType = 1)
            set @sql = @sql + ' , ct.campaignTypeID, ct.[name] ';

        set @sql = @sql + ' order by ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + ' b.date, ';

        set @sql = @sql + ' b.tariffTime, mm.name ';
    END
    ELSE
    BEGIN
        set @sql = 'select row_number() over(order by ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + ' i.date, ';

        if (@IsGroupByTariffWindow = 1)
            set @sql = @sql + ' i.tariffTime, ';

        set @sql = @sql + ' mm.name) as RowNum, ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + 'i.date as [date], ';

        if (@IsGroupByTariffWindow = 1)
            set @sql = @sql + ' convert(char(5), i.tariffTime, 108) as [tariffTime], ';

        set @sql = @sql + ' mm.[name] as [massmedia], mm.groupName, ';

        if (@IsGroupByPaymentType = 1)
            set @sql = @sql + 'pt.[name] as [paymentType], ';

        if (@IsGroupByCampaignType = 1)
            set @sql = @sql + 'ct.[name] as [campaignType], ';

        set @sql = @sql + 'dbo.fn_Int2Time(sum(i.duration)) as [realTime], 
                cast(sum(cast(i.duration as float)) / max(cast(i.fulltime as float)) * 100 as decimal(5,2)) as [fill]
            from #issues i
                inner join vMassmedia mm on i.mmid = mm.massmediaID ';

        if (@IsGroupByPaymentType = 1)
            set @sql = @sql + ' inner join PaymentType pt on i.paymenttypeid = pt.paymentTypeID ';

        if (@IsGroupByCampaignType = 1)
            set @sql = @sql + ' inner join iCampaignType ct on i.campaigntypeid = ct.campaignTypeID ';

        set @sql = @sql + ' WHERE 1 = 1 ';

        if (@PaymentTypeID IS NOT NULL)
            set @sql = @sql + ' AND i.paymentTypeID = ' + CAST(@PaymentTypeID AS varchar);

        if (@campaignTypeID IS NOT NULL)
            set @sql = @sql + ' AND i.campaignTypeID = ' + CAST(@campaignTypeID AS varchar);

        set @sql = @sql + ' group by ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + ' i.date, ';

        if (@IsGroupByTariffWindow = 1)
            set @sql = @sql + ' i.tariffTime, ';

        set @sql = @sql + ' mm.massmediaID, mm.[name], mm.groupName ';

        if (@IsGroupByPaymentType = 1)
            set @sql = @sql + ' , pt.paymentTypeID, pt.[name] ';

        if (@IsGroupByCampaignType = 1)
            set @sql = @sql + ' , ct.campaignTypeID, ct.[name] ';

        set @sql = @sql + ' order by ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + ' i.date, ';

        if (@IsGroupByTariffWindow = 1)
            set @sql = @sql + ' i.tariffTime, ';

        set @sql = @sql + ' mm.name ';
    END

    exec sp_executeSQL @sql;

    DROP TABLE #issues;
    DROP TABLE #availableResult;
    DROP TABLE #sold;
    DROP TABLE #massmedias;
    DROP TABLE #available;
    DROP TABLE #availableByDays;
END
GO

-- 2. TariffPassport
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
	+ case t.tuesday when 1 then ',вт' else '' end 
	+ case t.wednesday when 1 then ',ср' else '' end 
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

-- Проверка: обе процедуры в новой версии.
IF CHARINDEX(N'@dayMask', OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_FillPercentage'))) > 0
   AND CHARINDEX(N'DATEPART(WEEKDAY', OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_FillPercentage'))) = 0
   AND CHARINDEX(N'case t.tuesday when 1 then '',вт''', OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffPassport'))) > 0
    PRINT N'ГОТОВО: stat_FillPercentage (дни недели маской, без DATEFIRST) и TariffPassport (вт/ср на своих местах).';
ELSE
    RAISERROR(N'59: новая версия применена не ко всем процедурам.', 16, 1);
GO

-- Снять режим «не выполнять», если его включила защита в начале (иначе окно SSMS остаётся в NOEXEC).
SET NOEXEC OFF;
GO
