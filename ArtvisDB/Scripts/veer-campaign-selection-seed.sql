-- Веер: работа с частью линейных кампаний акции. Метаданные.
--
-- На форме веерного размещения (EditIssuesForm) появился чек-лист кампаний акции:
-- пользователь отмечает те линейные кампании, с которыми работает, и жмёт «Обновить»
-- на тулбаре. Дальше сетка, добавление, удаление и перенос выпусков идут только по ним
-- (параметр @campaignIDs в TariffWindowWithRange / AddRangeIssues / MasterIssueDelete).
--
-- Здесь добавляется только селектор атрибутов 4 у сущности 91 — колонки чек-листа.
-- По одной радиостанции в акции может идти несколько линейных кампаний, различающихся
-- типом оплаты и агентством (UIX_Campaign: actionID + massmediaID + campaignTypeID +
-- paymentTypeID + agencyID), поэтому одной «Радиостанции» в списке мало.
--
-- Скрипт идемпотентен, повторный прогон безопасен.
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i veer-campaign-selection-seed.sql

SET NOCOUNT ON;

DECLARE @entCampaign INT = 91; -- Линейная реклам. кампания

IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntity] WHERE entityID = @entCampaign)
BEGIN
	RAISERROR('Сущность 91 (Линейная рекламная кампания) не найдена - согласуйте с Merlin.Classes.Entities', 16, 1);
	RETURN;
END

-------------------------------------------------------------------------------
-- Селектор атрибутов 4 сущности 91 — колонки чек-листа кампаний веера.
-- Колонку галочек SmartGrid добавляет сам при CheckBoxes = true.
-------------------------------------------------------------------------------

MERGE [dbo].[iEntityAttribute] AS t
USING (VALUES
	(@entCampaign, 4, 1, N'Радиостанция', 'massmediaName'),
	(@entCampaign, 4, 2, N'Тип оплаты',   'paymentTypeName'),
	(@entCampaign, 4, 3, N'Агентство',    'agencyName')
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

PRINT '--- Веер: выбор кампаний, состояние метаданных ---';

SELECT 'iEntityAttribute (91, selector 4)' AS [объект], COUNT(*) AS [строк], '3' AS [ожидается]
FROM [dbo].[iEntityAttribute] WHERE entityID = @entCampaign AND selector = 4;
