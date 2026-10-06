/***************************************************************************************************
  broadcastStart, шаг 2: удаление мёртвых объектов слоя 0 (docs/broadcast-start.md, §4 и §7)

  Что удаляется (10 объектов)
    hlp_GetStartFinishFromIssueDateAndBroadcastStart   процедура целиком про broadcastStart
    rpt_Grid, rpt_Grid_v2                              вытеснены rpt_Grid_v3; rpt_Grid звал только
                                                       Client/Forms/GridReport/GridReportCreater.cs,
                                                       который не входит в Client.csproj
    fn_GetPrice, fn_GetPriceByPeriod, fn_GetPriceByPeriod1
    fn_statGetPrice, fn_statGetPriceByMonth            их звали только динамическим SQL две процедуры ниже
    stat_VolumeOfRealization2, stat_VolumeOfRealizationByMonth2
                                                       вытеснены версиями 3 (iStoredProcedure, меню)

  Проверено 06.10.2026 на ArtvisDev, Artvis (копия прода) и Tumen: ни одного вызова из других
  модулей (имя встречается только в комментариях stat_GetPrice_proc, stat_GetPriceByMonth_proc,
  stat_VolumeOfRealization3, stat_VolumeOfRealizationByMonth3, rpt_Grid_v3), из компилируемого C#
  (Client, FogSoft.Core, FogSoft.Web, FogSoft.WinForm), из iStoredProcedure и строковых колонок
  таблиц метаданных i*; прав (GRANT) на них нет. Тексты объектов остаются в истории git.

  Скрипт сам повторяет проверку по ссылкам: вырезает комментарии (/* */ и --) из текста каждого
  модуля, где встречается удаляемое имя, и останавливается, если имя осталось в коде. Отсутствующие
  объекты пропускает (DROP ... IF EXISTS). Повторный запуск безопасен.

  ── ПОРЯДОК ДЕПЛОЯ ────────────────────────────────────────────────────────────────────────────
  0. BACKUP DATABASE <база> TO DISK='...' WITH COPY_ONLY, INIT;
  1. Запустить ЭТОТ скрипт ЦЕЛИКОМ, от sysadmin. Он одним батчем, без GO, в транзакции.
     Скрипт применяется СРАЗУ (в конце COMMIT), пробного режима нет.
       sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 20_broadcast-start-dead-objects-drop.sql
  2. Клиент не нужен, перезапуск qd2/веба не нужен.

  ⚠ НЕ разбивать скрипт на батчи через GO с `SET XACT_ABORT ON`: при ошибке транзакция
     откатится, а sqlcmd продолжит следующие батчи в автокоммите.
***************************************************************************************************/

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 0. Проверка: на удаляемые объекты никто не ссылается из кода
-------------------------------------------------------------------------------------------------
DECLARE @drop TABLE (name SYSNAME PRIMARY KEY, kind NVARCHAR(20) NOT NULL);
INSERT @drop (name, kind) VALUES
    (N'hlp_GetStartFinishFromIssueDateAndBroadcastStart', N'PROCEDURE'),
    (N'rpt_Grid',                         N'PROCEDURE'),
    (N'rpt_Grid_v2',                      N'PROCEDURE'),
    (N'stat_VolumeOfRealization2',        N'PROCEDURE'),
    (N'stat_VolumeOfRealizationByMonth2', N'PROCEDURE'),
    (N'fn_GetPrice',                      N'FUNCTION'),
    (N'fn_GetPriceByPeriod',              N'FUNCTION'),
    (N'fn_GetPriceByPeriod1',             N'FUNCTION'),
    (N'fn_statGetPrice',                  N'FUNCTION'),
    (N'fn_statGetPriceByMonth',           N'FUNCTION');

-- Модули вне списка, где имя встречается хотя бы в тексте (с границами слова).
DECLARE @cand TABLE (objectName SYSNAME, refName SYSNAME, def NVARCHAR(MAX));
INSERT @cand (objectName, refName, def)
SELECT OBJECT_NAME(m.object_id), d.name, m.definition
FROM sys.sql_modules m
JOIN @drop d ON m.definition LIKE N'%[^a-z_0-9]' + d.name + N'[^a-z_0-9]%' COLLATE Latin1_General_CI_AS
WHERE OBJECT_NAME(m.object_id) NOT IN (SELECT name FROM @drop);

-- Вырезаем комментарии: сначала блочные /* ... */, потом -- до конца строки.
DECLARE @refs NVARCHAR(MAX) = N'';
DECLARE @obj SYSNAME, @ref SYSNAME, @def NVARCHAR(MAX), @s INT, @e INT;
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT objectName, refName, def FROM @cand;
OPEN c;
FETCH NEXT FROM c INTO @obj, @ref, @def;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @s = CHARINDEX(N'/*', @def);
    WHILE @s > 0
    BEGIN
        SET @e = CHARINDEX(N'*/', @def, @s + 2);
        IF @e = 0 SET @e = LEN(@def);
        SET @def = STUFF(@def, @s, @e - @s + 2, N' ');
        SET @s = CHARINDEX(N'/*', @def);
    END;

    IF EXISTS (
        SELECT 1
        FROM STRING_SPLIT(REPLACE(@def, NCHAR(13), N''), NCHAR(10)) l
        CROSS APPLY (SELECT N' ' + CASE WHEN CHARINDEX(N'--', l.value) > 0
                                        THEN LEFT(l.value, CHARINDEX(N'--', l.value) - 1)
                                        ELSE l.value END + N' ' AS code) x
        WHERE x.code LIKE N'%[^a-z_0-9]' + @ref + N'[^a-z_0-9]%' COLLATE Latin1_General_CI_AS)
        SET @refs += @obj + N' → ' + @ref + N'; ';

    FETCH NEXT FROM c INTO @obj, @ref, @def;
END;
CLOSE c;
DEALLOCATE c;

IF @refs <> N''
BEGIN
    RAISERROR(N'Остановлено: на удаляемые объекты ещё ссылаются из кода: %s Ничего не удалено.', 16, 1, @refs);
    ROLLBACK TRANSACTION;
    RETURN;
END;

SELECT objectName AS comment_only_mentions, refName FROM @cand ORDER BY objectName, refName;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 1. Удаление (сначала процедуры — две из них зовут функции динамическим SQL)
-------------------------------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS dbo.hlp_GetStartFinishFromIssueDateAndBroadcastStart;
DROP PROCEDURE IF EXISTS dbo.rpt_Grid;
DROP PROCEDURE IF EXISTS dbo.rpt_Grid_v2;
DROP PROCEDURE IF EXISTS dbo.stat_VolumeOfRealization2;
DROP PROCEDURE IF EXISTS dbo.stat_VolumeOfRealizationByMonth2;
DROP FUNCTION  IF EXISTS dbo.fn_GetPrice;
DROP FUNCTION  IF EXISTS dbo.fn_GetPriceByPeriod;
DROP FUNCTION  IF EXISTS dbo.fn_GetPriceByPeriod1;
DROP FUNCTION  IF EXISTS dbo.fn_statGetPrice;
DROP FUNCTION  IF EXISTS dbo.fn_statGetPriceByMonth;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 2. Сверка
-------------------------------------------------------------------------------------------------
DECLARE @leftover INT = (SELECT COUNT(*) FROM @drop d WHERE OBJECT_ID(N'dbo.' + d.name) IS NOT NULL);
SELECT @leftover AS leftover_objects,
       (SELECT COUNT(*) FROM iStoredProcedure sp JOIN @drop d ON d.name = sp.name) AS leftover_iStoredProcedure;

IF @leftover <> 0
BEGIN
    RAISERROR(N'Остановлено: после удаления осталось объектов: %d. Ничего не удалено.', 16, 1, @leftover);
    ROLLBACK TRANSACTION;
    RETURN;
END;

COMMIT TRANSACTION;
PRINT N'=== ГОТОВО: изменения применены и зафиксированы (COMMIT). Удалено мёртвых объектов broadcastStart (слой 0) — до 10.';
