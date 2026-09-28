/***************************************************************************************************
  dbo.stat_VolumeOfRealizationByMonth — починка ветвления «кампания целиком внутри периода»

  Зачем. В master (коммит 592173e от 11.09.2026, «finalPrice: … починка ветвления в отчёте по
  месяцам») условие перед GetPriceByPeriod переписано с явной проверкой NULL: у «between» с NULL
  результат unknown, и старое «if not (…)» тихо пропускало кампании с пустой датой начала или
  конца. На ArtvisDev 20.09.2026 обнаружена версия без этой правки.

  Что делает. Заменяет ОДНО условие (и добавляет 3 строки комментария) — правит ПРЯМО ИЗ
  РАЗВЁРНУТОГО ОПРЕДЕЛЕНИЯ (OBJECT_DEFINITION), всё остальное остаётся, как на этой базе.
  Повторный запуск безопасен (если правка уже есть — ничего не делает). Настройки
  QUOTED_IDENTIFIER/ANSI_NULLS сохраняются такими, какие они на базе.

  Запускать от sysadmin. Одним батчем, в транзакции. Скрипт применяется СРАЗУ (в конце COMMIT), пробного режима нет — перед запуском сделайте резервную копию. Не разбивать на батчи через GO.
***************************************************************************************************/

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @def NVARCHAR(MAX), @new NVARCHAR(MAX), @x NVARCHAR(MAX), @p INT, @q INT, @hdr INT;
DECLARE @qi BIT, @an BIT, @nl NVARCHAR(2);
DECLARE @proc SYSNAME = N'dbo.stat_VolumeOfRealizationByMonth';

IF OBJECT_ID(@proc, N'P') IS NULL
BEGIN
    PRINT N'stat_VolumeOfRealizationByMonth нет на этой базе — шаг не нужен.';
    ROLLBACK TRANSACTION;
    RETURN;
END;

SELECT @def = m.definition, @qi = m.uses_quoted_identifier, @an = m.uses_ansi_nulls
FROM sys.sql_modules m WHERE m.object_id = OBJECT_ID(@proc);
IF @def IS NULL
BEGIN
    RAISERROR(N'Остановлено: определение stat_VolumeOfRealizationByMonth недоступно (нужен sysadmin).', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

SET @nl = CASE WHEN CHARINDEX(NCHAR(13) + NCHAR(10), @def) > 0 THEN NCHAR(13) + NCHAR(10) ELSE NCHAR(10) END;

DECLARE @old NVARCHAR(400) = N'if not ((@campaignTypeID <> 4) and (@cStart between @start and @end) and (@cEnd between @start and @end))';

IF @def LIKE N'%@cStart is null%'
BEGIN
    PRINT N'stat_VolumeOfRealizationByMonth уже с правкой ветвления — ничего не делаем.';
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- Ровно одно старое условие.
SET @p = CHARINDEX(@old, @def);
IF @p = 0 OR CHARINDEX(@old, @def, @p + 1) > 0
BEGIN
    RAISERROR(N'Остановлено: в stat_VolumeOfRealizationByMonth нет ровно одного старого условия — править вручную.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

SET @new = LEFT(@def, @p - 1)
         + N'-- Кампания целиком внутри периода: @campaignPrice = c.finalPrice, а он' + @nl + N'			-- теперь уже со всеми скидками -- брать как есть. Иначе считаем по периоду.' + @nl + N'			-- Условие -- отрицание прежнего if, с явной проверкой NULL: у between' + @nl + N'			-- с NULL результат unknown, и not(unknown) ветку бы не открыл.' + @nl + N'			if @campaignTypeID = 4' + @nl + N'				or @cStart is null or @cEnd is null' + @nl + N'				or @cStart not between @start and @end' + @nl + N'				or @cEnd not between @start and @end'
         + SUBSTRING(@def, @p + LEN(@old), LEN(@def));

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
    RAISERROR(N'Остановлено: в stat_VolumeOfRealizationByMonth не найден заголовок CREATE PROC.', 16, 1);
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
       CASE WHEN m.definition LIKE N'%@cStart is null%' AND m.definition NOT LIKE N'%if not ((@campaignTypeID <> 4)%' THEN 1 ELSE 0 END AS has_null_safe_branch
FROM sys.sql_modules m JOIN sys.objects o ON o.object_id = m.object_id
WHERE o.object_id = OBJECT_ID(@proc);
SELECT settings_kept = CASE WHEN m.uses_quoted_identifier = @qi AND m.uses_ansi_nulls = @an THEN 1 ELSE 0 END
FROM sys.sql_modules m WHERE m.object_id = OBJECT_ID(@proc);   -- ожидается 1

COMMIT TRANSACTION;
PRINT N'=== ГОТОВО: изменения применены и зафиксированы (COMMIT). Если выше нет сообщения «Остановлено» — всё выполнено. ===';
