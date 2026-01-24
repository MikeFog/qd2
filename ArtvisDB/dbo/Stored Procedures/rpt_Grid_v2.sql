CREATE   PROCEDURE [dbo].[rpt_Grid_v2]
(
    @theDate     datetime,
    @massMediaID smallint
)
AS
BEGIN
    SET NOCOUNT ON;
    SET DATEFIRST 1;

    -------------------------------------------------------------------------
    -- normalize date
    -------------------------------------------------------------------------
    SET @theDate = dbo.ToShortDate(@theDate);

    -------------------------------------------------------------------------
    -- Pricelist + broadcastStart
    -------------------------------------------------------------------------
    DECLARE @broadcastStart smalldatetime;

    SELECT TOP (1)
        @broadcastStart = pl.broadcastStart
    FROM Pricelist pl
    WHERE pl.massmediaID = @massMediaID
      AND pl.startDate <= @theDate
      AND pl.finishDate >= @theDate
    ORDER BY pl.startDate DESC;

    DECLARE @bsMin int =
        DATEPART(hour, @broadcastStart) * 60 +
        DATEPART(minute, @broadcastStart);

    -------------------------------------------------------------------------
    -- #grid1 : tariff windows of the day
    -------------------------------------------------------------------------
    CREATE TABLE #grid1
    (
        tariffID int,
        [time] datetime,
        tariffMin int NOT NULL,
        tariffTime varchar(5) NOT NULL,
        cellRealDuration int,
        suffix nvarchar(16),
        needExt bit,
        needInJingle bit,
        needOutJingle bit,
        comment nvarchar(128),
        tariffUnionID int,
        windowId int NOT NULL,
        windowNextId int,
        windowPrevId int,
        durationTotal smallint
    );

    CREATE CLUSTERED INDEX CX_grid1 ON #grid1(windowId, tariffMin);

    INSERT INTO #grid1
    SELECT
        t.tariffID,
        tw.dayActual,
        CASE
            WHEN (DATEPART(hour, tw.windowDateActual) * 60 +
                  DATEPART(minute, tw.windowDateActual) - @bsMin) < 0
                THEN (DATEPART(hour, tw.windowDateActual) * 60 +
                      DATEPART(minute, tw.windowDateActual) - @bsMin) + 1440
            ELSE (DATEPART(hour, tw.windowDateActual) * 60 +
                  DATEPART(minute, tw.windowDateActual) - @bsMin)
        END,
        RIGHT('0' + CAST(
            (
                CASE
                    WHEN (DATEPART(hour, tw.windowDateActual) * 60 +
                          DATEPART(minute, tw.windowDateActual) - @bsMin) < 0
                        THEN ((DATEPART(hour, tw.windowDateActual) * 60 +
                               DATEPART(minute, tw.windowDateActual) - @bsMin) + 1440) / 60
                    ELSE (DATEPART(hour, tw.windowDateActual) * 60 +
                          DATEPART(minute, tw.windowDateActual) - @bsMin) / 60
                END
            ) AS varchar(2)), 2)
        + ':' +
        RIGHT('0' + CAST(
            (
                CASE
                    WHEN (DATEPART(hour, tw.windowDateActual) * 60 +
                          DATEPART(minute, tw.windowDateActual) - @bsMin) < 0
                        THEN ((DATEPART(hour, tw.windowDateActual) * 60 +
                               DATEPART(minute, tw.windowDateActual) - @bsMin) + 1440) % 60
                    ELSE (DATEPART(hour, tw.windowDateActual) * 60 +
                          DATEPART(minute, tw.windowDateActual) - @bsMin) % 60
                END
            ) AS varchar(2)), 2),
        tw.duration,
        t.suffix,
        t.needExt,
        t.needInJingle,
        t.needOutJingle,
        t.comment,
        tu.tariffUnionID,
        tw.windowId,
        tw.windowNextId,
        tw.windowPrevId,
        tw.duration_total
    FROM TariffWindow tw
        INNER JOIN Tariff t ON tw.tariffId = t.tariffID
        LEFT JOIN TariffUnion tu ON t.tariffID = tu.tariffID
    WHERE
        tw.massmediaID = @massMediaID
        AND tw.dayActual = @theDate;

    -------------------------------------------------------------------------
    -- #issue : confirmed issues only (no rights)
    -------------------------------------------------------------------------
    CREATE TABLE #issue
    (
        issueId int PRIMARY KEY,
        windowId int NOT NULL,
        tariffMin int NOT NULL,
        rollerID int,
        positionId float
    );

    CREATE INDEX IX_issue ON #issue(windowId, tariffMin);

    INSERT INTO #issue
    SELECT DISTINCT
        i.issueId,
        i.actualWindowID,
        CASE
            WHEN (DATEPART(hour, tw.windowDateActual) * 60 +
                  DATEPART(minute, tw.windowDateActual) - @bsMin) < 0
                THEN (DATEPART(hour, tw.windowDateActual) * 60 +
                      DATEPART(minute, tw.windowDateActual) - @bsMin) + 1440
            ELSE (DATEPART(hour, tw.windowDateActual) * 60 +
                  DATEPART(minute, tw.windowDateActual) - @bsMin)
        END,
        i.rollerID,
        i.positionId
    FROM Issue i
        INNER JOIN TariffWindow tw ON i.actualWindowID = tw.windowId
        INNER JOIN Campaign c ON i.campaignID = c.campaignID
    WHERE
        tw.massmediaID = @massMediaID
        AND tw.dayActual = @theDate
        AND i.isConfirmed = 1;

    -------------------------------------------------------------------------
    -- #grid2 : final grid
    -------------------------------------------------------------------------
    CREATE TABLE #grid2
    (
        tariffTime varchar(5),
        [description] nvarchar(256),
        rollerDurationString varchar(8),
        rollerDuration smallint,
        cellRealDuration int,
        [time] datetime,
        positionId float
    );

    INSERT INTO #grid2
    SELECT
        g1.tariffTime,
        r.name,
        dbo.fn_Int2Time(r.duration),
        r.duration,
        g1.cellRealDuration,
        g1.[time],
        i.positionId
    FROM #grid1 g1
        LEFT JOIN #issue i
            ON i.windowId = g1.windowId
           AND i.tariffMin = g1.tariffMin
        LEFT JOIN Roller r ON i.rollerID = r.rollerID;

    -------------------------------------------------------------------------
    -- final result
    -------------------------------------------------------------------------
    SELECT *
    FROM #grid2
    ORDER BY
        CASE WHEN [time] < @broadcastStart THEN 1 ELSE 0 END,
        tariffTime,
        positionId;
END
