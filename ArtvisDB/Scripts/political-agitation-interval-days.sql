-- Политическая агитация: ТОЛЬКО доработка "дни недели в интервалах-исключениях".
-- Применяется на базу, где интервалы-исключения УЖЕ развёрнуты
-- (political-agitation-seed.sql или political-agitation-intervals.sql).
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i political-agitation-interval-days.sql
--
-- Флаг -I (QUOTED_IDENTIFIER ON) обязателен, -f 65001 - тоже: в скрипте кириллица.
-- Скрипт идемпотентен, повторный прогон безопасен. Схема не меняется, старые
-- значения поля (без дней недели) продолжают работать как "все дни".
--
-- После скрипта нужно задеплоить 2 программных объекта (см. отчёт в конце):
--   dbo\Functions\fn_AgitationExcludeIntervals.sql   - ALTER (колонка dayMask)
--   dbo\Stored Procedures\AgitationFraming.sql       - ALTER (фильтр по дню недели)
-- и перезапустить клиент (метаданные форм кэшируются).

SET NOCOUNT ON;

-- ----------------------------------------------------------------------------
-- 1. Текст сообщения об ошибке формата - с описанием дней недели
-- ----------------------------------------------------------------------------
DECLARE @msg nvarchar(4000) = N'Интервалы-исключения заданы неверно. Формат: ЧЧ:ММ-ЧЧ:ММ, несколько интервалов в одной строке разделяются точкой с запятой. Строка может начинаться с дней недели, например: "пн-пт 16:00-16:55; 18:00-19:00", а на следующей строке - другой набор: "сб,вс 10:00-11:00". Дни: пн вт ср чт пт сб вс; допускаются диапазон (пн-пт) и перечисление (пн,ср,пт), строка без дней действует все дни недели. Начало интервала должно быть раньше конца, переход через полночь не поддерживается.';

IF EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'AgitationIntervalsInvalid')
BEGIN
	UPDATE [dbo].[iMessage] SET [message] = @msg WHERE name = 'AgitationIntervalsInvalid';
	PRINT 'Сообщение AgitationIntervalsInvalid обновлено';
END
ELSE
BEGIN
	INSERT INTO [dbo].[iMessage] (name, [message]) VALUES ('AgitationIntervalsInvalid', @msg);
	PRINT 'Сообщение AgitationIntervalsInvalid добавлено';
END

-- ----------------------------------------------------------------------------
-- 2. Подпись поля на вкладке "Политическая агитация" в паспорте станции.
--    Элемент <field> ищется по имени и заменяется целиком, какой бы ни была
--    текущая подпись; ничего больше в XML не трогаем.
-- ----------------------------------------------------------------------------
DECLARE @newField nvarchar(400) = N'<field caption="Интервалы, напр. пн-пт 16:00-16:55; 18:00-19:00" name="agitationExcludeIntervals" height="80"/>';
DECLARE @xml nvarchar(max) = (SELECT CAST(passport AS nvarchar(max)) FROM [dbo].[iEntity] WHERE entityID = 9);
DECLARE @namePos int = CHARINDEX(N'name="agitationExcludeIntervals"', @xml);

IF @namePos = 0
	PRINT 'ВНИМАНИЕ: поля agitationExcludeIntervals в паспорте станции нет - сначала разверните интервалы (political-agitation-intervals.sql)';
ELSE IF CHARINDEX(@newField, @xml) > 0
	PRINT 'Подпись поля интервалов уже актуальна';
ELSE
BEGIN
	-- начало элемента: ближайший '<field' слева от найденного имени
	DECLARE @fieldStart int = @namePos - CHARINDEX(N'dleif<', REVERSE(LEFT(@xml, @namePos))) - 4;
	DECLARE @fieldEnd int = CHARINDEX(N'/>', @xml, @namePos) + 1;

	IF @fieldStart <= 0 OR @fieldEnd <= @fieldStart
		PRINT 'ОШИБКА: не удалось найти границы элемента <field> с интервалами, подпись НЕ изменена';
	ELSE
	BEGIN
		SET @xml = STUFF(@xml, @fieldStart, @fieldEnd - @fieldStart + 1, @newField);

		-- страховка: если XML почему-то перестал быть валидным, ничего не сохраняем
		IF TRY_CAST(@xml AS xml) IS NULL
			PRINT 'ОШИБКА: после замены подписи XML паспорта стал невалидным, изменения НЕ сохранены';
		ELSE
		BEGIN
			UPDATE [dbo].[iEntity] SET passport = @xml WHERE entityID = 9;
			PRINT 'Подпись поля интервалов обновлена';
		END
	END
END

-- ----------------------------------------------------------------------------
-- 3. Отчёт: что готово и что ещё нужно задеплоить
-- ----------------------------------------------------------------------------
DECLARE @report table (пункт nvarchar(90), состояние nvarchar(24), деталь nvarchar(160));

INSERT INTO @report
SELECT N'Сообщение AgitationIntervalsInvalid про дни недели',
	CASE WHEN EXISTS (SELECT 1 FROM [dbo].[iMessage]
		WHERE name = 'AgitationIntervalsInvalid' AND [message] LIKE N'%пн-пт%') THEN N'OK' ELSE N'ОШИБКА' END,
	N'этот скрипт';

INSERT INTO @report
SELECT N'Подпись поля интервалов в паспорте станции',
	CASE WHEN CAST(passport AS nvarchar(max)) LIKE N'%пн-пт 16:00-16:55%' THEN N'OK' ELSE N'ОШИБКА' END,
	N'этот скрипт; после - перезапуск клиента'
FROM [dbo].[iEntity] WHERE entityID = 9;

INSERT INTO @report
SELECT N'XML паспорта станции валиден',
	CASE WHEN TRY_CAST(CAST(passport AS nvarchar(max)) AS xml) IS NOT NULL THEN N'OK' ELSE N'ОШИБКА' END,
	N'iEntity.entityID = 9'
FROM [dbo].[iEntity] WHERE entityID = 9;

INSERT INTO @report
SELECT N'fn_AgitationExcludeIntervals возвращает dayMask',
	CASE WHEN COALESCE(OBJECT_DEFINITION(OBJECT_ID('dbo.fn_AgitationExcludeIntervals')), '') LIKE '%dayMask%'
	THEN N'OK' ELSE N'НУЖЕН ДЕПЛОЙ' END,
	N'dbo\Functions\fn_AgitationExcludeIntervals.sql (ALTER)';

INSERT INTO @report
SELECT N'AgitationFraming учитывает день недели',
	CASE WHEN COALESCE(OBJECT_DEFINITION(OBJECT_ID('dbo.AgitationFraming')), '') LIKE '%windowDayBit%'
	THEN N'OK' ELSE N'НУЖЕН ДЕПЛОЙ' END,
	N'dbo\Stored Procedures\AgitationFraming.sql (ALTER)';

SELECT * FROM @report;

IF EXISTS (SELECT 1 FROM @report WHERE состояние <> N'OK')
	PRINT 'Есть пункты не в состоянии OK - смотрите таблицу выше (колонка "деталь" подсказывает, какой файл задеплоить)';
ELSE
	PRINT 'Всё на месте. Не забудьте перезапустить клиент - метаданные форм кэшируются.';
