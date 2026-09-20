/***************************************************************************************************
  Зачистка мёртвых веток меню: miPrintInquire, miDisabledWindows, miUpdateBanksList (+ лишний
  литерал miPayment, в базе он не нужен) — деплой на ПРОД и на базы заказчиков

  Все четыре пункта отсутствуют в iMenu (проверено на ArtvisDev, копии прода), ветки в
  MDIForm.MenuItemClick недостижимы. Здесь — только метаданные и процедуры; код — в ветке
  cleanup/dead-menu-branches. Данные не сохраняются (решение владельца, 2026-09-19).

  Что удаляется
    miPrintInquire      сценарий «Massmedia and Campaigns» (id 18, связь 9→78) и сущность 188
                        «Радиостанция» (vMassmedia; без действий, процедур и прав — её использовал
                        только фильтр контейнера MassmediasAndCampaignsContainer).
    miDisabledWindows   сценарий «Disabled windows» (id 7, связь 9→10), сущность 10 «Время
                        профилактики» с её действиями, атрибутами и процедурами (disabledWindows,
                        DisabledWindowIUD), действие AddDisabledWindow у Радиостанции (сущность 9)
                        с правами на него.
    miUpdateBanksList   процедура bankListUpdate и сообщения BanksListUpdatedSuccesfully /
                        BanksListUpdateFailed. Справочник банков (Bank) остаётся, ведётся руками.

  Что НЕ удаляется (сознательно)
    Таблица DisabledWindow и всё, что её читает: fn_IsDisabledWindow и проверки «времени
    профилактики» в hlp_IssueVerify, IssueTransfer, ProgramIssueIUD, TariffWindowIUD,
    GenerateTariffWindowByTemplate, sl_GenerateTariffWindowsDay, CampaignImportGrammofon,
    CampaignImportMediaPlus (9 объектов). Таблица пуста везде, но часть процедур горячие
    (проверка выпусков и окон), а веб их не касается; решение — отдельно (docs/IMPROVEMENTS.md,
    [SQL-04]). Процедура ShowDisabledWindows таблицу НЕ читает (это про флаг TariffWindow.isDisabled,
    живое действие прайс-листа «Показать заблокированные окна») и остаётся.

  ── ПОРЯДОК ДЕПЛОЯ ────────────────────────────────────────────────────────────────────────────
  0. BACKUP DATABASE <база> TO DISK='...' WITH COPY_ONLY, INIT;
  1. Запустить ЭТОТ скрипт ЦЕЛИКОМ, от sysadmin. Он одним батчем, без GO, в транзакции.
     Для проверки — оставить ROLLBACK в конце; для применения — заменить на COMMIT.
     Останавливается, если на удаляемые процедуры кто-то ещё ссылается.
  2. Рестарт клиентского приложения (сброс кеша метаданных iEntity).
     Клиент нового билда выкатывать после скрипта или вместе с ним.

  ⚠ НЕ разбивать скрипт на батчи через GO с `SET XACT_ABORT ON`: при ошибке транзакция
     откатится, а sqlcmd продолжит следующие батчи в автокоммите (так уже ломали dev).
***************************************************************************************************/

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 0. Проверки
-------------------------------------------------------------------------------------------------
DECLARE @refs NVARCHAR(MAX);
SELECT @refs = STRING_AGG(CAST(o.name AS NVARCHAR(MAX)), N', ')
FROM sys.sql_modules m
JOIN sys.objects o ON o.object_id = m.object_id
WHERE o.name NOT IN (N'disabledWindows', N'DisabledWindowIUD', N'bankListUpdate')
  AND ( m.definition LIKE N'%DisabledWindowIUD%'
     OR m.definition LIKE N'%bankListUpdate%'
     OR m.definition LIKE N'%[^a-z]disabledWindows[^a-z]%' );   -- с границей слова: ShowDisabledWindows — другая процедура
IF @refs IS NOT NULL
BEGIN
    RAISERROR(N'Остановлено: на disabledWindows/DisabledWindowIUD/bankListUpdate ещё ссылаются: %s.', 16, 1, @refs);
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- Метаданные должны быть именно такими, как на ArtvisDev; иначе не трогаем.
IF (SELECT COUNT(*) FROM dbo.iEntity WHERE (entityID = 10 AND tableName = N'DisabledWindow')
                                        OR (entityID = 188 AND tableName = N'vMassmedia')) NOT IN (0, 2)
   OR EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID IN (10, 188) AND tableName NOT IN (N'DisabledWindow', N'vMassmedia'))
BEGIN
    RAISERROR(N'Остановлено: сущности 10/188 не те, что ожидались (10 = DisabledWindow, 188 = vMassmedia).', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

IF EXISTS (SELECT 1 FROM dbo.iEntity WHERE parentId IN (10, 188))
   OR EXISTS (SELECT 1 FROM dbo.iEntityRelation r JOIN dbo.iRelationScenario s ON s.relationScenarioID = r.relationScenarioID
              WHERE s.name NOT IN (N'Disabled windows', N'Massmedia and Campaigns')
                AND (r.parentEntityID IN (10, 188) OR r.childEntityID IN (10, 188)))
BEGIN
    RAISERROR(N'Остановлено: сущности 10/188 используются в других сценариях или как родитель.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 1. Списки
-------------------------------------------------------------------------------------------------
DECLARE @ent TABLE(id INT PRIMARY KEY);
INSERT INTO @ent SELECT entityID FROM dbo.iEntity WHERE entityID IN (10, 188);

DECLARE @scn TABLE(id INT PRIMARY KEY);
INSERT INTO @scn SELECT relationScenarioID FROM dbo.iRelationScenario WHERE name IN (N'Disabled windows', N'Massmedia and Campaigns');

DECLARE @sp TABLE(id INT PRIMARY KEY);
INSERT INTO @sp SELECT storedProcedureID FROM dbo.iStoredProcedure WHERE name IN (N'disabledWindows', N'DisabledWindowIUD', N'bankListUpdate');

-- Действия, у которых надо снять права: все действия сущностей 10/188 и AddDisabledWindow у сущности 9.
DECLARE @act TABLE(id INT PRIMARY KEY);
INSERT INTO @act
SELECT entityActionID FROM dbo.iEntityAction
WHERE entityID IN (SELECT id FROM @ent) OR (entityID = 9 AND name = N'AddDisabledWindow');

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 2. DROP процедур
-------------------------------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS dbo.disabledWindows;
DROP PROCEDURE IF EXISTS dbo.DisabledWindowIUD;
DROP PROCEDURE IF EXISTS dbo.bankListUpdate;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 3. Очистка метаданных
-------------------------------------------------------------------------------------------------
DELETE FROM dbo.GroupRight        WHERE entityActionID IN (SELECT id FROM @act);
DELETE FROM dbo.UserAdditionRight WHERE entityActionID IN (SELECT id FROM @act);
DELETE FROM dbo.iEntityAction     WHERE entityActionID IN (SELECT id FROM @act);

DELETE FROM dbo.iModuleProcedure  WHERE entityID IN (SELECT id FROM @ent) OR storedProcedureID IN (SELECT id FROM @sp);
DELETE FROM dbo.iTableAlias       WHERE storedProcedureID IN (SELECT id FROM @sp);

DELETE FROM dbo.iEntityRelation   WHERE relationScenarioID IN (SELECT id FROM @scn)
                                     OR parentEntityID IN (SELECT id FROM @ent) OR childEntityID IN (SELECT id FROM @ent);
DELETE FROM dbo.iRelationScenario WHERE relationScenarioID IN (SELECT id FROM @scn);

DELETE FROM dbo.iEntityAttribute  WHERE entityID IN (SELECT id FROM @ent);
DELETE FROM dbo.iEntity           WHERE entityID IN (SELECT id FROM @ent);

DELETE FROM dbo.iStoredProcedure  WHERE storedProcedureID IN (SELECT id FROM @sp);

DELETE FROM dbo.iMessage          WHERE name IN (N'BanksListUpdatedSuccesfully', N'BanksListUpdateFailed');

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 4. Проверки (должны быть все нули / OK)
-------------------------------------------------------------------------------------------------
SELECT leftover_procs = COUNT(*) FROM sys.objects WHERE name IN (N'disabledWindows', N'DisabledWindowIUD', N'bankListUpdate');
SELECT leftover_iEntity = COUNT(*) FROM dbo.iEntity WHERE entityID IN (10, 188);
SELECT leftover_iEntityAction = COUNT(*) FROM dbo.iEntityAction WHERE entityID IN (10, 188) OR name = N'AddDisabledWindow';
SELECT leftover_iEntityAttribute = COUNT(*) FROM dbo.iEntityAttribute WHERE entityID IN (10, 188);
SELECT leftover_iModuleProcedure = COUNT(*) FROM dbo.iModuleProcedure WHERE entityID IN (10, 188);
SELECT leftover_scenarios = COUNT(*) FROM dbo.iRelationScenario WHERE name IN (N'Disabled windows', N'Massmedia and Campaigns');
SELECT leftover_iStoredProcedure = COUNT(*) FROM dbo.iStoredProcedure WHERE name IN (N'disabledWindows', N'DisabledWindowIUD', N'bankListUpdate');
SELECT leftover_iMessage = COUNT(*) FROM dbo.iMessage WHERE name LIKE N'BanksListUpdate%';
SELECT orphan_iModuleProcedure = COUNT(*) FROM dbo.iModuleProcedure mp WHERE NOT EXISTS (SELECT 1 FROM dbo.iStoredProcedure sp WHERE sp.storedProcedureID = mp.storedProcedureID);
SELECT orphan_iTableAlias = COUNT(*) FROM dbo.iTableAlias ta WHERE NOT EXISTS (SELECT 1 FROM dbo.iStoredProcedure sp WHERE sp.storedProcedureID = ta.storedProcedureID);
SELECT orphan_GroupRight = COUNT(*) FROM dbo.GroupRight gr WHERE NOT EXISTS (SELECT 1 FROM dbo.iEntityAction ea WHERE ea.entityActionID = gr.entityActionID);
SELECT orphan_UserAdditionRight = COUNT(*) FROM dbo.UserAdditionRight ur WHERE NOT EXISTS (SELECT 1 FROM dbo.iEntityAction ea WHERE ea.entityActionID = ur.entityActionID);
SELECT orphan_iEntityRelation = COUNT(*) FROM dbo.iEntityRelation r
    WHERE NOT EXISTS (SELECT 1 FROM dbo.iRelationScenario s WHERE s.relationScenarioID = r.relationScenarioID)
       OR NOT EXISTS (SELECT 1 FROM dbo.iEntity e WHERE e.entityID = r.parentEntityID)
       OR NOT EXISTS (SELECT 1 FROM dbo.iEntity e WHERE e.entityID = r.childEntityID);
-- нетронутое остаётся на месте:
SELECT kept_DisabledWindow_table = COUNT(*) FROM sys.tables WHERE name = N'DisabledWindow';
SELECT kept_ShowDisabledWindows  = COUNT(*) FROM sys.procedures WHERE name = N'ShowDisabledWindows';
SELECT kept_iEntity_Bank_and_Massmedia = COUNT(*) FROM dbo.iEntity WHERE entityID IN (2, 9);   -- ожидается 2
-- стартовые метаданные должны отработать:
EXEC dbo.ProcedureConfigurationRetrieve;

PRINT N'=== проверьте вывод выше. Для применения: заменить ROLLBACK на COMMIT ===';
ROLLBACK TRANSACTION;
-- COMMIT TRANSACTION;
