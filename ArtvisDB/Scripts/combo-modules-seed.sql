-- Комбо-модули: схема и метаданные (пункт меню, сущности, действия, дерево).
--
-- Комбо-модуль - объединение нескольких модулей в одну сущность. Устроен как
-- пакетный модуль (PackModule), но БЕЗ уровня прайс-листа: сразу
-- ComboModule -> ComboModuleContent (модули, из которых он состоит).
--
-- Скрипт идемпотентен: приводит базу к нужному состоянию из любого прежнего,
-- повторный прогон безопасен. В конце выводит отчёт о проверке.
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i combo-modules-seed.sql
--
-- Таблицы и процедуры разворачиваются отдельно (файлы в ArtvisDB\dbo\...),
-- порядок деплоя - в docs/tasks/combo-modules-deploy.md

SET NOCOUNT ON;

DECLARE @entComboModule INT = 1270;
DECLARE @entComboModuleContent INT = 1271;
DECLARE @entComboModuleIssue INT = 1272;

-- Идентификаторы сущностей зашиты в C# (Merlin.Classes.Entities), поэтому
-- должны совпадать во всех базах. Если номера заняты чем-то другим - стоп.
IF EXISTS (SELECT 1 FROM [dbo].[iEntity]
           WHERE entityID IN (@entComboModule, @entComboModuleContent, @entComboModuleIssue)
             AND ISNULL(tableName, '') NOT IN ('ComboModule', 'ComboModuleContent', ''))
BEGIN
	RAISERROR('entityID 1270-1272 заняты другими сущностями - согласуйте новые номера с Merlin.Classes.Entities', 16, 1);
	RETURN;
END

-------------------------------------------------------------------------------
-- 1. Сущности
-------------------------------------------------------------------------------

DECLARE @passportComboModule NVARCHAR(MAX) = N'<passport>
	<page caption="Общие">
		<field caption="Название:" name="name" />
	</page>
</passport>';

DECLARE @passportComboModuleContent NVARCHAR(MAX) = N'<passport>
	<page caption="Общие">
		<lookup caption="Радиостанция:" source="massmedia" name="massmediaID"/>
		<lookup caption="Модуль:" source="modules" columnWithID = "moduleId" name="moduleID" entity="module" parentLookupName="massmediaID" filter="massmediaID={0}"/>
	</page>
</passport>';

SET IDENTITY_INSERT [dbo].[iEntity] ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntity] WHERE entityID = @entComboModule)
	INSERT INTO [dbo].[iEntity]
		(entityID, name, passport, tableName, className, assemblyName, pkColumn, codeName, isGrantingAllowed, iconName, isObsolete)
	VALUES
		(@entComboModule, N'Комбо-модуль', @passportComboModule, 'ComboModule',
		 'FogSoft.WinForm.Classes.ObjectContainer', NULL, 'comboModuleID', 'comboModule', 1, 'PackModule.png', 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntity] WHERE entityID = @entComboModuleContent)
	INSERT INTO [dbo].[iEntity]
		(entityID, name, passport, tableName, className, assemblyName, pkColumn, codeName, isGrantingAllowed, iconName, isObsolete)
	VALUES
		(@entComboModuleContent, N'Модули комбо-модуля', @passportComboModuleContent, 'ComboModuleContent',
		 'FogSoft.WinForm.Classes.PresentationObject', NULL, 'comboModuleContentID', 'ComboModuleContent', 1, 'module.png', 0);

-- Выпуски, добавленные в форме размещения. Отдельная сущность, а не 130
-- «Выпуск модуля»: у той свой набор колонок без даты и радиостанции, и трогать
-- её нельзя - она используется в кампаниях модульного размещения. Тот же приём,
-- что и у веерного размещения с его сущностью 226.
IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntity] WHERE entityID = @entComboModuleIssue)
	INSERT INTO [dbo].[iEntity]
		(entityID, name, passport, tableName, className, assemblyName, pkColumn, codeName, isGrantingAllowed, iconName, isObsolete)
	VALUES
		(@entComboModuleIssue, N'Размещение комбо-модулями: выпуски', NULL, NULL,
		 'Merlin.Classes.ModuleIssue', 'Merlin', 'moduleIssueID', NULL, 1, NULL, 0);

SET IDENTITY_INSERT [dbo].[iEntity] OFF;

-- паспорта обновляем всегда - это единственное место их хранения
UPDATE [dbo].[iEntity] SET passport = @passportComboModule
WHERE entityID = @entComboModule AND (passport IS NULL OR CAST(passport AS NVARCHAR(MAX)) <> @passportComboModule);

UPDATE [dbo].[iEntity] SET passport = @passportComboModuleContent
WHERE entityID = @entComboModuleContent AND (passport IS NULL OR CAST(passport AS NVARCHAR(MAX)) <> @passportComboModuleContent);

-------------------------------------------------------------------------------
-- 2. Колонки списков (iEntityAttribute)
-------------------------------------------------------------------------------

MERGE [dbo].[iEntityAttribute] AS t
USING (VALUES
	(@entComboModule,        N'Название',      'name',          1,  0),
	(@entComboModuleContent, N'Радиостанция',  'massmediaName', 1,  0),
	(@entComboModuleContent, N'Группа',        'groupName',     2,  0),
	(@entComboModuleContent, N'Модуль',        'moduleName',    5,  0),
	(@entComboModuleIssue,   N'Дата',          'issueDate',     10, 0),
	(@entComboModuleIssue,   N'Радиостанция',  'massmediaName', 20, 0),
	(@entComboModuleIssue,   N'Модуль',        'moduleName',    30, 0),
	(@entComboModuleIssue,   N'Ролик',         'rollerName',    40, 0),
	(@entComboModuleIssue,   N'Порядок',       'issuePosition', 50, 0)
) AS s(entityID, alias, name, ordinal_position, selector)
	ON t.entityID = s.entityID AND t.alias = s.alias AND t.selector = s.selector
WHEN MATCHED AND (t.name <> s.name OR t.ordinal_position <> s.ordinal_position) THEN
	UPDATE SET name = s.name, ordinal_position = s.ordinal_position
WHEN NOT MATCHED BY TARGET THEN
	INSERT (entityID, alias, name, ordinal_position, selector)
	VALUES (s.entityID, s.alias, s.name, s.ordinal_position, s.selector);

-------------------------------------------------------------------------------
-- 3. Действия (iEntityAction)
-------------------------------------------------------------------------------

DECLARE @actions TABLE (
	entityID INT, alias NVARCHAR(128), name VARCHAR(64),
	ordinal_position SMALLINT, isHidden BIT, isGrantingAllowed BIT, imgResourceName VARCHAR(50));

INSERT INTO @actions VALUES
	(@entComboModule,        N'Обновить',       'RefreshItem', 1,  0, 0, 'Icons.RefreshItem.png'),
	(@entComboModule,        N'Удалить',        'DeleteItem',  5,  0, 1, 'Icons.DeleteItem.png'),
	(@entComboModule,        N'-',              NULL,          10, 0, 0, NULL),
	(@entComboModule,        N'Добавить модуль','AssignNew',   20, 0, 1, NULL),
	(@entComboModule,        N'-',              NULL,          30, 0, 0, NULL),
	(@entComboModule,        N'Свойства',       'Properties',  40, 0, 1, 'Icons.Properties.png'),
	(@entComboModuleContent, N'Обновить',       'RefreshItem', 1,  0, 0, 'Icons.RefreshItem.png'),
	(@entComboModuleContent, N'Удалить',        'DeleteItem',  5,  0, 1, 'Icons.DeleteItem.png'),
	(@entComboModuleContent, N'-',              NULL,          10, 0, 0, NULL),
	(@entComboModuleContent, N'Свойства',       'Properties',  15, 0, 1, 'Icons.Properties.png'),
	(@entComboModuleIssue,   N'Удалить',        'DeleteItem',  10, 0, 1, 'Icons.DeleteItem.png');

INSERT INTO [dbo].[iEntityAction] (entityID, alias, name, ordinal_position, isHidden, isGrantingAllowed, imgResourceName)
SELECT a.entityID, a.alias, a.name, a.ordinal_position, a.isHidden, a.isGrantingAllowed, a.imgResourceName
FROM @actions a
WHERE NOT EXISTS (
	SELECT 1 FROM [dbo].[iEntityAction] ea
	WHERE ea.entityID = a.entityID AND ea.ordinal_position = a.ordinal_position);

-------------------------------------------------------------------------------
-- 4. Привязка хранимых процедур (iStoredProcedure + iModuleProcedure)
--    moduleID: 0 - Fake Module (IUD), 118 - Simple Journal, 119 - Properties Page
-------------------------------------------------------------------------------

DECLARE @procs TABLE (
	procName VARCHAR(128), procedureType VARCHAR(32), isTransactionRequired BIT,
	entityID INT, moduleID INT, actionName VARCHAR(64));

INSERT INTO @procs VALUES
	('ComboModuleIUD',              'NO_DATA',   1, @entComboModule,        0,   'AddItem'),
	('ComboModuleIUD',              'NO_DATA',   1, @entComboModule,        0,   'DeleteItem'),
	('ComboModuleIUD',              'NO_DATA',   1, @entComboModule,        0,   'UpdateItem'),
	('ComboModules',                'RECORDSET', 0, @entComboModule,        118, 'Load'),
	('ComboModuleContentIUD',       'RECORDSET', 1, @entComboModuleContent, 0,   'AddItem'),
	('ComboModuleContentIUD',       'RECORDSET', 1, @entComboModuleContent, 0,   'DeleteItem'),
	('ComboModuleContentIUD',       'RECORDSET', 1, @entComboModuleContent, 0,   'UpdateItem'),
	('ComboModuleContentRetrieve',  'RECORDSET', 0, @entComboModuleContent, 118, 'Load'),
	('ComboModuleContentPassport',  'RECORDSET', 0, @entComboModuleContent, 119, 'Load'),
	-- удаление выпуска идёт штатной ModuleIssueIUD, список формы грузится
	-- ComboModuleIssuesRetrieve явным вызовом, поэтому Load здесь не нужен
	('ModuleIssueIUD',              'RECORDSET', 1, @entComboModuleIssue,   0,   'DeleteItem');

INSERT INTO [dbo].[iStoredProcedure] (name, procedureType, isTransactionRequired)
SELECT DISTINCT p.procName, p.procedureType, p.isTransactionRequired
FROM @procs p
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[iStoredProcedure] sp WHERE sp.name = p.procName);

INSERT INTO [dbo].[iModuleProcedure] (storedProcedureID, entityID, moduleID, actionNameID, connectionTimeout)
SELECT sp.storedProcedureID, p.entityID, p.moduleID, an.actionNameID, 60
FROM @procs p
	INNER JOIN [dbo].[iStoredProcedure] sp ON sp.name = p.procName
	INNER JOIN [dbo].[iActionName] an ON an.name = p.actionName
WHERE NOT EXISTS (
	SELECT 1 FROM [dbo].[iModuleProcedure] mp
	WHERE mp.entityID = p.entityID AND mp.moduleID = p.moduleID AND mp.actionNameID = an.actionNameID);

-- Результирующие наборы паспорта: 1 - радиостанции, 2 - модули
DECLARE @spContentPassport INT =
	(SELECT storedProcedureID FROM [dbo].[iStoredProcedure] WHERE name = 'ComboModuleContentPassport');

MERGE [dbo].[iTableAlias] AS t
USING (VALUES (@spContentPassport, 1, 'massmedia'), (@spContentPassport, 2, 'modules'))
	AS s(storedProcedureID, position, name)
	ON t.storedProcedureID = s.storedProcedureID AND t.position = s.position
WHEN MATCHED AND t.name <> s.name THEN UPDATE SET name = s.name
WHEN NOT MATCHED BY TARGET THEN
	INSERT (storedProcedureID, position, name) VALUES (s.storedProcedureID, s.position, s.name);

-------------------------------------------------------------------------------
-- 5. Сценарий дерева (комбо-модуль -> его модули)
-------------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM [dbo].[iRelationScenario] WHERE name = 'Combo Modules')
	INSERT INTO [dbo].[iRelationScenario] (name, startingEntityID, filter)
	VALUES ('Combo Modules', @entComboModule, NULL);

DECLARE @scenario INT = (SELECT relationScenarioID FROM [dbo].[iRelationScenario] WHERE name = 'Combo Modules');

IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntityRelation]
               WHERE relationScenarioID = @scenario
                 AND parentEntityID = @entComboModule AND childEntityID = @entComboModuleContent)
	INSERT INTO [dbo].[iEntityRelation]
		(relationScenarioID, parentEntityID, childEntityID, selector, isChildNodeExpandable)
	VALUES (@scenario, @entComboModule, @entComboModuleContent, 0, 0);

-------------------------------------------------------------------------------
-- 6. Пункт меню (Администрация -> Комбо-модули, сразу за Пакетными модулями)
-------------------------------------------------------------------------------

DECLARE @menuPackModules SMALLINT = (SELECT menuID FROM [dbo].[iMenu] WHERE codeName = 'miPackModules');
DECLARE @menuParent SMALLINT = (SELECT parentID FROM [dbo].[iMenu] WHERE codeName = 'miPackModules');

IF NOT EXISTS (SELECT 1 FROM [dbo].[iMenu] WHERE codeName = 'miComboModules')
	INSERT INTO [dbo].[iMenu] (name, parentID, position, codeName, align, isPublic, isObsolete)
	VALUES (N'Комбо-модули', @menuParent, 23, 'miComboModules', 'Left', 0, 0);

DECLARE @menuComboModules SMALLINT = (SELECT menuID FROM [dbo].[iMenu] WHERE codeName = 'miComboModules');

-- права на пункт меню - тем же группам, у кого есть Пакетные модули
INSERT INTO [dbo].[GroupMenu] (groupID, menuID)
SELECT gm.groupID, @menuComboModules
FROM [dbo].[GroupMenu] gm
WHERE gm.menuID = @menuPackModules
	AND NOT EXISTS (SELECT 1 FROM [dbo].[GroupMenu] x WHERE x.groupID = gm.groupID AND x.menuID = @menuComboModules);

-- права на действия - тем же группам, у кого есть права на действия пакетного модуля
INSERT INTO [dbo].[GroupRight] (groupID, entityActionID)
SELECT DISTINCT gr.groupID, eaNew.entityActionID
FROM [dbo].[GroupRight] gr
	INNER JOIN [dbo].[iEntityAction] eaOld ON eaOld.entityActionID = gr.entityActionID AND eaOld.entityID IN (133, 135)
	INNER JOIN [dbo].[iEntityAction] eaNew ON eaNew.entityID IN (@entComboModule, @entComboModuleContent, @entComboModuleIssue)
		AND eaNew.name = eaOld.name
WHERE NOT EXISTS (
	SELECT 1 FROM [dbo].[GroupRight] x WHERE x.groupID = gr.groupID AND x.entityActionID = eaNew.entityActionID);

-------------------------------------------------------------------------------
-- 7. Сообщение об ошибке
-------------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'ComboModuleContentDuplicate')
	INSERT INTO [dbo].[iMessage] (name, message)
	VALUES ('ComboModuleContentDuplicate', N'Этот модуль уже входит в состав комбо-модуля. Операция прервана.');

-------------------------------------------------------------------------------
-- Отчёт
-------------------------------------------------------------------------------

PRINT '--- Комбо-модули: состояние метаданных ---';

SELECT 'iEntity' AS [объект], COUNT(*) AS [строк], '3' AS [ожидается]
FROM [dbo].[iEntity] WHERE entityID IN (@entComboModule, @entComboModuleContent, @entComboModuleIssue)
UNION ALL SELECT 'iEntityAttribute', COUNT(*), '9'
FROM [dbo].[iEntityAttribute] WHERE entityID IN (@entComboModule, @entComboModuleContent, @entComboModuleIssue)
UNION ALL SELECT 'iEntityAction', COUNT(*), '11'
FROM [dbo].[iEntityAction] WHERE entityID IN (@entComboModule, @entComboModuleContent, @entComboModuleIssue)
UNION ALL SELECT 'iModuleProcedure', COUNT(*), '10'
FROM [dbo].[iModuleProcedure] WHERE entityID IN (@entComboModule, @entComboModuleContent, @entComboModuleIssue)
UNION ALL SELECT 'iTableAlias', COUNT(*), '2'
FROM [dbo].[iTableAlias] WHERE storedProcedureID = @spContentPassport
UNION ALL SELECT 'iEntityRelation', COUNT(*), '1'
FROM [dbo].[iEntityRelation] WHERE relationScenarioID = @scenario
UNION ALL SELECT 'iMenu', COUNT(*), '1'
FROM [dbo].[iMenu] WHERE codeName = 'miComboModules'
UNION ALL SELECT 'GroupMenu', COUNT(*), N'как у miPackModules'
FROM [dbo].[GroupMenu] WHERE menuID = @menuComboModules
UNION ALL SELECT 'GroupRight', COUNT(*), N'как у пакетных модулей'
FROM [dbo].[GroupRight] gr
	INNER JOIN [dbo].[iEntityAction] ea ON ea.entityActionID = gr.entityActionID
WHERE ea.entityID IN (@entComboModule, @entComboModuleContent, @entComboModuleIssue);
