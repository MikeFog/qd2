-- Флаг iEntity.isMassDeleteAllowed: SmartGrid может удалять несколько выделенных строк сразу (Del).
--
-- Флаг = 1 означает: после удаления объекта этой сущности никакой пост-обработки не нужно
-- (пересчёт акции и т.п.). Значение по умолчанию 0 - массовое удаление выключено.
-- Сейчас флаг стоит только у сущности Tariff (entityID = 81, «Тариф» прайс-листа).
--
-- Скрипт идемпотентен: колонка добавляется, только если её нет; флаг ставится повторно без вреда.
-- Один батч в транзакции, применяется сразу (COMMIT в конце), без GO внутри.
-- Клиент читает флаг при загрузке метаданных - пользователям достаточно перезапустить qd2.
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i entity-mass-delete-flag-deploy.sql

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.iEntity', N'isMassDeleteAllowed') IS NULL
BEGIN
    ALTER TABLE dbo.iEntity
        ADD isMassDeleteAllowed BIT NOT NULL
        CONSTRAINT DF_iEntity_isMassDeleteAllowed DEFAULT (0);
    PRINT N'Колонка iEntity.isMassDeleteAllowed добавлена.';
END
ELSE
    PRINT N'Колонка iEntity.isMassDeleteAllowed уже есть.';

-- Динамический SQL: колонка появляется в этом же батче, прямой UPDATE не скомпилируется.
DECLARE @rows INT;
EXEC sp_executesql
    N'UPDATE dbo.iEntity SET isMassDeleteAllowed = 1
      WHERE entityID = 81 AND codeName = N''tariff'' AND isMassDeleteAllowed = 0;
      SET @n = @@ROWCOUNT;',
    N'@n INT OUTPUT', @n = @rows OUTPUT;

IF @rows = 0 AND NOT EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 81 AND codeName = N'tariff')
BEGIN
    RAISERROR(N'Остановлено: сущность Tariff (entityID = 81, codeName = tariff) не найдена - база не совпадает с ожидаемой.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

PRINT N'Флаг isMassDeleteAllowed выставлен у сущностей: ' + CAST(@rows AS NVARCHAR(10)) + N' (0 - уже был выставлен).';

COMMIT TRANSACTION;
PRINT N'=== ГОТОВО: изменения применены и зафиксированы (COMMIT) ===';
