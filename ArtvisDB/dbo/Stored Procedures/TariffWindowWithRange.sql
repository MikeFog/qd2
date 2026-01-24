CREATE   PROCEDURE dbo.TariffWindowWithRange
(
    @actionID  int,
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
    -- 2) min/max broadcastStart по прайслистам этих СМИ на нужную неделю
    --------------------------------------------------------------------
    SELECT
        @minBroadcast = MIN(pl.broadcastStart),
        @maxBroadcast = MAX(pl.broadcastStart)
    FROM dbo.Pricelist pl
    JOIN #mm m ON m.massmediaID = pl.massmediaID
    WHERE pl.finishDate >= @dateStart
      AND pl.startDate  <= DATEADD(day, 7, @dateStart);

    -- Начало общего времени выпуска в рамках broadCast (как раньше: по maxBroadcast)
    SET @dateWithBroadCastStart =
        DATEADD(minute, DATEPART(minute, @maxBroadcast),
        DATEADD(hour,   DATEPART(hour,   @maxBroadcast), @dateStart));

    --------------------------------------------------------------------
    -- 3) Таблица результата: 7 дней * 48 получасов, но с ограничением по minBroadcast
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
            DATEADD(hour,   DATEPART(hour,   @minBroadcast), @dateStart))
        );

    --------------------------------------------------------------------
    -- 4) Предфильтрация TariffWindow на нужный диапазон + нужные СМИ + Tariff.isForModuleOnly=0
    --------------------------------------------------------------------
    DECLARE
        @minDate datetime = (SELECT MIN([date]) FROM #res),
        @maxEnd  datetime = (SELECT MAX([enddate]) FROM #res);

    SELECT
        tw.massmediaID,
        tw.windowDateOriginal,
        tw.duration,
        tw.timeInUseConfirmed,
        tw.timeInUseUnconfirmed,
        tw.isFirstPositionOccupied,
        tw.isSecondPositionOccupied,
        tw.isLastPositionOccupied,
        tw.firstPositionsUnconfirmed,
        tw.secondPositionsUnconfirmed,
        tw.lastPositionsUnconfirmed
    INTO #tw
    FROM dbo.TariffWindow tw
    JOIN #mm m      ON m.massmediaID = tw.massmediaID
    JOIN dbo.Tariff t ON t.tariffID = tw.tariffID AND t.isForModuleOnly = 0
    WHERE tw.maxCapacity = 0
      AND tw.isDisabled = 0
      AND tw.windowDateOriginal >= @minDate
      AND tw.windowDateOriginal <= @maxEnd;

    CREATE INDEX IX_tw_mm_date ON #tw(massmediaID, windowDateOriginal);

    --------------------------------------------------------------------
    -- 5) Аггрегация (как в старой версии!)
    --    5.1) внутри СМИ: MAX(free) + MIN(flags/counts)
    --    5.2) по всем СМИ: MIN(time) + MAX(flags/counts) + HAVING COUNT(*) = @mmCnt
    --------------------------------------------------------------------
    ;WITH per_mm AS
    (
        SELECT
            r.[date],
            tw.massmediaID,

            -- СТАРАЯ ЛОГИКА: берём MAX свободного в получасе по СМИ
            timeWithConfirmed   = MAX(tw.duration - tw.timeInUseConfirmed),
            timeWithUnConfirmed = MAX(tw.duration - tw.timeInUseConfirmed - tw.timeInUseUnconfirmed),

            -- СТАРАЯ ЛОГИКА: MIN по флагам/счётчикам внутри СМИ
            isFirstPositionOccupied  = MIN(CONVERT(int, tw.isFirstPositionOccupied)),
            isSecondPositionOccupied = MIN(CONVERT(int, tw.isSecondPositionOccupied)),
            isLastPositionOccupied   = MIN(CONVERT(int, tw.isLastPositionOccupied)),
            firstPositionsUnconfirmed  = MIN(tw.firstPositionsUnconfirmed),
            secondPositionsUnconfirmed = MIN(tw.secondPositionsUnconfirmed),
            lastPositionsUnconfirmed   = MIN(tw.lastPositionsUnconfirmed)
        FROM #res r
        JOIN #tw tw
          ON tw.windowDateOriginal BETWEEN r.[date] AND r.[enddate]
        GROUP BY r.[date], tw.massmediaID
    ),
    all_mm AS
    (
        SELECT
            x.[date],
            timeWithConfirmed   = MIN(x.timeWithConfirmed),
            timeWithUnConfirmed = MIN(x.timeWithUnConfirmed),

            -- как в старой версии: MAX после MIN
            isFirstPositionOccupied  = MAX(x.isFirstPositionOccupied),
            isSecondPositionOccupied = MAX(x.isSecondPositionOccupied),
            isLastPositionOccupied   = MAX(x.isLastPositionOccupied),

            firstPositionsUnconfirmed  = MAX(x.firstPositionsUnconfirmed),
            secondPositionsUnconfirmed = MAX(x.secondPositionsUnconfirmed),
            lastPositionsUnconfirmed   = MAX(x.lastPositionsUnconfirmed),

            cnt = COUNT(*)
        FROM per_mm x
        GROUP BY x.[date]
        HAVING COUNT(*) = @mmCnt
    )
    UPDATE r SET
        r.timeWithConfirmed = a.timeWithConfirmed,
        r.timeWithUnConfirmed = a.timeWithUnConfirmed,
        r.isFirstPositionOccupied  = CONVERT(bit, a.isFirstPositionOccupied),
        r.isSecondPositionOccupied = CONVERT(bit, a.isSecondPositionOccupied),
        r.isLastPositionOccupied   = CONVERT(bit, a.isLastPositionOccupied),
        r.firstPositionsUnconfirmed  = a.firstPositionsUnconfirmed,
        r.secondPositionsUnconfirmed = a.secondPositionsUnconfirmed,
        r.lastPositionsUnconfirmed   = a.lastPositionsUnconfirmed
    FROM #res r
    JOIN all_mm a ON a.[date] = r.[date]
    OPTION (RECOMPILE);

    --------------------------------------------------------------------
    -- 6) Возвраты (как было)
    --------------------------------------------------------------------
    SELECT *, DATEPART(hour, r.[date]) AS h, DATEPART(minute, r.[date]) AS m
    FROM #res r
    WHERE r.timeWithConfirmed IS NOT NULL;

    SELECT @maxBroadcast AS maxBroadcast, @minBroadcast AS minBroadcast;

    SELECT DATEPART(hour, r.[date]) AS h, DATEPART(minute, r.[date]) AS m
    FROM #res r
    WHERE r.timeWithConfirmed IS NOT NULL
    GROUP BY DATEPART(hour, r.[date]), DATEPART(minute, r.[date])
    ORDER BY
        (CASE
            WHEN DATEPART(hour, @minBroadcast) <  DATEPART(hour, r.[date])
              OR (DATEPART(hour, @minBroadcast) = DATEPART(hour, r.[date])
                  AND DATEPART(minute, @minBroadcast) < DATEPART(minute, r.[date]))
            THEN 0 ELSE 1 END),
        DATEPART(hour, r.[date]),
        DATEPART(minute, r.[date]);
END
