-- Реестр счетов: фильтр по статусу оплаты — три флажка вместо одиночного выбора.
-- Было: список «Оплата:» (не оплачен / частично / оплачен — один из трёх или все). Заказчику нужно «всё, кроме
-- оплаченных», а множественного выбора в фильтре нет, поэтому — три флажка: «Показывать неоплаченные», «Показывать
-- оплаченные частично», «Показывать оплаченные». По умолчанию включены «неоплаченные» и «частично», а «оплаченные» выключены
-- (решение владельца 28.10.2026: оплаченные неинтересны); чтобы увидеть и оплаченные, отметьте «Показывать оплаченные».
--
-- Что делает скрипт (идемпотентен, сразу COMMIT-ится):
--   1. BillRegister (CREATE OR ALTER): новые параметры @showNotPaid, @showPartial, @showPaid (по умолчанию 1: вызов без них, как у окон старой версии, оплаченные не прячет; скрывает их сам фильтр); прежний
--      @paymentStatusID оставлен ради окон старой версии, открытых во время обновления, — новый фильтр его не шлёт;
--   2. iEntity.filter сущности 1272: список «Оплата:» заменён тремя флажками;
--   3. испанский для веба (iTranslation): подписи трёх флажков.
-- BillRegisterFilter и iTableAlias (список bill_payment_status) не трогаются: теперь без дела, но безвредны.
-- Порядок: после 80 (реестр счетов). Не перезапускать после 83: 80 (несёт прежний текст BillRegister и прежний фильтр);
-- если перезапустили — повторить 83. Нужны новый клиент? Нет: флажки — обычные поля фильтра, нужен только перезапуск
-- qd2 и веб (метаданные читаются при старте). Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 83_bill-register-payment-status-checkboxes.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

IF OBJECT_ID(N'dbo.BillRegister') IS NULL
   OR CHARINDEX(N'paymentSignal', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.BillRegister')), N'')) = 0
BEGIN
    RAISERROR(N'83: сначала 80 (реестр счетов). Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
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
-- Фильтр по статусу — три флажка @showNotPaid / @showPartial / @showPaid (по умолчанию все три): отметьте нужные,
-- например «не оплачен» и «частично» — всё, кроме оплаченных. Прежний одиночный выбор @paymentStatusID (1/2/3)
-- оставлен ради окон старой версии, открытых во время обновления: новый фильтр его не шлёт, а если пришёл — работает как раньше.
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
@paymentStatusID tinyint = null, -- устарел (см. выше): 1 не оплачен, 2 частично, 3 оплачен; NULL — все
@showNotPaid bit = 1,
@showPartial bit = 1,
@showPaid bit = 1,
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
WHERE ((@showNotPaid = 1 AND x.statusID = 1) OR (@showPartial = 1 AND x.statusID = 2) OR (@showPaid = 1 AND x.statusID = 3))
	AND (@paymentStatusID IS NULL OR x.statusID = @paymentStatusID)
	AND (@hideZero = 0 OR x.billSum > 0)
ORDER BY x.billDate DESC, x.actionID DESC, x.agencyName, x.billNo DESC

GO

-- фильтр сущности 1272: единственное место его хранения, обновляем всегда
DECLARE @filter nvarchar(max) = N'<filter>
	<page caption="Общие">
		<field caption="Есть выпуски с: " name="startOfInterval" type="df_date" value="StartOfTheLastMonth"/>
		<field caption="Есть выпуски по: " name="endOfInterval" type="df_date"/>
		<objectPicker caption="Менеджер:" name="managerID" source="managers" entity="user" value="LoggedUser"/>
		<lookup caption="Агенство:" name="agencyID" entity="agency">
			<filter name="ShowActive" value="true" type="boolean" />
		</lookup>
		<separator/>
		<field caption="Показывать активированные" name="showActivated" type="boolean" value="true"/>
		<field caption="Показывать макеты" name="showUnconfirmed" type="boolean" value="false"/>
		<field caption="Не показывать нулевые счета" name="hideZero" type="boolean" value="true"/>
		<separator/>
		<field caption="Показывать неоплаченные" name="showNotPaid" type="boolean" value="true"/>
		<field caption="Показывать оплаченные частично" name="showPartial" type="boolean" value="true"/>
		<field caption="Показывать оплаченные" name="showPaid" type="boolean" value="false"/>
	</page>
</filter>';
UPDATE dbo.iEntity SET filter = @filter
WHERE entityID = 1272 AND CAST(filter AS nvarchar(max)) <> @filter;

IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (VALUES ('es', '', N'Показывать неоплаченные',       N'Mostrar no pagadas'),
                  ('es', '', N'Показывать оплаченные частично', N'Mostrar pagadas parcialmente'),
                  ('es', '', N'Показывать оплаченные',          N'Mostrar pagadas')) AS src (lang, context, [source], [text])
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);
GO
SET NOEXEC OFF;
GO
IF CHARINDEX(N'@showPartial', OBJECT_DEFINITION(OBJECT_ID(N'dbo.BillRegister'))) > 0
   AND EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 1272 AND CAST(filter AS nvarchar(max)) LIKE N'%name="showPartial"%'
                                                              AND CAST(filter AS nvarchar(max)) NOT LIKE N'%name="paymentStatusID"%')
    PRINT N'ГОТОВО: реестр счетов — фильтр по статусу оплаты тремя флажками';
ELSE
    PRINT N'ОШИБКА: что-то не накатилось';
GO
