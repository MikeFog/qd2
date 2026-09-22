-- Массовое создание тарифов: метаданные.
--
-- Действие «Добавить тариф массово» на прайс-листе (сущность 80) открывает паспорт
-- TariffMass (iPassport) - копию паспорта тарифа (сущность 81), где вместо времени
-- выхода: минута (0-59) + интервал часов (с, по включительно), а блок «Объединить
-- с блоком» убран. Клиент (MassmediaPricelist.AddTariffsMass -> Tariff.CreateMass)
-- создаёт по одному тарифу в каждом часе интервала через существующий TariffIUD,
-- не создавшиеся (дубль, спонсорский тариф, разрыв цепочки) показывает журналом.
--
-- Серверных объектов не добавляется. Скрипт идемпотентен, повторный прогон безопасен.
-- Клиент кэширует метаданные - после прогона перезапустить qd2.
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i tariff-mass-create-seed.sql

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @entPricelist INT = 80; -- Прайс-лист (Merlin.Classes.MassmediaPricelist)

IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntity] WHERE entityID = @entPricelist AND className = 'Merlin.Classes.MassmediaPricelist')
BEGIN
	RAISERROR('Сущность 80 (Прайс-лист) не найдена или изменена - согласуйте с Merlin.Classes.Entities', 16, 1);
	RETURN;
END

BEGIN TRANSACTION;

-------------------------------------------------------------------------------
-- 1. Паспорт TariffMass (iPassport)
-------------------------------------------------------------------------------

DECLARE @passport NVARCHAR(MAX) = N'<passport>
	<page caption="Общие">
		<field caption="Время выхода (минуты):" name="tariffMinute" type="int" min="0" max="59" mandatory="true"/>
		<field caption="Интервал: с часа:" name="hourFrom" type="int" min="0" max="23" mandatory="true"/>
		<field caption="Интервал: по час (включительно):" name="hourTo" type="int" min="0" max="23" mandatory="true"/>
		<separator/>
		<field caption="Цена:" name="price"/>
		<field caption="Продолжительность:" name="duration"/>
		<separator/>
		<field caption="Понедельник" name="monday"/>
		<field caption="Вторник" name="tuesday"/>
		<field caption="Среда" name="wednesday"/>
		<field caption="Четверг" name="thursday"/>
		<field caption="Пятница" name="friday"/>
		<field caption="Суббота" name="saturday"/>
		<field caption="Воскресенье" name="sunday"/>
	</page>
	<page caption="Прочее">
		<field caption="Комментарий" name="comment" />
		<field caption="Использовать только для модулей" name="isForModuleOnly"/>
		<field caption="Макс. вместимость:" name="maxCapacity"/>
	</page>
	<page caption="DJin">
		<field caption="ID" name="suffix" />
		<field caption="Полная продолжительность:" name="duration_total"/>
		<field caption="Джингл на вход" name="needInJingle" />
		<field caption="Джингл на выход" name="needOutJingle" />
		<separator/>
		<field caption="Не ранее (W)" name="notEarly" />
		<field caption="Не позднее (A)" name="notLater" />
		<field caption="Обрывать блоки (K)" name="openBlock" />
		<field caption="Обрывать фонограммы (H)" name="openPhonogram" />
		<lookup caption="Тип блока:" name="blockTypeID" source="blockTypes" columnWithID="blockTypeID" />
	</page>
</passport>';

IF EXISTS (SELECT 1 FROM [dbo].[iPassport] WHERE codeName = 'TariffMass')
	UPDATE [dbo].[iPassport] SET passport = @passport WHERE codeName = 'TariffMass';
ELSE
	INSERT INTO [dbo].[iPassport] (codeName, passport) VALUES ('TariffMass', @passport);

-------------------------------------------------------------------------------
-- 2. Действие на прайс-листе (iEntityAction): сразу после «Добавить тариф» (AssignNew, 15)
-------------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntityAction]
               WHERE entityID = @entPricelist AND name = 'AddTariffsMass')
	INSERT INTO [dbo].[iEntityAction]
		(entityID, alias, name, ordinal_position, isHidden, isGrantingAllowed, imgResourceName, parentID)
	VALUES
		(@entPricelist, N'Добавить тариф массово', 'AddTariffsMass', 20, 0, 1, NULL, NULL);

UPDATE [dbo].[iEntityAction]
SET alias = N'Добавить тариф массово'
WHERE entityID = @entPricelist AND name = 'AddTariffsMass'
  AND alias <> N'Добавить тариф массово';

DECLARE @newActionID SMALLINT =
	(SELECT entityActionID FROM [dbo].[iEntityAction]
	 WHERE entityID = @entPricelist AND name = 'AddTariffsMass');

-------------------------------------------------------------------------------
-- 3. Права групп - те же, что у «Добавить тариф» (AssignNew) на этом прайс-листе
-------------------------------------------------------------------------------

DECLARE @assignNewActionID SMALLINT =
	(SELECT entityActionID FROM [dbo].[iEntityAction]
	 WHERE entityID = @entPricelist AND name = 'AssignNew');

INSERT INTO [dbo].[GroupRight] (groupID, entityActionID)
SELECT gr.groupID, @newActionID
FROM [dbo].[GroupRight] gr
WHERE gr.entityActionID = @assignNewActionID
	AND NOT EXISTS (SELECT 1 FROM [dbo].[GroupRight] x
	                WHERE x.groupID = gr.groupID AND x.entityActionID = @newActionID);

COMMIT TRANSACTION;

-------------------------------------------------------------------------------
-- Отчёт
-------------------------------------------------------------------------------

PRINT '--- Массовое создание тарифов: состояние метаданных ---';

SELECT 'iPassport (TariffMass)' AS [объект], COUNT(*) AS [строк], '1' AS [ожидается]
FROM [dbo].[iPassport] WHERE codeName = 'TariffMass'
UNION ALL SELECT 'iEntityAction (80/AddTariffsMass)', COUNT(*), '1'
FROM [dbo].[iEntityAction] WHERE entityID = @entPricelist AND name = 'AddTariffsMass'
UNION ALL SELECT 'GroupRight (новое действие)', COUNT(*), N'как у AssignNew'
FROM [dbo].[GroupRight] WHERE entityActionID = @newActionID
UNION ALL SELECT 'GroupRight (AssignNew, для сверки)', COUNT(*), ''
FROM [dbo].[GroupRight] WHERE entityActionID = @assignNewActionID;
