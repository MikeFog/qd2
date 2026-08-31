/*  LIVE-ТРИАЖ: выполнять на ПРОДЕ в SSMS целиком, прямо сейчас.
    Отдаёт 6 результатов. Ничего не меняет. Повторить 2-3 раза подряд.  */
SET NOCOUNT ON; SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

/* 1. ЧТО СЕЙЧАС ВЫПОЛНЯЕТСЯ (кроме idle) — кто кого блокирует, чем занят */
SELECT
    r.session_id            AS spid,
    s.login_name,
    s.host_name,
    r.status,
    r.command,
    r.blocking_session_id   AS blk_by,
    r.wait_type,
    r.wait_time             AS wait_ms,
    r.last_wait_type,
    r.cpu_time,
    r.total_elapsed_time    AS elapsed_ms,
    r.reads, r.writes, r.logical_reads,
    r.open_transaction_count AS open_tran,
    OBJECT_NAME(st.objectid, st.dbid) AS proc_name,
    SUBSTRING(st.text, (r.statement_start_offset/2)+1,
        ((CASE r.statement_end_offset WHEN -1 THEN DATALENGTH(st.text)
          ELSE r.statement_end_offset END - r.statement_start_offset)/2)+1) AS running_stmt
FROM sys.dm_exec_requests r
JOIN sys.dm_exec_sessions s ON s.session_id = r.session_id
OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) st
WHERE r.session_id <> @@SPID AND s.is_user_process = 1
ORDER BY r.blocking_session_id DESC, r.total_elapsed_time DESC;

/* 2. ЦЕПОЧКИ БЛОКИРОВОК — кто голова */
SELECT
    w.session_id            AS waiter_spid,
    w.wait_type,
    w.wait_duration_ms,
    w.blocking_session_id   AS blocked_by_spid,
    w.resource_description,
    st.text AS blocked_stmt
FROM sys.dm_os_waiting_tasks w
OUTER APPLY (SELECT sql_handle FROM sys.dm_exec_requests WHERE session_id = w.session_id) rr
OUTER APPLY sys.dm_exec_sql_text(rr.sql_handle) st
WHERE w.session_id > 50 AND w.blocking_session_id IS NOT NULL
ORDER BY w.wait_duration_ms DESC;

/* 3. ГОЛОВА ЦЕПОЧКИ: что держит транзакцию и почему не отпускает */
SELECT
    s.session_id AS spid, s.login_name, s.host_name, s.status AS sess_status,
    s.last_request_start_time, s.last_request_end_time,
    DATEDIFF(SECOND, s.last_request_end_time, GETDATE()) AS idle_sec,
    t.open_transaction_count,
     tr.name AS tran_name, tr.transaction_begin_time,
    ib.event_type AS last_input_type,
    CAST(ib.event_info AS NVARCHAR(2000)) AS last_input
FROM sys.dm_exec_sessions s
LEFT JOIN sys.dm_exec_requests t ON t.session_id = s.session_id
LEFT JOIN sys.dm_tran_session_transactions tst ON tst.session_id = s.session_id
LEFT JOIN sys.dm_tran_active_transactions tr ON tr.transaction_id = tst.transaction_id
OUTER APPLY sys.dm_exec_input_buffer(s.session_id, NULL) ib
WHERE s.session_id IN (SELECT blocking_session_id FROM sys.dm_exec_requests WHERE blocking_session_id > 0)
   OR s.session_id IN (SELECT blocking_session_id FROM sys.dm_os_waiting_tasks WHERE blocking_session_id > 0);

/* 4. АГРЕГАТ ОЖИДАНИЙ ПРЯМО СЕЙЧАС (по пользовательским задачам, без фонового шума) */
SELECT wait_type, COUNT(*) AS tasks, SUM(wait_duration_ms) AS total_wait_ms
FROM sys.dm_os_waiting_tasks
WHERE session_id > 50
  AND wait_type NOT IN ('DISPATCHER_QUEUE_SEMAPHORE','BROKER_TO_FLUSH','BROKER_TASK_STOP',
      'SLEEP_TASK','WAITFOR','LAZYWRITER_SLEEP','XE_TIMER_EVENT','REQUEST_FOR_DEADLOCK_SEARCH',
      'SQLTRACE_INCREMENTAL_FLUSH_SLEEP','FT_IFTS_SCHEDULER_IDLE_WAIT','CHECKPOINT_QUEUE')
GROUP BY wait_type
ORDER BY total_wait_ms DESC;

/* 5. ПЛАНИРОВЩИКИ: очередь в CPU (runnable) + давление памяти */
SELECT
    (SELECT COUNT(*) FROM sys.dm_os_schedulers WHERE status='VISIBLE ONLINE') AS schedulers,
    (SELECT SUM(runnable_tasks_count) FROM sys.dm_os_schedulers WHERE status='VISIBLE ONLINE') AS runnable_now,
    (SELECT SUM(current_tasks_count)  FROM sys.dm_os_schedulers WHERE status='VISIBLE ONLINE') AS tasks_now,
    (SELECT cntr_value FROM sys.dm_os_performance_counters WHERE counter_name='Page life expectancy' AND object_name LIKE '%Buffer Manager%') AS ple_sec,
    (SELECT cntr_value/1024 FROM sys.dm_os_performance_counters WHERE counter_name='Total Server Memory (KB)') AS server_mem_mb,
    (SELECT cntr_value/1024 FROM sys.dm_os_performance_counters WHERE counter_name='Target Server Memory (KB)') AS target_mem_mb,
    (SELECT COUNT(*) FROM sys.dm_exec_requests r JOIN sys.dm_exec_sessions se ON se.session_id=r.session_id
        WHERE r.session_id>50 AND se.is_user_process=1) AS active_requests;

/* 6. ТОП КЭШ-ПЛАНОВ ПО СУММАРНОМУ CPU ЗА ПОСЛЕДНЕЕ ВРЕМЯ (что жрёт сервер) */
SELECT TOP 15
    OBJECT_NAME(ps.object_id, ps.database_id) AS proc_name,
    ps.execution_count,
    ps.total_worker_time/1000       AS cpu_ms_total,
    ps.total_worker_time/ps.execution_count/1000 AS cpu_ms_avg,
    ps.total_elapsed_time/ps.execution_count/1000 AS elapsed_ms_avg,
    ps.total_logical_reads/ps.execution_count     AS reads_avg,
    ps.last_execution_time
FROM sys.dm_exec_procedure_stats ps
ORDER BY ps.total_worker_time DESC;
