/***************************************************************************************************
  Зачистка модуля «Производство роликов» (Studio) — ЧЕРНОВИК

  НЕ ЗАПУСКАТЬ НА ПРОДЕ БЕЗ РЕВЬЮ. Скрипт целиком обёрнут в
      BEGIN TRAN ... ROLLBACK
  то есть в текущем виде это «сухой прогон»: он печатает, сколько строк/объектов
  затронул бы, и откатывается. Чтобы применить по-настоящему — заменить
  финальный ROLLBACK на COMMIT (после бэкапа БД!).

  Составлено 2026-09-08 по localhost\ArtvisDev (копия прода от ~2026-09-01).
  Перед применением на проде выполнить РАЗДЕЛ 0 и сверить списки.

  Порядок разделов:
    0. Инвентаризация и бэкап определений (только SELECT/печать).
    1. Правка общих процедур (студийная примесь).            <-- см. investigation.md §3c
    2. DROP студийных процедур / функций / вью.
    3. DROP студийных таблиц.
    4. Очистка метаданных (i*, права, меню).
    5. (опция) RolStyle.
***************************************************************************************************/

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 0. ИНВЕНТАРИЗАЦИЯ (сверить с investigation.md; на проде — обязательно)
-------------------------------------------------------------------------------------------------
PRINT '--- 0.1 Программные объекты модуля, реально существующие в этой БД ---';
SELECT o.type_desc, o.name
FROM sys.objects o
WHERE o.type IN ('P','FN','IF','TF','V')
  AND ( o.name LIKE 'Studio%' OR o.name LIKE '%StudioOrder%' OR o.name LIKE 'PaymentStudioOrder%'
     OR o.name LIKE 'SpecialStudio%' OR o.name LIKE 'SpecialSO%' OR o.name LIKE '%BalanceStudioOrder%'
     OR o.name IN ('f_GetStudioTariffId','f_OrderPrice','vStudio','rpt_StudioOrderAct','rpt_OrderActionBill',
                   'sl_StudioOrderActions','FirmStudioOrderManagers','sl_PaymentStudioOrders','FirmWithOrder',
                   'stat_BalanceManagerOrder','stat_VolumeOfRealizationForRollers',
                   'statVolumeOfRealizationForRollersFilter','stat_RollerStatisticCreated') )
ORDER BY o.type_desc, o.name;

PRINT '--- 0.2 Определения db-only процедур — СКОПИРОВАТЬ ВЫВОД В ФАЙЛ перед удалением ---';
SELECT o.name, m.definition
FROM sys.sql_modules m JOIN sys.objects o ON o.object_id = m.object_id
WHERE o.name IN (
  'Studios','StudioIUD','StudioPassport','StudioAgencyID',
  'StudioPricelists','StudioPricelistIUD',
  'StudioTariffList','StudioTariffPassport','StudioTariffIUD',
  'StudioOrders','StudioOrderIUD','StudioOrderPassport','StudioOrderFilter','StudioOrderAgencies',
  'StudioOrderActions','StudioOrderActionIUD','StudioOrderActionPassport','StudioOrderActionsFilter',
  'StudioOrderActionsForPayment','StudioOrderActionPriceForAgency',
  'StudioOrderBills','StudioOrderBillIUD','FirmWithOrder')
ORDER BY o.name;

PRINT '--- 0.3 Строки метаданных под удаление (перед удалением можно выгрузить в *.sql INSERT-ами) ---';
DECLARE @ent TABLE(entityID INT PRIMARY KEY);
INSERT INTO @ent VALUES (19),(113),(115),(116),(117),(119),(123),(124),(125),(126),(127),(128),(159),(169),(186),(200),(208);
-- (RolStyle = 4 — отдельно, РАЗДЕЛ 5)

DECLARE @sp TABLE(storedProcedureID INT PRIMARY KEY);
INSERT INTO @sp
SELECT storedProcedureID FROM dbo.iStoredProcedure
WHERE name LIKE '%Studio%' OR name LIKE 'SpecialSO%' OR name LIKE 'SpecialStudio%'
   OR name IN ('rpt_OrderActionBill','rpt_StudioOrderAct','sl_StudioOrderActions','sl_PaymentStudioOrders',
               'FirmStudioOrderManagers','FirmWithOrder','stat_BalanceStudioOrder','stat_BalanceManagerOrder',
               'stat_VolumeOfRealizationForRollers','statVolumeOfRealizationForRollersFilter',
               'stat_RollerStatisticCreated','BalanceStudioOrderFilter','FirmBalanceStudioOrderOnLoad');

SELECT 'iEntity'          AS meta, COUNT(*) AS rows FROM dbo.iEntity          WHERE entityID IN (SELECT entityID FROM @ent)
UNION ALL SELECT 'iEntityAction',     COUNT(*) FROM dbo.iEntityAction     WHERE entityID IN (SELECT entityID FROM @ent)
UNION ALL SELECT 'iEntityAttribute',  COUNT(*) FROM dbo.iEntityAttribute  WHERE entityID IN (SELECT entityID FROM @ent)
UNION ALL SELECT 'iEntityRelation',   COUNT(*) FROM dbo.iEntityRelation   WHERE parentEntityID IN (SELECT entityID FROM @ent) OR childEntityID IN (SELECT entityID FROM @ent)
UNION ALL SELECT 'iModuleProcedure(byProc)', COUNT(*) FROM dbo.iModuleProcedure WHERE storedProcedureID IN (SELECT storedProcedureID FROM @sp)
UNION ALL SELECT 'iModuleProcedure(byEnt)',  COUNT(*) FROM dbo.iModuleProcedure WHERE entityID IN (SELECT entityID FROM @ent)
UNION ALL SELECT 'iTableAlias',       COUNT(*) FROM dbo.iTableAlias       WHERE storedProcedureID IN (SELECT storedProcedureID FROM @sp)
UNION ALL SELECT 'iStoredProcedure',  COUNT(*) FROM dbo.iStoredProcedure  WHERE storedProcedureID IN (SELECT storedProcedureID FROM @sp)
UNION ALL SELECT 'GroupRight',        COUNT(*) FROM dbo.GroupRight        WHERE entityActionID IN (SELECT entityActionID FROM dbo.iEntityAction WHERE entityID IN (SELECT entityID FROM @ent))
UNION ALL SELECT 'iMenu(studio)',     COUNT(*) FROM dbo.iMenu             WHERE menuID IN (SELECT menuID FROM dbo.iMenu WHERE isObsolete = 1 AND (codeName LIKE '%Studio%' OR codeName LIKE '%ProductionStudio%' OR codeName LIKE '%RolStyle%' OR codeName IN ('miCreateProductionAction','miProductionActionsStudio','miStudioOrderActPrint','miStats.VolumeOfRealization4Roll','miStats.RollersCreated','miStats.BalanceManagerOrder','miSpecialStudioOrderActions')))
;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 1. ПРАВКА ОБЩИХ ПРОЦЕДУР  (расписано в investigation.md §3c; здесь — заглушки-напоминания)
-------------------------------------------------------------------------------------------------
--  ALTER PROCEDURE dbo.AgencyIUD ...        -- убрать «DELETE FROM [Studio] WHERE StudioID = @agencyID»
--  ALTER PROCEDURE dbo.agencyPassport ...   -- убрать 2-й result-set (vStudio/StudioAgency)
--  ALTER PROCEDURE dbo.LookupUsedAgency ... -- убрать «union select distinct o.agencyID from StudioOrder o»
--  ALTER PROCEDURE dbo.UserListByRights ... -- убрать @forStudioOrders + ветку (сверить вызовы!)
--  ALTER PROCEDURE dbo.GroupListByRights ...-- то же
--  ALTER PROCEDURE dbo.RollerIUD ...        -- убрать @studioOrderID + связанный SELECT/IF
--  ALTER PROCEDURE dbo.RollerPassport ...   -- (если удаляем RolStyle) убрать result-set RolStyle
--  косметика: agencies, sl_Agencies — вычистить мёртвые комментарии про Studio
PRINT '--- 1. Правки общих процедур — выполнить вручную по investigation.md §3c ---';

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 2. DROP ПРОЦЕДУР / ФУНКЦИЙ / ВЬЮ
-------------------------------------------------------------------------------------------------
PRINT '--- 2. DROP программных объектов ---';
DECLARE @drop NVARCHAR(MAX) = N'';
SELECT @drop = @drop + N'DROP ' +
       CASE o.type WHEN 'V' THEN N'VIEW ' WHEN 'P' THEN N'PROCEDURE ' ELSE N'FUNCTION ' END +
       QUOTENAME(SCHEMA_NAME(o.schema_id)) + N'.' + QUOTENAME(o.name) + N';' + CHAR(13)+CHAR(10)
FROM sys.objects o
WHERE o.type IN ('P','FN','IF','TF','V')
  AND ( o.name LIKE 'Studio%' OR o.name LIKE '%StudioOrder%' OR o.name LIKE 'PaymentStudioOrder%'
     OR o.name LIKE 'SpecialStudio%' OR o.name LIKE 'SpecialSO%' OR o.name LIKE '%BalanceStudioOrder%'
     OR o.name IN ('f_GetStudioTariffId','f_OrderPrice','vStudio','rpt_StudioOrderAct','rpt_OrderActionBill',
                   'sl_StudioOrderActions','FirmStudioOrderManagers','sl_PaymentStudioOrders','FirmWithOrder',
                   'stat_BalanceManagerOrder','stat_VolumeOfRealizationForRollers',
                   'statVolumeOfRealizationForRollersFilter','stat_RollerStatisticCreated') );
PRINT @drop;                 -- посмотреть, что удалится
EXEC sys.sp_executesql @drop;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 3. DROP ТАБЛИЦ (листья -> корень)
-------------------------------------------------------------------------------------------------
PRINT '--- 3. DROP таблиц ---';
DROP TABLE IF EXISTS dbo.PaymentStudioOrderAction;
DROP TABLE IF EXISTS dbo.PaymentStudioOrder;
DROP TABLE IF EXISTS dbo.StudioOrderBill;
DROP TABLE IF EXISTS dbo.StudioOrder;
DROP TABLE IF EXISTS dbo.StudioOrderAction;
DROP TABLE IF EXISTS dbo.StudioAgency;
DROP TABLE IF EXISTS dbo.StudioTariff;
DROP TABLE IF EXISTS dbo.StudioPricelist;
DROP TABLE IF EXISTS dbo.Studio;
DROP TABLE IF EXISTS dbo.iStudioOrderActionStatus;
DROP TABLE IF EXISTS dbo.iStudioTariffType;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 4. ОЧИСТКА МЕТАДАННЫХ
--   Порядок: сначала «детей», потом «родителей». Между i*-таблицами есть свои FK —
--   при ошибке FK переставить строки.
-------------------------------------------------------------------------------------------------
PRINT '--- 4. Метаданные ---';

DELETE gr FROM dbo.GroupRight gr
WHERE gr.entityActionID IN (SELECT entityActionID FROM dbo.iEntityAction WHERE entityID IN (SELECT entityID FROM @ent));

DELETE gm FROM dbo.GroupMenu gm
WHERE gm.menuID IN (
  SELECT menuID FROM dbo.iMenu WHERE isObsolete = 1
    AND (codeName LIKE '%Studio%' OR codeName LIKE '%ProductionStudio%' OR codeName LIKE '%RolStyle%'
      OR codeName IN ('miCreateProductionAction','miProductionActionsStudio','miStudioOrderActPrint',
                      'miStats.VolumeOfRealization4Roll','miStats.RollersCreated','miStats.BalanceManagerOrder',
                      'miSpecialStudioOrderActions','miPaymentStudioOrder','miBalanceStudioOrder',
                      'miFirmBalanceStudioOrder','miPaymentStudioOrderByManager')));

-- дочерние пункты меню -> родительские ветки 156 и 110
DELETE FROM dbo.iMenu WHERE parentID IN (156,110);
DELETE FROM dbo.iMenu WHERE menuID IN (156,110);
DELETE FROM dbo.iMenu WHERE codeName IN ('miProductionStudio','miRolStyle','miStudioTariff','miCreateProductionAction',
   'miProductionActionsStudio','miSpecialStudioOrderActions','miStats.VolumeOfRealization4Roll','miStats.RollersCreated');

DELETE FROM dbo.iModuleProcedure WHERE storedProcedureID IN (SELECT storedProcedureID FROM @sp);
DELETE FROM dbo.iModuleProcedure WHERE entityID IN (SELECT entityID FROM @ent);
-- студийные строки для общих сущностей (модуль 210 «Select For Studio Order»)
DELETE FROM dbo.iModuleProcedure WHERE moduleID = 210;

DELETE FROM dbo.iTableAlias WHERE storedProcedureID IN (SELECT storedProcedureID FROM @sp);

DELETE FROM dbo.iEntityRelation
WHERE parentEntityID IN (SELECT entityID FROM @ent) OR childEntityID IN (SELECT entityID FROM @ent);
DELETE FROM dbo.iRelationScenario WHERE relationScenarioID = 17;      -- ProductionAction
-- сценарий 16 (19 -> 113) — проверить, что больше нигде не используется, затем:
-- DELETE FROM dbo.iRelationScenario WHERE relationScenarioID = 16;

DELETE FROM dbo.iEntityAttribute WHERE entityID IN (SELECT entityID FROM @ent);
DELETE FROM dbo.iEntityAction    WHERE entityID IN (SELECT entityID FROM @ent);
DELETE FROM dbo.iEntity          WHERE entityID IN (SELECT entityID FROM @ent);

DELETE FROM dbo.iStoredProcedure WHERE storedProcedureID IN (SELECT storedProcedureID FROM @sp);

DELETE FROM dbo.iModules WHERE moduleID = 210;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 5. (ОПЦИЯ) RolStyle — см. investigation.md §6. Выполнять ТОЛЬКО после раздела 1
--   (правка RollerIUD / RollerPassport / vRoller) и ALTER TABLE Roller DROP COLUMN rolStyleID.
-------------------------------------------------------------------------------------------------
-- ALTER TABLE dbo.Roller DROP COLUMN rolStyleID;      -- FK нет, но есть в vRoller/RollerIUD
-- DROP VIEW dbo.vRoller; CREATE VIEW dbo.vRoller ...  -- пересоздать без rolStyleID
-- DROP PROCEDURE dbo.RollerStyles;
-- DROP PROCEDURE dbo.RolStyleIUD;
-- DROP TABLE dbo.RolStyle;
-- DELETE FROM dbo.iModuleProcedure WHERE entityID = 4;
-- DELETE FROM dbo.iEntityAction    WHERE entityID = 4;
-- DELETE FROM dbo.iEntityAttribute WHERE entityID = 4;
-- DELETE FROM dbo.iEntity          WHERE entityID = 4;
-- DELETE FROM dbo.iStoredProcedure WHERE name IN ('RollerStyles','RolStyleIUD');
-- DELETE FROM dbo.iMenu WHERE codeName = 'miRolStyle';

-------------------------------------------------------------------------------------------------
PRINT '=== СУХОЙ ПРОГОН: откат. Для применения — заменить ROLLBACK на COMMIT (после BACKUP). ===';
ROLLBACK TRAN;
-- COMMIT TRAN;
