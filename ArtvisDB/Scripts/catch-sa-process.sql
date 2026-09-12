/*
    Кто ходит в Artvis под login "sa" и что делает.
    Прод, SQL Server 2022 Express (XE в Express есть, работает).

    КОНТЕКСТ
      Дедлоки 10.09.2026: победитель — ActionRecalculate под login "sa",
      hostname URANUS, host_process_id 824, clientapp ".Net SqlClient Data
      Provider" (= System.Data.SqlClient без Application Name в строке
      подключения → старое .NET Framework приложение/утилита), явные
      транзакции (user_transaction, trancount=2). В qd2.log этих вызовов
      нет — значит это НЕ qd2. И не 1С (та ходит под AdvertAgAdmin).

    ЧТО ДЕЛАЕМ
      Шаг 1 — моментальный снимок всех живых "sa"-сессий: IP, имя процесса,
              PID клиента, что выполняется сейчас.
      Шаг 2 — Extended Events на час: каждый login/подключение + каждый
              RPC/батч под "sa", с client_hostname / client_pid / sql_text.
      Шаг 3 — по client_hostname + client_pid идём на ту машину и находим
              процесс (инструкция внизу).

    Ничего не меняет в БД. XE-сессия останавливается и удаляется в конце.
*/

SET NOCOUNT ON;

/* ================================================================
   ШАГ 1. Снимок живых "sa"-сессий прямо сейчас
   ================================================================ */
SELECT
    s.session_id,
    s.login_name,
    c.client_net_address                         AS client_ip,
    c.client_tcp_port,
    s.host_name,
    s.host_process_id                            AS client_pid,
    s.program_name,
    s.client_interface_name,
    c.auth_scheme,
    s.login_time,
    s.last_request_start_time,
    s.last_request_end_time,
    s.status,
    s.open_transaction_count,
    r.command,
    r.wait_type,
    r.blocking_session_id,
    DB_NAME(COALESCE(r.database_id, s.database_id)) AS db,
    SUBSTRING(t.text,
        (r.statement_start_offset/2)+1,
        ((CASE r.statement_end_offset WHEN -1 THEN DATALENGTH(t.text)
          ELSE r.statement_end_offset END - r.statement_start_offset)/2)+1) AS running_stmt,
    t.text                                        AS full_batch
FROM sys.dm_exec_sessions s
LEFT JOIN sys.dm_exec_connections c ON c.session_id = s.session_id
LEFT JOIN sys.dm_exec_requests    r ON r.session_id = s.session_id
OUTER APPLY sys.dm_exec_sql_text(COALESCE(r.sql_handle, c.most_recent_sql_handle)) t
WHERE s.is_user_process = 1
  AND s.login_name = 'sa'
ORDER BY s.login_time;

/* Что "sa" выполнял в последнее время (по кэшу планов) — ловит и уже
   завершённые вызовы, если план ещё в кэше */
SELECT TOP 50
    qs.last_execution_time,
    qs.execution_count,
    DB_NAME(st.dbid)                              AS db,
    OBJECT_NAME(st.objectid, st.dbid)             AS object_name,
    SUBSTRING(st.text, (qs.statement_start_offset/2)+1,
        ((CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(st.text)
          ELSE qs.statement_end_offset END - qs.statement_start_offset)/2)+1) AS stmt
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
WHERE st.text LIKE '%ActionRecalculate%'
   OR OBJECT_NAME(st.objectid, st.dbid) = 'ActionRecalculate'
ORDER BY qs.last_execution_time DESC;

/* ================================================================
   ШАГ 2. Extended Events: писать всю активность "sa" в файл
   ================================================================
   Запустить один раз, оставить на час рабочего времени, потом ШАГ 3.
   Файл: <папка Log инстанса>\catch_sa_*.xel (рядом с system_health).
*/
IF EXISTS (SELECT 1 FROM sys.server_event_sessions WHERE name = 'catch_sa')
    DROP EVENT SESSION [catch_sa] ON SERVER;
GO

CREATE EVENT SESSION [catch_sa] ON SERVER
ADD EVENT sqlserver.login (
    ACTION (sqlserver.client_app_name, sqlserver.client_hostname,
            sqlserver.client_pid, sqlserver.session_id, sqlserver.username,
            sqlserver.database_name)
    WHERE ([sqlserver].[username] = N'sa')
),
ADD EVENT sqlserver.existing_connection (          -- уже открытые пулы, которые login не переоткрывают
    ACTION (sqlserver.client_app_name, sqlserver.client_hostname,
            sqlserver.client_pid, sqlserver.session_id, sqlserver.username)
    WHERE ([sqlserver].[username] = N'sa')
),
ADD EVENT sqlserver.rpc_completed (
    ACTION (sqlserver.client_app_name, sqlserver.client_hostname,
            sqlserver.client_pid, sqlserver.session_id, sqlserver.sql_text,
            sqlserver.database_name)
    WHERE ([sqlserver].[username] = N'sa')
),
ADD EVENT sqlserver.sql_batch_completed (
    ACTION (sqlserver.client_app_name, sqlserver.client_hostname,
            sqlserver.client_pid, sqlserver.session_id, sqlserver.sql_text,
            sqlserver.database_name)
    WHERE ([sqlserver].[username] = N'sa')
)
ADD TARGET package0.event_file (SET filename = N'catch_sa', max_file_size = 50, max_rollover_files = 4)
WITH (MAX_DISPATCH_LATENCY = 5 SECONDS, STARTUP_STATE = OFF);
GO

ALTER EVENT SESSION [catch_sa] ON SERVER STATE = START;
GO
PRINT 'XE-сессия catch_sa запущена. Оставить на час, затем выполнить ШАГ 3.';
GO

/* ================================================================
   ШАГ 3. Прочитать собранное и остановить
   ================================================================ */
/*  -- снять комментарий и выполнить через час --

;WITH x AS (
    SELECT CAST(event_data AS xml) AS ev
    FROM sys.fn_xe_file_target_read_file('catch_sa*.xel', NULL, NULL, NULL)
)
SELECT
    ev.value('(event/@name)[1]', 'varchar(50)')                                  AS event,
    DATEADD(HOUR, 3, ev.value('(event/@timestamp)[1]', 'datetime2'))             AS ts_msk,
    ev.value('(event/action[@name="client_hostname"]/value)[1]', 'nvarchar(128)')AS client_host,
    ev.value('(event/action[@name="client_pid"]/value)[1]', 'int')              AS client_pid,
    ev.value('(event/action[@name="client_app_name"]/value)[1]','nvarchar(256)') AS client_app,
    ev.value('(event/action[@name="session_id"]/value)[1]', 'int')              AS spid,
    ev.value('(event/action[@name="database_name"]/value)[1]', 'nvarchar(128)')  AS db,
    ev.value('(event/action[@name="sql_text"]/value)[1]', 'nvarchar(max)')       AS sql_text
FROM x
ORDER BY ts_msk;

-- сводка: кто (host+pid+app) сколько вызовов
;WITH x AS (
    SELECT CAST(event_data AS xml) AS ev
    FROM sys.fn_xe_file_target_read_file('catch_sa*.xel', NULL, NULL, NULL)
)
SELECT
    ev.value('(event/action[@name="client_hostname"]/value)[1]', 'nvarchar(128)') AS client_host,
    ev.value('(event/action[@name="client_pid"]/value)[1]', 'int')               AS client_pid,
    ev.value('(event/action[@name="client_app_name"]/value)[1]', 'nvarchar(256)') AS client_app,
    COUNT(*)                                                                      AS events,
    SUM(CASE WHEN ev.value('(event/action[@name="sql_text"]/value)[1]','nvarchar(max)')
                  LIKE '%ActionRecalculate%' THEN 1 ELSE 0 END)                   AS actionrecalc_calls
FROM x
GROUP BY
    ev.value('(event/action[@name="client_hostname"]/value)[1]', 'nvarchar(128)'),
    ev.value('(event/action[@name="client_pid"]/value)[1]', 'int'),
    ev.value('(event/action[@name="client_app_name"]/value)[1]', 'nvarchar(256)')
ORDER BY events DESC;

ALTER EVENT SESSION [catch_sa] ON SERVER STATE = STOP;
DROP EVENT SESSION [catch_sa] ON SERVER;
*/

/* ================================================================
   ШАГ 4. По client_host + client_pid — на той машине:
   ================================================================
     PowerShell:
       Get-Process -Id <client_pid> | Select-Object Id, ProcessName, Path, StartTime, Company, Description
       # если процесс уже перезапустился и PID другой — искать по имени:
       Get-CimInstance Win32_Service |
         Where-Object { $_.State -eq 'Running' -and $_.PathName -match '\.exe' } |
         Select-Object Name, StartName, PathName
       # и запланированные задачи:
       Get-ScheduledTask | Where-Object { $_.State -eq 'Running' } |
         Select-Object TaskName, TaskPath
     Строку подключения этого процесса смотреть в его .exe.config / app.config
     рядом с exe (там будет user id=sa) — заодно повод завести ему свой login
     с минимальными правами вместо sa.
*/
