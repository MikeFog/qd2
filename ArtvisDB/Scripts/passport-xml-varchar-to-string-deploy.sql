-- =============================================================================
-- Сведение типа текстового поля в XML фильтров к одному написанию:
--     type="varchar"  ->  type="string"
--
-- Зачем. FieldTypeResolver трактует "varchar", "string" (и "password") одинаково —
-- все дают IsString. Три написания одного понятия означают три ветки в каждом
-- рендерере; перед появлением второго (веб) язык сводится к одному написанию.
-- Разбор: docs/tasks/passport-xml-audit.md.
--
-- Поведение НЕ меняется: обе ветки резолвера дают один и тот же контрол.
--
-- Затрагивает (на 2026-09-10, одинаково в ArtvisDev и Artvis): 6 вхождений
-- в 4 сущностях, все в колонке filter, все — поля текстового поиска по названию:
--     2   bank             "Часть названия"
--     20  roller           "Начало названия", "Упоминание в названии"
--     193 logDeletedIssue  "Упоминание в названии"
--     228                  "Начало названия", "Упоминание в названии"
--
-- ВАЖНО по форме скрипта: всё одним батчем, без GO между BEGIN TRAN и COMMIT.
-- Урок инцидента 2026-09-08 (docs/studio-cleanup/investigation.md): при GO внутри
-- транзакции sqlcmd продолжает выполнение следующих батчей в автокоммите уже
-- после отката, и часть изменений применяется вопреки ROLLBACK.
--
-- Скрипт безопасно запускать повторно: если менять нечего, он фиксирует пустую
-- транзакцию и сообщает 0.
-- =============================================================================

SET NOCOUNT ON;

BEGIN TRAN;

DECLARE @dq   nchar(1)     = CHAR(34);
DECLARE @from nvarchar(50) = N'type=' + @dq + N'varchar' + @dq;
DECLARE @to   nvarchar(50) = N'type=' + @dq + N'string'  + @dq;

-- Состояние до
DECLARE @strok_do int =
    (SELECT COUNT(*) FROM iEntity WHERE CONVERT(nvarchar(max), filter) LIKE N'%' + @from + N'%');

-- Сколько строк из затрагиваемых сейчас разбираются как валидный XML
DECLARE @xml_ok_do int =
    (SELECT COUNT(*) FROM iEntity
     WHERE CONVERT(nvarchar(max), filter) LIKE N'%' + @from + N'%'
       AND TRY_CAST(CONVERT(nvarchar(max), filter) AS xml) IS NOT NULL);

-- filter имеет тип ntext, поэтому REPLACE только через CONVERT в nvarchar(max)
UPDATE iEntity
SET filter = REPLACE(CONVERT(nvarchar(max), filter), @from, @to)
WHERE CONVERT(nvarchar(max), filter) LIKE N'%' + @from + N'%';

DECLARE @obnovleno int = @@ROWCOUNT;

-- Состояние после
DECLARE @ostalos int =
    (SELECT COUNT(*) FROM iEntity WHERE CONVERT(nvarchar(max), filter) LIKE N'%' + @from + N'%');

DECLARE @xml_ok_posle int =
    (SELECT COUNT(*) FROM iEntity
     WHERE entityID IN (SELECT entityID FROM iEntity WHERE CONVERT(nvarchar(max), filter) LIKE N'%' + @to + N'%')
       AND TRY_CAST(CONVERT(nvarchar(max), filter) AS xml) IS NOT NULL);

SELECT @strok_do AS strok_do, @obnovleno AS obnovleno, @ostalos AS ostalos_posle;

-- Фиксируем только при ожидаемом результате: заменено ровно столько строк,
-- сколько нашлось, ничего не осталось, и XML не испортился.
IF @ostalos = 0 AND @obnovleno = @strok_do AND @xml_ok_posle >= @xml_ok_do
BEGIN
    COMMIT;
    SELECT 'ЗАФИКСИРОВАНО' AS itog;
END
ELSE
BEGIN
    ROLLBACK;
    SELECT 'ОТКАЧЕНО: результат не совпал с ожидаемым' AS itog;
END
