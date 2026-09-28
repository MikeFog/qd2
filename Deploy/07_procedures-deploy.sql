/*
    ПРОД-ДЕПЛОЙ 07_procedures-deploy.sql
    Процедуры: сетка вещания, веер, медиаплан (этап 1), защита порядка объединённых окон.
    КОГДА: в любое время; совместимо со старым и новым клиентом.

    Склеено из ArtvisDB/Scripts (части ниже — без изменений, каждая со своей шапкой):
      - rpt-grid-manager-filter-deploy.sql
      - veer-modular-same-action-deploy.sql
      - mediaplan-stage1-deploy.sql
      - window-chain-guards-deploy.sql
    Запуск: sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 07_procedures-deploy.sql
    (-b — остановка на первой ошибке; части идемпотентны, повторный запуск безопасен)
*/

-- ============================================================================
-- ЧАСТЬ: rpt-grid-manager-filter-deploy.sql
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
/*
    ПРОД-ДЕПЛОЙ: dbo.rpt_Grid_v3 — вернуть фильтр «Менеджер» сетки вещания (@userID).

    ПОВОД
      Фильтр «Менеджер» на форме «Сетка вещания» не работал: клиент передаёт userID,
      но у rpt_Grid_v3 параметра нет (снят вместе с проверкой прав), и DataAccessor
      молча отбрасывает неизвестные параметры. Сетка всегда показывала всех.

    ПРАВКА
      @userID smallint = NULL, логика как в старой rpt_Grid:
        - выпуски роликов и спонсорских программ — только акций этого менеджера;
        - «ничьи» строки (модули без выпуска, непроспонсированные программы) —
          только без отбора.
      NULL — поведение не меняется (выгрузка для эфира менеджера не передаёт).
      Проверку прав (@loggedUserID) НЕ возвращаем — её сняли намеренно.

    ОПЦИИ   QUOTED_IDENTIFIER ON / ANSI_NULLS ON — как у процедуры на проде.
    КЛИЕНТ  совместимо со старым клиентом (новый параметр необязательный).
            Новый клиент (GridReportCreator) не передаёт менеджера при выгрузке.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO


/* 
rpt_Grid_v3:
- убрана проверка прав (параметр @loggedUserID удалён)
- добавлены замеры времени по шагам (@debug = 1)
- @userID (2026-09-25): фильтр «Менеджер» сетки вещания. При удалении проверки прав
  пропал вместе с ней, и сетка всегда показывала выпуски всех менеджеров. Вернули
  как в rpt_Grid: только выпуски и спонсорские программы акций этого менеджера,
  без «ничьих» строк (модули без выпуска, непроспонсированные программы).
  NULL — все, как раньше; выгрузка для эфира его не передаёт.
*/

CREATE OR ALTER PROCEDURE [dbo].[rpt_Grid_v3]
(
    @theDate      datetime,
    @massMediaID  smallint,
    @userID       smallint = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; -- Добавляем сюда
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
        AND i.[isConfirmed] = 1
        AND (@userID IS NULL OR a.userID = @userID);

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
        r.[name] + ' ' + ip.shortDescription as [description],
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
        @userID IS NULL -- модуль без выпуска ничей: при отборе по менеджеру не показываем
        AND mpl.isStandAlone IS NOT NULL AND mpl.isStandAlone = 1
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
        COALESCE(NULLIF(t.comment, ''), sp.[name]),
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
        AND CONVERT(varchar(5), i.issueDate, 108) = CONVERT(varchar(5), t.time, 108)
        AND (@userID IS NULL OR a.userID = @userID);

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
        COALESCE(NULLIF(st.comment, ''), sp.[name]),
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
        @userID IS NULL -- непроспонсированная программа ничья: при отборе по менеджеру не показываем
        AND sp.massmediaID = @massMediaID
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
        -- Тип 6 (политическая агитация) - обычный оплаченный ролик: он может стоять
        -- в окне несколько раз, поэтому идёт в ветку БЕЗ DISTINCT. Иначе два
        -- одинаковых ролика кандидата в одном окне схлопывались бы в один выход
        WHERE ([rolActionTypeID] = 1 OR [rolActionTypeID] = 6 OR [rolActionTypeID] IS NULL)

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
        -- DISTINCT здесь защищает служебные строки (новости, программы,
        -- идентификаторы СМИ, анонс агитации) - они по одной на окно
        WHERE ([rolActionTypeID] = 2 OR ([rolActionTypeID] >= 3 AND [rolActionTypeID] <> 6))
    ) X
    ORDER BY
        CASE WHEN [Time] < broadcastStart THEN '1' ELSE '0' END + [tariffTime],
        positionId,
        windowPrevId;

END
GO

-- ============================================================================
-- ЧАСТЬ: veer-modular-same-action-deploy.sql
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
/*
    ПРОД-ДЕПЛОЙ: dbo.TariffWindowWithRange — модульные кампании своей акции в веере.

    ПОВОД
      Если в акции есть и линейная, и модульная кампании на одной станции, выпуски
      модульной в веере не подсвечивались: в веер они не входят (только линейные),
      а бирюзовый/оранжевый считался только по ЧУЖИМ акциям фирмы (a.actionID <> @actionID).
      Риск поставить ролик заказчика в тот же блок, где уже стоит его модульный.

    ПРАВКА
      П.7 (флаги HasIssues*) и п.9 (подсказка + номера роликов): своя акция тоже
      учитывается, но только нелинейные кампании — (a.actionID <> @actionID OR c.campaignTypeID <> 1).
      Цвет — существующий бирюзовый/оранжевый; синий/красный не меняются.
      Подсказка в клиенте подписывает свою акцию «Модульная кампания этой акции».
      ArtvisDev, акция 186277: 28–33 слота в неделю стали оранжевыми вместо пустых,
      остальные слоты без изменений.

    ОПЦИИ   QUOTED_IDENTIFIER OFF / ANSI_NULLS ON — как у процедуры на проде (копия Artvis).
    КЛИЕНТ  совместимо со старым клиентом (набор колонок не менялся): старый клиент
            просто покрасит слоты, а в подсказке покажет свою акцию как «Акция №…».
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER OFF;
GO

ALTER PROCEDURE [dbo].[TariffWindowWithRange]
(
    @actionID  int,
    @dateStart datetime,
    -- Список кампаний акции (CSV campaignID), с которыми работает веер. NULL/пусто —
    -- все линейные кампании акции (прежнее поведение для вызовов без выбора).
    @campaignIDs varchar(max) = NULL,
    -- Фильтр «Предметы рекламы» веера (TariffWithRangeGrid.SetAdvertTypePresence).
    -- NULL — флаги HasAdvertType* не считаются (остаются 0).
    @advertTypeID smallint = NULL
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
    -- Веер работает только с линейными кампаниями (campaignTypeID = 1): выпуск ставится
    -- точечно в конкретное рекламное окно. Модульные (3), спонсорские (2) и пакетно-модульные (4)
    -- размещаются по модулям/программам с собственным ценообразованием и в веер попадать не должны.
    -- Пользователь может ограничить веер частью линейных кампаний акции (@campaignIDs):
    -- сетка, добавление и удаление идут только по ним. #sc — выбранные кампании,
    -- #mm — их СМИ (по одной станции может идти несколько кампаний, различающихся
    -- типом оплаты и агентством, — см. UIX_Campaign).
    SELECT c.campaignID, c.massmediaID
    INTO #sc
    FROM dbo.Campaign c
    WHERE c.actionID = @actionID
      AND c.massmediaID IS NOT NULL
      AND c.campaignTypeID = 1
      AND (@campaignIDs IS NULL
           OR c.campaignID IN (SELECT CONVERT(int, value) FROM STRING_SPLIT(@campaignIDs, ',')));
    CREATE UNIQUE CLUSTERED INDEX CX_sc ON #sc(campaignID);

    SELECT DISTINCT sc.massmediaID
    INTO #mm
    FROM #sc sc;
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
        HasIssuesThisActionAllCampaigns  bit NOT NULL DEFAULT 0,
        HasAdvertType                    bit NOT NULL DEFAULT 0,
        HasAdvertTypeUnconfirmed         bit NOT NULL DEFAULT 0,
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
        -- Раскладка веера — по фактическому времени выхода (windowDateActual).
        tw.windowDateActual,
        windowDay = CONVERT(datetime, CONVERT(varchar(8), tw.windowDateActual, 112), 112),
        tw.duration,
        tw.timeInUseConfirmed,
        tw.timeInUseUnconfirmed,
        tw.isFirstPositionOccupied,
        tw.isSecondPositionOccupied,
        tw.isLastPositionOccupied,
        tw.firstPositionsUnconfirmed,
        tw.secondPositionsUnconfirmed,
        tw.lastPositionsUnconfirmed,
        -- Цена берётся из самого рекламного окна, а не из тарифа: при генерации
        -- окон цену конкретного окна могли изменить, и прайм-тайм считается по ней.
        tw.price,
        isPrime = CONVERT(bit, 0)
    INTO #tw
    FROM dbo.TariffWindow tw
    JOIN #mm m ON m.massmediaID = tw.massmediaID
    JOIN dbo.Tariff t ON t.tariffID = tw.tariffID AND t.isForModuleOnly = 0
    WHERE tw.maxCapacity = 0
      AND tw.isDisabled = 0
      AND tw.windowDateActual >= @minDate
      AND tw.windowDateActual <= @maxEnd;
    CREATE INDEX IX_tw_mm_date ON #tw(massmediaID, windowDateActual);
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
            isFirstPositionOccupied    = MAX(CONVERT(int, tw.isFirstPositionOccupied)),
            isSecondPositionOccupied   = MAX(CONVERT(int, tw.isSecondPositionOccupied)),
            isLastPositionOccupied     = MAX(CONVERT(int, tw.isLastPositionOccupied)),
            firstPositionsUnconfirmed  = MIN(tw.firstPositionsUnconfirmed),
            secondPositionsUnconfirmed = MIN(tw.secondPositionsUnconfirmed),
            lastPositionsUnconfirmed   = MIN(tw.lastPositionsUnconfirmed),
            -- Прайм внутри одного СМИ: все окна, попавшие в получасовой блок, должны быть праймовыми
            isPrime = MIN(CONVERT(int, tw.isPrime))
        FROM #res r
        JOIN #tw tw ON tw.windowDateActual BETWEEN r.[date] AND r.[enddate]
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
    --    Выпуски фирмы вне веера: чужие акции и модульные/пакетно-модульные кампании
    --    своей акции (их выпуски тоже стоят в TariffWindow — риск пересечения в блоке).
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
            ON tw.windowDateActual BETWEEN r.[date] AND r.[enddate]
        JOIN #mm m
            ON m.massmediaID = tw.massmediaID
        JOIN dbo.Issue i
            ON i.actualWindowID = tw.windowId
        JOIN dbo.Campaign c
            ON c.campaignID = i.campaignID
        JOIN dbo.Action a
            ON a.actionID  = c.actionID
           AND a.firmID    = @firmID
           AND (a.actionID <> @actionID OR c.campaignTypeID <> 1)
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
    --    HasIssuesThisActionAllCampaigns — выпуск акции есть у КАЖДОЙ выбранной
    --    кампании (ролик и позиция любые, у каждой кампании свои) — синий цвет в сетке.
    --------------------------------------------------------------------
    DECLARE @scCnt int = (SELECT COUNT(*) FROM #sc);
    ;WITH this_action_issues AS
    (
        SELECT r.[date], campaignCnt = COUNT(DISTINCT i.campaignID)
        FROM #res r
        JOIN #tw tw
            ON tw.windowDateActual BETWEEN r.[date] AND r.[enddate]
        JOIN dbo.Issue i
            ON i.actualWindowID = tw.windowId
        JOIN #sc sc
            ON sc.campaignID = i.campaignID
        GROUP BY r.[date]
    )
    UPDATE r SET
        r.HasIssuesThisAction = CONVERT(bit, 1),
        r.HasIssuesThisActionAllCampaigns = CONVERT(bit, CASE WHEN x.campaignCnt = @scCnt THEN 1 ELSE 0 END)
    FROM #res r
    JOIN this_action_issues x ON x.[date] = r.[date];
    --------------------------------------------------------------------
    -- 8а) Предмет рекламы: хотя бы на ОДНОЙ станции веера в получасе есть выпуск
    --     ролика с этим ПР (или дочерним — как TariffWindowWithAdvertTypeRetrieve
    --     у линейной сетки). «Есть ПР» подсвечивает такие слоты, «Нет ПР» — остальные,
    --     т.е. где ПР нет ни на одной станции. Выпуски любых акций и фирм.
    --     HasAdvertType — только подтверждённые, HasAdvertTypeUnconfirmed — любые.
    --------------------------------------------------------------------
    IF @advertTypeID IS NOT NULL
    BEGIN
        -- Сначала окна недели с такими выпусками (по IX_TariffWindow_ActualDate), и
        -- только их — по получасам: range-join всей недели с #res стоил ~1 с.
        SELECT tw.windowDateActual, hasConfirmed = MAX(CONVERT(int, i.isConfirmed))
        INTO #advWindows
        FROM #mm m
        JOIN dbo.TariffWindow tw
            ON tw.massmediaID = m.massmediaID
           AND tw.windowDateActual BETWEEN @minDate AND @maxEnd
        JOIN dbo.Issue i
            ON i.actualWindowID = tw.windowId
        JOIN dbo.Roller rl
            ON rl.rollerID = i.rollerID
        LEFT JOIN dbo.AdvertType adt
            ON adt.advertTypeID = rl.advertTypeID
        WHERE rl.advertTypeID = @advertTypeID OR adt.parentID = @advertTypeID
        GROUP BY tw.windowDateActual
        OPTION (RECOMPILE); -- без него план по переменным @minDate/@maxEnd ~0,9 с вместо ~50 мс

        UPDATE r SET
            r.HasAdvertType            = CONVERT(bit, x.hasConfirmed),
            r.HasAdvertTypeUnconfirmed = CONVERT(bit, 1)
        FROM #res r
        CROSS APPLY
        (
            SELECT hasConfirmed = MAX(w.hasConfirmed)
            FROM #advWindows w
            WHERE w.windowDateActual BETWEEN r.[date] AND r.[enddate]
            HAVING COUNT(*) > 0
        ) x;
    END

    --------------------------------------------------------------------
    -- 9) Чужие акции той же фирмы по датам (подсказка бирюзовых/оранжевых
    --    ячеек — TariffWithRangeGrid.GetOtherFirmActions). Тот же джойн, что
    --    и all_issues в п.7 (не тянем ещё раз в базу отдельным запросом на
    --    ховер), но сгруппирован по ([date], actionID), а не только по [date].
    --    Из того же #otherIssues идёт и последний набор (п.10) — ролики чужих
    --    акций фирмы по слотам: в режиме номеров роликов бирюзовые/оранжевые
    --    ячейки показывают номера наравне со своими (ролики фирменные, номер берётся
    --    из той же карты).
    --    Как и в п.7, сюда же попадает своя акция (actionID = @actionID) — выпуски
    --    её модульных кампаний; C# подписывает её в подсказке отдельно.
    --------------------------------------------------------------------
    SELECT
        r.[date],
        a.actionID,
        i.rollerID,
        i.positionId,
        ownerName = u.lastName + ' ' + u.firstName,
        isConfirmed = CONVERT(int, i.isConfirmed)
    INTO #otherIssues
    FROM #res r
    JOIN dbo.TariffWindow tw
        ON tw.windowDateActual BETWEEN r.[date] AND r.[enddate]
    JOIN #mm m
        ON m.massmediaID = tw.massmediaID
    JOIN dbo.Issue i
        ON i.actualWindowID = tw.windowId
    JOIN dbo.Campaign c
        ON c.campaignID = i.campaignID
    JOIN dbo.Action a
        ON a.actionID  = c.actionID
       AND a.firmID    = @firmID
       AND (a.actionID <> @actionID OR c.campaignTypeID <> 1)
    LEFT JOIN dbo.[User] u
        ON u.userID = a.userID
    WHERE i.isConfirmed = 1
       OR a.deleteDate IS NULL;

    SELECT
        [date],
        actionID,
        ownerName = MAX(ownerName),
        hasConfirmed = MAX(CASE WHEN isConfirmed = 1 THEN 1 ELSE 0 END)
    INTO #otherActions
    FROM #otherIssues
    GROUP BY [date], actionID;
    --------------------------------------------------------------------
    -- 10) Возвраты  ← только после всех UPDATE
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
        r.HasIssuesThisActionAllCampaigns,
        r.HasAdvertType, r.HasAdvertTypeUnconfirmed,
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
                  AND DATEPART(minute, @minBroadcast) <= DATEPART(minute, r.[date]))  -- ← исправлено
            THEN 0 ELSE 1 END),
        DATEPART(hour, r.[date]),
        DATEPART(minute, r.[date]);
    SELECT [date], actionID, ownerName, hasConfirmed FROM #otherActions;
    SELECT
        [date],
        rollerID,
        positionId,
        hasConfirmed = MAX(isConfirmed)
    FROM #otherIssues
    GROUP BY [date], rollerID, positionId
    ORDER BY [date], rollerID, positionId;
END
GO

-- ============================================================================
-- ЧАСТЬ: mediaplan-stage1-deploy.sql
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
/*
    ДЕПЛОЙ: медиаплан, этап 1 плана docs/tasks/web-mediaplan.md (чистка десктопа).

    1) dbo.GetUniqueMMsForPackModuleCampaign — строка на станцию (GROUP BY,
       порядок первого выпуска) вместо строки на каждый выпуск. Единственный
       вызыватель — Client\Classes\CampaignPackModule.cs (GetUniqueMassmedias).
       Колонки date/rollerID сохранены (MIN), поэтому совместимо и со старым
       клиентом, и с новым — порядок накатки клиента и скрипта не важен.
       Сверка на ArtvisDev: 299 пакетных кампаний, набор (campaignID,
       massmediaID, name) совпадает, 0 расхождений; строк 37169 -> 1483.

    2) dbo.MediaPlanRetrieve (v1) — удаляется. Из кода не вызывается (клиент зовёт
       только MediaPlanRetrieve_v2), зависимостей в sys.sql_expression_dependencies
       и строк в iStoredProcedure нет (проверено на Artvis и ArtvisDev).
*/
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

ALTER PROCEDURE [dbo].[GetUniqueMMsForPackModuleCampaign]
(
	@campaignID int,
	@isFact bit = 1
)
AS
begin
SET NOCOUNT on
	-- Станции пакетной кампании, по строке на станцию, в порядке первого выпуска.
	-- Раньше отдавала строку на КАЖДЫЙ выпуск, а клиент всё равно сводил их к
	-- списку станций. date/rollerID клиент больше не читает; оставлены (MIN) для
	-- совместимости со старыми клиентами (до этапа 1 медиаплана), которые их парсят.
	SELECT
		mm.[massmediaID], mm.[name],
		MIN(CASE WHEN @isFact = 1 THEN tw.windowDateActual ELSE tw.windowDateOriginal END) AS [date],
		MIN(i.[rollerID]) AS [rollerID]
	FROM
		Issue i
		inner join TariffWindow tw on tw.windowId = CASE WHEN @isFact = 1 THEN i.actualWindowID ELSE i.originalWindowID END
		INNER JOIN [vMassmedia] mm ON tw.[massmediaID] = mm.[massmediaID]
	WHERE
		i.campaignID = @campaignID
	GROUP BY mm.[massmediaID], mm.[name]
	ORDER BY MIN(i.issueID)
END
GO

IF OBJECT_ID(N'dbo.MediaPlanRetrieve', N'P') IS NOT NULL
    DROP PROCEDURE dbo.MediaPlanRetrieve;
GO

-- ============================================================================
-- ЧАСТЬ: window-chain-guards-deploy.sql
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
/*
    ПРОД-ДЕПЛОЙ: запрет переноса объединённых окон в неправильный порядок.
    Справочник и разбор слабых мест — docs/window-merging.md §3.

    ЗАЧЕМ
      Объединённые окна трафика (windowPrevId/windowNextId) — это непрерывный
      кусок эфира из нескольких окон подряд. «Голова» цепочки в эфире идёт раньше
      «хвоста». Перенос времени выхода (windowDateActual) раньше ничего про
      объединение не знал, и окно-«голову» можно было передвинуть на время позже
      окна-«хвоста» — или наоборот. Порядок в эфире ломался, а это молча портило:
        - DJin-выгрузку: пара выходила двумя отдельными блоками в обратном
          порядке (хвост без заголовка блока и входного джингла, суммирование
          длительности не находило партнёра);
        - обвязку политагитации: локальный (44) и федеральный (55) идентификаторы
          СМИ звучали в обратном порядке.

    ЧТО ДЕЛАЕТ ЭТОТ СКРИПТ (идемпотентно)
      1. TariffWindowMoveTime (шаблонный перенос из сетки трафика) — перед
         переносом проверяет, что порядок каждой затронутой цепочки по БУДУЩЕМУ
         фактическому времени сохранится; иначе RAISERROR('LinkedWindowsWrongOrder').
      2. TariffWindowIUD @actionName='UpdateItem' (правка одного окна через паспорт
         «Свойства» → «Время выхода реальное»; там же пишутся связи при
         объединении/отмене объединения) — та же проверка порядка. Срабатывает
         только при реальном изменении времени ИЛИ при установке связи; правки
         isDisabled/price/продолжительности объединение не задевают.
      3. iMessage: текст кода ошибки LinkedWindowsWrongOrder.

      Тело TariffWindowIUD — из master после слияния (28.09.2026): вместе с проверкой
      объединённых окон в нём защита DurationExceedsTotal (6a2afc0, уже на проде).
      Прежняя сборка скрипта её не содержала и сняла бы при накате.

    ЧЕГО НЕ ДЕЛАЕТ
      - НЕ запрещает менять продолжительность объединённого окна (обсуждается с
        заказчиком).
      - НЕ запрещает объединять окна, если тариф уже «тариф-продолжение»
        (обсуждается с заказчиком).
      Не переделывает механизм. Существующие рассинхроны в данных не лечит.

    ОТКАТ
      Прежние версии процедур — в git до этого коммита. Строку iMessage можно
      оставить (безвредна) либо удалить по name.
*/

SET NOCOUNT ON;
GO

-- Код ошибки продолжительности из ранней версии этого скрипта больше не нужен.
IF EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'CannotChangeDurationOfLinkedWindow')
    DELETE FROM [dbo].[iMessage] WHERE name = 'CannotChangeDurationOfLinkedWindow';
GO

-- =====================================================================
-- 1. TariffWindowMoveTime
-- =====================================================================
GO
CREATE OR ALTER PROCEDURE [dbo].[TariffWindowMoveTime]
(
    @time datetime,
    @newtime datetime,
    @startdate datetime,
    @finishdate datetime,
    @pricelistid int,
    @monday bit = 0,
    @tuesday bit = 0,
    @wednesday bit = 0,
    @thursday bit = 0,
    @friday bit = 0,
    @saturday bit = 0,
    @sunday bit = 0
)
AS
BEGIN
    SET NOCOUNT ON;
    SET DATEFIRST 1; -- Понедельник = 1

    DECLARE @needaddday bit

    IF EXISTS(
        SELECT *
        FROM Pricelist pl
        WHERE pl.PricelistID = @pricelistID
            AND @time < pl.broadcastStart
    )
        SET @needaddday = 1
    ELSE
        SET @needaddday = 0

    -- Окна под перенос + их будущее фактическое время выхода.
    DECLARE @moved TABLE (windowId int PRIMARY KEY, newActual datetime NOT NULL);

    INSERT INTO @moved (windowId, newActual)
    SELECT
        tw.windowId,
        CONVERT(datetime,
            LEFT(CONVERT(varchar, CASE @needaddday WHEN 1 THEN DATEADD(day, 1, tw.dayOriginal) ELSE tw.dayOriginal END, 120), 11)
            + RIGHT(CONVERT(varchar, @newtime, 120), 8),
        120)
    FROM TariffWindow tw
        INNER JOIN Pricelist pl ON tw.massmediaID = pl.massmediaID
            AND pl.pricelistID = @pricelistid
    WHERE tw.dayOriginal BETWEEN @startdate AND @finishdate
        AND tw.windowDateOriginal = CONVERT(datetime,
                LEFT(CONVERT(varchar, CASE @needaddday WHEN 1 THEN DATEADD(day, 1, tw.dayOriginal) ELSE tw.dayOriginal END, 120), 11)
                + RIGHT(CONVERT(varchar, @time, 120), 8),
            120)
        AND (
            (@monday    = 1 AND DATEPART(dw, tw.dayOriginal) = 1) OR
            (@tuesday   = 1 AND DATEPART(dw, tw.dayOriginal) = 2) OR
            (@wednesday = 1 AND DATEPART(dw, tw.dayOriginal) = 3) OR
            (@thursday  = 1 AND DATEPART(dw, tw.dayOriginal) = 4) OR
            (@friday    = 1 AND DATEPART(dw, tw.dayOriginal) = 5) OR
            (@saturday  = 1 AND DATEPART(dw, tw.dayOriginal) = 6) OR
            (@sunday    = 1 AND DATEPART(dw, tw.dayOriginal) = 7)
        );

    -- Порядок цепочки по будущему факт. времени: голова строго раньше хвоста.
    -- Драйвер — @moved (мало строк), соседи — по PK. Полусвязи не ловятся.
    IF EXISTS (
        SELECT 1
        FROM @moved m
            INNER JOIN TariffWindow cur ON cur.windowId = m.windowId
            LEFT JOIN TariffWindow p  ON p.windowId = cur.windowPrevId
            LEFT JOIN @moved mp       ON mp.windowId = p.windowId
            LEFT JOIN TariffWindow n  ON n.windowId = cur.windowNextId
            LEFT JOIN @moved mn       ON mn.windowId = n.windowId
        WHERE
            (p.windowId IS NOT NULL
                AND COALESCE(mp.newActual, p.windowDateActual) >= m.newActual)
            OR
            (n.windowId IS NOT NULL
                AND m.newActual >= COALESCE(mn.newActual, n.windowDateActual))
    )
    BEGIN
        RAISERROR('LinkedWindowsWrongOrder', 16, 1);
        RETURN;
    END

    UPDATE tw
    SET tw.windowDateActual = m.newActual
    FROM TariffWindow tw
        INNER JOIN @moved m ON m.windowId = tw.windowId;
END
GO

-- =====================================================================
-- 2. TariffWindowIUD — правка одного окна через паспорт («Свойства»)
--    Добавлена одна проверка в ветку UpdateItem; остальное без изменений.
-- =====================================================================
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
		tw.dayActual = Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), @windowDateActual)), 112), 112)
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
		,Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, pl.broadcastStart)
		,DATEADD(hh, -DATEPART(hh, pl.broadcastStart), @windowDateActual)), 112), 112)
		,Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, pl.broadcastStart)
		, DATEADD(hh, -DATEPART(hh, pl.broadcastStart), @windowDateOriginal)), 112), 112)
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

-- =====================================================================
-- 3. Текст сообщения (upsert — при повторном запуске текст обновляется)
-- =====================================================================
DECLARE @msgWrongOrder nvarchar(4000) = N'Перенос не выполнен: после него объединённые рекламные окна вышли бы в эфир в неправильном порядке — окно, которое должно идти позже, оказалось бы раньше. Отмените объединение окон, выполните перенос и объедините их заново.';

IF EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'LinkedWindowsWrongOrder')
	UPDATE [dbo].[iMessage] SET [message] = @msgWrongOrder WHERE name = 'LinkedWindowsWrongOrder';
ELSE
	INSERT INTO [dbo].[iMessage] (name, [message]) VALUES ('LinkedWindowsWrongOrder', @msgWrongOrder);
GO

-- =====================================================================
-- Проверка
-- =====================================================================
SELECT name, [message] FROM [dbo].[iMessage] WHERE name = 'LinkedWindowsWrongOrder';

SELECT o.name, o.modify_date
FROM sys.objects o
WHERE o.name IN ('TariffWindowMoveTime', 'TariffWindowIUD')
ORDER BY o.name;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
