-- Политическая агитация: ТОЛЬКО доработка "интервалы-исключения".
-- Применяется на базу, где основной функционал агитации УЖЕ развёрнут
-- (типы роликов 6/7/44/55, три колонки роликов обвязки, вкладка паспорта).
-- Полное развёртывание с нуля - political-agitation-seed.sql.
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i political-agitation-intervals.sql
--
-- Флаг -I (QUOTED_IDENTIFIER ON) обязателен.
-- Скрипт идемпотентен, повторный прогон безопасен.
--
-- После скрипта нужно задеплоить 4 программных объекта (см. отчёт в конце):
--   dbo\Functions\fn_AgitationExcludeIntervals.sql   - НОВАЯ функция (CREATE)
--   dbo\Views\vMassmedia.sql                          - ALTER
--   dbo\Stored Procedures\MassmediaIUD.sql            - ALTER
--   dbo\Stored Procedures\AgitationFraming.sql        - ALTER
-- и перезапустить клиент (метаданные форм кэшируются).

SET NOCOUNT ON;

-- ----------------------------------------------------------------------------
-- 1. Колонка с интервалами. Тип doubleString - движок паспорта рисует его
--    многострочным полем.
-- ----------------------------------------------------------------------------
IF COL_LENGTH('dbo.MassMedia', 'agitationExcludeIntervals') IS NULL
BEGIN
	ALTER TABLE [dbo].[MassMedia] ADD [agitationExcludeIntervals] [dbo].[doubleString] NULL;
	PRINT 'Колонка MassMedia.agitationExcludeIntervals добавлена';
END
ELSE
	PRINT 'Колонка MassMedia.agitationExcludeIntervals уже есть';

-- ----------------------------------------------------------------------------
-- 2. Сообщение об ошибке формата
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'AgitationIntervalsInvalid')
BEGIN
	INSERT INTO [dbo].[iMessage] (name, [message])
	VALUES ('AgitationIntervalsInvalid',
		N'Интервалы-исключения заданы неверно. Формат: ЧЧ:ММ-ЧЧ:ММ, несколько интервалов разделяются точкой с запятой, например: 16:00-16:55; 18:00-19:00. Начало интервала должно быть раньше конца, переход через полночь не поддерживается.');
	PRINT 'Сообщение AgitationIntervalsInvalid добавлено';
END
ELSE
	PRINT 'Сообщение AgitationIntervalsInvalid уже есть';

-- ----------------------------------------------------------------------------
-- 3. Поле на вкладке "Политическая агитация" в паспорте станции.
--    Вставляем перед закрывающим </page> уже существующей вкладки, ничего
--    больше в XML не трогаем.
-- ----------------------------------------------------------------------------
DECLARE @xml nvarchar(max) = (SELECT CAST(passport AS nvarchar(max)) FROM [dbo].[iEntity] WHERE entityID = 9);
DECLARE @pageStart int = CHARINDEX(N'<page caption="Политическая агитация">', @xml);

IF @pageStart = 0
	PRINT 'ВНИМАНИЕ: вкладки "Политическая агитация" в паспорте станции нет - сначала разверните основной функционал (political-agitation-seed.sql)';
ELSE IF @xml LIKE N'%name="agitationExcludeIntervals"%'
	PRINT 'Поле интервалов на вкладке паспорта уже есть';
ELSE
BEGIN
	DECLARE @pageEnd int = CHARINDEX(N'</page>', @xml, @pageStart);

	SET @xml = STUFF(@xml, @pageEnd, 0,
		N'	<separator />
		<field caption="Интервалы без идентификаторов СМИ, напр. 16:00-16:55; 18:00-19:00" name="agitationExcludeIntervals"/>
	');

	-- страховка: если XML почему-то перестал быть валидным, ничего не сохраняем
	IF TRY_CAST(@xml AS xml) IS NULL
		PRINT 'ОШИБКА: после вставки поля XML паспорта стал невалидным, изменения НЕ сохранены';
	ELSE
	BEGIN
		UPDATE [dbo].[iEntity] SET passport = @xml WHERE entityID = 9;
		PRINT 'Поле интервалов добавлено на вкладку паспорта';
	END
END

-- ----------------------------------------------------------------------------
-- 4. Отчёт: что готово и что ещё нужно задеплоить
-- ----------------------------------------------------------------------------
DECLARE @report table (пункт nvarchar(90), состояние nvarchar(24), деталь nvarchar(160));

INSERT INTO @report
SELECT N'Колонка MassMedia.agitationExcludeIntervals',
	CASE WHEN COL_LENGTH('dbo.MassMedia', 'agitationExcludeIntervals') IS NOT NULL THEN N'OK' ELSE N'ОШИБКА' END,
	N'этот скрипт';

INSERT INTO @report
SELECT N'Сообщение AgitationIntervalsInvalid',
	CASE WHEN EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'AgitationIntervalsInvalid') THEN N'OK' ELSE N'ОШИБКА' END,
	N'этот скрипт';

INSERT INTO @report
SELECT N'Поле интервалов в паспорте станции',
	CASE WHEN CAST(passport AS nvarchar(max)) LIKE N'%name="agitationExcludeIntervals"%' THEN N'OK' ELSE N'ОШИБКА' END,
	N'этот скрипт; после - перезапуск клиента'
FROM [dbo].[iEntity] WHERE entityID = 9;

INSERT INTO @report
SELECT N'XML паспорта станции валиден',
	CASE WHEN TRY_CAST(CAST(passport AS nvarchar(max)) AS xml) IS NOT NULL THEN N'OK' ELSE N'ОШИБКА' END,
	N'iEntity.entityID = 9'
FROM [dbo].[iEntity] WHERE entityID = 9;

INSERT INTO @report
SELECT N'Функция fn_AgitationExcludeIntervals',
	CASE WHEN OBJECT_ID('dbo.fn_AgitationExcludeIntervals') IS NOT NULL THEN N'OK' ELSE N'НУЖЕН ДЕПЛОЙ' END,
	N'dbo\Functions\fn_AgitationExcludeIntervals.sql (CREATE)';

INSERT INTO @report
SELECT N'vMassmedia отдаёт колонку интервалов',
	CASE WHEN COL_LENGTH('dbo.vMassmedia', 'agitationExcludeIntervals') IS NOT NULL THEN N'OK' ELSE N'НУЖЕН ДЕПЛОЙ' END,
	N'dbo\Views\vMassmedia.sql (ALTER)';

INSERT INTO @report
SELECT N'MassmediaIUD пишет и валидирует интервалы',
	CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.MassmediaIUD')) LIKE '%agitationExcludeIntervals%'
		AND OBJECT_DEFINITION(OBJECT_ID('dbo.MassmediaIUD')) LIKE '%AgitationIntervalsInvalid%'
	THEN N'OK' ELSE N'НУЖЕН ДЕПЛОЙ' END,
	N'dbo\Stored Procedures\MassmediaIUD.sql (ALTER)';

INSERT INTO @report
SELECT N'AgitationFraming учитывает интервалы',
	CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.AgitationFraming')) LIKE '%fn_AgitationExcludeIntervals%'
	THEN N'OK' ELSE N'НУЖЕН ДЕПЛОЙ' END,
	N'dbo\Stored Procedures\AgitationFraming.sql (ALTER)';

-- Контроль базового функционала: если что-то из него на этой базе отсутствует,
-- интервалы работать не будут. Проверяем по наличию хука в теле процедур.
INSERT INTO @report
SELECT N'Базовый функционал: ' + o.name,
	CASE
		WHEN OBJECT_ID('dbo.' + o.name) IS NULL THEN N'ОБЪЕКТА НЕТ'
		WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.' + o.name)) LIKE '%AgitationFraming%' THEN N'OK'
		ELSE N'БЕЗ ХУКА'
	END,
	o.note
FROM (VALUES
	('ActionActivate',       N'вставка обвязки при активации'),
	('IssueIUD',             N'вставка и снятие обвязки'),
	('ActionDeactivate',     N'снятие при деактивации'),
	('IssueTransfer',        N'перенос выпуска'),
	('CampaignsIssueDelete', N'удаление строки ролика/дня'),
	('CampaignTransferDay',  N'перенос дня')
) o(name, note);

SELECT * FROM @report;

IF EXISTS (SELECT 1 FROM @report WHERE состояние <> N'OK')
	PRINT 'Есть пункты не в состоянии OK - смотрите таблицу выше (колонка "деталь" подсказывает, какой файл задеплоить)';
ELSE
	PRINT 'Всё на месте. Не забудьте перезапустить клиент - метаданные форм кэшируются.';
