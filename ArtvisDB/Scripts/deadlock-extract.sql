/*
    Выгрузка дедлоков из system_health на ПРОДЕ (SQL Server 2022 Express, MSSQL16.SQLEXPRESS).
    Запускать в SSMS на прод-инстансе. Читает event_file, НЕ ring_buffer (там дедлоки вытесняются за часы).
    См. reference: путь к .xel, грабли XML-разбора.

    Результат 1 — разбивка по дням за весь диапазон файлов (проверить, что 10.09 попал).
    Результат 2 — полный граф каждого дедлока за нужную дату: process-list + resource-list.
    Результат 3 — сырой XML (сохранить как .xdl: правый клик по ячейке -> Save As -> *.xdl,
                  открывается графически).
*/

SET NOCOUNT ON;

DECLARE @xelPath nvarchar(400) =
    N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Log\system_health*.xel';

DECLARE @targetDate date = '2026-09-10';   -- интересующий день

------------------------------------------------------------------
-- Разобрать все xml_deadlock_report во временную таблицу
------------------------------------------------------------------
IF OBJECT_ID('tempdb..#dl') IS NOT NULL DROP TABLE #dl;

SELECT
    CAST(event_data AS xml) AS x,
    -- timestamp события берём из самого XML, не из имени файла (LastWriteTime врёт)
    CAST(CAST(event_data AS xml).value('(event/@timestamp)[1]', 'datetime2') AS datetime) AS ts_utc
INTO #dl
FROM sys.fn_xe_file_target_read_file(@xelPath, NULL, NULL, NULL)
WHERE object_name = N'xml_deadlock_report';

------------------------------------------------------------------
-- Результат 1: сколько дедлоков в день за весь диапазон
------------------------------------------------------------------
SELECT
    CAST(DATEADD(HOUR, 3, ts_utc) AS date) AS day_msk,   -- +3 = МСК
    COUNT(*)                                AS deadlocks
FROM #dl
GROUP BY CAST(DATEADD(HOUR, 3, ts_utc) AS date)
ORDER BY day_msk;

SELECT MIN(ts_utc) AS first_event_utc, MAX(ts_utc) AS last_event_utc, COUNT(*) AS total
FROM #dl;

------------------------------------------------------------------
-- Результат 2: процессы и ресурсы дедлоков за @targetDate
------------------------------------------------------------------
;WITH d AS (
    SELECT DATEADD(HOUR, 3, ts_utc) AS ts_msk, x
    FROM #dl
    WHERE CAST(DATEADD(HOUR, 3, ts_utc) AS date) = @targetDate
)
SELECT
    d.ts_msk,
    p.value('@id', 'nvarchar(50)')                                            AS process_id,
    p.value('@spid', 'int')                                                   AS spid,
    p.value('@status', 'nvarchar(20)')                                        AS status,
    p.value('@lockMode', 'nvarchar(20)')                                      AS lock_mode_held,
    p.value('@waitresource', 'nvarchar(200)')                                 AS wait_resource,
    p.value('@transactionname', 'nvarchar(100)')                              AS tran_name,
    p.value('@clientapp', 'nvarchar(100)')                                    AS client_app,
    p.value('@hostname', 'nvarchar(100)')                                     AS host,
    p.value('@loginname', 'nvarchar(128)')                                    AS login,
    p.value('@isolationlevel', 'nvarchar(60)')                                AS iso_level,
    -- верхний кадр стека = что выполнялось В МОМЕНТ дедлока (не что набрало блокировки раньше)
    p.value('(executionStack/frame/@procname)[1]', 'nvarchar(257)')           AS top_procname,
    p.value('(executionStack/frame/@line)[1]', 'int')                         AS top_line,
    LTRIM(RTRIM(p.value('(inputbuf)[1]', 'nvarchar(max)')))                    AS input_buf
FROM d
CROSS APPLY d.x.nodes('event/data/value/deadlock/process-list/process') AS pl(p)
ORDER BY d.ts_msk, process_id;

-- ресурсы (кто что держит / кто чего ждёт) — без этого диагноз не поставить
;WITH d AS (
    SELECT DATEADD(HOUR, 3, ts_utc) AS ts_msk, x
    FROM #dl
    WHERE CAST(DATEADD(HOUR, 3, ts_utc) AS date) = @targetDate
)
SELECT
    d.ts_msk,
    r.value('local-name(.)', 'nvarchar(50)')          AS resource_kind,   -- keylock / pagelock / objectlock ...
    r.value('@objectname', 'nvarchar(200)')           AS object_name,
    r.value('@indexname', 'nvarchar(200)')            AS index_name,
    r.value('@mode', 'nvarchar(20)')                  AS request_mode,
    o.value('@id', 'nvarchar(50)')                    AS owner_process,
    o.value('@mode', 'nvarchar(20)')                  AS owner_mode,
    w.value('@id', 'nvarchar(50)')                    AS waiter_process,
    w.value('@mode', 'nvarchar(20)')                  AS waiter_mode
FROM d
CROSS APPLY d.x.nodes('event/data/value/deadlock/resource-list/*') AS rl(r)
OUTER APPLY r.nodes('owner-list/owner') AS ol(o)
OUTER APPLY r.nodes('waiter-list/waiter') AS wl(w)
ORDER BY d.ts_msk, resource_kind, object_name;

------------------------------------------------------------------
-- Результат 3: сырой XML графа за @targetDate (сохранить как .xdl)
------------------------------------------------------------------
SELECT
    DATEADD(HOUR, 3, ts_utc) AS ts_msk,
    x.query('event/data/value/deadlock') AS deadlock_graph
FROM #dl
WHERE CAST(DATEADD(HOUR, 3, ts_utc) AS date) = @targetDate
ORDER BY ts_msk;
