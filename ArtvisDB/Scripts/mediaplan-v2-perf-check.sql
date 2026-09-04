/*
    ЭКВИВАЛЕНТНОСТЬ + ЗАМЕРЫ: MediaPlanRetrieve_v2 (ветка hotfix/mediaplan-v2-perf).

    ЧТО ПРОВЕРЯЕТСЯ
      Правка состоит РОВНО из двух строк `OPTION (RECOMPILE)` после GROUP BY в
      обеих ветках заполнения #issue. Это подсказка плана: логический результат
      запроса она изменить не может, меняется только форма плана (полный скан
      Issue + TariffWindow с Hash Match -> seek Campaign -> seek Issue -> seek
      TariffWindow). Все пять результирующих наборов процедуры — чистые функции
      от #issue, #mm, #act, @massmediaCount и @monthStart; ни один из их SELECT
      не тронут. Поэтому доказать эквивалентность = доказать, что #issue
      совпадает построчно.

      Раздел 2 сравнивает содержимое #issue, полученное СТАРОЙ и НОВОЙ формой
      плана: один и тот же текст запроса выполняется дважды, второй раз — с
      добавленным ` OPTION (RECOMPILE)`. Текст строится один раз в переменную,
      поэтому «разъехаться» две версии не могут по построению. Сравнение —
      EXCEPT в обе стороны по всем колонкам плюс сверка количества строк.

      Раздел 3 сравнивает СКВОЗНО набор №1 (ролики) двух реальных процедур
      через INSERT ... EXEC с @onlyRollers = 1. Это единственный набор, который
      T-SQL умеет захватить (INSERT ... EXEC падает, если процедура возвращает
      несколько наборов разной формы), зато он агрегирует ВСЁ содержимое #issue:
      любое расхождение по строкам изменило бы quantity или состав роликов.
      Раздел работает, только если рядом лежит вторая процедура для сравнения —
      dbo.MediaPlanRetrieve_v2_new. Если её нет, раздел пропускается, и это
      нормально: на проде после наката сравнивать уже не с чем, а
      доказательством остаётся раздел 2.
      Чтобы прогнать раздел 3 ДО наката на dev-копии прода: взять
      mediaplan-v2-perf-deploy.sql, заменить в нём
      `ALTER PROCEDURE [dbo].[MediaPlanRetrieve_v2]` на
      `CREATE PROCEDURE [dbo].[MediaPlanRetrieve_v2_new]`, развернуть, прогнать
      этот скрипт, затем `DROP PROCEDURE dbo.MediaPlanRetrieve_v2_new`.

      Раздел 4 — сквозные замеры развёрнутой процедуры с @onlyRollers = 1.
      По умолчанию он выполняется ТОЛЬКО если процедура уже с RECOMPILE
      (то есть после наката): на старом плане 35 вызовов растягиваются на
      много минут и грузят сервер, а полезной информации не добавляют —
      цифры «до» и «после» по каждому случаю уже есть в разделе 2
      (колонки ms_old / ms_new). Принудительно включить: @forceBench = 1.

    ВРЕМЯ ПРОГОНА
      Раздел 2 на копии прода: ~3 минуты (старая форма плана честно
      отрабатывает по каждому случаю). Раздел 3: ~4 минуты до наката.
      После наката весь скрипт — меньше минуты.

    ВЫБОРКА
      Не хардкод: набор случаев строится из живых данных (Раздел 1) и покрывает
      campaignTypeID 1/2/3/4, @isFact 0 и 1, с @year+@month и без, с
      @startDate+@finishDate и без, @onlyRollers 0 и 1, режим набора акций
      (@actionIDString), фильтры @rollerIDString / @massmediaIDString / @agencyId.

    ЧТО ЖДЁМ
      Раздел 2: «расхождений всего» = 0.
      Раздел 3: «расхождений всего» = 0 (или отметка «пропущено»).

    СКРИПТ НИЧЕГО НЕ ПИШЕТ В ПОСТОЯННЫЕ ТАБЛИЦЫ.

    ЗАПУСК
      sqlcmd -S <сервер> -d <БД> -E -b -I -i mediaplan-v2-perf-check.sql
      либо открыть в SSMS и выполнить целиком.
      Полезно прогнать ДО наката (снять базовые цифры) и ПОСЛЕ.
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

DECLARE @nIssue bigint = (SELECT SUM(row_count) FROM sys.dm_db_partition_stats
                          WHERE object_id = OBJECT_ID('dbo.Issue') AND index_id IN (0,1));
DECLARE @nTw bigint = (SELECT SUM(row_count) FROM sys.dm_db_partition_stats
                       WHERE object_id = OBJECT_ID('dbo.TariffWindow') AND index_id IN (0,1));

PRINT 'БД           : ' + DB_NAME();
PRINT 'Сервер       : ' + CONVERT(sysname, SERVERPROPERTY('ServerName'));
PRINT 'Issue        : ' + CONVERT(varchar(20), @nIssue) + ' строк';
PRINT 'TariffWindow : ' + CONVERT(varchar(20), @nTw) + ' строк';
PRINT 'Развёрнутая MediaPlanRetrieve_v2: '
      + CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.MediaPlanRetrieve_v2')) LIKE '%OPTION (RECOMPILE)%'
             THEN 'с RECOMPILE (новая)' ELSE 'без RECOMPILE (старая)' END;
GO

/* ======================================================================= */
/* Раздел 1. Набор случаев из живых данных                                 */
/* ======================================================================= */

IF OBJECT_ID('tempdb..#cases') IS NOT NULL DROP TABLE #cases;
CREATE TABLE #cases
(
    id             int IDENTITY(1,1) PRIMARY KEY,
    descr          varchar(80)   NOT NULL,
    campaignId     int           NULL,
    campaignTypeId tinyint       NULL,
    actionId       int           NULL,
    actionIDString varchar(8000) NULL,
    isFact         bit           NOT NULL,
    mmString       varchar(8000) NULL,
    yr             smallint      NULL,
    mn             tinyint       NULL,
    startDate      datetime      NULL,
    finishDate     datetime      NULL,
    onlyRollers    bit           NOT NULL,
    rollerIDString varchar(8000) NULL,
    agencyId       int           NULL
);

/* -- самая крупная кампания каждого типа, вместе с её СМИ и месяцем -- */
IF OBJECT_ID('tempdb..#topCamp') IS NOT NULL DROP TABLE #topCamp;
CREATE TABLE #topCamp
(
    campaignTypeID tinyint, campaignID int, actionID int, agencyID int,
    mmString varchar(200), yr smallint, mn tinyint, d1 datetime, d2 datetime, n int
);
INSERT INTO #topCamp
SELECT campaignTypeID, campaignID, actionID, agencyID, mmString, yr, mn, d1, d2, n
FROM
(
    SELECT c.campaignTypeID, c.campaignID, c.actionID, c.agencyID,
           mmString = CONVERT(varchar(200), MIN(tw.massmediaID)) + ',',
           yr = YEAR(MIN(tw.dayOriginal)), mn = MONTH(MIN(tw.dayOriginal)),
           d1 = MIN(tw.dayOriginal), d2 = MAX(tw.dayOriginal),
           n = COUNT_BIG(*),
           rn = ROW_NUMBER() OVER (PARTITION BY c.campaignTypeID ORDER BY COUNT_BIG(*) DESC)
    FROM dbo.Campaign c WITH (NOLOCK)
        INNER JOIN dbo.Issue i WITH (NOLOCK) ON i.campaignID = c.campaignID
        INNER JOIN dbo.TariffWindow tw WITH (NOLOCK) ON tw.windowId = i.originalWindowID
    GROUP BY c.campaignTypeID, c.campaignID, c.actionID, c.agencyID
) z
WHERE rn = 1;

/* -- самая крупная акция и акция с переносами выпусков -- */
DECLARE @bigAction int =
(
    SELECT TOP 1 c.actionID
    FROM dbo.Issue i WITH (NOLOCK)
        INNER JOIN dbo.Campaign c WITH (NOLOCK) ON c.campaignID = i.campaignID
    GROUP BY c.actionID ORDER BY COUNT_BIG(*) DESC
);
DECLARE @bigActionMM varchar(8000) =
(
    SELECT CONVERT(varchar(20), mm.massmediaID) + ','
    FROM (SELECT DISTINCT c.massmediaID FROM dbo.Campaign c WITH (NOLOCK)
          WHERE c.actionID = @bigAction AND c.massmediaID IS NOT NULL) mm
    ORDER BY mm.massmediaID FOR XML PATH(''), TYPE
).value('.', 'varchar(8000)');
DECLARE @bigActionAgency int =
    (SELECT TOP 1 c.agencyID FROM dbo.Campaign c WITH (NOLOCK)
     WHERE c.actionID = @bigAction AND c.agencyID IS NOT NULL ORDER BY c.campaignID);
DECLARE @bigActionRoller varchar(8000) =
(
    SELECT TOP 1 CONVERT(varchar(20), i.rollerID) + ','
    FROM dbo.Issue i WITH (NOLOCK)
        INNER JOIN dbo.Campaign c WITH (NOLOCK) ON c.campaignID = i.campaignID
    WHERE c.actionID = @bigAction AND i.rollerID IS NOT NULL
    GROUP BY i.rollerID ORDER BY COUNT_BIG(*) DESC
);
DECLARE @shiftAction int =            -- акция, где actualWindowID <> originalWindowID
(
    SELECT TOP 1 c.actionID
    FROM dbo.Issue i WITH (NOLOCK)
        INNER JOIN dbo.Campaign c WITH (NOLOCK) ON c.campaignID = i.campaignID
    WHERE i.actualWindowID <> i.originalWindowID
    GROUP BY c.actionID ORDER BY COUNT_BIG(*) DESC
);
DECLARE @multiActions varchar(8000) = -- две «широкие» по СМИ акции: сводный медиаплан
(
    SELECT CONVERT(varchar(20), a.actionID) + ','
    FROM (SELECT TOP 2 c.actionID
          FROM dbo.Campaign c WITH (NOLOCK)
          WHERE EXISTS (SELECT 1 FROM dbo.Issue i WITH (NOLOCK) WHERE i.campaignID = c.campaignID)
          GROUP BY c.actionID ORDER BY COUNT(DISTINCT c.massmediaID) DESC, c.actionID) a
    ORDER BY a.actionID FOR XML PATH(''), TYPE
).value('.', 'varchar(8000)');
DECLARE @sponsorAction int =
(
    SELECT TOP 1 c.actionID
    FROM dbo.ProgramIssue pri WITH (NOLOCK)
        INNER JOIN dbo.Campaign c WITH (NOLOCK) ON c.campaignID = pri.campaignID
    GROUP BY c.actionID ORDER BY COUNT_BIG(*) DESC
);

/* кампании: по типу, факт/план, месяц, период, без фильтра СМИ */
INSERT INTO #cases (descr, campaignId, campaignTypeId, isFact, mmString, yr, mn, startDate, finishDate, onlyRollers)
SELECT 'кампания тип ' + CONVERT(varchar(3), campaignTypeID) + ', план',
       campaignID, campaignTypeID, 0, mmString, NULL, NULL, NULL, NULL, 0 FROM #topCamp
UNION ALL
SELECT 'кампания тип ' + CONVERT(varchar(3), campaignTypeID) + ', факт',
       campaignID, campaignTypeID, 1, mmString, NULL, NULL, NULL, NULL, 0 FROM #topCamp
UNION ALL
SELECT 'кампания тип ' + CONVERT(varchar(3), campaignTypeID) + ', план за месяц',
       campaignID, campaignTypeID, 0, mmString, yr, mn, NULL, NULL, 0 FROM #topCamp
UNION ALL
SELECT 'кампания тип ' + CONVERT(varchar(3), campaignTypeID) + ', факт за период',
       campaignID, campaignTypeID, 1, mmString, NULL, NULL, d1, DATEADD(day, 30, d1), 0 FROM #topCamp
UNION ALL
SELECT 'кампания тип ' + CONVERT(varchar(3), campaignTypeID) + ', без фильтра СМИ',
       campaignID, campaignTypeID, 0, NULL, NULL, NULL, NULL, NULL, 0 FROM #topCamp
UNION ALL
SELECT 'кампания тип ' + CONVERT(varchar(3), campaignTypeID) + ', onlyRollers',
       campaignID, campaignTypeID, 1, NULL, NULL, NULL, NULL, NULL, 1 FROM #topCamp;

/* акции */
INSERT INTO #cases (descr, actionId, actionIDString, isFact, mmString, yr, mn, startDate, finishDate, onlyRollers, rollerIDString, agencyId)
VALUES
 ('акция крупная, факт, onlyRollers',  NULL, NULL, 1, NULL, NULL, NULL, NULL, NULL, 1, NULL, NULL),
 ('акция крупная, план, onlyRollers',  NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, 1, NULL, NULL),
 ('акция крупная, факт, все СМИ',      NULL, NULL, 1, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL),
 ('акция крупная, план, фильтр СМИ',   NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL),
 ('акция крупная, факт, агентство',    NULL, NULL, 1, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL),
 ('акция крупная, факт, фильтр роликов', NULL, NULL, 1, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL),
 ('акция с переносами выпусков, факт', NULL, NULL, 1, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL),
 ('акция с переносами выпусков, план', NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL),
 ('акция спонсорская (ProgramIssue)',  NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL),
 ('сводный медиаплан, набор акций',    NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL),
 ('сводный медиаплан, onlyRollers',    NULL, NULL, 1, NULL, NULL, NULL, NULL, NULL, 1, NULL, NULL);

UPDATE #cases SET actionId = @bigAction              WHERE descr LIKE 'акция крупная%';
UPDATE #cases SET actionId = @shiftAction            WHERE descr LIKE 'акция с переносами%';
UPDATE #cases SET actionId = @sponsorAction          WHERE descr LIKE 'акция спонсорская%';
UPDATE #cases SET actionIDString = @multiActions     WHERE descr LIKE 'сводный медиаплан%';
UPDATE #cases SET mmString = @bigActionMM            WHERE descr = 'акция крупная, план, фильтр СМИ';
UPDATE #cases SET agencyId = @bigActionAgency        WHERE descr = 'акция крупная, факт, агентство';
UPDATE #cases SET rollerIDString = @bigActionRoller  WHERE descr = 'акция крупная, факт, фильтр роликов';

DELETE FROM #cases WHERE actionId IS NULL AND actionIDString IS NULL AND campaignId IS NULL;

SELECT [случаев в выборке] = COUNT(*) FROM #cases;
SELECT id, descr, campaignId, campaignTypeId, actionId, actionIDString, isFact,
       mmString, yr, mn, startDate, finishDate, onlyRollers, rollerIDString, agencyId
FROM #cases ORDER BY id;
GO

/* ======================================================================= */
/* Раздел 2. Эквивалентность #issue: старая форма плана vs новая            */
/* ======================================================================= */

IF OBJECT_ID('tempdb..#capOld') IS NOT NULL DROP TABLE #capOld;
IF OBJECT_ID('tempdb..#capNew') IS NOT NULL DROP TABLE #capNew;
CREATE TABLE #capOld
(
    issueID int NOT NULL, rollerId int NULL, issueDate datetime NOT NULL,
    comment nvarchar(32) NULL, positionID smallint NULL, price decimal(18,2) NULL,
    broadcast datetime NULL, mmID smallint NOT NULL
);
CREATE TABLE #capNew
(
    issueID int NOT NULL, rollerId int NULL, issueDate datetime NOT NULL,
    comment nvarchar(32) NULL, positionID smallint NULL, price decimal(18,2) NULL,
    broadcast datetime NULL, mmID smallint NOT NULL
);
IF OBJECT_ID('tempdb..#mm')  IS NOT NULL DROP TABLE #mm;
IF OBJECT_ID('tempdb..#rr')  IS NOT NULL DROP TABLE #rr;
IF OBJECT_ID('tempdb..#act') IS NOT NULL DROP TABLE #act;
CREATE TABLE #mm  (massmediaID int NOT NULL PRIMARY KEY);
CREATE TABLE #rr  (rollerID    int NOT NULL PRIMARY KEY);
CREATE TABLE #act (actionID    int NOT NULL PRIMARY KEY);

IF OBJECT_ID('tempdb..#eqres') IS NOT NULL DROP TABLE #eqres;
CREATE TABLE #eqres
(
    id int, descr varchar(80), rows_old int, rows_new int,
    diff_old_minus_new int, diff_new_minus_old int, ms_old int, ms_new int
);

/*
   Текст запроса строится ОДИН раз. Старая форма — как есть, новая — тот же
   текст плюс ` OPTION (RECOMPILE)`. Разъехаться версии не могут по построению.
   Тело дословно (с точностью до пробелов) повторяет заполнение #issue из
   MediaPlanRetrieve_v2; подстановки: {TGT} — целевая таблица, {K}/{K2} —
   ветка Actual/Original (@isFact = 1 / 0).
*/
DECLARE @tpl nvarchar(max) = N'
INSERT INTO {TGT} (issueID, rollerId, issueDate, comment, positionID, price, broadcast, mmID)
SELECT
    i.issueID, i.rollerId, tw.windowDate{K}, MAX(COALESCE(t.comment, N'''')),
    i.positionId, tw.price, pl1.broadcastStart, tw.massmediaID
FROM Issue i WITH (NOLOCK)
INNER JOIN TariffWindow tw ON i.{K2}WindowID = tw.windowId
INNER JOIN #mm mmm ON tw.massmediaID = mmm.massmediaID
INNER JOIN Campaign c ON i.campaignID = c.campaignID
LEFT  JOIN Tariff t ON t.tariffId = tw.tariffId
LEFT  JOIN #rr rr ON i.rollerID = rr.rollerID
OUTER APPLY
(
    SELECT TOP (1) pl.broadcastStart
    FROM dbo.Pricelist pl
    WHERE pl.massmediaID = tw.massmediaID
      AND pl.startDate <= tw.day{K}
      AND pl.finishDate >= tw.day{K}
    ORDER BY pl.startDate DESC
) pl1
WHERE
    i.campaignId = ISNULL(@campaignId, i.campaignID)
    AND c.actionID = ISNULL(@actionID, c.actionID)
    AND (@actionIDString IS NULL OR c.actionID IN (SELECT actionID FROM #act))
    AND c.agencyID = ISNULL(@agencyId, c.agencyID)
    AND (@rollerIDString IS NULL OR rr.rollerID IS NOT NULL)
    AND (@monthStart IS NULL OR (tw.day{K} >= @monthStart AND tw.day{K} < @monthEndExcl))
    AND ((@startDate IS NULL AND @finishDate IS NULL) OR (tw.day{K} BETWEEN @startDate AND @finishDate))
GROUP BY
    i.issueID, i.rollerId, tw.windowDate{K}, tw.price, i.positionId, tw.massmediaID, pl1.broadcastStart';

DECLARE @params nvarchar(max) = N'@campaignId int, @actionID int, @actionIDString varchar(8000),
    @agencyId int, @rollerIDString varchar(8000), @monthStart datetime, @monthEndExcl datetime,
    @startDate datetime, @finishDate datetime';

DECLARE @sqlBase nvarchar(max);

DECLARE @id int, @descr varchar(80), @campaignId int, @campaignTypeId tinyint, @actionId int,
        @actionIDString varchar(8000), @isFact bit, @mmString varchar(8000), @yr smallint, @mn tinyint,
        @startDate datetime, @finishDate datetime, @onlyRollers bit, @rollerIDString varchar(8000),
        @agencyId int, @monthStart datetime, @monthEndExcl datetime,
        @sql nvarchar(max), @t datetime2(7), @msOld int, @msNew int, @d1 int, @d2 int;

DECLARE cases CURSOR LOCAL FAST_FORWARD FOR
    SELECT id, descr, campaignId, campaignTypeId, actionId, actionIDString, isFact, mmString,
           yr, mn, startDate, finishDate, onlyRollers, rollerIDString, agencyId
    FROM #cases ORDER BY id;
OPEN cases;
FETCH NEXT FROM cases INTO @id, @descr, @campaignId, @campaignTypeId, @actionId, @actionIDString,
                           @isFact, @mmString, @yr, @mn, @startDate, @finishDate, @onlyRollers,
                           @rollerIDString, @agencyId;

WHILE @@FETCH_STATUS = 0
BEGIN
    TRUNCATE TABLE #capOld;  TRUNCATE TABLE #capNew;
    TRUNCATE TABLE #mm;      TRUNCATE TABLE #rr;      TRUNCATE TABLE #act;

    IF @actionIDString IS NOT NULL
        INSERT INTO #act (actionID) SELECT DISTINCT CAST([ID] AS int) FROM dbo.fn_CreateTableFromString(@actionIDString);
    IF @mmString IS NOT NULL
        INSERT INTO #mm (massmediaID) SELECT CAST([ID] AS int) FROM dbo.fn_CreateTableFromString(@mmString);
    ELSE
        INSERT INTO #mm (massmediaID) SELECT [massmediaID] FROM dbo.MassMedia;
    IF @rollerIDString IS NOT NULL
        INSERT INTO #rr (rollerID) SELECT CAST([ID] AS int) FROM dbo.fn_CreateTableFromString(@rollerIDString);

    SET @monthStart = NULL; SET @monthEndExcl = NULL;
    IF @yr IS NOT NULL AND @mn IS NOT NULL
    BEGIN
        SET @monthStart = CONVERT(datetime, '01.' + CAST(@mn AS varchar(2)) + '.' + CAST(@yr AS varchar(4)), 104);
        SET @monthEndExcl = DATEADD(day, 1, dbo.fn_LastDateOfMonth(@monthStart));
    END

    SET @sqlBase = REPLACE(REPLACE(@tpl, '{K}',  CASE WHEN @isFact = 1 THEN 'Actual' ELSE 'Original' END),
                                         '{K2}', CASE WHEN @isFact = 1 THEN 'actual' ELSE 'original' END);

    /* старая форма плана */
    SET @sql = REPLACE(@sqlBase, '{TGT}', '#capOld');
    SET @t = SYSDATETIME();
    EXEC sp_executesql
        @stmt = @sql, @params = @params,
        @campaignId = @campaignId, @actionID = @actionId, @actionIDString = @actionIDString,
        @agencyId = @agencyId, @rollerIDString = @rollerIDString,
        @monthStart = @monthStart, @monthEndExcl = @monthEndExcl,
        @startDate = @startDate, @finishDate = @finishDate;
    SET @msOld = DATEDIFF(millisecond, @t, SYSDATETIME());

    /* новая форма плана: тот же текст + подсказка */
    SET @sql = REPLACE(@sqlBase, '{TGT}', '#capNew') + N' OPTION (RECOMPILE)';
    SET @t = SYSDATETIME();
    EXEC sp_executesql
        @stmt = @sql, @params = @params,
        @campaignId = @campaignId, @actionID = @actionId, @actionIDString = @actionIDString,
        @agencyId = @agencyId, @rollerIDString = @rollerIDString,
        @monthStart = @monthStart, @monthEndExcl = @monthEndExcl,
        @startDate = @startDate, @finishDate = @finishDate;
    SET @msNew = DATEDIFF(millisecond, @t, SYSDATETIME());

    SELECT @d1 = COUNT(*) FROM
    (
        SELECT issueID, rollerId, issueDate, comment, positionID, price, broadcast, mmID FROM #capOld
        EXCEPT
        SELECT issueID, rollerId, issueDate, comment, positionID, price, broadcast, mmID FROM #capNew
    ) x;
    SELECT @d2 = COUNT(*) FROM
    (
        SELECT issueID, rollerId, issueDate, comment, positionID, price, broadcast, mmID FROM #capNew
        EXCEPT
        SELECT issueID, rollerId, issueDate, comment, positionID, price, broadcast, mmID FROM #capOld
    ) x;

    INSERT INTO #eqres (id, descr, rows_old, rows_new, diff_old_minus_new, diff_new_minus_old, ms_old, ms_new)
    SELECT @id, @descr, (SELECT COUNT(*) FROM #capOld), (SELECT COUNT(*) FROM #capNew), @d1, @d2, @msOld, @msNew;

    FETCH NEXT FROM cases INTO @id, @descr, @campaignId, @campaignTypeId, @actionId, @actionIDString,
                               @isFact, @mmString, @yr, @mn, @startDate, @finishDate, @onlyRollers,
                               @rollerIDString, @agencyId;
END
CLOSE cases; DEALLOCATE cases;

PRINT '';
PRINT '=== Раздел 2: эквивалентность #issue (ожидается 0 расхождений) ===';
SELECT [расхождений всего] = SUM(diff_old_minus_new + diff_new_minus_old),
       [случаев]           = COUNT(*),
       [строк проверено]   = SUM(CONVERT(bigint, rows_old))
FROM #eqres;

SELECT id, descr, rows_old, rows_new,
       [old-new] = diff_old_minus_new, [new-old] = diff_new_minus_old,
       ms_old, ms_new,
       [ускорение] = CAST(ms_old * 1.0 / NULLIF(ms_new, 0) AS decimal(8,1))
FROM #eqres ORDER BY id;

SELECT [СЛУЧАИ С РАСХОЖДЕНИЯМИ] = id, descr, rows_old, rows_new,
       diff_old_minus_new, diff_new_minus_old
FROM #eqres WHERE diff_old_minus_new <> 0 OR diff_new_minus_old <> 0 OR rows_old <> rows_new;
GO

/* ======================================================================= */
/* Раздел 3. Сквозная сверка набора №1 (ролики) двух процедур              */
/* ======================================================================= */

IF OBJECT_ID('dbo.MediaPlanRetrieve_v2_new') IS NULL
BEGIN
    PRINT '';
    PRINT '=== Раздел 3: ПРОПУЩЕН — нет dbo.MediaPlanRetrieve_v2_new для сравнения. ===';
    PRINT '    Это нормально после наката на прод: сравнивать уже не с чем,';
    PRINT '    доказательством эквивалентности остаётся раздел 2.';
END
ELSE
BEGIN
    DECLARE @rsOld TABLE (id int, rollerId int, nm nvarchar(4000), atn nvarchar(4000), dur int, q int);
    DECLARE @rsNew TABLE (id int, rollerId int, nm nvarchar(4000), atn nvarchar(4000), dur int, q int);
    DECLARE @buf  TABLE (rollerId int, nm nvarchar(4000), atn nvarchar(4000), dur int, q int);

    DECLARE @i int, @dsc varchar(80), @ci int, @ct tinyint, @ai int, @ais varchar(8000), @f bit,
            @mms varchar(8000), @y smallint, @m tinyint, @sd datetime, @fd datetime, @rs varchar(8000), @ag int;
    DECLARE @e3 int = 0;

    DECLARE c3 CURSOR LOCAL FAST_FORWARD FOR
        SELECT id, descr, campaignId, campaignTypeId, actionId, actionIDString, isFact,
               mmString, yr, mn, startDate, finishDate, rollerIDString, agencyId
        FROM #cases ORDER BY id;
    OPEN c3;
    FETCH NEXT FROM c3 INTO @i, @dsc, @ci, @ct, @ai, @ais, @f, @mms, @y, @m, @sd, @fd, @rs, @ag;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            DELETE FROM @buf;
            INSERT INTO @buf EXEC dbo.MediaPlanRetrieve_v2
                @campaignId = @ci, @campaignTypeId = @ct, @isFact = @f, @massmediaIDString = @mms,
                @year = @y, @month = @m, @actionId = @ai, @startDate = @sd, @finishDate = @fd,
                @onlyRollers = 1, @rollerIDString = @rs, @agencyId = @ag, @actionIDString = @ais;
            INSERT INTO @rsOld SELECT @i, rollerId, nm, atn, dur, q FROM @buf;

            DELETE FROM @buf;
            INSERT INTO @buf EXEC dbo.MediaPlanRetrieve_v2_new
                @campaignId = @ci, @campaignTypeId = @ct, @isFact = @f, @massmediaIDString = @mms,
                @year = @y, @month = @m, @actionId = @ai, @startDate = @sd, @finishDate = @fd,
                @onlyRollers = 1, @rollerIDString = @rs, @agencyId = @ag, @actionIDString = @ais;
            INSERT INTO @rsNew SELECT @i, rollerId, nm, atn, dur, q FROM @buf;
        END TRY
        BEGIN CATCH
            SET @e3 = @e3 + 1;
            PRINT 'Раздел 3, случай ' + CONVERT(varchar(10), @i) + ': ' + ERROR_MESSAGE();
        END CATCH
        FETCH NEXT FROM c3 INTO @i, @dsc, @ci, @ct, @ai, @ais, @f, @mms, @y, @m, @sd, @fd, @rs, @ag;
    END
    CLOSE c3; DEALLOCATE c3;

    PRINT '';
    PRINT '=== Раздел 3: сквозная сверка набора №1 (ожидается 0 расхождений) ===';
    SELECT [расхождений всего] =
           (SELECT COUNT(*) FROM (SELECT * FROM @rsOld EXCEPT SELECT * FROM @rsNew) x)
         + (SELECT COUNT(*) FROM (SELECT * FROM @rsNew EXCEPT SELECT * FROM @rsOld) x),
           [строк старая] = (SELECT COUNT(*) FROM @rsOld),
           [строк новая]  = (SELECT COUNT(*) FROM @rsNew),
           [ошибок вызова] = @e3;

    SELECT [РАСХОЖДЕНИЯ] = 'old', * FROM (SELECT * FROM @rsOld EXCEPT SELECT * FROM @rsNew) x
    UNION ALL
    SELECT 'new', * FROM (SELECT * FROM @rsNew EXCEPT SELECT * FROM @rsOld) x
    ORDER BY 2, 3;
END
GO

/* ======================================================================= */
/* Раздел 4. Сквозные замеры развёрнутой процедуры (@onlyRollers = 1)       */
/* ======================================================================= */

DECLARE @forceBench bit = 0;   -- 1 = мерить даже на старом плане (это надолго)

IF OBJECT_ID('tempdb..#bench') IS NOT NULL DROP TABLE #bench;
CREATE TABLE #bench (id int, descr varchar(80), ms int);

IF @forceBench = 0
   AND OBJECT_DEFINITION(OBJECT_ID('dbo.MediaPlanRetrieve_v2')) NOT LIKE '%OPTION (RECOMPILE)%'
BEGIN
    PRINT '';
    PRINT '=== Раздел 4: ПРОПУЩЕН — развёрнута старая версия процедуры. ===';
    PRINT '    На старом плане 35 сквозных вызовов идут много минут и грузят сервер,';
    PRINT '    а цифры «до» и «после» по каждому случаю уже есть в разделе 2';
    PRINT '    (колонки ms_old / ms_new). Нужно всё равно — поставьте @forceBench = 1.';
END
ELSE
BEGIN
    DECLARE @bi int, @bd varchar(80), @bci int, @bct tinyint, @bai int, @bais varchar(8000), @bf bit,
            @bmms varchar(8000), @by smallint, @bm tinyint, @bsd datetime, @bfd datetime,
            @brs varchar(8000), @bag int, @bt datetime2(7);

    DECLARE c4 CURSOR LOCAL FAST_FORWARD FOR
        SELECT id, descr, campaignId, campaignTypeId, actionId, actionIDString, isFact,
               mmString, yr, mn, startDate, finishDate, rollerIDString, agencyId
        FROM #cases ORDER BY id;
    OPEN c4;
    FETCH NEXT FROM c4 INTO @bi, @bd, @bci, @bct, @bai, @bais, @bf, @bmms, @by, @bm, @bsd, @bfd, @brs, @bag;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @bt = SYSDATETIME();
        EXEC dbo.MediaPlanRetrieve_v2
            @campaignId = @bci, @campaignTypeId = @bct, @isFact = @bf, @massmediaIDString = @bmms,
            @year = @by, @month = @bm, @actionId = @bai, @startDate = @bsd, @finishDate = @bfd,
            @onlyRollers = 1, @rollerIDString = @brs, @agencyId = @bag, @actionIDString = @bais;
        INSERT INTO #bench (id, descr, ms) VALUES (@bi, @bd, DATEDIFF(millisecond, @bt, SYSDATETIME()));
        FETCH NEXT FROM c4 INTO @bi, @bd, @bci, @bct, @bai, @bais, @bf, @bmms, @by, @bm, @bsd, @bfd, @brs, @bag;
    END
    CLOSE c4; DEALLOCATE c4;

    PRINT '';
    PRINT '=== Раздел 4: сквозные замеры dbo.MediaPlanRetrieve_v2 (@onlyRollers = 1) ===';
    SELECT id, descr, ms FROM #bench ORDER BY id;
    SELECT [сумма мс] = SUM(ms), [худший случай мс] = MAX(ms) FROM #bench;
END
GO

DROP TABLE #cases, #topCamp, #capOld, #capNew, #mm, #rr, #act, #eqres, #bench;
GO
PRINT '';
PRINT 'Готово. Ожидается: раздел 2 «расхождений всего» = 0, раздел 3 «расхождений всего» = 0.';
GO
