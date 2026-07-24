-- Политическая агитация, этап 1: справочные данные
-- См. docs/tasks/political-agitation-ads.md
-- Скрипт идемпотентен, деплоится вручную:
--   sqlcmd -S <server> -d <база> -E -f 65001 -i political-agitation-seed.sql

-- Новые типы роликов (iRollerActionType):
--   6  - ролик политической агитации (создаётся менеджером);
--   44 - идентификатор локального СМИ, авто-вставляемый системой при активации
--        акции с агитацией (пара к ручному типу 4);
--   55 - идентификатор федерального СМИ, авто-вставляемый системой (пара к типу 5).
-- Номера 44/55 выбраны по договорённости со встречи 24.07.2026 ("тип 44"):
-- система по номеру отличает своё авто-обрамление от расставленного вручную.

SET IDENTITY_INSERT [dbo].[iRollerActionType] ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[iRollerActionType] WHERE rolActionTypeId = 6)
	INSERT INTO [dbo].[iRollerActionType] (rolActionTypeId, name)
	VALUES (6, N'Политическая агитация');

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
