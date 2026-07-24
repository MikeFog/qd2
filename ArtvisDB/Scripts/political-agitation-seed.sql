-- Политическая агитация, этап 1: справочные данные
-- См. docs/tasks/political-agitation-ads.md
-- Скрипт идемпотентен, деплоится вручную:
--   sqlcmd -S <server> -d <база> -E -f 65001 -i political-agitation-seed.sql

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

-- Ролики авто-обвязки в карточке радиостанции: три необязательных текстовых поля
-- (путь или название ролика), по образцу DJin-путей. Первая редакция делала их
-- ссылками на Roller с lookup-списками - ниже она откатывается, если встретилась.

-- Откат первой редакции: INT FK колонки, алиасы lookup-источников, старая страница XML
IF COL_LENGTH('dbo.MassMedia', 'agitationLocalRollerID') IS NOT NULL
BEGIN
	IF OBJECT_ID('dbo.FK_MassMedia_AgitationLocalRoller', 'F') IS NOT NULL
		ALTER TABLE [dbo].[MassMedia] DROP CONSTRAINT [FK_MassMedia_AgitationLocalRoller];
	IF OBJECT_ID('dbo.FK_MassMedia_AgitationAnnounceRoller', 'F') IS NOT NULL
		ALTER TABLE [dbo].[MassMedia] DROP CONSTRAINT [FK_MassMedia_AgitationAnnounceRoller];
	IF OBJECT_ID('dbo.FK_MassMedia_AgitationFederalRoller', 'F') IS NOT NULL
		ALTER TABLE [dbo].[MassMedia] DROP CONSTRAINT [FK_MassMedia_AgitationFederalRoller];
	ALTER TABLE [dbo].[MassMedia]
		DROP COLUMN [agitationLocalRollerID], [agitationAnnounceRollerID], [agitationFederalRollerID];
END

DELETE ta FROM [dbo].[iTableAlias] ta
	INNER JOIN [dbo].[iStoredProcedure] sp ON sp.storedProcedureID = ta.storedProcedureID
WHERE sp.name = 'massmediaPassport'
	AND ta.name IN ('rollersAgitLocal', 'rollersAgitAnnounce', 'rollersAgitFederal');

-- Старая страница удаляется по позиции (от "<page caption=...агитация">" до "</page>"),
-- чтобы не зависеть от переводов строк внутри блока
DECLARE @passportXml nvarchar(max) =
	(SELECT CAST(passport AS nvarchar(max)) FROM [dbo].[iEntity] WHERE entityID = 9);

IF @passportXml LIKE N'%agitationLocalRollerID%'
BEGIN
	DECLARE @pageStart int = CHARINDEX(N'<page caption="Политическая агитация">', @passportXml);
	IF @pageStart > 0
	BEGIN
		DECLARE @pageEnd int = CHARINDEX(N'</page>', @passportXml, @pageStart);
		SET @passportXml = STUFF(@passportXml, @pageStart, @pageEnd + 7 - @pageStart, N'');
		UPDATE [dbo].[iEntity] SET passport = @passportXml WHERE entityID = 9;
	END
END

-- Текущая редакция: текстовые колонки
IF COL_LENGTH('dbo.MassMedia', 'agitationLocalRoller') IS NULL
	ALTER TABLE [dbo].[MassMedia] ADD
		[agitationLocalRoller]    NVARCHAR (255) NULL,
		[agitationAnnounceRoller] NVARCHAR (255) NULL,
		[agitationFederalRoller]  NVARCHAR (255) NULL;

-- Страница в паспорте радиостанции (iEntity, entityID = 9): три текстовых поля.
-- Имена полей = имена колонок vMassmedia = имена параметров MassmediaIUD.
SET @passportXml =
	(SELECT CAST(passport AS nvarchar(max)) FROM [dbo].[iEntity] WHERE entityID = 9);

IF @passportXml NOT LIKE N'%name="agitationLocalRoller"%'
BEGIN
	DECLARE @newPage nvarchar(max) = N'	<page caption="Политическая агитация">
		<field caption="Локальное СМИ (агитация):" name="agitationLocalRoller"/>
		<field caption="Анонс агитации:" name="agitationAnnounceRoller"/>
		<field caption="Федеральное СМИ (агитация):" name="agitationFederalRoller"/>
	</page>
</passport>';

	UPDATE [dbo].[iEntity]
	SET passport = REPLACE(@passportXml, N'</passport>', @newPage)
	WHERE entityID = 9;
END
