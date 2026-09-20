/***************************************************************************************************
  Удаление «Брэндов», ШАГ 1: убрать колонку brandList из dbo.stat_RollerStatistic

  Колонку brandList (dbo.fn_BrandListByRollerId(rollerID)) никто не читает: RollerBrand пуста.
  Процедура правится ПРЯМО ИЗ РАЗВЁРНУТОГО ОПРЕДЕЛЕНИЯ (OBJECT_DEFINITION): вырезаются только
  два вызова функции; всё остальное остаётся, как на этой базе. Так на базу не уедут чужие
  правки репозитория. Идёт ПЕРЕД brand-cleanup-deploy.sql (тот проверит и остановится, если
  на функцию ещё кто-то ссылается).

  Запускать от sysadmin (иначе OBJECT_DEFINITION = NULL). Одним батчем, в транзакции.
  Для проверки — оставить ROLLBACK в конце; для применения — заменить на COMMIT.
  QUOTED_IDENTIFIER/ANSI_NULLS выставляются в самом скрипте (у процедуры они ON; sqlcmd без -I
  молча перезаписал бы их на OFF). Повторный запуск безопасен (ничего не найдёт — ничего не сделает).
***************************************************************************************************/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRANSACTION;

DECLARE @def NVARCHAR(MAX), @new NVARCHAR(MAX), @x NVARCHAR(MAX), @hdr INT, @q INT, @cnt INT, @all INT;
DECLARE @needle NVARCHAR(100) = N'dbo.fn_BrandListByRollerId(rollerID) as brandList,';

IF OBJECT_ID(N'dbo.stat_RollerStatistic', N'P') IS NULL
BEGIN
    PRINT N'stat_RollerStatistic нет на этой базе — шаг не нужен.';
    ROLLBACK TRANSACTION;
    RETURN;
END;

SET @def = OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_RollerStatistic'));
IF @def IS NULL
BEGIN
    RAISERROR(N'Остановлено: определение stat_RollerStatistic недоступно (нужен sysadmin).', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

IF @def NOT LIKE N'%fn_BrandListByRollerId%'
BEGIN
    PRINT N'stat_RollerStatistic уже без brandList — ничего не делаем.';
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- Все упоминания функции — именно вызовы вида «... as brandList,» (иначе форма другая — править вручную).
SET @cnt = (LEN(@def) - LEN(REPLACE(@def, @needle, N''))) / LEN(@needle);
SET @all = (LEN(@def) - LEN(REPLACE(@def, N'fn_BrandListByRollerId', N''))) / LEN(N'fn_BrandListByRollerId');
IF @cnt = 0 OR @cnt <> @all
BEGIN
    RAISERROR(N'Остановлено: упоминания fn_BrandListByRollerId в stat_RollerStatistic не такие, как ожидалось (вызовов: %d, всего упоминаний: %d).', 16, 1, @cnt, @all);
    ROLLBACK TRANSACTION;
    RETURN;
END;

SET @new = REPLACE(@def, @needle, N'');

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
    RAISERROR(N'Остановлено: в stat_RollerStatistic не найден заголовок CREATE PROC.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;
SET @new = STUFF(@new, @hdr, 6, N'ALTER');

EXEC sys.sp_executesql @new;

-- Проверки
SELECT removed_calls = @cnt;
SELECT o.name, m.uses_quoted_identifier, m.uses_ansi_nulls,
       refs_to_function = CASE WHEN m.definition LIKE N'%fn_BrandListByRollerId%' THEN 1 ELSE 0 END
FROM sys.sql_modules m JOIN sys.objects o ON o.object_id = m.object_id
WHERE o.name = N'stat_RollerStatistic';   -- ожидается 1 / 1 / 0

PRINT N'=== проверьте вывод выше. Для применения: заменить ROLLBACK на COMMIT ===';
ROLLBACK TRANSACTION;
-- COMMIT TRANSACTION;
