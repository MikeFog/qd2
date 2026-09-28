/***************************************************************************************************
  dbo.FirmWithActions1 — вернуть READ UNCOMMITTED (фикс дедлоков «журнал акций ⇄ ActionRecalculate»)

  Зачем. Журнальный SELECT не должен брать S-блокировки и попадать в дедлок с ActionRecalculate
  (пара №3, коммит 1a90ff1 от 10.09.2026, файл в master уже с этой строкой). На ArtvisDev 20.09.2026
  обнаружена версия без неё.

  Что делает. Вставляет ОДНУ строку сразу после «SET NOCOUNT on» — правит ПРЯМО ИЗ РАЗВЁРНУТОГО
  ОПРЕДЕЛЕНИЯ (OBJECT_DEFINITION), всё остальное остаётся, как на этой базе. Повторный запуск
  безопасен (если строка уже есть — ничего не делает). Настройки QUOTED_IDENTIFIER/ANSI_NULLS у
  процедуры сохраняются такими, какие они на базе (на ArtvisDev QUOTED_IDENTIFIER = OFF).

  Запускать от sysadmin. Одним батчем, в транзакции. Скрипт применяется СРАЗУ (в конце COMMIT), пробного режима нет — перед запуском сделайте резервную копию. Не разбивать на батчи через GO.
***************************************************************************************************/

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @def NVARCHAR(MAX), @new NVARCHAR(MAX), @x NVARCHAR(MAX), @p INT, @q INT, @hdr INT;
DECLARE @qi BIT, @an BIT, @nl NVARCHAR(2);
DECLARE @proc SYSNAME = N'dbo.FirmWithActions1';

IF OBJECT_ID(@proc, N'P') IS NULL
BEGIN
    PRINT N'FirmWithActions1 нет на этой базе — шаг не нужен.';
    ROLLBACK TRANSACTION;
    RETURN;
END;

SELECT @def = m.definition, @qi = m.uses_quoted_identifier, @an = m.uses_ansi_nulls
FROM sys.sql_modules m WHERE m.object_id = OBJECT_ID(@proc);
IF @def IS NULL
BEGIN
    RAISERROR(N'Остановлено: определение FirmWithActions1 недоступно (нужен sysadmin).', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

SET @nl = CASE WHEN CHARINDEX(NCHAR(13) + NCHAR(10), @def) > 0 THEN NCHAR(13) + NCHAR(10) ELSE NCHAR(10) END;

IF @def LIKE N'%READ UNCOMMITTED%'
BEGIN
    PRINT N'FirmWithActions1 уже с READ UNCOMMITTED — ничего не делаем.';
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- Место вставки: ровно одно «SET NOCOUNT on», перед ним — «AS».
SET @p = CHARINDEX(N'SET NOCOUNT on', @def);
IF @p = 0 OR CHARINDEX(N'SET NOCOUNT on', @def, @p + 14) > 0
BEGIN
    RAISERROR(N'Остановлено: в FirmWithActions1 не одно вхождение «SET NOCOUNT on» — править вручную.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;
SET @x = REPLACE(REPLACE(REPLACE(REPLACE(LEFT(@def, @p - 1), CHAR(13), N''), CHAR(10), N''), CHAR(9), N''), N' ', N'');
IF RIGHT(@x, 2) <> N'AS'
BEGIN
    RAISERROR(N'Остановлено: перед «SET NOCOUNT on» в FirmWithActions1 не «AS» — править вручную.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

SET @new = LEFT(@def, @p + 13) + @nl
         + N'SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; -- Важно для продакшена: журнальный SELECT не должен брать S-локи и попадать в дедлок с ActionRecalculate (см. project_deadlocks_prod пара №3). Как в HeadCompaniesWithActions.'
         + SUBSTRING(@def, @p + 14, LEN(@def));

-- Заголовок: первый «CREATE», за которым идёт PROC (не «Create date» из комментария).
SET @hdr = 0;
SET @q = CHARINDEX(N'CREATE', @new);
WHILE @q > 0 AND @hdr = 0
BEGIN
    SET @x = LTRIM(REPLACE(REPLACE(REPLACE(SUBSTRING(@new, @q + 6, 20), CHAR(13), N' '), CHAR(10), N' '), CHAR(9), N' '));
    IF @x LIKE N'PROC%' SET @hdr = @q;
    ELSE SET @q = CHARINDEX(N'CREATE', @new, @q + 6);
END;
IF @hdr = 0
BEGIN
    RAISERROR(N'Остановлено: в FirmWithActions1 не найден заголовок CREATE PROC.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;
SET @new = STUFF(@new, @hdr, 6, N'ALTER');

-- Настройки процедуры сохраняем такими, какие они на этой базе, а не такими, как у сеанса.
-- SET QUOTED_IDENTIFIER / ANSI_NULLS действуют на этапе разбора батча, поэтому ALTER идёт во
-- вложенном запросе: внешний выставляет нужные SET, внутренний sp_executesql их наследует.
DECLARE @outer NVARCHAR(200) =
      N'SET QUOTED_IDENTIFIER ' + CASE WHEN @qi = 1 THEN N'ON' ELSE N'OFF' END + N'; '
    + N'SET ANSI_NULLS ' + CASE WHEN @an = 1 THEN N'ON' ELSE N'OFF' END + N'; '
    + N'EXEC sys.sp_executesql @sql;';
EXEC sys.sp_executesql @outer, N'@sql NVARCHAR(MAX)', @sql = @new;

-- Проверки
SELECT o.name, m.uses_quoted_identifier AS quoted_identifier, m.uses_ansi_nulls AS ansi_nulls,
       CASE WHEN m.definition LIKE N'%READ UNCOMMITTED%' THEN 1 ELSE 0 END AS has_read_uncommitted
FROM sys.sql_modules m JOIN sys.objects o ON o.object_id = m.object_id
WHERE o.object_id = OBJECT_ID(@proc);
SELECT settings_kept = CASE WHEN m.uses_quoted_identifier = @qi AND m.uses_ansi_nulls = @an THEN 1 ELSE 0 END
FROM sys.sql_modules m WHERE m.object_id = OBJECT_ID(@proc);   -- ожидается 1

COMMIT TRANSACTION;
PRINT N'=== ГОТОВО: изменения применены и зафиксированы (COMMIT). Если выше нет сообщения «Остановлено» — всё выполнено. ===';
