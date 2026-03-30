
-- ЧАСТЬ 2: Обновлённая процедура

CREATE PROCEDURE [dbo].[stat_FillPercentage]
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
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
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
                inner join Roller r on i.rollerID = r.rollerID and r.rolActionTypeID = 1
                inner join #available r1 ON r1.mmid = tw.massmediaID
            where tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and i.isConfirmed = 1
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
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
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
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
                inner join Roller r on i.rollerID = r.rollerID and r.rolActionTypeID = 1
            where tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and i.isConfirmed = 1
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
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
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
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
                inner join Roller r on i.rollerID = r.rollerID and r.rolActionTypeID = 1
                inner join #availableByDays r1 ON r1.mmid = tw.massmediaID and r1.[date] = tw.dayActual
            where tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and i.isConfirmed = 1
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
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
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
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
                inner join Roller r on i.rollerID = r.rollerID and r.rolActionTypeID = 1
            where tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and i.isConfirmed = 1
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
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