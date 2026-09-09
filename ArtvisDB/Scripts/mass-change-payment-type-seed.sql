-- Массовая смена типа оплаты для рекламной акции: метаданные.
--
-- Одиночная операция «Сменить тип оплаты» уже есть на кампанийных сущностях
-- (91/92/93/171 -> C# Campaign.ChangePaymentType -> CampaignIUD.UpdateItem).
-- Здесь добавляется акционный пункт (сущность 77) с формой
-- ChangePaymentTypeMassForm: комбо типа оплаты + список кампаний акции с
-- галочками (SmartGrid, селектор атрибутов 3 у сущности 91).
--
-- Серверных объектов не добавляется: применение идёт по одной кампании через
-- существующий CampaignIUD, серверные проверки (UIX_Campaign,
-- CannotChangePaymentType_PaymentExists) уже на месте.
--
-- Скрипт идемпотентен, повторный прогон безопасен.
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i mass-change-payment-type-seed.sql

SET NOCOUNT ON;

DECLARE @entAction INT = 77;   -- Рекламная акция (Merlin.Classes.ActionOnMassmedia)
DECLARE @entCampaign INT = 91; -- Линейная реклам. кампания (базовая сущность списка)

IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntity] WHERE entityID = @entAction AND tableName = 'Action')
BEGIN
	RAISERROR('Сущность 77 (Рекламная акция) не найдена или изменена - согласуйте с Merlin.Classes.Entities', 16, 1);
	RETURN;
END

-------------------------------------------------------------------------------
-- 1. Действие на узле акции (iEntityAction)
-------------------------------------------------------------------------------

-- ordinal_position 187 - между ChangeFirm (180) и ActionRollers (185)/Recalculate (190)
IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntityAction]
               WHERE entityID = @entAction AND name = 'ChangePaymentTypeMass')
	INSERT INTO [dbo].[iEntityAction]
		(entityID, alias, name, ordinal_position, isHidden, isGrantingAllowed, imgResourceName, parentID)
	VALUES
		(@entAction, N'Сменить тип оплаты', 'ChangePaymentTypeMass', 187, 0, 1, NULL, NULL);

UPDATE [dbo].[iEntityAction]
SET alias = N'Сменить тип оплаты'
WHERE entityID = @entAction AND name = 'ChangePaymentTypeMass'
  AND alias <> N'Сменить тип оплаты';

DECLARE @newActionID SMALLINT =
	(SELECT entityActionID FROM [dbo].[iEntityAction]
	 WHERE entityID = @entAction AND name = 'ChangePaymentTypeMass');

-------------------------------------------------------------------------------
-- 2. Права групп - те же, что у кампанийного ChangePaymentType (сущность 91)
-------------------------------------------------------------------------------

DECLARE @campaignActionID SMALLINT =
	(SELECT entityActionID FROM [dbo].[iEntityAction]
	 WHERE entityID = @entCampaign AND name = 'ChangePaymentType');

INSERT INTO [dbo].[GroupRight] (groupID, entityActionID)
SELECT gr.groupID, @newActionID
FROM [dbo].[GroupRight] gr
WHERE gr.entityActionID = @campaignActionID
	AND NOT EXISTS (SELECT 1 FROM [dbo].[GroupRight] x
	                WHERE x.groupID = gr.groupID AND x.entityActionID = @newActionID);

-------------------------------------------------------------------------------
-- 3. Селектор атрибутов 3 сущности 91 - колонки списка кампаний в форме
--    (по образцу селекторов 1/2, плюс «Тип оплаты»; колонку галочек SmartGrid
--     добавляет сам при CheckBoxes = true, атрибут isSelected не нужен)
-------------------------------------------------------------------------------

MERGE [dbo].[iEntityAttribute] AS t
USING (VALUES
	(@entCampaign, 3,  1, N'Радиостанция',  'massmediaName'),
	(@entCampaign, 3,  2, N'Группа',        'groupname'),
	(@entCampaign, 3,  3, N'Тип',           'campaignTypeName'),
	(@entCampaign, 3,  4, N'Тип оплаты',    'paymentTypeName'),
	(@entCampaign, 3, 20, N'Начало',        'startDate'),
	(@entCampaign, 3, 70, N'Окончание',     'finishDate')
) AS s(entityID, selector, ordinal_position, alias, name)
	ON t.entityID = s.entityID AND t.selector = s.selector AND t.ordinal_position = s.ordinal_position
WHEN MATCHED AND (t.alias <> s.alias OR t.name <> s.name) THEN
	UPDATE SET alias = s.alias, name = s.name
WHEN NOT MATCHED BY TARGET THEN
	INSERT (entityID, alias, name, ordinal_position, selector)
	VALUES (s.entityID, s.alias, s.name, s.ordinal_position, s.selector);

-------------------------------------------------------------------------------
-- Отчёт
-------------------------------------------------------------------------------

PRINT '--- Массовая смена типа оплаты: состояние метаданных ---';

SELECT 'iEntityAction (77/ChangePaymentTypeMass)' AS [объект], COUNT(*) AS [строк], '1' AS [ожидается]
FROM [dbo].[iEntityAction] WHERE entityID = @entAction AND name = 'ChangePaymentTypeMass'
UNION ALL SELECT 'GroupRight (новое действие)', COUNT(*), N'как у кампанийного ChangePaymentType'
FROM [dbo].[GroupRight] WHERE entityActionID = @newActionID
UNION ALL SELECT 'iEntityAttribute (91, selector 3)', COUNT(*), '6'
FROM [dbo].[iEntityAttribute] WHERE entityID = @entCampaign AND selector = 3;
