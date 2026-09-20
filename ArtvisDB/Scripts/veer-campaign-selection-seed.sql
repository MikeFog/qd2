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

-- «Группа радиостанций» (groupName из процедуры Campaigns) стоит между радиостанцией и
-- типом оплаты, поэтому строки селектора пересоздаются целиком: MERGE по ordinal_position
-- при сдвиге позиций упёрся бы в PK (entityID, alias, selector) и UIX (ordinal_position).
BEGIN TRAN;

DELETE FROM [dbo].[iEntityAttribute] WHERE entityID = @entCampaign AND selector = 4;

INSERT INTO [dbo].[iEntityAttribute] (entityID, alias, name, ordinal_position, selector)
VALUES
	(@entCampaign, N'Радиостанция',        'massmediaName',   1, 4),
	(@entCampaign, N'Группа радиостанций', 'groupName',       2, 4),
	(@entCampaign, N'Тип оплаты',          'paymentTypeName', 3, 4),
	(@entCampaign, N'Агентство',           'agencyName',      4, 4);

COMMIT;

-------------------------------------------------------------------------------
-- Отчёт
-------------------------------------------------------------------------------

PRINT '--- Веер: выбор кампаний, состояние метаданных ---';

SELECT 'iEntityAttribute (91, selector 4)' AS [объект], COUNT(*) AS [строк], '4' AS [ожидается]
FROM [dbo].[iEntityAttribute] WHERE entityID = @entCampaign AND selector = 4;
