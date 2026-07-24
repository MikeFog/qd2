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
	</page>
</passport>';

UPDATE [dbo].[iEntity]
SET passport = REPLACE(@passportXml, N'</passport>', @newPage)
WHERE entityID = 9;
