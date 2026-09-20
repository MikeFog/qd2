/***************************************************************************************************
  Удаление «Брэндов» (Brand / FirmBrand / RollerBrand) — деплой на ПРОД и на базы заказчиков

  Зачем. «Брэнд» заменён предметом рекламы (AdvertType). RollerBrand пуста, экранов у
  Brand/FirmBrand нет: пункта miBrand нет в iMenu, форма FirmJournalForm никем не создаётся.
  Удаляем всё, что осталось: три таблицы, функцию, 6 процедур, метаданные сущностей 15/120/121
  и набор строк «firmbrand» в паспорте ролика. Данные не сохраняются (решение владельца, 2026-09-19).

  ── ПОРЯДОК ДЕПЛОЯ ────────────────────────────────────────────────────────────────────────────
  0. BACKUP DATABASE <база> TO DISK='...' WITH COPY_ONLY, INIT;
  1. ALTER PROCEDURE dbo.stat_RollerStatistic из репозитория
       ArtvisDB/dbo/Stored Procedures/stat_RollerStatistic.sql  (убрана колонка brandList,
       которую никто не читал: RollerBrand пуста).
     Это ДО скрипта: функцию fn_BrandListByRollerId он вызывает. Скрипт проверит и остановится,
     если на функцию или таблицы ещё кто-то ссылается.
  2. Запустить ЭТОТ скрипт ЦЕЛИКОМ, от sysadmin (иначе definition модулей = NULL и проверка
     ссылок слепая). Он одним батчем, без GO, в транзакции. Для проверки — оставить ROLLBACK
     в конце; для применения — заменить на COMMIT.
     Внутри скрипта заменяется dbo.RollerPassport (нет набора №4 «firmbrand») и в iTableAlias
     алиас rolActionType сдвигается с позиции 5 на 4 — в одной транзакции, чтобы наборы строк
     паспорта ролика не разошлись с названиями.
  3. Рестарт клиентского приложения (сброс кеша метаданных iEntity).
     Клиент нового билда (без сущностей Brand/FirmBrand) выкатывать после скрипта или вместе с ним.

  ⚠ НЕ разбивать скрипт на батчи через GO с `SET XACT_ABORT ON`: при ошибке транзакция
     откатится, а sqlcmd продолжит следующие батчи в автокоммите (так уже ломали dev).
***************************************************************************************************/

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 0. Проверки: то, что удаляем, больше никому не нужно
-------------------------------------------------------------------------------------------------
DECLARE @refs NVARCHAR(MAX);
SELECT @refs = STRING_AGG(CAST(o.name AS NVARCHAR(MAX)), N', ')
FROM sys.sql_modules m
JOIN sys.objects o ON o.object_id = m.object_id
WHERE m.definition LIKE N'%Brand%'
  AND o.name NOT IN (N'Brands', N'BrandFirms', N'BrandIUD', N'BrandFilter', N'FirmBrandID', N'FirmBrands',
                     N'fn_BrandListByRollerId', N'RollerPassport');
IF @refs IS NOT NULL
BEGIN
    RAISERROR(N'Остановлено: на Brand/FirmBrand/RollerBrand/fn_BrandListByRollerId ещё ссылаются: %s. Сначала выкатить их новые версии (шаг 1 порядка деплоя).', 16, 1, @refs);
    ROLLBACK TRANSACTION;
    RETURN;
END;

DECLARE @rp INT = (SELECT storedProcedureID FROM dbo.iStoredProcedure WHERE name = N'RollerPassport');
IF NOT EXISTS (SELECT 1 FROM dbo.iTableAlias WHERE storedProcedureID = @rp AND position = 4 AND name = N'firmbrand')
   OR NOT EXISTS (SELECT 1 FROM dbo.iTableAlias WHERE storedProcedureID = @rp AND position = 5 AND name = N'rolActionType')
   OR (SELECT COUNT(*) FROM dbo.iTableAlias WHERE storedProcedureID = @rp) <> 5
BEGIN
    RAISERROR(N'Остановлено: алиасы RollerPassport не такие, как ожидалось (1 rolType, 2 rolStyle, 3 firm, 4 firmbrand, 5 rolActionType).', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 1. Списки
-------------------------------------------------------------------------------------------------
DECLARE @ent TABLE(id INT PRIMARY KEY);
INSERT INTO @ent VALUES (15),(120),(121);      -- Брэнд, Фирма (Брэнды), Брэнды (привязанные к фирмам)

DECLARE @sp TABLE(id INT PRIMARY KEY);
INSERT INTO @sp
SELECT storedProcedureID FROM dbo.iStoredProcedure
WHERE name IN (N'Brands', N'BrandFirms', N'BrandIUD', N'BrandFilter', N'FirmBrandID', N'FirmBrands');

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 2. Паспорт ролика без набора «firmbrand»
-------------------------------------------------------------------------------------------------
EXEC sys.sp_executesql N'ALTER PROCEDURE [dbo].[RollerPassport] (
@RollerId int = null
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON	

-- 1. Roller type
SELECT rt.rolTypeID as id, rt.name, rt.isLoadable 
FROM iRolType rt
ORDER BY rt.name

-- 2. roller style — справочник «стиль ролика» удалён вместе с модулем
--    «Производство роликов»; паспорт ролика поле стиля не показывает.
--    Пустой набор нужной формы, чтобы не сдвигать позиции iTableAlias.
SELECT CAST(NULL AS smallint) as id, CAST(NULL AS nvarchar(64)) as name
WHERE 1 = 0

-- 3. firms
CREATE TABLE #Firm(firmID int)
INSERT INTO #Firm SELECT firmID FROM Firm
EXEC sl_Firms

-- 4. Roller ActionType
SELECT rat.rolActionTypeID AS id, rat.NAME AS name FROM dbo.iRollerActionType rat';

DELETE FROM dbo.iTableAlias WHERE storedProcedureID = @rp AND position = 4;
UPDATE dbo.iTableAlias SET position = 4 WHERE storedProcedureID = @rp AND position = 5;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 3. DROP программных объектов и таблиц (листья -> корень)
-------------------------------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS dbo.Brands;
DROP PROCEDURE IF EXISTS dbo.BrandFirms;
DROP PROCEDURE IF EXISTS dbo.BrandIUD;
DROP PROCEDURE IF EXISTS dbo.BrandFilter;
DROP PROCEDURE IF EXISTS dbo.FirmBrandID;
DROP PROCEDURE IF EXISTS dbo.FirmBrands;
DROP FUNCTION  IF EXISTS dbo.fn_BrandListByRollerId;

DROP TABLE IF EXISTS dbo.FirmBrand;
DROP TABLE IF EXISTS dbo.RollerBrand;
DROP TABLE IF EXISTS dbo.Brand;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 4. Очистка метаданных
-------------------------------------------------------------------------------------------------
DELETE FROM dbo.GroupRight        WHERE entityActionID IN (SELECT entityActionID FROM dbo.iEntityAction WHERE entityID IN (SELECT id FROM @ent));
DELETE FROM dbo.UserAdditionRight WHERE entityActionID IN (SELECT entityActionID FROM dbo.iEntityAction WHERE entityID IN (SELECT id FROM @ent));

-- Строка с entityID = 120 ссылается на FirmIUD (общая процедура сущности Firm) — удаляется по entityID, сама процедура остаётся.
DELETE FROM dbo.iModuleProcedure  WHERE entityID IN (SELECT id FROM @ent) OR storedProcedureID IN (SELECT id FROM @sp);
DELETE FROM dbo.iTableAlias       WHERE storedProcedureID IN (SELECT id FROM @sp);

DELETE FROM dbo.iEntityRelation   WHERE parentEntityID IN (SELECT id FROM @ent) OR childEntityID IN (SELECT id FROM @ent);
DELETE FROM dbo.iEntityAttribute  WHERE entityID IN (SELECT id FROM @ent);
DELETE FROM dbo.iEntityAction     WHERE entityID IN (SELECT id FROM @ent);
DELETE FROM dbo.iEntity           WHERE entityID IN (SELECT id FROM @ent);

DELETE FROM dbo.iStoredProcedure  WHERE storedProcedureID IN (SELECT id FROM @sp);

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 5. Проверки (должны быть все нули / OK)
-------------------------------------------------------------------------------------------------
SELECT leftover_objects = COUNT(*) FROM sys.objects
    WHERE name IN (N'Brand', N'FirmBrand', N'RollerBrand', N'fn_BrandListByRollerId', N'Brands', N'BrandFirms',
                   N'BrandIUD', N'BrandFilter', N'FirmBrandID', N'FirmBrands');
SELECT leftover_iEntity = COUNT(*) FROM dbo.iEntity WHERE entityID IN (SELECT id FROM @ent);
SELECT leftover_iEntityAction = COUNT(*) FROM dbo.iEntityAction WHERE entityID IN (SELECT id FROM @ent);
SELECT leftover_iEntityAttribute = COUNT(*) FROM dbo.iEntityAttribute WHERE entityID IN (SELECT id FROM @ent);
SELECT leftover_iModuleProcedure = COUNT(*) FROM dbo.iModuleProcedure WHERE entityID IN (SELECT id FROM @ent);
SELECT leftover_iStoredProcedure = COUNT(*) FROM dbo.iStoredProcedure WHERE name LIKE N'%Brand%';
SELECT orphan_iModuleProcedure = COUNT(*) FROM dbo.iModuleProcedure mp WHERE NOT EXISTS (SELECT 1 FROM dbo.iStoredProcedure sp WHERE sp.storedProcedureID = mp.storedProcedureID);
SELECT orphan_iTableAlias = COUNT(*) FROM dbo.iTableAlias ta WHERE NOT EXISTS (SELECT 1 FROM dbo.iStoredProcedure sp WHERE sp.storedProcedureID = ta.storedProcedureID);
SELECT orphan_GroupRight = COUNT(*) FROM dbo.GroupRight gr WHERE NOT EXISTS (SELECT 1 FROM dbo.iEntityAction ea WHERE ea.entityActionID = gr.entityActionID);
SELECT orphan_UserAdditionRight = COUNT(*) FROM dbo.UserAdditionRight ur WHERE NOT EXISTS (SELECT 1 FROM dbo.iEntityAction ea WHERE ea.entityActionID = ur.entityActionID);
-- алиасы паспорта ролика: 1 rolType, 2 rolStyle, 3 firm, 4 rolActionType
SELECT a.position, a.name FROM dbo.iTableAlias a WHERE a.storedProcedureID = @rp ORDER BY a.position;
-- паспорт ролика и метаданные старта должны отработать (у паспорта ровно 4 набора строк):
DECLARE @anyRoller INT = (SELECT TOP 1 rollerID FROM dbo.Roller);
EXEC dbo.RollerPassport @RollerId = @anyRoller;
EXEC dbo.ProcedureConfigurationRetrieve;

PRINT N'=== проверьте вывод выше. Для применения: заменить ROLLBACK на COMMIT ===';
ROLLBACK TRANSACTION;
-- COMMIT TRANSACTION;
