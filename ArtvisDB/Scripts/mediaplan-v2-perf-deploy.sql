/*
    ПРОД-ДЕПЛОЙ: MediaPlanRetrieve_v2 — OPTION (RECOMPILE) на заполнение #issue.
    Ветка hotfix/mediaplan-v2-perf.

    ЗАЧЕМ
      Медиаплан — #2 процедура на проде по числу вызовов дольше 100 мс
      (717 вызовов за 1–4 сентября 2026: 574 в полосе 1–3 с, 24 в 3–6 с,
      13 дольше 6 с, худший 14,8 с). Уступает только ActionRecalculate.

      Причина — «универсальные» предикаты в отборе #issue:
          i.campaignId = ISNULL(@campaignId, i.campaignID)
          c.actionID   = ISNULL(@actionID,   c.actionID)
          c.agencyID   = ISNULL(@agencyId,   c.agencyID)
      Оптимизатор не умеет оценивать такое выражение и берёт догадку 10% от
      таблицы. Один закешированный план обслуживал все три режима вызова
      (по кампании / по акции / сводный по набору акций), и это всегда был
      Hash Match поверх ПОЛНОГО скана Issue (3,44 млн строк, 29,7 тыс. страниц)
      и ПОЛНОГО скана TariffWindow (2,0 млн строк, 23,8 тыс. страниц) —
      ~425 МБ чтений и ~2,3 с CPU на каждый вызов, сколько бы выпусков в
      кампании реально ни было. Замер плана на ArtvisDev это подтвердил:
      Clustered Index Scan(Issue) EstimateRows=344423 — ровно 10% от 3 444 229.

      Процедура НИКОГДА не зовётся без фильтра: Client/Classes/MediaPlan.cs
      всегда передаёт campaignId, либо actionId, либо actionIDString.
      RECOMPILE включает parameter embedding — NULL-ветки ISNULL сворачиваются,
      остаётся sargable `c.actionID = <литерал>`, и план становится
      seek Campaign(UIX_Campaign) -> seek Issue(IX_Issue_campaignID_originalWindowID)
      -> seek TariffWindow(UX_TariffWindow_WindowID_DayOriginal).

    ЗАМЕРЫ (ArtvisDev, копия прода, Issue 3,44 млн / TariffWindow 2,01 млн)
      сценарий                                  было      стало   ускорение
      кампания тип 1, 4380 выпусков, факт       2896 мс   288 мс     10,1x
      кампания тип 1, план за месяц              930 мс   106 мс      8,8x
      кампания тип 2 (спонсорская), 320 вып.    1367 мс    84 мс     16,3x
      кампания тип 3 (модульная)                1446 мс   209 мс      6,9x
      кампания тип 4 (пакет модулей)            1759 мс   186 мс      9,5x
      акция 165676 (13 138 вып.), onlyRollers   3427 мс   490 мс      7,0x
      акция 165676, полный набор, 3 СМИ         2558 мс   614 мс      4,2x
      сводный медиаплан, 2 акции / 18 СМИ       2463 мс   726 мс      3,4x
      акция 172510 (перенесённые выпуски)      31092 мс   109 мс    285,2x
      кампания в 10 выпусков (пол компиляции)    846 мс    25 мс     33,8x
      На холодном кеше (DBCC DROPCLEANBUFFERS) худший случай:
      2584 -> 698 мс, CPU 2281 -> 500 мс, физических чтений 23 762 -> ~900.

    ЦЕНА
      ~25 мс компиляции на вызов (это пол нового времени: столько стоит
      кампания в 10 выпусков). Отчёт строится интерактивно при выгрузке в
      Excel, из цикла не вызывается — окупается на порядок в любом сценарии.

    ЧТО ИМЕННО МЕНЯЕТСЯ
      Ровно две строки: `OPTION (RECOMPILE)` после GROUP BY в обеих ветках
      заполнения #issue (@isFact = 1 и @isFact = 0). Больше в теле процедуры
      не изменено ничего — ни один SELECT результирующих наборов не тронут.
      RECOMPILE — подсказка плана: логический результат запроса она изменить
      не может.

    ЧТО НЕ ДЕЛАЛОСЬ И ПОЧЕМУ (замерено, отвергнуто)
      * Покрывающий индекс Issue(campaignID) INCLUDE(actualWindowID, rollerID,
        positionId) убирает key lookup: логические чтения Issue 53 126 -> 58.
        Но физических чтений на холодном кеше он экономит ~900 страниц (~10 мс),
        а стоит 93 МБ на самой горячей на запись таблице системы (Issue уже
        несёт ~1,09 ГБ индексов, и по ней идёт шторм ActionRecalculate).
        Не окупается. Оставлено как возможная доработка.
      * Переписать OUTER APPLY Pricelist на предрасчёт: 13 138 коррелированных
        TOP 1 стоят 26 276 логических чтений по таблице в 94 строки, то есть
        ~50 мс из 339 мс. Не окупает риск.
      * Убрать GROUP BY (он избыточен: все join'ы 1:1 по PK, i.issueID
        уникален) — выигрыш 0 мс. Не трогаем.
      * Убрать join к #mm, когда @massmediaIDString IS NULL: 2 логических
        чтения. Плюс #mm используется дальше в наборе 3
        (`SELECT TOP 1 massmediaID FROM #mm ORDER BY massmediaID`) —
        трогать его семантику нельзя.

    ПОЧЕМУ SET QUOTED_IDENTIFIER ON / SET ANSI_NULLS ON ОБЯЗАТЕЛЬНЫ
      В базе есть индекс по вычисляемому столбцу TariffWindow.windowTime.
      Модуль, развёрнутый с QUOTED_IDENTIFIER OFF, ломает планы с участием
      TariffWindow (ошибка 1934). Настройки SET фиксируются вместе с текстом
      модуля в момент ALTER, поэтому их надо выставить в ОТДЕЛЬНОМ батче
      перед ALTER.

    ИДЕМПОТЕНТНОСТЬ
      Повторный запуск просто перезальёт то же тело с теми же SET-опциями.

    ОТКАТ
      git show master:"ArtvisDB/dbo/Stored Procedures/MediaPlanRetrieve_v2.sql"
      Заменить CREATE на ALTER и развернуть тем же батчем
      SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; GO ... GO.
      Либо просто удалить из тела две строки `OPTION (RECOMPILE)`.

    ПОСЛЕ ДЕПЛОЯ
      Перезапуск клиентов не требуется: сигнатура процедуры не менялась.
      Проверка эквивалентности и замеры — ArtvisDB/Scripts/mediaplan-v2-perf-check.sql
      (его полезно прогнать и ДО деплоя, чтобы снять базовые цифры).

    ЗАПУСК
      sqlcmd -S <прод-сервер> -d <прод-БД> -E -b -i mediaplan-v2-perf-deploy.sql
      либо открыть в SSMS на нужной БД и выполнить целиком.
*/

-- При необходимости раскомментировать и подставить имя прод-БД:
-- USE [Artvis];
-- GO

SET NOCOUNT ON;
GO

/* -- Преполёт: та ли база ----------------------------------------------- */
IF OBJECT_ID('dbo.MediaPlanRetrieve_v2') IS NULL
   OR OBJECT_ID('dbo.TariffWindow') IS NULL
   OR OBJECT_ID('dbo.fn_CreateTableFromString') IS NULL
   OR OBJECT_ID('dbo.fn_LastDateOfMonth') IS NULL
   OR OBJECT_ID('dbo.vRoller') IS NULL
BEGIN
    RAISERROR('НЕ ТА БАЗА: не найдены dbo.MediaPlanRetrieve_v2 / dbo.TariffWindow / dbo.fn_CreateTableFromString / dbo.fn_LastDateOfMonth / dbo.vRoller. Деплой прерван.', 16, 1);
    SET NOEXEC ON;
END
GO
/* Тело, которое разворачивает этот скрипт, содержит @actionIDString (сводный
   медиаплан по набору акций). Если на базе лежит версия БЕЗ него — значит, не
   накачен multi-action-media-plan-procs-deploy.sql, и накат этого скрипта
   изменил бы контракт процедуры. Останавливаемся. */
IF OBJECT_ID('dbo.MediaPlanRetrieve_v2') IS NOT NULL
   AND OBJECT_DEFINITION(OBJECT_ID('dbo.MediaPlanRetrieve_v2')) NOT LIKE '%@actionIDString%'
BEGIN
    RAISERROR('На базе версия MediaPlanRetrieve_v2 БЕЗ @actionIDString. Сначала накатите multi-action-media-plan-procs-deploy.sql. Деплой прерван.', 16, 1);
    SET NOEXEC ON;
END
GO
PRINT 'БД      : ' + DB_NAME();
PRINT 'Сервер  : ' + CONVERT(sysname, SERVERPROPERTY('ServerName'));
PRINT 'До      : ' + CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.MediaPlanRetrieve_v2')) LIKE '%OPTION (RECOMPILE)%'
                          THEN 'уже с RECOMPILE' ELSE 'старая версия (один план на все режимы)' END;
GO

/* -- MediaPlanRetrieve_v2 ----------------------------------------------- */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
-- Created by GitHub Copilot in SSMS - review carefully before executing
ALTER PROCEDURE [dbo].[MediaPlanRetrieve_v2]
(
    @campaignId int = null,
    @campaignTypeId tinyint = null,
    @isFact bit = 1,
    @massmediaIDString VARCHAR(8000) = null,
    @year smallint = null,
    @month tinyint = null,
    @actionId int = null,
    @startDate datetime = null,
    @finishDate datetime = null,
    @onlyRollers bit = 0,
    @rollerIDString VARCHAR(8000) = null,
    @agencyId int = null,                         -- опциональный фильтр по агентству
    @actionIDString VARCHAR(8000) = null          -- набор акций для сводного медиаплана
)
AS
BEGIN
    SET NOCOUNT ON;

    --------------------------------------------------------------------
    -- #mm, #rr
    --------------------------------------------------------------------
    CREATE TABLE #mm (massmediaID int NOT NULL PRIMARY KEY);
    CREATE TABLE #rr (rollerID int NOT NULL PRIMARY KEY);
    CREATE TABLE #act (actionID int NOT NULL PRIMARY KEY);

    IF @actionIDString IS NOT NULL
        INSERT INTO #act(actionID)
        SELECT DISTINCT CAST([ID] AS int)
        FROM fn_CreateTableFromString(@actionIDString);

    IF @massmediaIDString IS NOT NULL
        INSERT INTO #mm(massmediaID)
        SELECT CAST([ID] AS int)
        FROM fn_CreateTableFromString(@massmediaIDString);
    ELSE
        INSERT INTO #mm(massmediaID)
        SELECT [massmediaID]
        FROM [MassMedia];

    IF @rollerIDString IS NOT NULL
        INSERT INTO #rr(rollerID)
        SELECT CAST([ID] AS int)
        FROM fn_CreateTableFromString(@rollerIDString);

    --------------------------------------------------------------------
    -- Месячный диапазон (считаем 1 раз)
    --------------------------------------------------------------------
    DECLARE @monthStart datetime = NULL;
    DECLARE @monthEndExcl datetime = NULL;

    IF @year IS NOT NULL AND @month IS NOT NULL
    BEGIN
        SET @monthStart = CONVERT(datetime, '01.' + CAST(@month AS varchar(2)) + '.' + CAST(@year AS varchar(4)), 104);
        SET @monthEndExcl = DATEADD(day, 1, dbo.fn_LastDateOfMonth(@monthStart));
    END

    --------------------------------------------------------------------
    -- #issue вместо @issue
    --------------------------------------------------------------------
    CREATE TABLE #issue
    (
        issueID INT NOT NULL,
        rollerId int NULL,
        issueDate datetime NOT NULL,
        comment nvarchar(32) NULL,
        positionID SMALLINT NULL,
        price decimal(18,2) NULL,
        broadcast datetime NULL,
        mmID smallint NOT NULL,
        timeString varchar(5) NULL,
        shiftedDate datetime NULL,
        radioDate datetime NULL
    );

    CREATE CLUSTERED INDEX CX_issue_issueDate ON #issue(issueDate);
    CREATE NONCLUSTERED INDEX IX_issue_roller ON #issue(rollerId);
    CREATE NONCLUSTERED INDEX IX_issue_mmID ON #issue(mmID);

    --------------------------------------------------------------------
    -- Заполнение #issue
    --
    -- ЗАЧЕМ ЗДЕСЬ RECOMPILE (см. hint после GROUP BY в обеих ветках ниже)
    --   Отбор построен на «универсальных» предикатах вида
    --       i.campaignId = ISNULL(@campaignId, i.campaignID)
    --       c.actionID   = ISNULL(@actionID,   c.actionID)
    --       c.agencyID   = ISNULL(@agencyId,   c.agencyID)
    --   Такое выражение неsargable: оптимизатор не может оценить его
    --   селективность и берёт догадку 10% от таблицы (344 тыс. строк из 3,44 млн
    --   по Issue). В результате ОДИН закешированный план обслуживал и вызов по
    --   одной кампании, и по одной акции, и сводный медиаплан по набору акций,
    --   и всегда это был Hash Match поверх ПОЛНОГО скана Issue (3,44 млн строк)
    --   и ПОЛНОГО скана TariffWindow (2,0 млн строк) — ~53 тыс. страниц (~425 МБ)
    --   и ~2,3 с процессорного времени на КАЖДЫЙ вызов, независимо от того,
    --   сколько выпусков реально в кампании.
    --
    --   Процедура НИКОГДА не вызывается без фильтра (Client/Classes/MediaPlan.cs:
    --   CombineRollers и LoadData всегда передают campaignId, либо actionId,
    --   либо actionIDString), поэтому правильный план — seek по Campaign/Issue.
    --   RECOMPILE включает parameter embedding: NULL-ветки ISNULL сворачиваются,
    --   остаётся sargable `c.actionID = <литерал>`, и план становится
    --   seek Campaign -> seek Issue по campaignID -> seek TariffWindow по windowId.
    --
    --   Цена: ~25 мс компиляции на вызов (замер на ArtvisDev). Отчёт строится
    --   интерактивно, из цикла его никто не дёргает, так что это окупается
    --   на порядок: даже кампания в 10 выпусков стоила 846 мс, стала 25 мс.
    --------------------------------------------------------------------
    IF @isFact = 1
    BEGIN
        INSERT INTO #issue (issueID, rollerId, issueDate, comment, positionID, price, broadcast, mmID)
        SELECT
            i.issueID,
            i.rollerId,
            tw.windowDateActual,
            MAX(COALESCE(t.comment, N'')),
            i.positionId,
            tw.price,
            pl1.broadcastStart,
            tw.massmediaID
        FROM Issue i WITH (NOLOCK)
        INNER JOIN TariffWindow tw ON i.actualWindowID = tw.windowId
        INNER JOIN #mm mmm ON tw.massmediaID = mmm.massmediaID
        INNER JOIN Campaign c ON i.campaignID = c.campaignID
        LEFT  JOIN Tariff t ON t.tariffId = tw.tariffId
        LEFT  JOIN #rr rr ON i.rollerID = rr.rollerID
        OUTER APPLY
        (
            SELECT TOP (1) pl.broadcastStart
            FROM dbo.Pricelist pl
            WHERE pl.massmediaID = tw.massmediaID
              AND pl.startDate <= tw.dayActual
              AND pl.finishDate >= tw.dayActual
            ORDER BY pl.startDate DESC
        ) pl1
        WHERE
            i.campaignId = ISNULL(@campaignId, i.campaignID)
            AND c.actionID = ISNULL(@actionID, c.actionID)
            AND (@actionIDString IS NULL OR c.actionID IN (SELECT actionID FROM #act))
            AND c.agencyID = ISNULL(@agencyId, c.agencyID)        -- фильтр по агентству
            AND (@rollerIDString IS NULL OR rr.rollerID IS NOT NULL)
            AND (@monthStart IS NULL OR (tw.dayActual >= @monthStart AND tw.dayActual < @monthEndExcl))
            AND ((@startDate IS NULL AND @finishDate IS NULL) OR (tw.dayActual BETWEEN @startDate AND @finishDate))
        GROUP BY
            i.issueID, i.rollerId, tw.windowDateActual, tw.price, i.positionId, tw.massmediaID, pl1.broadcastStart
        -- RECOMPILE обязателен: см. комментарий перед блоком заполнения #issue.
        OPTION (RECOMPILE);
    END
    ELSE
    BEGIN
        INSERT INTO #issue (issueID, rollerId, issueDate, comment, positionID, price, broadcast, mmID)
        SELECT
            i.issueID,
            i.rollerId,
            tw.windowDateOriginal,
            MAX(COALESCE(t.comment, N'')),
            i.positionId,
            tw.price,
            pl1.broadcastStart,
            tw.massmediaID
        FROM Issue i WITH (NOLOCK)
        INNER JOIN TariffWindow tw ON i.originalWindowID = tw.windowId
        INNER JOIN #mm mmm ON tw.massmediaID = mmm.massmediaID
        INNER JOIN Campaign c ON i.campaignID = c.campaignID
        LEFT  JOIN Tariff t ON t.tariffId = tw.tariffId
        LEFT  JOIN #rr rr ON i.rollerID = rr.rollerID
        OUTER APPLY
        (
            SELECT TOP (1) pl.broadcastStart
            FROM dbo.Pricelist pl
            WHERE pl.massmediaID = tw.massmediaID
              AND pl.startDate <= tw.dayOriginal
              AND pl.finishDate >= tw.dayOriginal
            ORDER BY pl.startDate DESC
        ) pl1
        WHERE
            i.campaignId = ISNULL(@campaignId, i.campaignID)
            AND c.actionID = ISNULL(@actionID, c.actionID)
            AND (@actionIDString IS NULL OR c.actionID IN (SELECT actionID FROM #act))
            AND c.agencyID = ISNULL(@agencyId, c.agencyID)        -- фильтр по агентству
            AND (@rollerIDString IS NULL OR rr.rollerID IS NOT NULL)
            AND (@monthStart IS NULL OR (tw.dayOriginal >= @monthStart AND tw.dayOriginal < @monthEndExcl))
            AND ((@startDate IS NULL AND @finishDate IS NULL) OR (tw.dayOriginal BETWEEN @startDate AND @finishDate))
        GROUP BY
            i.issueID, i.rollerId, tw.windowDateOriginal, tw.price, i.positionId, tw.massmediaID, pl1.broadcastStart
        -- RECOMPILE обязателен: см. комментарий перед блоком заполнения #issue.
        OPTION (RECOMPILE);
    END

    --------------------------------------------------------------------
    -- @massmediaCount считаем по фактической выборке
    --------------------------------------------------------------------
    DECLARE @massmediaCount int = NULL;
    SELECT @massmediaCount = COUNT(DISTINCT mmID) FROM #issue;
    IF @massmediaCount IS NULL OR @massmediaCount = 0 SET @massmediaCount = 1;

    --------------------------------------------------------------------
    -- Вычисляем timeString / shiftedDate / radioDate (один раз)
    --------------------------------------------------------------------
    UPDATE i
    SET
        shiftedDate = DATEADD(minute,
                        - (DATEPART(hour, i.broadcast) * 60 + DATEPART(minute, i.broadcast)),
                        i.issueDate),
        radioDate =
            CONVERT(datetime,
                CONVERT(varchar(8),
                    DATEADD(minute,
                        - (DATEPART(hour, i.broadcast) * 60 + DATEPART(minute, i.broadcast)),
                        i.issueDate
                    ),
                112), 112),
        timeString =
        (
            RIGHT('0' + CAST(
                (DATEPART(hour, i.issueDate) +
                    CASE WHEN (DATEPART(hour, i.issueDate) * 60 + DATEPART(minute, i.issueDate))
                              < (DATEPART(hour, i.broadcast) * 60 + DATEPART(minute, i.broadcast))
                         THEN 24 ELSE 0 END
                ) AS varchar(3)), 2)
            + ':'
            + RIGHT('0' + CAST(DATEPART(minute, i.issueDate) AS varchar(2)), 2)
        )
    FROM #issue i;

    --------------------------------------------------------------------
    -- 1) Ролики
    --------------------------------------------------------------------
    SELECT
        r.rollerId,
        r.[name],
        r.advertTypeName,
        r.duration,
        COUNT(*) AS quantity
    FROM #issue i
    INNER JOIN vRoller r ON i.rollerId = r.rollerId
    GROUP BY r.[name], r.advertTypeName, r.duration, r.rollerId;

    IF @onlyRollers = 1
        RETURN;

    --------------------------------------------------------------------
    -- 2) Тайм-слоты
    --------------------------------------------------------------------
    IF @campaignTypeId IS NOT NULL AND @campaignTypeId = 1
    BEGIN
        SELECT
            i.timeString AS [time],
            MAX(i.comment) AS comment,
            i.price,
            SUM(r.duration) / @massmediaCount AS totalDuration
        FROM #issue i
        INNER JOIN Roller r ON i.rollerId = r.rollerId
        GROUP BY i.price, i.timeString
        ORDER BY i.timeString;
    END
    ELSE
    BEGIN
        SELECT
            i.timeString AS [time],
            MAX(i.comment) AS comment,
            SUM(r.duration) / @massmediaCount AS totalDuration
        FROM #issue i
        INNER JOIN Roller r ON i.rollerId = r.rollerId
        GROUP BY i.timeString
        ORDER BY i.timeString;
    END

    --------------------------------------------------------------------
    -- 3) Детализация
    --------------------------------------------------------------------
    IF @campaignTypeId IS NULL OR @campaignTypeId = 4
    BEGIN
        SELECT
            i.rollerId,
            i.radioDate AS issueDate,
            i.timeString AS [time],
            i.positionID
        FROM #issue i
        WHERE i.mmID = (SELECT TOP 1 massmediaID FROM #mm ORDER BY massmediaID)
        ORDER BY i.radioDate, i.timeString;
    END
    ELSE IF @campaignTypeId IS NULL OR @campaignTypeId = 1
    BEGIN
        SELECT
            i.rollerId,
            i.radioDate AS issueDate,
            i.timeString AS [time],
            i.price,
            i.positionID
        FROM #issue i
        ORDER BY i.radioDate, i.timeString;
    END
    ELSE
    BEGIN
        SELECT
            i.rollerId,
            i.radioDate AS issueDate,
            i.timeString AS [time],
            i.positionID
        FROM #issue i
        ORDER BY i.radioDate, i.timeString;
    END

    --------------------------------------------------------------------
    -- 4) Счётчики по дням
    --------------------------------------------------------------------
    IF @monthStart IS NOT NULL
    BEGIN
        ;WITH d AS
        (
            SELECT 1 AS [day]
            UNION ALL SELECT [day] + 1
            FROM d
            WHERE [day] < DAY(dbo.fn_LastDateOfMonth(@monthStart))
        ),
        c AS
        (
            SELECT
                DAY(shiftedDate) AS [day],
                COUNT(*) AS cnt
            FROM #issue
            GROUP BY DAY(shiftedDate)
        )
        SELECT ISNULL(c.cnt, 0) AS c
        FROM d
        LEFT JOIN c ON c.[day] = d.[day]
        ORDER BY d.[day]
        OPTION (MAXRECURSION 100);
    END
    ELSE
    BEGIN
        SELECT
            COUNT(i.rollerId) AS [COUNT]
        FROM #issue i
        GROUP BY i.radioDate
        ORDER BY i.radioDate;
    END

    --------------------------------------------------------------------
    -- 5) ProgramIssues
    --------------------------------------------------------------------
    IF (@campaignTypeId IS NOT NULL AND @campaignTypeId = 2) OR (@actionId IS NOT NULL) OR (@actionIDString IS NOT NULL)
    BEGIN
        SELECT
            sp.[name],
            pri.issueDate
        FROM ProgramIssue pri
        INNER JOIN SponsorProgram sp ON pri.programID = sp.sponsorProgramID
        INNER JOIN Campaign c ON pri.campaignID = c.campaignID
        INNER JOIN #mm mm ON c.massmediaID = mm.massmediaID
        WHERE pri.campaignId = COALESCE(@campaignID, pri.campaignID)
          AND c.actionID = COALESCE(@actionID, c.actionID)
          AND (@actionIDString IS NULL OR c.actionID IN (SELECT actionID FROM #act))
          AND c.agencyID = COALESCE(@agencyId, c.agencyID);      -- фильтр по агентству
    END
END
GO

/* -- Сброс кеша планов именно этой процедуры ------------------------------
   ALTER и так инвалидирует план; блок оставлен страховкой — старый
   «сканирующий» план не должен пережить накат ни в каком виде. */
DECLARE @ph varbinary(64);
DECLARE plans CURSOR LOCAL FAST_FORWARD FOR
    SELECT DISTINCT qs.plan_handle
    FROM sys.dm_exec_query_stats qs
    CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
    WHERE st.objectid = OBJECT_ID('dbo.MediaPlanRetrieve_v2')
      AND st.dbid = DB_ID();
OPEN plans;
FETCH NEXT FROM plans INTO @ph;
WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        DBCC FREEPROCCACHE (@ph) WITH NO_INFOMSGS;
    END TRY
    BEGIN CATCH
    END CATCH
    FETCH NEXT FROM plans INTO @ph;
END
CLOSE plans;
DEALLOCATE plans;
GO

/* -- Проверка ------------------------------------------------------------ */
SET NOEXEC OFF;
GO
SELECT
    [процедура]           = o.name,
    [quoted_identifier]   = m.uses_quoted_identifier,   -- ожидается 1
    [ansi_nulls]          = m.uses_ansi_nulls,          -- ожидается 1
    [recompile_в_теле]    = (LEN(m.definition) - LEN(REPLACE(m.definition, 'OPTION (RECOMPILE)', '')))
                            / LEN('OPTION (RECOMPILE)'),                                       -- ожидается 2
    [есть_actionIDString] = CASE WHEN m.definition LIKE '%@actionIDString%' THEN 1 ELSE 0 END, -- ожидается 1
    [изменена]            = o.modify_date
FROM sys.sql_modules m
JOIN sys.objects o ON o.object_id = m.object_id
WHERE o.name = 'MediaPlanRetrieve_v2';
GO
PRINT 'Готово. Ожидается: quoted_identifier=1, ansi_nulls=1, recompile_в_теле=2, есть_actionIDString=1.';
PRINT 'Дальше: ArtvisDB/Scripts/mediaplan-v2-perf-check.sql — эквивалентность и замеры.';
GO
