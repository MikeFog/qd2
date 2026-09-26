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
