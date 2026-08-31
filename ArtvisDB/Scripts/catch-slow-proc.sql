/*  Поймать зависший вызов процедуры «в моменте».
    Выполнять на ПРОДЕ, пока идёт долгий ActionDeactivate/ActionActivate.
    Прогнать 3-4 раза с интервалом ~5 сек. Только читает.
    По умолчанию ловит ActionDeactivate — поменять @proc при необходимости.  */
SET NOCOUNT ON; SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
DECLARE @proc sysname = N'ActionDeactivate';

SELECT
    r.session_id               AS spid,
    s.login_name, s.host_name,
    r.status,
    r.blocking_session_id      AS blk_by,
    r.wait_type,                                 -- на чём стоит: LCK_* = блокировка, PAGEIOLATCH_* = диск, SOS_SCHEDULER_YIELD = CPU
    r.wait_time                AS wait_ms,
    r.last_wait_type,
    r.wait_resource,
    r.total_elapsed_time/1000  AS elapsed_sec,
    r.cpu_time/1000            AS cpu_sec,
    r.reads                    AS phys_reads,
    r.logical_reads,
    r.granted_query_memory*8/1024 AS granted_mb,
    -- какой именно оператор процедуры сейчас выполняется:
    SUBSTRING(t.text, (r.statement_start_offset/2)+1,
        ((CASE r.statement_end_offset WHEN -1 THEN DATALENGTH(t.text)
          ELSE r.statement_end_offset END - r.statement_start_offset)/2)+1) AS current_statement
FROM sys.dm_exec_requests r
JOIN sys.dm_exec_sessions s ON s.session_id = r.session_id
OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE t.text LIKE '%' + @proc + '%'
   OR EXISTS (SELECT 1 FROM sys.dm_exec_sql_text(r.plan_handle) pt WHERE pt.objectid = OBJECT_ID('dbo.'+@proc));

/* Кто блокирует, если blk_by <> 0 */
SELECT
    blocker = w.blocking_session_id,
    victim  = w.session_id,
    w.wait_type, w.wait_duration_ms, w.resource_description,
    blocker_login = bs.login_name, blocker_host = bs.host_name,
    blocker_last_input = CAST(ib.event_info AS nvarchar(1000))
FROM sys.dm_os_waiting_tasks w
JOIN sys.dm_exec_sessions bs ON bs.session_id = w.blocking_session_id
OUTER APPLY sys.dm_exec_input_buffer(w.blocking_session_id, NULL) ib
WHERE w.session_id IN (
    SELECT r.session_id FROM sys.dm_exec_requests r
    OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) t
    WHERE t.text LIKE '%' + @proc + '%');

/* Эскалация блокировок Issue/TariffWindow прямо сейчас.
   resource_associated_entity_id для OBJECT-локов = object_id (int),
   для KEY/RID/PAGE/HOBT = hobt_id (bigint) — резолвим через sys.partitions,
   иначе OBJECT_NAME(bigint) даёт arithmetic overflow. */
SELECT l.resource_type, l.request_mode, l.request_status, COUNT(*) AS cnt,
       obj = MAX(CASE WHEN l.resource_type = 'OBJECT'
                      THEN OBJECT_NAME(CONVERT(int, l.resource_associated_entity_id))
                      ELSE OBJECT_NAME(p.object_id) END)
FROM sys.dm_tran_locks l
LEFT JOIN sys.partitions p
       ON l.resource_type <> 'OBJECT'
      AND p.hobt_id = l.resource_associated_entity_id
WHERE l.resource_database_id = DB_ID()
  AND l.resource_type IN ('OBJECT','HOBT','PAGE','KEY','RID')
GROUP BY l.resource_type, l.request_mode, l.request_status,
         CASE WHEN l.resource_type = 'OBJECT'
              THEN l.resource_associated_entity_id ELSE p.object_id END
HAVING COUNT(*) > 50 OR l.request_mode IN ('X','IX','SIX','U')
ORDER BY cnt DESC;
