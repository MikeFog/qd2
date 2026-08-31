/*  LIVE-ТРИАЖ #2 — память и подозрительные сессии.
    Парный к live-triage.sql. Выполнять на ПРОДЕ целиком, только читает.
    Отвечает на: почему буферный пул не растёт (редакция Express? давление ОС?),
    кто держит долгие/странные подключения, какие ожидания копятся с рестарта.
    В блоке A подставить нужные spid в IN (...) — по результату live-triage.sql набор 1.  */
SET NOCOUNT ON; SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

/* A. Кто эти сессии: логин, программа, откуда, что прислали, полный текст запроса.
      Подставить spid из live-triage.sql набор 1; строка про sa ловит внешние подключения. */
SELECT
    s.session_id AS spid,
    s.login_name, s.host_name, s.program_name, s.client_interface_name,
    s.login_time, s.status AS sess_status,
    r.status AS req_status, r.wait_type, r.wait_resource,
    r.total_elapsed_time/1000 AS elapsed_sec,
    r.granted_query_memory*8/1024 AS granted_mem_mb,
    CAST(ib.event_info AS NVARCHAR(4000)) AS last_input,
    t.text AS full_sql
FROM sys.dm_exec_sessions s
LEFT JOIN sys.dm_exec_requests r ON r.session_id = s.session_id
OUTER APPLY sys.dm_exec_input_buffer(s.session_id, NULL) ib
OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE s.session_id IN (54, 64, 61)
   OR (s.login_name = 'sa' AND s.session_id > 50);

/* B. Когда стартовал SQL Server + давление памяти ОС */
SELECT
    si.sqlserver_start_time,
    DATEDIFF(MINUTE, si.sqlserver_start_time, GETDATE()) AS uptime_min,
    pm.physical_memory_in_use_kb/1024 AS sql_mem_in_use_mb,
    pm.large_page_allocations_kb/1024 AS large_pages_mb,
    pm.locked_page_allocations_kb/1024 AS locked_pages_mb,
    pm.page_fault_count,
    pm.memory_utilization_percentage AS mem_util_pct,
    pm.process_physical_memory_low,
    pm.process_virtual_memory_low
FROM sys.dm_os_sys_info si
CROSS JOIN sys.dm_os_process_memory pm;

/* C. Настройки памяти + сколько реально держит буферный пул */
SELECT
    (SELECT value_in_use FROM sys.configurations WHERE name='max server memory (MB)') AS max_mem_cfg_mb,
    (SELECT value_in_use FROM sys.configurations WHERE name='min server memory (MB)') AS min_mem_cfg_mb,
    (SELECT cntr_value/1024 FROM sys.dm_os_performance_counters WHERE counter_name='Total Server Memory (KB)') AS total_mem_mb,
    (SELECT cntr_value/1024 FROM sys.dm_os_performance_counters WHERE counter_name='Target Server Memory (KB)') AS target_mem_mb,
    (SELECT cntr_value/1024 FROM sys.dm_os_performance_counters WHERE counter_name='Database Cache Memory (KB)') AS buffer_pool_mb,
    (SELECT cntr_value    FROM sys.dm_os_performance_counters WHERE counter_name='Page life expectancy' AND object_name LIKE '%Buffer Manager%') AS ple_sec,
    (SELECT cntr_value    FROM sys.dm_os_performance_counters WHERE counter_name='Memory Grants Pending') AS mem_grants_pending,
    total_physical_memory_kb/1024 AS box_ram_mb,
    available_physical_memory_kb/1024 AS box_ram_free_mb
FROM sys.dm_os_sys_memory;

/* D. Все активные пользовательские запросы + суммарный запрошенный грант памяти */
SELECT
    r.session_id AS spid, s.login_name, s.host_name,
    OBJECT_NAME(t.objectid, t.dbid) AS proc_name,
    r.status, r.wait_type, r.blocking_session_id AS blk_by,
    r.total_elapsed_time/1000 AS elapsed_sec,
    r.cpu_time/1000 AS cpu_sec,
    r.granted_query_memory*8/1024 AS granted_mb,
    r.logical_reads, r.reads AS phys_reads
FROM sys.dm_exec_requests r
JOIN sys.dm_exec_sessions s ON s.session_id = r.session_id
OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE s.is_user_process = 1 AND r.session_id <> @@SPID
ORDER BY r.total_elapsed_time DESC;

/* E. Ожидания с момента старта — что копит сервер (top-10, без фонового шума) */
SELECT TOP 10 wait_type,
    wait_time_ms/1000 AS wait_sec_total,
    signal_wait_time_ms/1000 AS signal_sec_total,
    waiting_tasks_count
FROM sys.dm_os_wait_stats
WHERE wait_type NOT LIKE '%SLEEP%' AND wait_type NOT LIKE '%IDLE%'
  AND wait_type NOT LIKE 'XE%' AND wait_type NOT LIKE 'BROKER%'
  AND wait_type NOT IN ('CLR_AUTO_EVENT','DISPATCHER_QUEUE_SEMAPHORE','CHECKPOINT_QUEUE',
      'REQUEST_FOR_DEADLOCK_SEARCH','SQLTRACE_INCREMENTAL_FLUSH_SLEEP','FT_IFTS_SCHEDULER_IDLE_WAIT',
      'HADR_FILESTREAM_IOMGR_IOCOMPLETION','SP_SERVER_DIAGNOSTICS_SLEEP','WAIT_XTP_HOST_WAIT')
ORDER BY wait_time_ms DESC;
