/*
    ПРОД-ДЕПЛОЙ 09_metadata-seed.sql
    Метаданные: «График размещения по нескольким акциям» (меню + фильтр акций), фильтры и паспорта веба.
    КОГДА: СТРОГО ПОСЛЕ 08 (фильтр по № акции безопасен только с новым Actions1); после наката перезапустить qd2 и веб.

    Склеено из ArtvisDB/Scripts (части ниже — без изменений, каждая со своей шапкой):
      - multi-action-media-plan-select-seed.sql
      - web-filters-roller-statistic-print-grid-seed.sql
      - web-grid-export-seed.sql
    Запуск: sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 09_metadata-seed.sql
    (-b — остановка на первой ошибке; части идемпотентны, повторный запуск безопасен)
*/

-- ============================================================================
-- ЧАСТЬ: multi-action-media-plan-select-seed.sql
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
-- Сводный медиаплан с выбором акций из списка
-- («Рекламный отдел → График размещения по нескольким акциям»).
--
-- 1. Фильтр сущности «Рекламная акция» (entityID 77). До этого у сущности
--    фильтра не было. Взята первая закладка («Общие») фильтра сценария
--    ConfirmedAction (журнал подтверждённых акций). Поле «№ рекламной акции»
--    безопасно только вместе с правкой Actions1 (отбор по @userID в ветке
--    @actionID) — сначала actions1-actionid-userid-deploy.sql.
-- 2. Списки для фильтра (типы оплаты/кампании, группы станций, менеджеры) —
--    процедура ActionsFilter, уже привязанная к сущности 1255 как FilterPage;
--    здесь привязывается и к сущности 77. Алиасы таблиц (iTableAlias)
--    висят на самой процедуре, их добавлять не нужно.
-- 3. Пункт меню в ветке «Рекламный отдел». Диспетчеризация — MDIForm.cs по
--    codeName 'miMultiActionMediaPlanSelect'. Старый пункт «Трафик → …»
--    (miMultiActionMediaPlan, ввод номеров) остаётся как есть.
--
-- Права на пункт (GroupMenu) НЕ раздаются намеренно — их назначают
-- администраторы штатными средствами.
--
-- Метаданные клиент читает при входе — после прогона перезапустить qd2.
-- Скрипт идемпотентен, повторный прогон безопасен.
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i multi-action-media-plan-select-seed.sql

SET NOCOUNT ON;

DECLARE @entityAction INT = 77;
DECLARE @moduleFilterPage INT = 2;   -- InterfaceObjects.FilterPage
DECLARE @actionLoad INT = 4;         -- Constants.Actions.Load

-------------------------------------------------------------------------------
-- 1. Фильтр сущности «Рекламная акция»
-------------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntity] WHERE entityID = @entityAction)
BEGIN
    RAISERROR('Сущность 77 («Рекламная акция») не найдена — проверьте базу', 16, 1);
    RETURN;
END

UPDATE [dbo].[iEntity]
SET filter = N'<filter>
  <page caption="Общие">
    <field caption="Начало интервала: " name="startOfInterval" type="df_date" value="StartOfTheMonth"/>
    <field caption="Окончание интервала: " name="endOfInterval" type="df_date"/>
    <field caption="№ Рекламной акции: " name="actionID" type="int" min="1"/>
    <objectPicker caption="Фирма-заказчик:" name="firmID2" entity="Firm"/>
    <objectPicker caption="Группа компаний:" name="headCompanyID" entity="HeadCompany"/>
    <separator/>
    <objectPicker caption="Менеджер:" name="userID" value="LoggedUser" source="managers" entity="user" />
    <objectPicker caption="Агентство:" name="agencyID" entity="agency">
      <filter name="ShowActive" type="boolean" value="true" />
    </objectPicker>
    <objectPicker caption="Радиостанция:" name="massmediaID" entity="massmedia"/>
    <lookup caption="Группа: " name="massmediaGroupID" source="manager_group" columnWithID="massmediaGroupID"/>
    <separator/>
    <field caption="Показывать с оплатой" name="showWhite" type="boolean" value="true"/>
    <field caption="Показывать без оплаты" name="showBlack" type="boolean" value="true"/>
    <lookup caption="Тип кампании: " name="campaignTypeID" source="campaign_type" columnWithID="campaignTypeID"/>
    <lookup caption="Тип оплаты: " name="paymentTypeID" source="payment_type" columnWithID="paymentTypeID"/>
  </page>
</filter>'
WHERE entityID = @entityAction;

-------------------------------------------------------------------------------
-- 2. ActionsFilter — источник списков фильтра для сущности 77
-------------------------------------------------------------------------------

DECLARE @spActionsFilter INT =
    (SELECT storedProcedureID FROM [dbo].[iStoredProcedure] WHERE name = 'ActionsFilter');

IF @spActionsFilter IS NULL
BEGIN
    RAISERROR('Процедура ActionsFilter не зарегистрирована в iStoredProcedure — проверьте базу', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[iModuleProcedure]
               WHERE entityID = @entityAction AND moduleID = @moduleFilterPage AND actionNameID = @actionLoad)
    INSERT INTO [dbo].[iModuleProcedure] (storedProcedureID, entityID, moduleID, actionNameID, connectionTimeout)
    VALUES (@spActionsFilter, @entityAction, @moduleFilterPage, @actionLoad, 60);

-------------------------------------------------------------------------------
-- 3. Пункт меню «Рекламный отдел → График размещения по нескольким акциям»
-------------------------------------------------------------------------------

DECLARE @menuAdvertParent SMALLINT =
    (SELECT parentID FROM [dbo].[iMenu] WHERE codeName = 'miActionJournal');

IF @menuAdvertParent IS NULL
BEGIN
    RAISERROR('Ветка меню «Рекламный отдел» (miActionJournal) не найдена — проверьте базу', 16, 1);
    RETURN;
END

-- Позиция 38 — после «Журнала использования бонусов» (37), в блоке печати/отчётов.
IF NOT EXISTS (SELECT 1 FROM [dbo].[iMenu] WHERE codeName = 'miMultiActionMediaPlanSelect')
    INSERT INTO [dbo].[iMenu] (name, parentID, position, codeName, align, isPublic, isObsolete)
    VALUES (N'График размещения по нескольким акциям', @menuAdvertParent, 38,
            'miMultiActionMediaPlanSelect', 'Left', 0, 0);

-------------------------------------------------------------------------------
-- Отчёт
-------------------------------------------------------------------------------

PRINT '--- Выбор акций для сводного медиаплана: состояние метаданных ---';

SELECT 'iEntity.filter' AS [объект],
       CASE WHEN filter LIKE N'%name="userID"%' AND filter LIKE N'%name="actionID"%' THEN 1 ELSE 0 END AS [строк],
       '1' AS [ожидается]
FROM [dbo].[iEntity] WHERE entityID = @entityAction
UNION ALL
SELECT 'iModuleProcedure', COUNT(*), '1'
FROM [dbo].[iModuleProcedure]
WHERE entityID = @entityAction AND moduleID = @moduleFilterPage AND actionNameID = @actionLoad
    AND storedProcedureID = @spActionsFilter
UNION ALL
SELECT 'iMenu', COUNT(*), '1'
FROM [dbo].[iMenu] WHERE codeName = 'miMultiActionMediaPlanSelect';
GO

-- ============================================================================
-- ЧАСТЬ: web-filters-roller-statistic-print-grid-seed.sql
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
-- Веб: отбор «Журнала использования роликов» и «Сетки вещания» — метаданными, как у журналов.
--
-- До этого оба веб-экрана рисовали отбор своей разметкой (как десктопные формы
-- RollerStatisticForm и FrmGridReport). Теперь — общей панелью отбора (FilterPanel) по XML:
--   1. iEntity.filter сущности 139 «Использование роликов» — был пуст; десктоп его не читает
--      (RollerStatisticForm строит отбор сама), так что для десктопа ничего не меняется.
--      Имена полей = параметры stat_RollerStatistic. «Радиостанции» — selector с multiselect:
--      в веб-фильтре это выбор нескольких станций, значение «id,id,» (massmediaString).
--   2. iPassport «BroadcastGridFilter» — именованный отбор сетки вещания (своей сущности у неё
--      нет). Имена полей = параметры rpt_Grid_v3. mandatory="true" — поле отбирает всегда.
--
-- Идемпотентен: повторный прогон перезаписывает XML тем же. После наката — перезапуск веба
-- (метаданные кэшируются).
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i web-filters-roller-statistic-print-grid-seed.sql

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntity] WHERE entityID = 139 AND codeName = 'rollerStatistic')
BEGIN
	RAISERROR(N'Сущность 139 (rollerStatistic) не найдена - согласуйте с Merlin.Entities.RollerStatistic', 16, 1);
	RETURN;
END

BEGIN TRANSACTION;

UPDATE [dbo].[iEntity]
SET filter = N'<filter>
	<page caption="Общие">
		<field caption="Начало интервала: " name="startDate" type="df_date" value="TODAY"/>
		<field caption="Окончание интервала: " name="finishDate" type="df_date" value="TODAY"/>
		<selector caption="Радиостанции: " name="massmediaString" entity="massmedia" multiselect="true" mandatory="true"/>
		<objectPicker caption="Менеджер: " name="userID" entity="user"/>
		<objectPicker caption="Фирма-заказчик: " name="firmID" entity="Firm"/>
		<objectPicker caption="Группа компаний: " name="headCompanyID" entity="HeadCompany"/>
		<objectPicker caption="Предмет рекламы:" name="advertTypeID" entity="adverttype" isCreateNewAllowed="false" relationScenario="Предметы рекламы"/>
		<field caption="Показывать с оплатой" name="showWhite" type="boolean" value="true"/>
		<field caption="Показывать без оплаты" name="showBlack" type="boolean" value="true"/>
	</page>
	<page caption="Разбивка">
		<field caption="С разбивкой по менеджерам" name="splitByManager" type="boolean" value="false"/>
		<field caption="С разбивкой по дням" name="splitByDays" type="boolean" value="false"/>
	</page>
</filter>'
WHERE entityID = 139;

DECLARE @grid VARCHAR(4000) = '<filter>
	<page caption="Общие">
		<objectPicker caption="Радиостанция: " name="massmediaID" entity="massmedia" mandatory="true"/>
		<field caption="Дата: " name="theDate" type="df_date" value="TODAY" mandatory="true"/>
		<objectPicker caption="Менеджер: " name="userID" entity="user"/>
	</page>
</filter>';

IF EXISTS (SELECT 1 FROM [dbo].[iPassport] WHERE codeName = 'BroadcastGridFilter')
	UPDATE [dbo].[iPassport] SET passport = @grid WHERE codeName = 'BroadcastGridFilter';
ELSE
	INSERT INTO [dbo].[iPassport] (codeName, passport) VALUES ('BroadcastGridFilter', @grid);

COMMIT TRANSACTION;

PRINT N'Отбор сущности 139 и iPassport BroadcastGridFilter записаны.';
GO

-- ============================================================================
-- ЧАСТЬ: web-grid-export-seed.sql
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
-- Веб: параметры «Экспорта сеток вещания» — именованным паспортом, как отбор своих экранов
-- (правило docs/AI_AGENT_PLAYBOOK.md, «Отбор своего веб-экрана — тоже метаданными»).
--
-- iPassport «BroadcastGridExport»: станции (выбор нескольких, значение «id,id,»), дата, что
-- выгружать — файлы для эфира (DJin) и сетки в Word. Десктоп паспорт не читает (ExportGridForm
-- строит форму сама).
--
-- Идемпотентен. После наката — перезапуск веба (метаданные кэшируются).
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i web-grid-export-seed.sql

SET NOCOUNT ON;

DECLARE @passport VARCHAR(4000) = '<filter>
	<page caption="Общие">
		<selector caption="Радиостанции: " name="massmediaString" entity="massmedia" multiselect="true" mandatory="true"/>
		<field caption="Дата: " name="theDate" type="df_date" value="TODAY" mandatory="true"/>
		<field caption="Файлы для эфира (DJin)" name="exportDJin" type="boolean" value="true"/>
		<field caption="Сетки в Word" name="exportWord" type="boolean" value="true"/>
	</page>
</filter>';

IF EXISTS (SELECT 1 FROM [dbo].[iPassport] WHERE codeName = 'BroadcastGridExport')
	UPDATE [dbo].[iPassport] SET passport = @passport WHERE codeName = 'BroadcastGridExport';
ELSE
	INSERT INTO [dbo].[iPassport] (codeName, passport) VALUES ('BroadcastGridExport', @passport);

PRINT N'iPassport BroadcastGridExport записан.';
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
