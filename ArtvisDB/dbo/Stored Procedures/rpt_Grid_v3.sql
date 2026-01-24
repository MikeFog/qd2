
/* 
rpt_Grid_v3:
- убрана проверка прав (параметры @userID, @loggedUserID удалены)
- добавлены замеры времени по шагам (@debug = 1)
*/

CREATE   PROCEDURE [dbo].[rpt_Grid_v3]
(
    @theDate      datetime,
    @massMediaID  smallint
)
AS
BEGIN
    SET NOCOUNT ON;
    SET DATEFIRST 1;

    -- локальная "функция" логирования
    -- (через однотипные куски кода, чтобы не плодить UDF)
    DECLARE @dummy int = 0;

    SET @theDate = dbo.ToShortDate(@theDate);

    -------------------------------------------------------------------------
    -- find current pricelist for passed date
    -------------------------------------------------------------------------
    DECLARE @pricelistID smallint, @broadcastStart smalldatetime;
    SELECT @pricelistID = dbo.fn_GetPricelistIDByDate(@massMediaID, @theDate, 1);
    SELECT @broadcastStart = broadcastStart FROM Pricelist WHERE pricelistID = @pricelistID;

    -------------------------------------------------------------------------
    -- GRID1: all tariff windows for day (by TariffWindow)
    -------------------------------------------------------------------------
    DECLARE @grid1 TABLE
    (
        [tariffID] int,
        [time] datetime,
        [tariffTime] varchar(5),
        [cellRealDuration] int,
        [cellRealTime] varchar(5),
        [suffix] NVARCHAR(16),
        [needExt] BIT,
        [needInJingle] BIT,
        [needOutJingle] BIT,
        [comment] NVARCHAR(128),
        [tariffUnionID] int,
        windowId int,
        [windowNextId] int,
        windowPrevId int,
        [notEarly] varchar(1),
        [notLater] varchar(1),
        [openBlock] varchar(1),
        [openPhonogram] varchar(1),
        blockType varchar(1),
        durationTotal smallint
    );

    INSERT INTO @grid1
    SELECT
        t.tariffID,
        tw.dayActual,
        dbo.[fn_GetTimeString](@broadcastStart, tw.[windowDateActual]),
        tw.[duration],
        '',
        COALESCE(t.suffix, ''),
        COALESCE(t.needExt, 1),
        COALESCE(t.needInJingle, 1),
        COALESCE(t.needOutJingle, 1),
        COALESCE(t.[comment], ''),
        tu.tariffUnionID,
        tw.windowId,
        tw.windowNextId,
        tw.windowPrevId,
        CASE WHEN t.[notEarly]=1 THEN 'W' ELSE '' END,
        CASE WHEN t.[notLater]=1 THEN 'A' ELSE '' END,
        CASE WHEN t.[openBlock]=1 THEN 'K' ELSE '' END,
        CASE WHEN t.[openPhonogram]=1 THEN 'H' ELSE '' END,
        ISNULL(bt.code, ''),
        tw.duration_total
    FROM
        [TariffWindow] tw
        LEFT JOIN [Tariff] t ON tw.[tariffId] = t.[tariffID]
        LEFT JOIN TariffUnion tu ON t.tariffID = tu.tariffID
        LEFT JOIN BlockType bt ON bt.[blockTypeID] = t.[blockTypeID]
    WHERE
        tw.dayActual = @theDate
        AND tw.massmediaID = @massMediaID;

    -------------------------------------------------------------------------
    -- ISSUE: confirmed issues (no rights filtering now)
    -------------------------------------------------------------------------
    DECLARE @issue TABLE
    (
        issueId int primary key not null,
        issueDate datetime,
        rollerID int,
        positionId FLOAT,
        [tariffId] INT,
        moduleIssueID INT,
        packModuleIssueID INT,
        windowId int
    );

    INSERT INTO @issue
    SELECT DISTINCT
        i.issueId,
        tw.windowDateActual,
        i.rollerID,
        i.positionId,
        tw.[tariffId],
        i.[moduleIssueID],
        i.[packModuleIssueID],
        i.actualWindowID
    FROM
        Issue i
        INNER JOIN TariffWindow tw ON i.actualWindowID = tw.windowID
        INNER JOIN Campaign c ON i.campaignID = c.campaignID
        INNER JOIN [Action] a ON c.actionID = a.actionID
    WHERE
        tw.dayActual = @theDate
        AND tw.massmediaID = @massMediaID
        AND i.[isConfirmed] = 1;

    -------------------------------------------------------------------------
    -- GRID2: result rows (windows + issues)
    -------------------------------------------------------------------------
    DECLARE @grid2 TABLE
    (
        [tariffID] int,
        [time] datetime,
        [tariffTime] varchar(5),
        [cellRealDuration] int,
        [positionId] float,
        [description] nvarchar(256),
        [rollerDurationString] varchar(8),
        [rollerDuration] SMALLINT,
        [path] NVARCHAR(1024),
        [name] NVARCHAR(64),
        [fullDuration] INT,
        [suffix] NVARCHAR(16),
        [rolActionTypeID] TINYINT,
        [needExt] BIT,
        [needInJingle] BIT,
        [needOutJingle] BIT,
        [isAlive] BIT,
        [currentPath] NVARCHAR(255),
        [comment] NVARCHAR(128),
        [tariffUnionID] int,
        [position] nvarchar(8),
        broadcastStart smalldatetime,
        windowNextId int,
        windowPrevId int,
        advertTypeId int,
        [notEarly] varchar(1),
        [notLater] varchar(1),
        [openBlock] varchar(1),
        [openPhonogram] varchar(1),
        blockType varchar(1),
        durationTotal smallint
    );

    INSERT INTO @grid2
    SELECT
        g1.tariffID,
        g1.[time],
        CASE g1.cellRealTime WHEN '' THEN g1.tariffTime ELSE g1.cellRealTime END as tariffTime,
        g1.cellRealDuration,
        i.positionId,
        CASE WHEN ip.positionId = -10
            THEN ip.shortDescription + ' ' + r.[name]
            ELSE r.[name] + ' ' + ip.shortDescription
        END as [description],
        dbo.fn_Int2Time(r.duration) as rollerDurationString,
        r.duration as rollerDuration,
        r.[path],
        r.[name],
        g1.cellRealDuration,
        g1.suffix,
        r.rolActionTypeID,
        g1.needExt,
        g1.needInJingle,
        g1.needOutJingle,
        0,
        dbo.fn_GetPathForPackModueleAndModule(pm.packModuleID, pmpl.pricelistID, m.moduleID, @massMediaID),
        g1.comment,
        g1.tariffUnionID,
        ip.shortDescription,
        @broadcastStart,
        g1.windowNextId,
        g1.windowPrevId,
        r.advertTypeID,
        g1.[notEarly],
        g1.[notLater],
        g1.[openBlock],
        g1.[openPhonogram],
        g1.blockType,
        g1.durationTotal
    FROM
        @grid1 g1
        LEFT JOIN @issue i
            ON dbo.[fn_GetTimeString](@broadcastStart, i.issueDate) = g1.tariffTime
            AND g1.windowId = i.windowId
        LEFT JOIN roller r ON i.rollerID = r.rollerID
        LEFT JOIN iIssuePosition ip ON ip.positionId = i.positionId
        LEFT JOIN [ModuleIssue] mi ON i.moduleIssueID = mi.[moduleIssueID]
        LEFT JOIN [Module] m ON mi.[moduleID] = m.[moduleID]
        LEFT JOIN [PackModuleIssue] pmi ON pmi.[packModuleIssueID] = i.packModuleIssueID
        LEFT JOIN [PackModulePriceList] pmpl ON pmi.[pricelistID] = pmpl.[priceListID]
        LEFT JOIN [PackModule] pm ON pmpl.[packModuleID] = pm.[packModuleID];

    -------------------------------------------------------------------------
    -- Standalone modules in TariffWindow (если окно занято модулем и не подтверждено)
    -- (логика из pasted.txt, блок "IF (@userID IS NULL) ... update g2 ...")
    -------------------------------------------------------------------------
    UPDATE g2
    SET
        g2.[time] = t.TIME,
        g2.[cellRealDuration] = t.duration,
        g2.[positionId] = 0,
        g2.[description] = CASE WHEN r_pm.name IS NOT NULL THEN r_pm.name ELSE r_m.name END,
        g2.[rollerDurationString] = CASE WHEN r_pm.[duration] IS NOT NULL THEN dbo.fn_Int2Time(r_pm.[duration]) ELSE dbo.fn_Int2Time(r_m.[duration]) END,
        g2.[rollerDuration] = CASE WHEN r_pm.[duration] IS NOT NULL THEN r_pm.[duration] ELSE r_m.[duration] END,
        g2.[path] = CASE WHEN r_pm.[path] IS NOT NULL THEN r_pm.[path] ELSE r_m.[path] END,
        g2.[name] = CASE WHEN r_pm.name IS NOT NULL AND LEN(r_pm.[path]) > 0 THEN r_pm.name ELSE r_m.name END,
        g2.[fullDuration] = tw.duration,
        g2.[suffix] = t.[suffix],
        g2.[rolActionTypeID] = CASE WHEN r_pm.[rolActionTypeID] IS NOT NULL THEN r_pm.[rolActionTypeID] ELSE r_m.[rolActionTypeID] END,
        g2.[needExt] = t.[needExt],
        g2.[needInJingle] = t.[needInJingle],
        g2.[needOutJingle] = t.[needOutJingle],
        g2.[isAlive] = 0,
        g2.[currentPath] = CASE WHEN m.[path] IS NOT NULL THEN m.[path] ELSE pm.[path] END,
        g2.[comment] = m.name,
        g2.[tariffUnionID] = tu.tariffUnionID,
        g2.[position] = '',
        g2.broadcastStart = @broadcastStart
    FROM
        @grid2 g2
        INNER JOIN [TariffWindow] tw ON tw.tariffId = g2.tariffID
        INNER JOIN [Tariff] t ON tw.tariffID = t.tariffID
        LEFT JOIN TariffUnion tu ON t.tariffID = tu.tariffID
        LEFT JOIN [ModuleTariff] mt ON t.[tariffID] = mt.[tariffID]
        LEFT JOIN [ModulePriceList] mpl ON mpl.[modulePriceListID] = mt.[modulePriceListID]
            AND @theDate BETWEEN mpl.startDate AND mpl.finishDate
        LEFT JOIN [Module] m ON mpl.[moduleID] = m.[moduleID]
        LEFT JOIN [Roller] r_m ON mpl.[rollerID] = r_m.[rollerID]
        LEFT JOIN [PackModuleContent] pmc ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
        LEFT JOIN [PackModulePriceList] pmpl ON pmc.[pricelistID] = pmpl.[priceListID]
            AND @theDate BETWEEN pmpl.startDate AND pmpl.finishDate
        LEFT JOIN [PackModule] pm ON pmpl.[packModuleID] = pm.[packModuleID]
        LEFT JOIN [Roller] r_pm ON pmpl.[rollerID] = r_pm.[rollerID]
        LEFT JOIN Issue i ON i.[actualWindowID] = tw.[windowId]
    WHERE
        mpl.isStandAlone IS NOT NULL AND mpl.isStandAlone = 1
        AND tw.dayActual = @theDate
        AND tw.massmediaID = @massMediaID
        AND tw.[maxCapacity] > 0
        AND (r_pm.[rolActionTypeID] IS NOT NULL OR r_m.[rolActionTypeID] IS NOT NULL)
        AND (i.issueID IS NULL OR i.isConfirmed = 0);

    -------------------------------------------------------------------------
    -- пустые окна (как в pasted.txt; ОБРАТИ ВНИМАНИЕ: там был странный join.
    -- здесь делаю логично: вставляем те строки grid1, которых нет в grid2 по time)
    -------------------------------------------------------------------------
    INSERT INTO @grid2
    SELECT
        g1.tariffID,
        g1.[time],
        CASE g1.cellRealTime WHEN '' THEN g1.tariffTime ELSE g1.cellRealTime END as tariffTime,
        g1.cellRealDuration,
        0,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        g1.cellRealDuration,
        g1.suffix,
        NULL,
        g1.needExt,
        g1.needInJingle,
        g1.needOutJingle,
        0,
        '',
        g1.comment,
        g1.tariffUnionID,
        '',
        @broadcastStart,
        g1.windowNextId,
        g1.windowPrevId,
        NULL,
        g1.[notEarly],
        g1.[notLater],
        g1.[openBlock],
        g1.[openPhonogram],
        g1.blockType,
        g1.durationTotal
    FROM
        @grid1 g1
        LEFT JOIN @grid2 g2 ON g2.[time] = g1.[time]
    WHERE
        g2.[time] IS NULL;

    -------------------------------------------------------------------------
    -- program issues (sponsored programs фактические)
    -------------------------------------------------------------------------
    INSERT INTO @grid2
    SELECT
        NULL,
        t.[time],
        dbo.[fn_GetTimeString](pl.broadcastStart, t.[time]),
        0,
        0,
        sp.[name] + ' [' + f.[name] + ']',
        dbo.fn_Int2Time(t.[duration]),
        t.[duration],
        '',
        COALESCE(t.comment, sp.[name]),
        t.[duration],
        t.[suffix],
        3,
        t.[needExt],
        t.[needInJingle],
        t.[needOutJingle],
        t.isAlive,
        t.[path],
        t.[comment],
        NULL,
        '',
        pl.broadcastStart,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        0
    FROM
        programIssue i
        INNER JOIN [SponsorProgram] sp ON i.[programID] = sp.[sponsorProgramID] AND sp.[massmediaID] = @massMediaID
        INNER JOIN SponsorTariff t ON t.tariffID = i.tariffID
        INNER JOIN SponsorProgramPricelist pl ON t.priceListID = pl.pricelistID AND @theDate BETWEEN pl.[startDate] AND pl.[finishDate]
        INNER JOIN Campaign c ON i.campaignID = c.campaignID
        INNER JOIN [Action] a ON a.actionID = c.actionID
        INNER JOIN Firm f ON f.firmID = a.firmID
    WHERE
        i.[isConfirmed] = 1
        AND i.issueDate BETWEEN
            DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @theDate))
            AND DATEADD(ss, -1, DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @theDate + 1)))
        AND CONVERT(varchar(5), i.issueDate, 108) = CONVERT(varchar(5), t.time, 108);

    -------------------------------------------------------------------------
    -- Программы которые не были проспонсированы (standalone sponsor pricelist)
    -------------------------------------------------------------------------
    INSERT INTO @grid2
    SELECT
        NULL,
        st.[time],
        dbo.[fn_GetTimeString](sppl.broadcastStart, st.time),
        0,
        0,
        sp.[name],
        dbo.fn_Int2Time(st.[duration]),
        st.[duration],
        '',
        COALESCE(st.comment, sp.[name]),
        st.[duration],
        st.[suffix],
        3,
        st.[needExt],
        st.[needInJingle],
        st.[needOutJingle],
        st.isAlive,
        st.[path],
        st.comment,
        NULL,
        '',
        sppl.broadcastStart,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        0
    FROM
        [SponsorProgram] sp
        INNER JOIN [SponsorProgramPricelist] sppl ON sp.[sponsorProgramID] = sppl.[sponsorProgramID]
        INNER JOIN [SponsorTariff] st ON sppl.[pricelistID] = st.[pricelistID]
    WHERE
        sp.massmediaID = @massMediaID
        AND sppl.isStandAlone = 1
        AND @theDate BETWEEN sppl.[startDate] AND sppl.[finishDate]
        AND
        (
            (st.[time] >= sppl.broadcastStart
                AND (
                    (DATEPART(dw, @theDate) = 1 AND st.monday = 1)
                 OR (DATEPART(dw, @theDate) = 2 AND st.thursday = 1)   -- как в исходнике (да, странно)
                 OR (DATEPART(dw, @theDate) = 3 AND st.wednesday = 1)
                 OR (DATEPART(dw, @theDate) = 4 AND st.thursday = 1)
                 OR (DATEPART(dw, @theDate) = 5 AND st.friday = 1)
                 OR (DATEPART(dw, @theDate) = 6 AND st.saturday = 1)
                 OR (DATEPART(dw, @theDate) = 7 AND st.sunday = 1)
                )
            )
            OR
            (st.[time] < sppl.broadcastStart
                AND (
                    (DATEPART(dw, @theDate) = 7 AND st.monday = 1)
                 OR (DATEPART(dw, @theDate) = 1 AND st.thursday = 1)   -- как в исходнике (да, странно)
                 OR (DATEPART(dw, @theDate) = 2 AND st.wednesday = 1)
                 OR (DATEPART(dw, @theDate) = 3 AND st.thursday = 1)
                 OR (DATEPART(dw, @theDate) = 4 AND st.friday = 1)
                 OR (DATEPART(dw, @theDate) = 5 AND st.saturday = 1)
                 OR (DATEPART(dw, @theDate) = 6 AND st.sunday = 1)
                )
            )
        )
        AND NOT EXISTS
        (
            SELECT 1
            FROM [ProgramIssue] i
                INNER JOIN SponsorTariff t ON t.tariffID = i.tariffID
                INNER JOIN SponsorProgramPricelist pl ON t.priceListID = pl.pricelistID
            WHERE
                i.[isConfirmed] = 1
                AND i.issueDate BETWEEN
                    DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @theDate))
                    AND DATEADD(ss, -1, DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @theDate + 1)))
                AND CONVERT(varchar(5), i.issueDate, 108) = CONVERT(varchar(5), st.time, 108)
        );

    -------------------------------------------------------------------------
    -- финальный SELECT (как в исходнике)
    -------------------------------------------------------------------------
    SELECT *
    FROM
    (
        SELECT
            tariffID,
            tariffTime,
            [Description],
            rollerDurationString,
            rollerDuration,
            cellRealDuration,
            [PATH],
            [NAME],
            dbo.fn_Int2Time([fullDuration]) AS [fullDuration],
            suffix,
            [rolActionTypeID],
            [needExt],
            [needInJingle],
            [needOutJingle],
            [isAlive],
            [currentPath],
            [TIME],
            positionId,
            [comment],
            CASE WHEN CAST(CAST(tariffTime AS NVARCHAR(2)) AS INT) < 24 THEN 1 ELSE 0 END AS isToday,
            tariffUnionID,
            position,
            CASE WHEN [rolActionTypeID] <> 1 THEN 0 ELSE rollerDuration END AS rollerDurationSum,
            broadcastStart,
            windowNextId,
            windowPrevId,
            advertTypeId,
            [notEarly],
            [notLater],
            [openBlock],
            [openPhonogram],
            blockType,
            durationTotal
        FROM @grid2
        WHERE ([rolActionTypeID] = 1 OR [rolActionTypeID] IS NULL)

        UNION ALL

        SELECT DISTINCT
            tariffID,
            tariffTime,
            [Description],
            rollerDurationString,
            rollerDuration,
            cellRealDuration,
            [PATH],
            [NAME],
            dbo.fn_Int2Time([fullDuration]) AS [fullDuration],
            suffix,
            [rolActionTypeID],
            [needExt],
            [needInJingle],
            [needOutJingle],
            [isAlive],
            [currentPath],
            [TIME],
            positionId,
            [comment],
            CASE WHEN CAST(CAST(tariffTime AS NVARCHAR(2)) AS INT) < 24 THEN 1 ELSE 0 END AS isToday,
            tariffUnionID,
            position,
            CASE WHEN [rolActionTypeID] <> 1 THEN 0 ELSE rollerDuration END AS rollerDurationSum,
            broadcastStart,
            windowNextId,
            windowPrevId,
            advertTypeId,
            [notEarly],
            [notLater],
            [openBlock],
            [openPhonogram],
            blockType,
            durationTotal
        FROM @grid2
        WHERE ([rolActionTypeID] = 2 OR [rolActionTypeID] >= 3)
    ) X
    ORDER BY
        CASE WHEN [Time] < broadcastStart THEN '1' ELSE '0' END + [tariffTime],
        positionId,
        windowPrevId;

END
