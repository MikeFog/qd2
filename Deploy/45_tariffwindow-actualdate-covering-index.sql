-- Веер: быстрее неделя и клик — индекс IX_TariffWindow_ActualDate по окну выхода становится покрывающим
-- (docs/tasks/web-action-forms.md, этап 4в).
--
-- TariffWindowWithRange (неделя веера) выбирает окна станций за неделю по фактическому времени выхода (windowDateActual) и берёт из
-- окна время занятости, флаги и счётчики позиций, цену, ёмкость и признак блокировки. Индекс по (massmediaID, windowDateActual)
-- содержал только windowId и duration, поэтому выборка шла полным сканированием кластерного индекса: 2 011 204 строк (23,7 тыс. страниц,
-- ~190 МБ) ради 1,3 тыс. окон — 283 мс процессора на каждый клик в веере, на проде с холодным кэшем — чтение с диска. Покрывающие индексы
-- по исходному времени (IX_TariffWindow_mm_date) после перехода на окно выхода (Deploy/24–35) для этой выборки не годятся.
--
-- Что делает скрипт: пересоздаёт IX_TariffWindow_ActualDate с теми же ключами и расширенным INCLUDE (tariffId, время занятости,
-- флаги и счётчики позиций, price, maxCapacity, isDisabled); если индекса на базе нет — создаёт его. Рост индекса ~56 → ~117 МБ.
-- Процедуры и клиенты не меняются.
--
-- SQL Server Express — без ONLINE: пока перестраивается (на ArtvisDev 5,5 с, на проде ожидается до минуты), окна нельзя менять —
-- выпуски нельзя ставить, переносить и удалять (чтение работает). Запускать в нерабочее время. Идемпотентен: если price уже в
-- INCLUDE индекса — ничего не делает. После наката перезапуск qd2 и веба не нужен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 45_tariffwindow-actualdate-covering-index.sql

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
    WHERE i.object_id = OBJECT_ID(N'dbo.TariffWindow') AND i.name = N'IX_TariffWindow_ActualDate'
      AND c.name = N'price' AND ic.is_included_column = 1)
BEGIN
    DECLARE @t DATETIME2 = SYSDATETIME();
    -- DROP_EXISTING на несуществующем индексе — ошибка 7999, поэтому на базе, где индекса нет, он просто создаётся.
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TariffWindow') AND name = N'IX_TariffWindow_ActualDate')
        CREATE NONCLUSTERED INDEX [IX_TariffWindow_ActualDate]
            ON [dbo].[TariffWindow]([massmediaID] ASC, [windowDateActual] ASC)
            INCLUDE([duration], [windowId], [tariffId], [timeInUseConfirmed], [timeInUseUnconfirmed],
                    [isFirstPositionOccupied], [isSecondPositionOccupied], [isLastPositionOccupied],
                    [firstPositionsUnconfirmed], [secondPositionsUnconfirmed], [lastPositionsUnconfirmed],
                    [price], [maxCapacity], [isDisabled])
            WITH (DROP_EXISTING = ON);
    ELSE
        CREATE NONCLUSTERED INDEX [IX_TariffWindow_ActualDate]
            ON [dbo].[TariffWindow]([massmediaID] ASC, [windowDateActual] ASC)
            INCLUDE([duration], [windowId], [tariffId], [timeInUseConfirmed], [timeInUseUnconfirmed],
                    [isFirstPositionOccupied], [isSecondPositionOccupied], [isLastPositionOccupied],
                    [firstPositionsUnconfirmed], [secondPositionsUnconfirmed], [lastPositionsUnconfirmed],
                    [price], [maxCapacity], [isDisabled]);
    PRINT N'Индекс построен за ' + LTRIM(DATEDIFF(MILLISECOND, @t, SYSDATETIME())) + N' мс.';
END
ELSE
    PRINT N'price уже в индексе — ничего не сделано.';
GO
-- Проверка
IF EXISTS (
    SELECT 1
    FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.TariffWindow') AND i.name = N'IX_TariffWindow_ActualDate'
      AND c.name = N'price' AND ic.is_included_column = 1)
    PRINT N'ГОТОВО: IX_TariffWindow_ActualDate покрывает выборку окон веера.';
ELSE
    RAISERROR(N'45: индекс не перестроен.', 16, 1);
GO
