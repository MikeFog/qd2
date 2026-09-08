/***************************************************************************************************
  Зачистка модуля «Производство роликов» (Studio) + RolStyle — деплой на ПРОД (Artvis)

  Эта последовательность уже выполнена и проверена на ArtvisDev (копия прода) 2026-09-08.
  См. docs/studio-cleanup/investigation.md.

  ── ПОРЯДОК ДЕПЛОЯ ────────────────────────────────────────────────────────────────────────────
  0. BACKUP DATABASE Artvis TO DISK='...' WITH COPY_ONLY, INIT;
  1. Выкатить изменённые процедуры/вью из репозитория (коммит 229ddbd):
       ArtvisDB/dbo/Stored Procedures/{agencies,AgencyIUD,agencyPassport,LookupUsedAgency,
         sl_Agencies,RollerIUD,RollerPassport,ActionRollerSetAdvertType,SetAdvertTypeForCommmonRoller}.sql
       ArtvisDB/dbo/Views/vRoller.sql   ← БЕЗ хвоста с EXECUTE sp_addextendedproperty (иначе Msg 15233)
     Все как ALTER. Порядок между собой не важен, но ВСЕ до раздела 2 этого скрипта.
  2. Запустить ЭТОТ скрипт ЦЕЛИКОМ (он одним батчем, без GO, в транзакции).
     Для проверки — оставить ROLLBACK в конце; для применения — заменить на COMMIT.
  3. Рестарт клиентского приложения (сброс кеша метаданных iEntity).

  ⚠ НЕ разбивать раздел 2 на батчи через GO с `SET XACT_ABORT ON`: при ошибке транзакция
     откатится, а sqlcmd продолжит следующие батчи в автокоммите (так уже сломали dev).
***************************************************************************************************/

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 1. Списки
-------------------------------------------------------------------------------------------------
DECLARE @ent TABLE(id INT PRIMARY KEY);
INSERT INTO @ent VALUES (4),(19),(113),(115),(116),(117),(119),(123),(124),(125),(126),(127),(128),(159),(169),(186),(200),(208);

DECLARE @sp TABLE(id INT PRIMARY KEY);
INSERT INTO @sp
SELECT storedProcedureID FROM dbo.iStoredProcedure
WHERE name LIKE '%Studio%' OR name LIKE 'SpecialSO%' OR name LIKE 'SpecialStudio%'
   OR name IN ('rpt_OrderActionBill','rpt_StudioOrderAct','sl_StudioOrderActions','sl_PaymentStudioOrders',
               'FirmStudioOrderManagers','FirmWithOrder','stat_BalanceStudioOrder','stat_BalanceManagerOrder',
               'stat_VolumeOfRealizationForRollers','statVolumeOfRealizationForRollersFilter',
               'stat_RollerStatisticCreated','BalanceStudioOrderFilter','FirmBalanceStudioOrderOnLoad',
               'RollerStyles','RolStyleIUD');

DECLARE @mnu TABLE(id INT PRIMARY KEY);
INSERT INTO @mnu SELECT menuID FROM dbo.iMenu WHERE menuID IN (156,110) OR parentID IN (156,110);

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 2. DROP программных объектов (процедуры/функции/вью модуля)
-------------------------------------------------------------------------------------------------
DECLARE @drop NVARCHAR(MAX) = N'';
SELECT @drop = @drop + N'DROP ' +
       CASE o.type WHEN 'V' THEN N'VIEW ' WHEN 'P' THEN N'PROCEDURE ' ELSE N'FUNCTION ' END +
       QUOTENAME(SCHEMA_NAME(o.schema_id)) + N'.' + QUOTENAME(o.name) + N';' + CHAR(10)
FROM sys.objects o
WHERE o.type IN ('P','FN','IF','TF','V')
  AND ( o.name LIKE 'Studio%' OR o.name LIKE '%StudioOrder%' OR o.name LIKE 'PaymentStudioOrder%'
     OR o.name LIKE 'SpecialStudio%' OR o.name LIKE 'SpecialSO%' OR o.name LIKE '%BalanceStudioOrder%'
     OR o.name IN ('f_GetStudioTariffId','f_OrderPrice','vStudio','rpt_StudioOrderAct','rpt_OrderActionBill',
                   'sl_StudioOrderActions','FirmStudioOrderManagers','sl_PaymentStudioOrders','FirmWithOrder',
                   'stat_BalanceManagerOrder','stat_VolumeOfRealizationForRollers',
                   'statVolumeOfRealizationForRollersFilter','stat_RollerStatisticCreated',
                   'RollerStyles','RolStyleIUD') );
PRINT @drop;
EXEC sys.sp_executesql @drop;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 3. DROP таблиц (листья -> корень), затем колонка Roller.rolStyleID, затем RolStyle
-------------------------------------------------------------------------------------------------
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
IF COL_LENGTH('dbo.Roller','rolStyleID') IS NOT NULL
    ALTER TABLE dbo.Roller DROP COLUMN rolStyleID;   -- раздел 1 деплоя должен был убрать все ссылки
DROP TABLE IF EXISTS dbo.RolStyle;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 4. Очистка метаданных
-------------------------------------------------------------------------------------------------
DELETE FROM dbo.GroupRight       WHERE entityActionID IN (SELECT entityActionID FROM dbo.iEntityAction WHERE entityID IN (SELECT id FROM @ent));
DELETE FROM dbo.GroupMenu        WHERE menuID IN (SELECT id FROM @mnu);
DELETE FROM dbo.UserAdditionMenu WHERE menuID IN (SELECT id FROM @mnu);
DELETE FROM dbo.UserAdditionRight WHERE entityActionID IN (SELECT entityActionID FROM dbo.iEntityAction WHERE entityID IN (SELECT id FROM @ent));

DELETE FROM dbo.iMenu WHERE menuID IN (SELECT id FROM @mnu) AND parentID IN (156,110);
DELETE FROM dbo.iMenu WHERE menuID IN (156,110);

DELETE FROM dbo.iModuleProcedure WHERE storedProcedureID IN (SELECT id FROM @sp) OR entityID IN (SELECT id FROM @ent) OR moduleID = 210;
DELETE FROM dbo.iTableAlias      WHERE storedProcedureID IN (SELECT id FROM @sp);

DELETE FROM dbo.iEntityRelation  WHERE parentEntityID IN (SELECT id FROM @ent) OR childEntityID IN (SELECT id FROM @ent);
DELETE FROM dbo.iRelationScenario WHERE relationScenarioID IN (16,17);   -- 16: 19->113, 17: ProductionAction
DELETE FROM dbo.iEntityAttribute WHERE entityID IN (SELECT id FROM @ent);
DELETE FROM dbo.iEntityAction    WHERE entityID IN (SELECT id FROM @ent);
DELETE FROM dbo.iEntity          WHERE entityID IN (SELECT id FROM @ent);

DELETE FROM dbo.iStoredProcedure WHERE storedProcedureID IN (SELECT id FROM @sp);
DELETE FROM dbo.iModules         WHERE moduleID = 210;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 5. Проверки (должны быть все нули / OK)
-------------------------------------------------------------------------------------------------
SELECT leftover_studio_objects = COUNT(*) FROM sys.objects
    WHERE name LIKE '%Studio%' OR name IN ('RolStyle','RollerStyles','RolStyleIUD','f_OrderPrice','f_GetStudioTariffId','vStudio');
SELECT leftover_iStoredProcedure = COUNT(*) FROM dbo.iStoredProcedure WHERE name LIKE '%Studio%' OR name LIKE '%RolStyle%';
SELECT leftover_iEntity = COUNT(*) FROM dbo.iEntity WHERE entityID IN (SELECT id FROM @ent);
SELECT orphan_iModuleProcedure = COUNT(*) FROM dbo.iModuleProcedure mp WHERE NOT EXISTS (SELECT 1 FROM dbo.iStoredProcedure sp WHERE sp.storedProcedureID = mp.storedProcedureID);
SELECT orphan_iTableAlias = COUNT(*) FROM dbo.iTableAlias ta WHERE NOT EXISTS (SELECT 1 FROM dbo.iStoredProcedure sp WHERE sp.storedProcedureID = ta.storedProcedureID);
SELECT orphan_GroupRight = COUNT(*) FROM dbo.GroupRight gr WHERE NOT EXISTS (SELECT 1 FROM dbo.iEntityAction ea WHERE ea.entityActionID = gr.entityActionID);
SELECT orphan_iMenu_parent = COUNT(*) FROM dbo.iMenu m WHERE m.parentID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.iMenu p WHERE p.menuID = m.parentID);
-- login + startup metadata proc должны отработать:
EXEC dbo.ProcedureConfigurationRetrieve;

PRINT '=== проверьте вывод выше. Для применения: заменить ROLLBACK на COMMIT ===';
ROLLBACK TRANSACTION;
-- COMMIT TRANSACTION;
