-- Политическая агитация: схема, справочные данные и метаданные форм.
-- Этот же скрипт используется для обновления продуктивной базы,
-- порядок деплоя и список программных объектов - в docs/tasks/political-agitation-deploy.md
--
-- Скрипт идемпотентен и конвергентен: приводит базу к нужному состоянию из
-- любого прежнего (чистая база, промежуточные отменённые редакции), повторный
-- прогон безопасен. В конце выводит отчёт о проверке.
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i political-agitation-seed.sql
--
-- ВНИМАНИЕ: флаг -I (QUOTED_IDENTIFIER ON) обязателен и для этого скрипта,
-- и для деплоя процедур - без него UPDATE в IssueIUD падает на индексах.

-- Новые типы роликов (iRollerActionType):
--   6  - ролик политической агитации кандидата (создаётся менеджером);
--   7  - анонс "а теперь политическая агитация", авто-вставляемый системой
--        перед роликами кандидатов ("тип шестой и тип седьмой" со встречи);
--   44 - идентификатор локального СМИ, авто-вставляемый системой при активации
--        акции с агитацией (пара к ручному типу 4);
--   55 - идентификатор федерального СМИ, авто-вставляемый системой (пара к типу 5).
-- Номера 44/55 выбраны по договорённости со встречи 24.07.2026 ("тип 44"):
-- система по номеру отличает своё авто-обрамление от расставленного вручную.
-- Порядок в блоке: влёт -> обычная реклама -> 44 -> 7 -> ролики 6 -> 55 -> аут.

SET IDENTITY_INSERT [dbo].[iRollerActionType] ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[iRollerActionType] WHERE rolActionTypeId = 6)
	INSERT INTO [dbo].[iRollerActionType] (rolActionTypeId, name)
	VALUES (6, N'Политическая агитация');

IF NOT EXISTS (SELECT 1 FROM [dbo].[iRollerActionType] WHERE rolActionTypeId = 7)
	INSERT INTO [dbo].[iRollerActionType] (rolActionTypeId, name)
	VALUES (7, N'Анонс политической агитации');

IF NOT EXISTS (SELECT 1 FROM [dbo].[iRollerActionType] WHERE rolActionTypeId = 44)
	INSERT INTO [dbo].[iRollerActionType] (rolActionTypeId, name)
	VALUES (44, N'Локальное СМИ (агитация)');

IF NOT EXISTS (SELECT 1 FROM [dbo].[iRollerActionType] WHERE rolActionTypeId = 55)
	INSERT INTO [dbo].[iRollerActionType] (rolActionTypeId, name)
	VALUES (55, N'Федеральное СМИ (агитация)');

SET IDENTITY_INSERT [dbo].[iRollerActionType] OFF;

-- Сообщения для новых кодов ошибок (iMessage)

IF NOT EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'AgitationPositionForbidden')
	INSERT INTO [dbo].[iMessage] (name, [message])
	VALUES ('AgitationPositionForbidden',
		N'Для роликов политической агитации позиционирование не применяется. Операция прервана.');

IF NOT EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'AgitationMixError')
	INSERT INTO [dbo].[iMessage] (name, [message])
	VALUES ('AgitationMixError',
		N'В одной рекламной акции нельзя размещать политическую агитацию вместе с другой рекламой. Операция прервана.');

IF NOT EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'RolType7AlreadyExistInWindow')
	INSERT INTO [dbo].[iMessage] (name, [message])
	VALUES ('RolType7AlreadyExistInWindow',
		N'В окне уже есть ролик с типом "Анонс политической агитации". Операция прервана.');

IF NOT EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'AgitationStationRollersNotSet')
	INSERT INTO [dbo].[iMessage] (name, [message])
	VALUES ('AgitationStationRollersNotSet',
		N'У радиостанции не заполнены ролики обвязки политической агитации (карточка радиостанции, вкладка "Политическая агитация"). Операция прервана.');

IF NOT EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'AgitationIntervalsInvalid')
	INSERT INTO [dbo].[iMessage] (name, [message])
	VALUES ('AgitationIntervalsInvalid',
		N'Интервалы-исключения заданы неверно. Формат: ЧЧ:ММ-ЧЧ:ММ, несколько интервалов в одной строке разделяются точкой с запятой. Строка может начинаться с дней недели, например: "пн-пт 16:00-16:55; 18:00-19:00", а на следующей строке - другой набор: "сб,вс 10:00-11:00". Дни: пн вт ср чт пт сб вс; допускаются диапазон (пн-пт) и перечисление (пн,ср,пт), строка без дней действует все дни недели. Начало интервала должно быть раньше конца, переход через полночь не поддерживается.');

-- Сообщение для окна результатов активации (ActionActivate читает iMessageToActivate)
IF NOT EXISTS (SELECT 1 FROM [dbo].[iMessageToActivate] WHERE name = 'AgitationStationRollersNotSet')
	INSERT INTO [dbo].[iMessageToActivate] (name, [message])
	VALUES ('AgitationStationRollersNotSet',
		N'У радиостанции не заполнены ролики обвязки политической агитации (карточка радиостанции, вкладка "Политическая агитация"). Активация невозможна.');

-- Ролики авто-обвязки в карточке радиостанции: три необязательные ссылки на Roller
-- (тип 44/7/55), lookup-списки на странице "Политическая агитация".
-- Ссылки, а не текст: авто-вставка этапа 3 создаёт настоящие выпуски (Issue),
-- которым нужен реальный Roller с длительностью.
-- Промежуточная текстовая редакция (nvarchar-поля) откатывается, если встретилась.

IF COL_LENGTH('dbo.MassMedia', 'agitationLocalRoller') IS NOT NULL
	ALTER TABLE [dbo].[MassMedia]
		DROP COLUMN [agitationLocalRoller], [agitationAnnounceRoller], [agitationFederalRoller];

IF COL_LENGTH('dbo.MassMedia', 'agitationLocalRollerID') IS NULL
	ALTER TABLE [dbo].[MassMedia] ADD
		[agitationLocalRollerID]    INT NULL
			CONSTRAINT [FK_MassMedia_AgitationLocalRoller] REFERENCES [dbo].[Roller] ([rollerID]),
		[agitationAnnounceRollerID] INT NULL
			CONSTRAINT [FK_MassMedia_AgitationAnnounceRoller] REFERENCES [dbo].[Roller] ([rollerID]),
		[agitationFederalRollerID]  INT NULL
			CONSTRAINT [FK_MassMedia_AgitationFederalRoller] REFERENCES [dbo].[Roller] ([rollerID]);

-- Интервалы-исключения: время, когда идентификаторы СМИ (44/55) не добавляются.
-- Тип doubleString - движок паспорта рисует его многострочным полем
IF COL_LENGTH('dbo.MassMedia', 'agitationExcludeIntervals') IS NULL
	ALTER TABLE [dbo].[MassMedia] ADD [agitationExcludeIntervals] [dbo].[doubleString] NULL;

-- Имена result set'ов massmediaPassport для источников lookup (движок паспорта
-- ищет таблицу DataSet по алиасу из iTableAlias)
DECLARE @passportProcID int;
SELECT @passportProcID = storedProcedureID FROM [dbo].[iStoredProcedure] WHERE name = 'massmediaPassport';

IF @passportProcID IS NOT NULL
BEGIN
	IF NOT EXISTS (SELECT 1 FROM [dbo].[iTableAlias] WHERE storedProcedureID = @passportProcID AND position = 5)
		INSERT INTO [dbo].[iTableAlias] (storedProcedureID, position, name)
		VALUES (@passportProcID, 5, 'rollersAgitLocal');
	IF NOT EXISTS (SELECT 1 FROM [dbo].[iTableAlias] WHERE storedProcedureID = @passportProcID AND position = 6)
		INSERT INTO [dbo].[iTableAlias] (storedProcedureID, position, name)
		VALUES (@passportProcID, 6, 'rollersAgitAnnounce');
	IF NOT EXISTS (SELECT 1 FROM [dbo].[iTableAlias] WHERE storedProcedureID = @passportProcID AND position = 7)
		INSERT INTO [dbo].[iTableAlias] (storedProcedureID, position, name)
		VALUES (@passportProcID, 7, 'rollersAgitFederal');
END

-- Страница "Политическая агитация" в паспорте радиостанции (iEntity, entityID = 9).
-- Скрипт конвергентный: существующая страница (любой прежней редакции) вырезается
-- по позиции (от '<page caption="Политическая агитация">' до '</page>') и
-- вставляется актуальная. Имена полей = колонки vMassmedia = параметры MassmediaIUD.
DECLARE @passportXml nvarchar(max) =
	(SELECT CAST(passport AS nvarchar(max)) FROM [dbo].[iEntity] WHERE entityID = 9);

DECLARE @pageStart int = CHARINDEX(N'<page caption="Политическая агитация">', @passportXml);
IF @pageStart > 0
BEGIN
	DECLARE @pageEnd int = CHARINDEX(N'</page>', @passportXml, @pageStart);
	SET @passportXml = STUFF(@passportXml, @pageStart, @pageEnd + 7 - @pageStart, N'');
END

DECLARE @newPage nvarchar(max) = N'	<page caption="Политическая агитация">
		<lookup caption="Локальное СМИ (агитация):" name="agitationLocalRollerID" source="rollersAgitLocal" columnWithID="rollerID"/>
		<lookup caption="Анонс агитации:" name="agitationAnnounceRollerID" source="rollersAgitAnnounce" columnWithID="rollerID"/>
		<lookup caption="Федеральное СМИ (агитация):" name="agitationFederalRollerID" source="rollersAgitFederal" columnWithID="rollerID"/>
		<separator />
		<field caption="Интервалы, напр. пн-пт 16:00-16:55; 18:00-19:00" name="agitationExcludeIntervals"/>
	</page>
</passport>';

UPDATE [dbo].[iEntity]
SET passport = REPLACE(@passportXml, N'</passport>', @newPage)
WHERE entityID = 9;

-- ============================================================================
-- Отчёт о проверке: все строки должны быть в состоянии OK.
-- Программные объекты (процедуры, функции, представление) этот скрипт НЕ
-- деплоит - их список в docs/tasks/political-agitation-deploy.md, здесь только
-- проверяется, что они уже на месте.
-- ============================================================================

DECLARE @report table (пункт nvarchar(80), состояние nvarchar(20), деталь nvarchar(200));

INSERT INTO @report
SELECT N'Типы роликов 6/7/44/55',
	CASE WHEN COUNT(*) = 4 THEN N'OK' ELSE N'ОШИБКА' END,
	N'найдено: ' + CAST(COUNT(*) AS nvarchar(10)) + N' из 4'
FROM [dbo].[iRollerActionType] WHERE rolActionTypeId IN (6, 7, 44, 55);

INSERT INTO @report
SELECT N'Сообщения в iMessage',
	CASE WHEN COUNT(*) = 5 THEN N'OK' ELSE N'ОШИБКА' END,
	N'найдено: ' + CAST(COUNT(*) AS nvarchar(10)) + N' из 5'
FROM [dbo].[iMessage]
WHERE name IN ('AgitationPositionForbidden', 'AgitationMixError',
	'RolType7AlreadyExistInWindow', 'AgitationStationRollersNotSet', 'AgitationIntervalsInvalid');

INSERT INTO @report
SELECT N'Сообщение в iMessageToActivate',
	CASE WHEN COUNT(*) = 1 THEN N'OK' ELSE N'ОШИБКА' END,
	N'AgitationStationRollersNotSet'
FROM [dbo].[iMessageToActivate] WHERE name = 'AgitationStationRollersNotSet';

INSERT INTO @report
SELECT N'Колонки MassMedia',
	CASE WHEN COL_LENGTH('dbo.MassMedia', 'agitationLocalRollerID') IS NOT NULL
		AND COL_LENGTH('dbo.MassMedia', 'agitationAnnounceRollerID') IS NOT NULL
		AND COL_LENGTH('dbo.MassMedia', 'agitationFederalRollerID') IS NOT NULL
		AND COL_LENGTH('dbo.MassMedia', 'agitationExcludeIntervals') IS NOT NULL
	THEN N'OK' ELSE N'ОШИБКА' END,
	N'4 колонки агитации';

INSERT INTO @report
SELECT N'Колонки в vMassmedia',
	CASE WHEN COL_LENGTH('dbo.vMassmedia', 'agitationExcludeIntervals') IS NOT NULL
	THEN N'OK' ELSE N'ОШИБКА: задеплойте vMassmedia' END,
	N'представление должно быть обновлено';

INSERT INTO @report
SELECT N'Алиасы result set в iTableAlias',
	CASE WHEN COUNT(*) = 3 THEN N'OK' ELSE N'ОШИБКА' END,
	N'найдено: ' + CAST(COUNT(*) AS nvarchar(10)) + N' из 3'
FROM [dbo].[iTableAlias] ta
	INNER JOIN [dbo].[iStoredProcedure] sp ON sp.storedProcedureID = ta.storedProcedureID
WHERE sp.name = 'massmediaPassport'
	AND ta.name IN ('rollersAgitLocal', 'rollersAgitAnnounce', 'rollersAgitFederal');

INSERT INTO @report
SELECT N'Страница паспорта станции',
	CASE WHEN x.pageCount = 1 THEN N'OK' ELSE N'ОШИБКА' END,
	N'страниц "Политическая агитация": ' + CAST(x.pageCount AS nvarchar(10))
FROM (SELECT (LEN(CAST(passport AS nvarchar(max)))
		- LEN(REPLACE(CAST(passport AS nvarchar(max)), N'<page caption="Политическая агитация">', N'')))
		/ LEN(N'<page caption="Политическая агитация">') AS pageCount
	FROM [dbo].[iEntity] WHERE entityID = 9) x;

INSERT INTO @report
SELECT N'XML паспорта станции валиден',
	CASE WHEN TRY_CAST(CAST(passport AS nvarchar(max)) AS xml) IS NOT NULL THEN N'OK' ELSE N'ОШИБКА' END,
	N'iEntity.entityID = 9'
FROM [dbo].[iEntity] WHERE entityID = 9;

INSERT INTO @report
SELECT N'Программный объект: ' + o.name,
	CASE WHEN OBJECT_ID('dbo.' + o.name) IS NOT NULL THEN N'OK' ELSE N'НЕ ЗАДЕПЛОЕН' END,
	o.kind
FROM (VALUES
	('AgitationFraming', N'новая процедура'),
	('fn_AgitationChainFirst', N'новая функция'),
	('fn_AgitationExcludeIntervals', N'новая функция')
) o(name, kind);

SELECT * FROM @report;

IF EXISTS (SELECT 1 FROM @report WHERE состояние <> N'OK')
	PRINT 'ВНИМАНИЕ: есть пункты не в состоянии OK - смотрите таблицу выше';
ELSE
	PRINT 'Все проверки пройдены. Не забудьте задеплоить процедуры из списка в docs/tasks/political-agitation-deploy.md и перезапустить клиент.';
