-- Массовое редактирование тарифов («Изменить похожие тарифы...»): развёртывание.
--
-- Пункт на тарифе (сущность 81) открывает паспорт TariffMassEdit (iPassport), предзаполненный значениями
-- тарифа. «Похожие» тарифы (та же минута, все прочие атрибуты и дни совпадают) ищет процедура TariffSimilar.
-- Правка идёт по одному тарифу через существующий TariffIUD (Tariff.ApplyMassEdit): дни в форме - область
-- применения, часть дней - тариф делится на два; тарифы с окнами и в цепочках объединения пропускаются.
--
-- Объекты: процедура TariffSimilar (читающая), iPassport TariffMassEdit, iEntityAction 81/EditSimilarTariffs
-- + права групп как у «Клонировать». Скрипт идемпотентен. После прогона перезапустить qd2 (кэш метаданных).
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i tariff-mass-edit-deploy.sql

SET NOCOUNT ON;
GO

-------------------------------------------------------------------------------
-- 1. Процедура TariffSimilar
-------------------------------------------------------------------------------
-- «Похожие тарифы» для массового редактирования: тарифы того же прайс-листа с той же минутой выхода
-- и полностью совпадающими остальными атрибутами (включая набор дней недели) - то есть отличающиеся
-- от исходного тарифа только часом. Исходный тариф входит в результат.
-- hasWindows - у тарифа есть сгенерированные окна (TariffIUD не даст его править),
-- inUnion - тариф входит в цепочку объединения (TariffUnion) в любой роли.
CREATE OR ALTER PROCEDURE [dbo].[TariffSimilar]
(
@tariffID int
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

SELECT
	t.tariffID,
	DATEPART(hour, t.[time]) AS [hour],
	CAST(CASE WHEN EXISTS (SELECT 1 FROM TariffWindow w WHERE w.tariffID = t.tariffID) THEN 1 ELSE 0 END AS bit) AS hasWindows,
	CAST(CASE WHEN EXISTS (SELECT 1 FROM TariffUnion u WHERE u.tariffID = t.tariffID OR u.tariffUnionID = t.tariffID) THEN 1 ELSE 0 END AS bit) AS inUnion
FROM Tariff s
	INNER JOIN Tariff t ON t.pricelistID = s.pricelistID
		AND DATEPART(minute, t.[time]) = DATEPART(minute, s.[time])
		AND t.price = s.price
		AND t.duration = s.duration
		AND t.duration_total = s.duration_total
		AND t.maxCapacity = s.maxCapacity
		AND t.isForModuleOnly = s.isForModuleOnly
		AND t.needExt = s.needExt
		AND t.needInJingle = s.needInJingle
		AND t.needOutJingle = s.needOutJingle
		AND ISNULL(t.blockTypeID, 0) = ISNULL(s.blockTypeID, 0)
		AND t.notEarly = s.notEarly
		AND t.notLater = s.notLater
		AND t.openBlock = s.openBlock
		AND t.openPhonogram = s.openPhonogram
		AND t.monday = s.monday AND t.tuesday = s.tuesday AND t.wednesday = s.wednesday
		AND t.thursday = s.thursday AND t.friday = s.friday AND t.saturday = s.saturday AND t.sunday = s.sunday
		AND ISNULL(t.comment, '') = ISNULL(s.comment, '')
		AND ISNULL(t.suffix, '') = ISNULL(s.suffix, '')
WHERE s.tariffID = @tariffID
ORDER BY t.[time]
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[TariffSimilar] TO PUBLIC
    AS [dbo];

-------------------------------------------------------------------------------
-- 2. Метаданные (одной транзакцией)
-------------------------------------------------------------------------------

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @entTariff INT = 81; -- Тариф (Merlin.Classes.Tariff)

IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntity] WHERE entityID = @entTariff AND className = 'Merlin.Classes.Tariff')
BEGIN
	RAISERROR('Сущность 81 (Тариф) не найдена или изменена - согласуйте с Merlin.Classes.Entities', 16, 1);
	RETURN;
END

BEGIN TRANSACTION;

DECLARE @passport NVARCHAR(MAX) = N'<passport>
	<page caption="Общие">
		<label caption="Будут изменены:" name="massEditHint"/>
		<separator/>
		<field caption="Время выхода (минуты):" name="tariffMinute" type="int" min="0" max="59" mandatory="true"/>
		<field caption="Только часы: с:" name="hourFrom" type="int" min="0" max="23" mandatory="true"/>
		<field caption="Только часы: по (включительно):" name="hourTo" type="int" min="0" max="23" mandatory="true"/>
		<separator/>
		<field caption="Цена:" name="price"/>
		<field caption="Продолжительность:" name="duration"/>
		<separator/>
		<label caption="Применить к дням:" name="massDaysHint"/>
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

IF EXISTS (SELECT 1 FROM [dbo].[iPassport] WHERE codeName = 'TariffMassEdit')
	UPDATE [dbo].[iPassport] SET passport = @passport WHERE codeName = 'TariffMassEdit';
ELSE
	INSERT INTO [dbo].[iPassport] (codeName, passport) VALUES ('TariffMassEdit', @passport);

-- ordinal 17: после «Клонировать» (15) и разделителя (16), перед «Свойства» (20)
IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntityAction] WHERE entityID = @entTariff AND name = 'EditSimilarTariffs')
	INSERT INTO [dbo].[iEntityAction]
		(entityID, alias, name, ordinal_position, isHidden, isGrantingAllowed, imgResourceName, parentID)
	VALUES
		(@entTariff, N'Изменить похожие тарифы...', 'EditSimilarTariffs', 17, 0, 1, NULL, NULL);

UPDATE [dbo].[iEntityAction]
SET alias = N'Изменить похожие тарифы...'
WHERE entityID = @entTariff AND name = 'EditSimilarTariffs' AND alias <> N'Изменить похожие тарифы...';

DECLARE @newActionID SMALLINT =
	(SELECT entityActionID FROM [dbo].[iEntityAction] WHERE entityID = @entTariff AND name = 'EditSimilarTariffs');
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

PRINT '--- Массовое редактирование тарифов: состояние ---';

SELECT 'TariffSimilar (процедура)' AS [объект], COUNT(*) AS [строк], '1' AS [ожидается]
FROM sys.procedures WHERE name = 'TariffSimilar'
UNION ALL SELECT 'iPassport (TariffMassEdit)', COUNT(*), '1'
FROM [dbo].[iPassport] WHERE codeName = 'TariffMassEdit'
UNION ALL SELECT 'iEntityAction (81/EditSimilarTariffs)', COUNT(*), '1'
FROM [dbo].[iEntityAction] WHERE entityID = @entTariff AND name = 'EditSimilarTariffs'
UNION ALL SELECT 'GroupRight (новое действие)', COUNT(*), N'как у Clone'
FROM [dbo].[GroupRight] WHERE entityActionID = @newActionID
UNION ALL SELECT 'GroupRight (Clone, для сверки)', COUNT(*), ''
FROM [dbo].[GroupRight] WHERE entityActionID = @cloneActionID;