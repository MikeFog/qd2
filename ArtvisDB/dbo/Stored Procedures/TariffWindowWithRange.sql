-- Created by GitHub Copilot in SSMS - review carefully before executing
CREATE PROCEDURE [dbo].[TariffWindowWithRange]
(
    @actionID int,
    @dateStart datetime
)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE
        @minBroadcast datetime,
        @maxBroadcast datetime,
        @dateWithBroadCastStart datetime;
    --------------------------------------------------------------------
    -- 1) Список СМИ в рамках акции (ВАЖНО: DISTINCT!)
    --------------------------------------------------------------------
    SELECT DISTINCT c.massmediaID
    INTO #mm
    FROM dbo.Campaign c
    WHERE c.actionID = @actionID
      AND c.massmediaID IS NOT NULL;
    CREATE UNIQUE CLUSTERED INDEX CX_mm ON #mm(massmediaID);
    DECLARE @mmCnt int = (SELECT COUNT(*) FROM #mm);
    --------------------------------------------------------------------
    -- 2) Фирма текущей акции
    --------------------------------------------------------------------
    DECLARE @firmID smallint = (SELECT firmID FROM dbo.Action WHERE actionID = @actionID);
    --------------------------------------------------------------------
    -- 3) min/max broadcastStart по прайслистам этих СМИ на нужную неделю
    --------------------------------------------------------------------
    SELECT
        @minBroadcast = MIN(pl.broadcastStart),
        @maxBroadcast = MAX(pl.broadcastStart)
    FROM dbo.Pricelist pl
    JOIN #mm m ON m.massmediaID = pl.massmediaID
    WHERE pl.finishDate >= @dateStart
      AND pl.startDate <= DATEADD(day, 7, @dateStart);
    SET @dateWithBroadCastStart =
        DATEADD(minute, 0, --DATEPART(minute, @maxBroadcast), проблема из-за минут при редактирвании веерной акции
        DATEADD(hour, DATEPART(hour, @maxBroadcast), @dateStart));
    --------------------------------------------------------------------
    -- 4) Таблица результата
    --------------------------------------------------------------------
    CREATE TABLE #res
    (
        [date] datetime NOT NULL,
        [enddate] datetime NOT NULL,
        [col] smallint NULL,
        [row] smallint NULL,
        timeWithConfirmed int NULL,
        timeWithUnConfirmed int NULL,
        isFirstPositionOccupied bit NULL,
        isSecondPositionOccupied bit NULL,
        isLastPositionOccupied bit NULL,
        firstPositionsUnconfirmed int NULL,
        secondPositionsUnconfirmed int NULL,
        lastPositionsUnconfirmed int NULL,
        isPrime bit NULL,
        HasIssues                        bit NOT NULL DEFAULT 0,
        HasIssuesAllMassmedia            bit NOT NULL DEFAULT 0,
        HasIssuesUnconfirmed             bit NOT NULL DEFAULT 0,
        HasIssuesUnconfirmedAllMassmedia bit NOT NULL DEFAULT 0,
        HasIssuesThisAction              bit NOT NULL DEFAULT 0,  -- ← новая
        CONSTRAINT PK_res PRIMARY KEY CLUSTERED ([date], [enddate])
    );
    INSERT INTO #res([date],[enddate],[col],[row])
    SELECT
        DATEADD(minute, 30*((z.x-1)*8 + (y.x-1)), DATEADD(day, x.x - 1, @dateWithBroadCastStart)) AS [date],
        DATEADD(second, -1, DATEADD(minute, 30,
            DATEADD(minute, 30*((z.x-1)*8 + (y.x-1)), DATEADD(day, x.x - 1, @dateWithBroadCastStart))
        )) AS [enddate],
        x.x AS [col],
        ((z.x-1)*8 + (y.x-1)) AS [row]
    FROM
        (SELECT 1 AS x UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7) x
        CROSS JOIN (SELECT 1 AS x UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8) y
        CROSS JOIN (SELECT 1 AS x UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6) z
    WHERE
        DATEADD(minute, 30*((z.x-1)*8 + (y.x-1)), @dateWithBroadCastStart)
        <
        DATEADD(day, 1,
            DATEADD(minute, DATEPART(minute, @minBroadcast),
            DATEADD(hour, DATEPART(hour, @minBroadcast), @dateStart))
        );
    --------------------------------------------------------------------
    -- 5) Предфильтрация TariffWindow
    --------------------------------------------------------------------
    DECLARE
        @minDate datetime = (SELECT MIN([date]) FROM #res),
        @maxEnd  datetime = (SELECT MAX([enddate]) FROM #res);
    SELECT
        tw.massmediaID,
        tw.windowId,                  -- ← добавлено
        tw.windowDateOriginal,
        windowDay = CONVERT(datetime, CONVERT(varchar(8), tw.windowDateOriginal, 112), 112),
        tw.duration,
        tw.timeInUseConfirmed,
        tw.timeInUseUnconfirmed,
        tw.isFirstPositionOccupied,
        tw.isSecondPositionOccupied,
        tw.isLastPositionOccupied,
        tw.firstPositionsUnconfirmed,
        tw.secondPositionsUnconfirmed,
        tw.lastPositionsUnconfirmed,
        t.price,
        isPrime = CONVERT(bit, 0)
    INTO #tw
    FROM dbo.TariffWindow tw
    JOIN #mm m ON m.massmediaID = tw.massmediaID
    JOIN dbo.Tariff t ON t.tariffID = tw.tariffID AND t.isForModuleOnly = 0
    WHERE tw.maxCapacity = 0
      AND tw.isDisabled = 0
      AND tw.windowDateOriginal >= @minDate
      AND tw.windowDateOriginal <= @maxEnd;
    CREATE INDEX IX_tw_mm_date ON #tw(massmediaID, windowDateOriginal);
    CREATE INDEX IX_tw_mm_day_price ON #tw(massmediaID, windowDay, price);
    ;WITH max_price AS
    (
        SELECT massmediaID, windowDay, maxPrice = MAX(price)
        FROM #tw
        GROUP BY massmediaID, windowDay
    )
    UPDATE tw SET isPrime = CONVERT(bit, 1)
    FROM #tw tw
    JOIN max_price mp
      ON mp.massmediaID = tw.massmediaID
     AND mp.windowDay   = tw.windowDay
     AND mp.maxPrice    = tw.price;
    --------------------------------------------------------------------
    -- 6) Аггрегация (как в старой версии!)
    --    6.1) внутри СМИ: MAX(free) + MIN(flags/counts)
    --    6.2) по всем СМИ: MIN(time) + MAX(flags/counts) + HAVING COUNT(*) = @mmCnt
    --------------------------------------------------------------------
    ;WITH per_mm AS
    (
        SELECT
            r.[date],
            tw.massmediaID,
            -- СТАРАЯ ЛОГИКА: берем MAX свободного в получасе по СМИ
            timeWithConfirmed          = MAX(tw.duration - tw.timeInUseConfirmed),
            timeWithUnConfirmed        = MAX(tw.duration - tw.timeInUseConfirmed - tw.timeInUseUnconfirmed),
            -- СТАРАЯ ЛОГИКА: MIN по флагам/счетчикам внутри СМИ
            isFirstPositionOccupied    = MIN(CONVERT(int, tw.isFirstPositionOccupied)),
            isSecondPositionOccupied   = MIN(CONVERT(int, tw.isSecondPositionOccupied)),
            isLastPositionOccupied     = MIN(CONVERT(int, tw.isLastPositionOccupied)),
            firstPositionsUnconfirmed  = MIN(tw.firstPositionsUnconfirmed),
            secondPositionsUnconfirmed = MIN(tw.secondPositionsUnconfirmed),
            lastPositionsUnconfirmed   = MIN(tw.lastPositionsUnconfirmed),
            -- Прайм внутри одного СМИ: все окна, попавшие в получасовой блок, должны быть праймовыми
            isPrime = MIN(CONVERT(int, tw.isPrime))
        FROM #res r
        JOIN #tw tw ON tw.windowDateOriginal BETWEEN r.[date] AND r.[enddate]
        GROUP BY r.[date], tw.massmediaID
    ),
    all_mm AS
    (
        SELECT
            x.[date],
            timeWithConfirmed          = MIN(x.timeWithConfirmed),
            timeWithUnConfirmed        = MIN(x.timeWithUnConfirmed),
            -- как в старой версии: MAX после MIN
            isFirstPositionOccupied    = MAX(x.isFirstPositionOccupied),
            isSecondPositionOccupied   = MAX(x.isSecondPositionOccupied),
            isLastPositionOccupied     = MAX(x.isLastPositionOccupied),
            firstPositionsUnconfirmed  = MAX(x.firstPositionsUnconfirmed),
            secondPositionsUnconfirmed = MAX(x.secondPositionsUnconfirmed),
            lastPositionsUnconfirmed   = MAX(x.lastPositionsUnconfirmed),
            -- Общий прайм: все СМИ в получасовом блоке должны быть праймовыми
            isPrime = MIN(x.isPrime),
            cnt = COUNT(*)
        FROM per_mm x
        GROUP BY x.[date]
        HAVING COUNT(*) = @mmCnt
    )
    UPDATE r SET
        r.timeWithConfirmed            = a.timeWithConfirmed,
        r.timeWithUnConfirmed          = a.timeWithUnConfirmed,
        r.isFirstPositionOccupied      = CONVERT(bit, a.isFirstPositionOccupied),
        r.isSecondPositionOccupied     = CONVERT(bit, a.isSecondPositionOccupied),
        r.isLastPositionOccupied       = CONVERT(bit, a.isLastPositionOccupied),
        r.firstPositionsUnconfirmed    = a.firstPositionsUnconfirmed,
        r.secondPositionsUnconfirmed   = a.secondPositionsUnconfirmed,
        r.lastPositionsUnconfirmed     = a.lastPositionsUnconfirmed,
        r.isPrime                      = CONVERT(bit, a.isPrime)
    FROM #res r
    JOIN all_mm a ON a.[date] = r.[date]
    OPTION (RECOMPILE);
    --------------------------------------------------------------------
    -- 7) Все 4 колонки — один проход по данным
    --    FIX: исправлен некорректный JOIN #mm m ON m.massmediaID = m.massmediaID
    --    OPT: ранний WHERE отсекает строки, не influencing ни на одну из 4 колонок
    --------------------------------------------------------------------
    ;WITH all_issues AS
    (
        SELECT
            r.[date],
            tw.massmediaID,
            i.isConfirmed,
            a.deleteDate
        FROM #res r
        JOIN dbo.TariffWindow tw
            ON tw.windowDateOriginal BETWEEN r.[date] AND r.[enddate]
        JOIN #mm m
            ON m.massmediaID = tw.massmediaID
        JOIN dbo.Issue i
            ON i.actualWindowID = tw.windowId
        JOIN dbo.Campaign c
            ON c.campaignID = i.campaignID
        JOIN dbo.Action a
            ON a.actionID  = c.actionID
           AND a.firmID    = @firmID
           AND a.actionID <> @actionID
        WHERE i.isConfirmed = 1        -- нужен для HasIssues
           OR a.deleteDate IS NULL     -- нужен для HasIssuesUnconfirmed
    ),
    per_mm AS
    (
        SELECT
            [date],
            massmediaID,
            hasConfirmed     = MAX(CASE WHEN isConfirmed = 1    THEN 1 ELSE 0 END),
            hasAnyNonDeleted = MAX(CASE WHEN deleteDate IS NULL THEN 1 ELSE 0 END)
        FROM all_issues
        GROUP BY [date], massmediaID
    ),
    slots AS
    (
        SELECT
            [date],
            mmWithConfirmed     = SUM(CASE WHEN hasConfirmed = 1     THEN 1 ELSE 0 END),
            mmWithAnyNonDeleted = SUM(CASE WHEN hasAnyNonDeleted = 1 THEN 1 ELSE 0 END)
        FROM per_mm
        GROUP BY [date]
    )
    UPDATE r SET
        r.HasIssues                        = CONVERT(bit, CASE WHEN s.mmWithConfirmed >= 1         THEN 1 ELSE 0 END),
        r.HasIssuesAllMassmedia            = CONVERT(bit, CASE WHEN s.mmWithConfirmed = @mmCnt     THEN 1 ELSE 0 END),
        r.HasIssuesUnconfirmed             = CONVERT(bit, CASE WHEN s.mmWithAnyNonDeleted >= 1     THEN 1 ELSE 0 END),
        r.HasIssuesUnconfirmedAllMassmedia = CONVERT(bit, CASE WHEN s.mmWithAnyNonDeleted = @mmCnt THEN 1 ELSE 0 END)
    FROM #res r
    JOIN slots s ON s.[date] = r.[date];
    --------------------------------------------------------------------
    -- 8) HasIssuesThisAction  ← должен быть ДО финальных SELECT-ов
    --------------------------------------------------------------------
    ;WITH this_action_issues AS
    (
        SELECT r.[date]
        FROM #res r
        JOIN #tw tw
            ON tw.windowDateOriginal BETWEEN r.[date] AND r.[enddate]
        JOIN dbo.Issue i
            ON i.actualWindowID = tw.windowId
        JOIN dbo.Campaign c
            ON c.campaignID = i.campaignID
           AND c.actionID   = @actionID
        GROUP BY r.[date]
    )
    UPDATE r SET
        r.HasIssuesThisAction = CONVERT(bit, 1)
    FROM #res r
    JOIN this_action_issues x ON x.[date] = r.[date];

    --------------------------------------------------------------------
    -- 9) Возвраты  ← только после всех UPDATE
    --------------------------------------------------------------------
    SELECT
        r.[date], r.[enddate], r.[col], r.[row],
        r.timeWithConfirmed, r.timeWithUnConfirmed,
        r.isFirstPositionOccupied, r.isSecondPositionOccupied, r.isLastPositionOccupied,
        r.firstPositionsUnconfirmed, r.secondPositionsUnconfirmed, r.lastPositionsUnconfirmed,
        r.isPrime,
        r.HasIssues, r.HasIssuesAllMassmedia,
        r.HasIssuesUnconfirmed, r.HasIssuesUnconfirmedAllMassmedia,
        r.HasIssuesThisAction,  -- ← новая
        DATEPART(hour, r.[date]) AS h,
        DATEPART(minute, r.[date]) AS m
    FROM #res r
    WHERE r.timeWithConfirmed IS NOT NULL;
    SELECT @maxBroadcast AS maxBroadcast, @minBroadcast AS minBroadcast;
    SELECT DATEPART(hour, r.[date]) AS h, DATEPART(minute, r.[date]) AS m
    FROM #res r
    WHERE r.timeWithConfirmed IS NOT NULL
    GROUP BY DATEPART(hour, r.[date]), DATEPART(minute, r.[date])
    ORDER BY
        (CASE
            WHEN DATEPART(hour, @minBroadcast) < DATEPART(hour, r.[date])
              OR (DATEPART(hour, @minBroadcast) = DATEPART(hour, r.[date])
                  AND DATEPART(minute, @minBroadcast) < DATEPART(minute, r.[date]))
            THEN 0 ELSE 1 END),
        DATEPART(hour, r.[date]),
        DATEPART(minute, r.[date]);
END