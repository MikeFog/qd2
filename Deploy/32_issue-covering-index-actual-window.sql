-- Фактическое окно выпуска, этап 4 «Деньги» — подготовка (docs/tasks/window-actual-switch.md §5.3; решение 10.10.2026).
-- Индекс IX_Issue_campaignID_covering получает actualWindowID в INCLUDE. Денежные процедуры этапа 4 (даты и пересчёт акции,
-- распределение по месяцам, счёт, бонусы, «По месяцам») ищут выпуски кампании по этому индексу; без окна выхода в нём
-- они читают всю таблицу Issue или ходят за каждым выпуском в кластерный индекс. Замер на ArtvisDev (месяц по всем
-- линейным кампаниям, join по окну выхода): см. Deploy/README.md, строка 32.
--
-- Перестройка ~160 МБ на 3,4 млн строк. SQL Server Express — без ONLINE: пока идёт, выпуски нельзя ставить, переносить
-- и удалять (чтение работает). Запускать в нерабочее время. Клиент не нужен. Идемпотентен: если actualWindowID уже в
-- индексе — ничего не делает.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 32_issue-covering-index-actual-window.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.Issue') AND i.name = N'IX_Issue_campaignID_covering'
      AND c.name = N'actualWindowID' AND ic.is_included_column = 1)
BEGIN
    DECLARE @t DATETIME2 = SYSDATETIME();
    CREATE NONCLUSTERED INDEX [IX_Issue_campaignID_covering]
        ON [dbo].[Issue]([campaignID] ASC, [originalWindowID] ASC)
        INCLUDE([rollerID], [issueID], [moduleIssueID], [packModuleIssueID], [ratio], [tariffPrice], [actualWindowID])
        WITH (DROP_EXISTING = ON);
    PRINT N'Индекс перестроен за ' + LTRIM(DATEDIFF(SECOND, @t, SYSDATETIME())) + N' с.';
END
ELSE
    PRINT N'actualWindowID уже в индексе — ничего не сделано.';
GO
-- Проверка
IF EXISTS (
    SELECT 1
    FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.Issue') AND i.name = N'IX_Issue_campaignID_covering'
      AND c.name = N'actualWindowID' AND ic.is_included_column = 1)
    PRINT N'ГОТОВО: IX_Issue_campaignID_covering включает actualWindowID.';
ELSE
    RAISERROR(N'32: индекс не перестроен.', 16, 1);
GO
