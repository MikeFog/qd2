-- Массовое клонирование тарифа («Клонировать массово»): метаданные.
--
-- Пункт на тарифе (сущность 81) открывает паспорт TariffMass (тот же, что у «Добавить тариф
-- массово» на прайс-листе), предзаполненный значениями этого тарифа; интервал часов по умолчанию
-- 0-23. По «ОК» клиент (Tariff.CloneTariffsMass -> Tariff.CreateMass) создаёт по тарифу в каждом
-- часе интервала через TariffIUD. Паспорт TariffMass уже на месте
-- (tariff-mass-create-seed.sql), здесь только пункт меню и права.
--
-- Серверных объектов нет. Скрипт идемпотентен. После прогона перезапустить qd2 (кэш метаданных).
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i tariff-clone-mass-seed.sql

SET NOCOUNT ON;
GO
SET XACT_ABORT ON;

DECLARE @entTariff INT = 81; -- Тариф (Merlin.Classes.Tariff)

IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntity] WHERE entityID = @entTariff AND className = 'Merlin.Classes.Tariff')
BEGIN
	RAISERROR('Сущность 81 (Тариф) не найдена или изменена - согласуйте с Merlin.Classes.Entities', 16, 1);
	RETURN;
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[iPassport] WHERE codeName = 'TariffMass')
BEGIN
	RAISERROR('Паспорт TariffMass не найден - сначала tariff-mass-create-seed.sql', 16, 1);
	RETURN;
END

BEGIN TRANSACTION;

-- ordinal 18: после «Изменить похожие тарифы» (17), перед «Свойства» (20)
IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntityAction] WHERE entityID = @entTariff AND name = 'CloneTariffsMass')
	INSERT INTO [dbo].[iEntityAction]
		(entityID, alias, name, ordinal_position, isHidden, isGrantingAllowed, imgResourceName, parentID)
	VALUES
		(@entTariff, N'Клонировать массово', 'CloneTariffsMass', 18, 0, 1, NULL, NULL);

UPDATE [dbo].[iEntityAction]
SET alias = N'Клонировать массово'
WHERE entityID = @entTariff AND name = 'CloneTariffsMass' AND alias <> N'Клонировать массово';

DECLARE @newActionID SMALLINT =
	(SELECT entityActionID FROM [dbo].[iEntityAction] WHERE entityID = @entTariff AND name = 'CloneTariffsMass');
DECLARE @cloneActionID SMALLINT =
	(SELECT entityActionID FROM [dbo].[iEntityAction] WHERE entityID = @entTariff AND name = 'Clone');

-- права: те же группы, что у «Клонировать» на тарифе
INSERT INTO [dbo].[GroupRight] (groupID, entityActionID)
SELECT gr.groupID, @newActionID
FROM [dbo].[GroupRight] gr
WHERE gr.entityActionID = @cloneActionID
	AND NOT EXISTS (SELECT 1 FROM [dbo].[GroupRight] x
	                WHERE x.groupID = gr.groupID AND x.entityActionID = @newActionID);

COMMIT TRANSACTION;

PRINT '--- Массовое клонирование тарифов: состояние ---';

SELECT 'iEntityAction (81/CloneTariffsMass)' AS [объект], COUNT(*) AS [строк], '1' AS [ожидается]
FROM [dbo].[iEntityAction] WHERE entityID = @entTariff AND name = 'CloneTariffsMass'
UNION ALL SELECT 'GroupRight (новое действие)', COUNT(*), N'как у Clone'
FROM [dbo].[GroupRight] WHERE entityActionID = @newActionID
UNION ALL SELECT 'GroupRight (Clone, для сверки)', COUNT(*), ''
FROM [dbo].[GroupRight] WHERE entityActionID = @cloneActionID;
