-- Реестр счетов: «Рекламный отдел → Реестр счетов» (сущность 1272 BillRegister), десктоп и веб.
-- Одна строка — один счёт (пара «акция + агентство» из Bill). Колонки: «Статус» (светофор), Дата счёта, № счёта,
-- № Акции, Активирована (галочка: акция активирована, у макета пусто), Дата начала акции, Фирма-заказчик, Сумма (с НДС, как в печатном счёте),
-- Агентство, Менеджер, Оплата (Не оплачен / Частично / Оплачен).
-- Отбор: период «Есть выпуски с / по» (пересечение Action.startDate..finishDate с выбранным; по умолчанию с 1-го числа прошлого
-- месяца, конец не задан), менеджер (по умолчанию вошедший; без права смотреть чужие акции поле заблокировано клиентом,
-- процедура сама подставляет только его), агентство, статус оплаты, «Показывать активированные» (вкл.), «Показывать
-- макеты» (выкл.), «Не показывать нулевые счета» (вкл.). Удалённые акции не показываются.
-- Светофор («Статус»): только активированные акции с оплатой не «Оплачен» — бордовый кружок, если акция уже в эфире;
-- оранжевый, если до выхода в эфир осталось не больше N РАБОЧИХ дней (суббота и воскресенье не считаются; N — настройка
-- BillUnpaidWarnDays, по умолчанию 2; её поле в «Настройках...» Протектора добавляет Deploy/81). Остальным — пусто.
-- Решения владельца 26.10.2026: пункт — в «Рекламном отделе» рядом с «Журналом оплат» (позиция 22), а не в «Бухгалтерии»;
-- светофор колонкой, а не заливкой строки; зелёных кружков нет.
--
-- Что делает скрипт (идемпотентен, сразу COMMIT-ится — транзакции нет, каждый шаг сам проверяет «уже есть»):
--   1. CREATE OR ALTER функций fn_AddWorkDays (дата ± N рабочих дней) и f_SysParamsBillUnpaidWarnDays (настройка N).
--   2. CREATE OR ALTER процедур BillRegister (данные) и BillRegisterFilter (списки фильтра).
--   3. Метаданные: iEntity 1272 (класс строки Merlin.Classes.BillRegisterRow), iEntityAction «Открыть журнал акций» (ПКМ) (SET IDENTITY_INSERT — id одинаковый на всех базах; на ArtvisDev, Artvis, Tumen он свободен),
--      iEntityAttribute (11 колонок, «Статус» — dataType 'signal'), iStoredProcedure ×2, iTableAlias (managers,
--      bill_payment_status), iModuleProcedure (Simple Journal Load и FilterPage Load).
--   4. Пункт меню miBillRegister (iMenu, позиция 22) и права: те же группы и личные разрешения, что у «Журнала оплат»
--      (miPaymentFRS) — то есть администраторы и менеджеры, не бухгалтерия.
--   5. Испанский для веба (iTranslation): подписи колонок, фильтра, статусов, подсказок, пункта меню (iMenu.name_es).
-- Клиент: нужен новый Merlin.exe (ветка miBillRegister в MDIForm, колонка-светофор в SmartGrid) и новая сборка веба
-- (маршрут в MenuRoutes, колонка-светофор в ObjectList, действие «Открыть журнал акций»): накатывать вместе с ними или после —
-- старый клиент не знает класс BillRegisterRow, и журнал в нём не откроется. После наката — перезапустить qd2 и веб (метаданные читаются при старте).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 80_bill-register.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

IF OBJECT_ID(N'dbo.fn_Translate') IS NULL OR OBJECT_ID(N'dbo.fn_GetMassmediasForUser') IS NULL
BEGIN
    RAISERROR(N'80: сначала Deploy/08 (fn_Translate). Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO

-- =============================================
-- Дата, сдвинутая на @days рабочих дней (отрицательное число — назад). Рабочие дни — понедельник–пятница;
-- праздничного календаря в системе нет, поэтому выходные — только суббота и воскресенье (появится таблица
-- праздников — учитывать здесь). Как WORKDAY в Excel: от выходного дня назад первый рабочий — пятница.
-- День недели считается от понедельника 01.01.1900, а не DATEPART(weekday), чтобы не зависеть от SET DATEFIRST.
-- Читают: BillRegister («Реестр счетов», светофор по акциям, не оплаченным к выходу в эфир).
-- =============================================
CREATE OR ALTER FUNCTION [dbo].[fn_AddWorkDays]
(
	@date date,
	@days int
)
RETURNS date
AS
BEGIN
	DECLARE @step int = CASE WHEN @days < 0 THEN -1 ELSE 1 END,
		@left int = CASE WHEN ABS(@days) > 366 THEN 366 ELSE ABS(@days) END

	WHILE @left > 0
	BEGIN
		SET @date = DATEADD(day, @step, @date)
		IF DATEDIFF(day, '19000101', @date) % 7 < 5 -- 0 = пн ... 4 = пт
			SET @left = @left - 1
	END

	RETURN @date
END

GO

-- =============================================
-- Настройка «Системные настройки» (iInternalVariable, 'BillUnpaidWarnDays'): за сколько РАБОЧИХ дней до
-- первого дня акции «Реестр счетов» начинает показывать оранжевый кружок у активированной, но не оплаченной акции.
-- Не задана — 2. Читает BillRegister; поле в паспорте сущности 214 («Настройки...» Протектора) — Deploy/81.
-- =============================================
CREATE OR ALTER FUNCTION [dbo].[f_SysParamsBillUnpaidWarnDays]
(
)
RETURNS int
AS
BEGIN
	RETURN COALESCE((SELECT TOP 1 CAST([value] AS int) FROM iInternalVariable WHERE [name] = 'BillUnpaidWarnDays'), 2)
END

GO

-- =============================================================================
-- Реестр счетов (Рекламный отдел → Реестр счетов, сущность 1272 BillRegister).
-- Одна строка — один счёт: пара «акция + агентство» из Bill.
--
-- Сумма счёта — как в печатной форме (rpt_GenericBill): finalPrice кампаний акции у этого агентства
-- по «белым» видам оплаты (PaymentType.isHidden = 0). НДС входит в сумму («в том числе»), отдельно его
-- не считаем и не вычитаем.
-- Оплачено — PaymentAction по акции из платежей ЭТОГО агентства, тоже «белых» видов оплаты (агентство
-- берётся из платежа, как в журнале оплат: платежи привязаны к акции, а не к счёту; счёт на пару
-- «акция + агентство» один, поэтому однозначно).
-- Статус оплаты: 1 — не оплачен (оплачено 0), 2 — частично, 3 — оплачен (оплачено >= суммы; нулевой
-- счёт и переплата тоже «оплачен» — платить нечего).
--
-- Период (@startOfInterval, @endOfInterval) — по ЭФИРУ акции: акция попадает, если её период
-- Action.startDate..finishDate пересекается с выбранным (граница не задана — не ограничивает).
-- Не по дате счёта (решение заказчика, 26.10.2026).
-- @showActivated / @showUnconfirmed — активированные акции / макеты (по умолчанию только активированные);
-- @hideZero — не показывать счета с нулевой суммой (по умолчанию да).
--
-- Колонка-светофор paymentSignal (iEntityAttribute.dataType = 'signal', см. SignalColumn): только для АКТИВИРОВАННЫХ
-- акций, оплата которых не «Оплачен» (не оплачен или частично):
--   bordeaux — акция уже в эфире (Action.startDate <= сегодня);
--   orange   — до выхода в эфир осталось не больше N РАБОЧИХ дней (суббота и воскресенье не считаются;
--              N — настройка BillUnpaidWarnDays, по умолчанию 2, Протектор → «Настройки...»).
-- Остальным — NULL (кружка нет). Подсказка к кружку — paymentSignalHint. Сегодня — по дате сервера.
--
-- Видимость акций — как в PaymentCommonActions/ActionsForPaymentCommon: свои, чужие по праву
-- fn_IsRightToViewForeignActions, группы по fn_IsRightToViewGroupActions, станции — по fn_GetMassmediasForUser.
-- Пользователь без права смотреть чужие акции всегда получает только свои: @managerID подменяется
-- на @loggedUserID, даже если клиент прислал пусто (фильтр «Менеджер» в клиенте для него заблокирован).
-- Удалённые акции (Action.deleteDate) в реестр не попадают.
--
-- Служебные колонки результата (в iEntityAttribute их нет, в таблице не видны): agencyID (ключ), paymentSignalHint.
-- isConfirmed — колонка «Активирована» (галочка, dataType 'bit', как в журнале акций); по ней же ПКМ выбирает журнал акций
-- (подтверждённых или макетов).
-- =============================================================================
CREATE OR ALTER PROCEDURE [dbo].[BillRegister]
(
@startOfInterval datetime = null,
@endOfInterval datetime = null,
@agencyID smallint = null,
@managerID smallint = null,
@paymentStatusID tinyint = null, -- 1 не оплачен, 2 частично, 3 оплачен; NULL — все
@showActivated bit = 1,
@showUnconfirmed bit = 0,
@hideZero bit = 1,
@loggedUserID smallint,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DECLARE @tNotPaid NVARCHAR(50) = dbo.fn_Translate(@languageCode, N'Не оплачен'),
	@tPartial NVARCHAR(50) = dbo.fn_Translate(@languageCode, N'Частично'),
	@tPaid NVARCHAR(50) = dbo.fn_Translate(@languageCode, N'Оплачен'),
	@tOnAir NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Акция уже в эфире с {0}; оплата: {1}'),
	@tSoon NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Акция выходит в эфир {0}; оплата: {1}');

DECLARE @today date = CAST(GETDATE() AS date),
	@warnDays int = dbo.f_SysParamsBillUnpaidWarnDays()

DECLARE @isRightToViewForeignActions bit, @isRightToViewGroupActions bit
SELECT @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

-- без права смотреть чужие акции — только свои, что бы ни прислал клиент
IF @isRightToViewForeignActions = 0 AND @isRightToViewGroupActions = 0
	SET @managerID = @loggedUserID

DECLARE @massmedias TABLE(massmediaID smallint PRIMARY KEY, myMassmedia bit, foreignMassmedia bit)
INSERT INTO @massmedias (massmediaID, myMassmedia, foreignMassmedia)
SELECT * FROM dbo.fn_GetMassmediasForUser(@loggedUserID)

DECLARE @ugroups TABLE(id int)
INSERT INTO @ugroups (id)
SELECT * FROM dbo.[fn_GetUserGroups](@loggedUserID)

-- счета акций, чей эфир пересекается с периодом, и которые пользователь вправе видеть
SELECT b.actionID, b.agencyID, b.billNo, b.billDate, a.firmID, a.userID, a.isConfirmed, a.startDate
INTO #bill
FROM Bill b
	INNER JOIN [Action] a ON a.actionID = b.actionID
WHERE a.deleteDate IS NULL
	AND ((@showActivated = 1 AND a.isConfirmed = 1) OR (@showUnconfirmed = 1 AND a.isConfirmed = 0))
	AND (@startOfInterval IS NULL OR ISNULL(a.finishDate, a.startDate) >= @startOfInterval)
	AND (@endOfInterval IS NULL OR a.startDate < DATEADD(day, 1, CAST(@endOfInterval AS date)))
	AND (@agencyID IS NULL OR b.agencyID = @agencyID)
	AND (@managerID IS NULL OR a.userID = @managerID)
	AND (a.userID = @loggedUserID OR @isRightToViewForeignActions = 1
		OR (@isRightToViewGroupActions = 1 AND EXISTS (
			SELECT 1 FROM GroupMember gm INNER JOIN @ugroups ug ON ug.id = gm.groupID WHERE gm.userID = a.userID)))
	AND EXISTS (
		SELECT 1
		FROM Campaign c
			LEFT JOIN @massmedias umm ON umm.massmediaID = c.massmediaID
		WHERE c.actionID = b.actionID
			AND (a.isSpecial = 1
				OR (c.campaignTypeID <> 4 AND umm.massmediaID IS NOT NULL
					AND ((a.userID = @loggedUserID AND umm.myMassmedia = 1)
						OR (a.userID <> @loggedUserID AND umm.foreignMassmedia = 1)))
				OR (c.campaignTypeID = 4 AND NOT EXISTS (
					SELECT 1
					FROM PackModuleIssue pmi
						INNER JOIN PackModuleContent pmc ON pmi.pricelistID = pmc.pricelistID
						INNER JOIN Module m ON pmc.moduleID = m.moduleID
						LEFT JOIN @massmedias ummm ON m.massmediaID = ummm.massmediaID
					WHERE pmi.campaignID = c.campaignID
						AND (ummm.massmediaID IS NULL
							OR (a.userID = @loggedUserID AND ummm.myMassmedia = 0)
							OR (a.userID <> @loggedUserID AND ummm.foreignMassmedia = 0))))))

SELECT
	x.actionID,
	x.agencyID,
	x.billNo,
	x.billDate,
	CAST(x.startDate AS date) AS actionStartDate,
	x.firmName,
	x.billSum,
	x.agencyName,
	x.manager,
	x.paymentStatus,
	x.isConfirmed,
	x.paymentSignal,
	CASE
		WHEN x.paymentSignal IS NULL THEN NULL
		ELSE REPLACE(REPLACE(CASE x.paymentSignal WHEN 'bordeaux' THEN @tOnAir ELSE @tSoon END,
			N'{0}', CONVERT(nvarchar(10), x.startDate, 104)), N'{1}', LOWER(x.paymentStatus))
	END AS paymentSignalHint
FROM (
	SELECT
		b.actionID,
		b.agencyID,
		b.billNo,
		b.billDate,
		b.isConfirmed,
		b.startDate,
		f.name AS firmName,
		s.billSum,
		ag.name AS agencyName,
		u.userName AS manager,
		CASE s.statusID WHEN 1 THEN @tNotPaid WHEN 2 THEN @tPartial ELSE @tPaid END AS paymentStatus,
		s.statusID,
		CASE
			WHEN b.isConfirmed = 1 AND s.statusID <> 3 THEN
				CASE
					WHEN CAST(b.startDate AS date) <= @today THEN 'bordeaux'
					WHEN @today >= dbo.fn_AddWorkDays(b.startDate, -@warnDays) THEN 'orange'
				END
		END AS paymentSignal
	FROM #bill b
		INNER JOIN Firm f ON f.firmID = b.firmID
		INNER JOIN Agency ag ON ag.agencyID = b.agencyID
		LEFT JOIN [User] u ON u.userID = b.userID
		CROSS APPLY (
			SELECT ISNULL(SUM(c.finalPrice), 0) AS billSum
			FROM Campaign c
				INNER JOIN PaymentType pt ON pt.paymentTypeID = c.paymentTypeID AND pt.isHidden = 0
			WHERE c.actionID = b.actionID AND c.agencyID = b.agencyID
		) sm
		CROSS APPLY (
			SELECT ISNULL(SUM(pa.summa), 0) AS paid
			FROM PaymentAction pa
				INNER JOIN Payment p ON p.paymentID = pa.paymentID AND p.agencyID = b.agencyID
				INNER JOIN PaymentType pt ON pt.paymentTypeID = p.paymentTypeID AND pt.isHidden = 0
			WHERE pa.actionID = b.actionID
		) pd
		CROSS APPLY (
			SELECT sm.billSum,
				CASE WHEN pd.paid >= sm.billSum THEN 3 WHEN pd.paid = 0 THEN 1 ELSE 2 END AS statusID
		) s
) x
WHERE (@paymentStatusID IS NULL OR x.statusID = @paymentStatusID)
	AND (@hideZero = 0 OR x.billSum > 0)
ORDER BY x.billDate DESC, x.actionID DESC, x.agencyName, x.billNo DESC

GO

-- Списки фильтра «Реестр счетов» (сущность 1272, FilterPage Load). Таблицы названы по позиции в iTableAlias:
-- 1 managers — менеджеры, которых пользователь вправе выбирать (как у журнала оплат по менеджерам);
-- 2 bill_payment_status — статусы оплаты счёта (id — тот же @paymentStatusID, что у BillRegister).
CREATE OR ALTER PROCEDURE [dbo].[BillRegisterFilter]
(
	@loggedUserID smallint,
	@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба; десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

	-- 1. managers
	EXEC [UserListByRights] @loggedUserID = @loggedUserID

	-- 2. bill_payment_status
	SELECT CAST(1 AS tinyint) AS paymentStatusID, dbo.fn_Translate(@languageCode, N'Не оплачен') AS [name]
	UNION ALL SELECT 2, dbo.fn_Translate(@languageCode, N'Частично')
	UNION ALL SELECT 3, dbo.fn_Translate(@languageCode, N'Оплачен')
END

GO

-- =====================================================================
-- Метаданные
-- =====================================================================
DECLARE @entity int = 1272;
DECLARE @moduleSimpleJournal int = 118, @moduleFilterPage int = 2, @actionLoad int = 4;

DECLARE @filter nvarchar(max) = N'<filter>
	<page caption="Общие">
		<field caption="Есть выпуски с: " name="startOfInterval" type="df_date" value="StartOfTheLastMonth"/>
		<field caption="Есть выпуски по: " name="endOfInterval" type="df_date"/>
		<objectPicker caption="Менеджер:" name="managerID" source="managers" entity="user" value="LoggedUser"/>
		<lookup caption="Агенство:" name="agencyID" entity="agency">
			<filter name="ShowActive" value="true" type="boolean" />
		</lookup>
		<lookup caption="Оплата:" name="paymentStatusID" source="bill_payment_status" columnWithID="paymentStatusID"/>
		<separator/>
		<field caption="Показывать активированные" name="showActivated" type="boolean" value="true"/>
		<field caption="Показывать макеты" name="showUnconfirmed" type="boolean" value="false"/>
		<field caption="Не показывать нулевые счета" name="hideZero" type="boolean" value="true"/>
	</page>
</filter>';

SET IDENTITY_INSERT dbo.iEntity ON;
IF NOT EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = @entity)
    INSERT INTO dbo.iEntity (entityID, name, passport, tableName, className, assemblyName, pkColumn, codeName, filter,
                             isGrantingAllowed, iconName, entityClassName, parentId, isObsolete, isMassDeleteAllowed)
    VALUES (@entity, N'Реестр счетов', NULL, 'Bill', 'Merlin.Classes.BillRegisterRow', 'Merlin',
            'actionID,agencyID', 'BillRegister', @filter, 0, 'Money.png', NULL, NULL, 0, 0);
SET IDENTITY_INSERT dbo.iEntity OFF;

-- значок: стопка монет (Action.png у реестра был по ошибке — он у акций и оплат акций); обновляем всегда
UPDATE dbo.iEntity SET iconName = 'Money.png' WHERE entityID = @entity AND ISNULL(iconName, '') <> 'Money.png';

-- класс строки (ради действия «Открыть журнал акций», по образцу BalanceStatRow у сущности 1269); обновляем всегда
UPDATE dbo.iEntity SET className = 'Merlin.Classes.BillRegisterRow', assemblyName = 'Merlin'
WHERE entityID = @entity AND (ISNULL(className, '') <> 'Merlin.Classes.BillRegisterRow' OR ISNULL(assemblyName, '') <> 'Merlin');

-- действие ПКМ «Открыть журнал акций»: активированная акция — в журнал подтверждённых, макет — в журнал макетов.
-- Вне контроля прав (isGrantingAllowed = 0): открывает только чтение, доступ к журналу акций проверяет меню.
IF NOT EXISTS (SELECT 1 FROM dbo.iEntityAction WHERE entityID = @entity AND name = 'OpenActionJournal')
    INSERT INTO dbo.iEntityAction (entityID, alias, name, ordinal_position, isHidden, isGrantingAllowed, imgResourceName, parentID)
    VALUES (@entity, N'Открыть журнал акций', 'OpenActionJournal', 1, 0, 0, NULL, NULL);

-- фильтр обновляем всегда: это единственное место его хранения
UPDATE dbo.iEntity SET filter = @filter
WHERE entityID = @entity AND CAST(filter AS nvarchar(max)) <> @filter;

-- колонки (billSum — «Сумма»; agencyID в результате есть, но не колонка: нужен ключу pkColumn)
-- колонка «Статус акции» (actionStatus) заменена галочкой «Активирована» (isConfirmed) — старую строку убираем
DELETE FROM dbo.iEntityAttribute WHERE entityID = @entity AND name = 'actionStatus';

MERGE dbo.iEntityAttribute AS dst
USING (VALUES (1,  N'Статус',              'paymentSignal',   'signal'),
              (2,  N'Дата счёта',         'billDate',        'date2'),
              (3,  N'№ счёта',            'billNo',          NULL),
              (4,  N'№ Акции',            'actionID',        NULL),
              (5,  N'Активирована',       'isConfirmed',     'bit'),
              (6,  N'Дата начала акции',  'actionStartDate', 'date2'),
              (7,  N'Фирма-заказчик',     'firmName',        NULL),
              (8,  N'Сумма',              'billSum',         'money'),
              (9,  N'Агентство',          'agencyName',      NULL),
              (10, N'Менеджер',           'manager',         NULL),
              (11, N'Оплата',             'paymentStatus',   NULL))
      AS src (ordinal_position, alias, name, dataType)
   ON dst.entityID = @entity AND dst.name = src.name AND dst.selector = 0
 WHEN MATCHED AND (dst.alias <> src.alias OR dst.ordinal_position <> src.ordinal_position
                   OR ISNULL(dst.dataType, '') <> ISNULL(src.dataType, '')) THEN
    UPDATE SET alias = src.alias, ordinal_position = src.ordinal_position, dataType = src.dataType
 WHEN NOT MATCHED BY TARGET THEN
    INSERT (entityID, alias, name, ordinal_position, selector, dataType)
    VALUES (@entity, src.alias, src.name, src.ordinal_position, 0, src.dataType);

-- процедуры и привязки к модулям
IF NOT EXISTS (SELECT 1 FROM dbo.iStoredProcedure WHERE name = 'BillRegister')
    INSERT INTO dbo.iStoredProcedure (name, procedureType, isTransactionRequired, cachingTime)
    VALUES ('BillRegister', 'RECORDSET', 0, 5);
IF NOT EXISTS (SELECT 1 FROM dbo.iStoredProcedure WHERE name = 'BillRegisterFilter')
    INSERT INTO dbo.iStoredProcedure (name, procedureType, isTransactionRequired, cachingTime)
    VALUES ('BillRegisterFilter', 'RECORDSET', 0, 5);

DECLARE @spData int = (SELECT storedProcedureID FROM dbo.iStoredProcedure WHERE name = 'BillRegister');
DECLARE @spFilter int = (SELECT storedProcedureID FROM dbo.iStoredProcedure WHERE name = 'BillRegisterFilter');

IF NOT EXISTS (SELECT 1 FROM dbo.iModuleProcedure WHERE entityID = @entity AND moduleID = @moduleSimpleJournal AND actionNameID = @actionLoad)
    INSERT INTO dbo.iModuleProcedure (storedProcedureID, entityID, moduleID, actionNameID, connectionTimeout)
    VALUES (@spData, @entity, @moduleSimpleJournal, @actionLoad, 60);
IF NOT EXISTS (SELECT 1 FROM dbo.iModuleProcedure WHERE entityID = @entity AND moduleID = @moduleFilterPage AND actionNameID = @actionLoad)
    INSERT INTO dbo.iModuleProcedure (storedProcedureID, entityID, moduleID, actionNameID, connectionTimeout)
    VALUES (@spFilter, @entity, @moduleFilterPage, @actionLoad, 60);

IF NOT EXISTS (SELECT 1 FROM dbo.iTableAlias WHERE storedProcedureID = @spFilter AND position = 1)
    INSERT INTO dbo.iTableAlias (storedProcedureID, position, name) VALUES (@spFilter, 1, 'managers');
IF NOT EXISTS (SELECT 1 FROM dbo.iTableAlias WHERE storedProcedureID = @spFilter AND position = 2)
    INSERT INTO dbo.iTableAlias (storedProcedureID, position, name) VALUES (@spFilter, 2, 'bill_payment_status');

-- пункт меню: Рекламный отдел, позиция 22 — между «Журналом оплат» (21) и «Журналом оплат по менеджерам» (23)
DECLARE @menuAdvertParent smallint = (SELECT parentID FROM dbo.iMenu WHERE codeName = 'miPaymentFRS');
IF @menuAdvertParent IS NULL
BEGIN
    RAISERROR(N'80: пункт miPaymentFRS («Рекламный отдел → Журнал оплат») не найден. Меню не добавлено.', 16, 1);
END
ELSE IF NOT EXISTS (SELECT 1 FROM dbo.iMenu WHERE codeName = 'miBillRegister')
    INSERT INTO dbo.iMenu (name, parentID, position, codeName, align, isPublic, isObsolete, name_es)
    VALUES (N'Реестр счетов', @menuAdvertParent, 22, 'miBillRegister', 'Left', 0, 0, N'Registro de facturas');

-- права: как у «Журнала оплат» Рекламного отдела
DECLARE @menuRegister smallint = (SELECT menuID FROM dbo.iMenu WHERE codeName = 'miBillRegister');
DECLARE @menuPayments smallint = (SELECT menuID FROM dbo.iMenu WHERE codeName = 'miPaymentFRS');

INSERT INTO dbo.GroupMenu (groupID, menuID)
SELECT gm.groupID, @menuRegister
FROM dbo.GroupMenu gm
WHERE gm.menuID = @menuPayments AND @menuRegister IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.GroupMenu x WHERE x.groupID = gm.groupID AND x.menuID = @menuRegister);

INSERT INTO dbo.UserAdditionMenu (userID, menuID, isGrant)
SELECT um.userID, @menuRegister, um.isGrant
FROM dbo.UserAdditionMenu um
WHERE um.menuID = @menuPayments AND @menuRegister IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.UserAdditionMenu x WHERE x.userID = um.userID AND x.menuID = @menuRegister);

-- испанский для веба
IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (VALUES ('es', '', N'Реестр счетов',   N'Registro de facturas'),
                  ('es', '', N'Дата счёта',      N'Fecha de factura'),
                  ('es', '', N'№ счёта',         N'N.º de factura'),
                  ('es', '', N'№ Акции',         N'N.º de acción'),
                  ('es', '', N'Фирма-заказчик',  N'Empresa cliente'),
                  ('es', '', N'Сумма',           N'Importe'),
                  ('es', '', N'Агентство',       N'Agencia'),
                  ('es', '', N'Менеджер',        N'Gerente'),
                  ('es', '', N'Оплата',          N'Pago'),
                  ('es', '', N'Оплата:',         N'Pago:'),
                  ('es', '', N'Не оплачен',      N'No pagada'),
                  ('es', '', N'Частично',        N'Parcial'),
                  ('es', '', N'Оплачен',         N'Pagada'),
                  ('es', '', N'Дата начала акции', N'Fecha de inicio de la acción'),
                  ('es', '', N'Активирована',    N'Activada'),
                  ('es', '', N'Есть выпуски с: ',  N'Con emisiones desde: '),
                  ('es', '', N'Есть выпуски по: ', N'Con emisiones hasta: '),
                  ('es', '', N'Показывать активированные', N'Mostrar activadas'),
                  ('es', '', N'Показывать макеты', N'Mostrar borradores'),
                  ('es', '', N'Не показывать нулевые счета', N'No mostrar facturas en cero'),
                  ('es', '', N'Акция уже в эфире с {0}; оплата: {1}', N'La acción ya está en el aire desde {0}; pago: {1}'),
                  ('es', '', N'Акция выходит в эфир {0}; оплата: {1}', N'La acción sale al aire el {0}; pago: {1}'),
                  ('es', '', N'Открыть журнал акций', N'Abrir el registro de campañas'),
                  ('es', '', N'Нет доступа к журналу макетов рекламных акций.', N'Sin acceso al registro de borradores de campañas publicitarias.')) AS src (lang, context, [source], [text])
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);
GO

-- =====================================================================
-- Проверка
-- =====================================================================
SELECT 'iEntity' AS [объект], COUNT(*) AS [есть], '1' AS [ожидается] FROM dbo.iEntity WHERE entityID = 1272 AND codeName = 'BillRegister'
UNION ALL SELECT 'iEntityAttribute', COUNT(*), '11' FROM dbo.iEntityAttribute WHERE entityID = 1272
UNION ALL SELECT 'iModuleProcedure', COUNT(*), '2' FROM dbo.iModuleProcedure WHERE entityID = 1272
UNION ALL SELECT 'iTableAlias', COUNT(*), '2' FROM dbo.iTableAlias a JOIN dbo.iStoredProcedure sp ON sp.storedProcedureID = a.storedProcedureID WHERE sp.name = 'BillRegisterFilter'
UNION ALL SELECT 'iEntityAction', COUNT(*), '1' FROM dbo.iEntityAction WHERE entityID = 1272 AND name = 'OpenActionJournal'
UNION ALL SELECT 'iMenu', COUNT(*), '1' FROM dbo.iMenu WHERE codeName = 'miBillRegister'
UNION ALL SELECT 'GroupMenu (как у miPaymentFRS)', COUNT(*), CAST((SELECT COUNT(*) FROM dbo.GroupMenu g JOIN dbo.iMenu m ON m.menuID = g.menuID WHERE m.codeName = 'miPaymentFRS') AS varchar(10))
    FROM dbo.GroupMenu g JOIN dbo.iMenu m ON m.menuID = g.menuID WHERE m.codeName = 'miBillRegister';
GO
SET NOEXEC OFF;
GO
