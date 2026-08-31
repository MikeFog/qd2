/*
    ПРОД-ДЕПЛОЙ: AgitationFraming (ветка CleanupWindow) — убрать скалярную UDF
    fn_AgitationChainFirst из WHERE курсора удаления обвязки.
    Ветка hotfix/agitation-cleanup-perf.

    ПОВОД
      31.08.2026 — ActionDeactivate акции 185985 (280 агит-окон) не укладывался
      в клиентский таймаут 60 с, 3 попытки sveta подряд по 60 с.
      ActionDeactivate в конце крутит курсор по всем окнам акции с агитацией и
      на каждое зовёт AgitationFraming @actionName='CleanupWindow'. Внутри
      CleanupWindow курсор cur_del имел предикат
          dbo.fn_AgitationChainFirst(i.actualWindowID) = @cFirst
      fn_AgitationChainFirst — скалярная функция с циклом WHILE, не встраивается,
      выполняется построчно по ВСЕМ выпускам обвязки (типы 7/44/55) служебной
      фирмы. Замер на ArtvisDev: один CleanupWindow — 276 мс; курсор по 244 окнам
      акции — 41 318 мс. Умножается на число окон акции.

    ПРАВКА
      Окна цепочки и так перечисляются циклом WHILE @cur IS NOT NULL (проверка
      "осталась ли подтверждённая агитация"). Собираем их по ходу в табличную
      переменную @chainWindows и в cur_del меняем предикат на
          i.actualWindowID IN (SELECT windowID FROM @chainWindows)
      Семантически то же самое (fn_AgitationChainFirst(x)=@cFirst ⟺ x в цепочке
      от @cFirst), но индексный seek вместо RBAR-функции.

    ЭКВИВАЛЕНТНОСТЬ (ArtvisDev)
      300 случайных агит-окон, множество (окно, выпуск) для удаления:
      848 пар, СТАРАЯ форма == НОВАЯ, 0 расхождений НА КОРРЕКТНЫХ ЦЕПОЧКАХ.
      Бенчмарк: CleanupWindow x 244 окна  41 318 мс -> 410 мс (в 100 раз),
      итог удаления идентичен.
      На полуразорванной forward-связи (w.windowNextId->v, но v.windowPrevId<>w)
      новый код НЕ удаляет обвязку из окон за разрывом, старый удалял (даже не
      проверив их на агитацию) - новое поведение строго безопаснее. Детект
      осиротевшей обвязки после деплоя: political-agitation-orphan-framing-check.sql.

    QUOTED_IDENTIFIER ON
      AgitationFraming пишет TariffWindow (индекс по вычисляемому windowTime) —
      ALTER в отдельном батче с SET QUOTED_IDENTIFIER ON / SET ANSI_NULLS ON.

    ИДЕМПОТЕНТНОСТЬ  повторный запуск перезаливает то же тело.
    ОТКАТ  git show master:"ArtvisDB/dbo/Stored Procedures/AgitationFraming.sql"

    ПОСЛЕ ДЕПЛОЯ  зависшую акцию 185985 можно деактивировать из клиента штатно.
      Если нужно снять её ДО деплоя — EXEC dbo.ActionDeactivate прямо в SSMS
      (там нет 60-секундного таймаута клиента).

    ЗАПУСК
      sqlcmd -S <прод-сервер> -d <прод-БД> -E -b -I -i agitation-cleanup-perf-deploy.sql
      либо открыть в SSMS на нужной БД и выполнить целиком.
*/

-- USE [Artvis];
-- GO

SET NOCOUNT ON;
GO

/* ── Преполёт ─────────────────────────────────────────────────────────── */
IF OBJECT_ID('dbo.AgitationFraming') IS NULL
   OR OBJECT_ID('dbo.ActionDeactivate') IS NULL
   OR OBJECT_ID('dbo.fn_AgitationChainFirst') IS NULL
   OR OBJECT_ID('dbo.TariffWindow') IS NULL
BEGIN
    RAISERROR('НЕ ТА БАЗА: нет dbo.AgitationFraming / ActionDeactivate / fn_AgitationChainFirst / TariffWindow. Деплой прерван.', 16, 1);
    SET NOEXEC ON;
END
GO
PRINT 'БД     : ' + DB_NAME();
PRINT 'Сервер : ' + CONVERT(sysname, SERVERPROPERTY('ServerName'));
PRINT 'AgitationFraming до: ' + CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.AgitationFraming')) LIKE '%@chainWindows%'
                                     THEN 'уже с @chainWindows' ELSE 'старая (UDF в WHERE)' END;
GO

/* ── ALTER dbo.AgitationFraming ───────────────────────────────────────── */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[AgitationFraming]
(
@actionName varchar(32),
@actionID int = null,
@windowID int = null,
@loggedUserID smallint
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DECLARE @SERVICE_FIRM_NAME nvarchar(64) = N'(Служебная) Обвязка политагитации'

DECLARE @serviceFirmID smallint, @serviceActionID int

SELECT @serviceFirmID = firmID FROM Firm WHERE [name] = @SERVICE_FIRM_NAME

IF @actionName = 'InsertForWindow'
BEGIN
	DECLARE @mmID smallint, @first int, @last int, @prev int, @next int,
		@rLocal int, @rAnnounce int, @rFederal int, @svcCampaignID int,
		@insRollerID int, @insWindowID int,
		@excludeIntervals varchar(256), @windowMinute int, @windowDayBit int, @isExcluded bit = 0

	SELECT @mmID = tw.massmediaID FROM TariffWindow tw WHERE tw.windowId = @windowID
	SELECT @rLocal = agitationLocalRollerID, @rAnnounce = agitationAnnounceRollerID, @rFederal = agitationFederalRollerID,
		@excludeIntervals = agitationExcludeIntervals
	FROM MassMedia WHERE massmediaID = @mmID

	-- Интервал-исключение: в это время станция и так идентифицирует себя в эфире,
	-- поэтому идентификаторы СМИ (44/55) не нужны - как если бы в окне уже стояли
	-- ручные 4/5. Анонс агитации (7) добавляется в любом случае.
	-- Сравниваем по фактическим дате и времени выхода окна, границы интервала
	-- включаются. dayMask - маска дней недели интервала (бит 0 = пн ... бит 6 = вс);
	-- строка настройки без указания дней даёт 127, т.е. все дни.
	SELECT @windowMinute = DATEPART(hour, tw.windowDateActual) * 60 + DATEPART(minute, tw.windowDateActual),
		-- 0 = пн ... 6 = вс, независимо от текущего SET DATEFIRST
		@windowDayBit = CAST(POWER(2, (DATEPART(weekday, tw.windowDateActual) + @@DATEFIRST - 2) % 7) AS int)
	FROM TariffWindow tw WHERE tw.windowId = @windowID

	IF EXISTS (SELECT 1 FROM dbo.fn_AgitationExcludeIntervals(@excludeIntervals)
		WHERE @windowMinute BETWEEN startMin AND finishMin
			AND dayMask & @windowDayBit <> 0)
		SET @isExcluded = 1

	-- страховка: вызывающая сторона обязана была это проверить
	-- (AgitationStationRollersNotSet в ActionActivate / hlp_IssueVerify)
	IF @rLocal IS NULL OR @rAnnounce IS NULL OR @rFederal IS NULL RETURN

	IF @serviceFirmID IS NULL
	BEGIN
		INSERT INTO Firm ([name]) VALUES (@SERVICE_FIRM_NAME)
		SET @serviceFirmID = SCOPE_IDENTITY()
	END

	-- Служебных акций фирмы в норме одна. Если руками завели вторую, берём
	-- самую старую детерминированно, чтобы новые выпуски обвязки кучковались
	-- в одной акции, а не расползались по нескольким.
	SELECT TOP 1 @serviceActionID = actionID FROM [Action]
	WHERE firmID = @serviceFirmID AND deleteDate IS NULL
	ORDER BY actionID

	IF @serviceActionID IS NULL
	BEGIN
		-- userID обязателен на практике: журналы и гриды (WindowIssuesRetrieve,
		-- Actions1, SpecialActions, TransferLogRetrieve...) джойнят [User]
		-- через INNER JOIN, и акция с NULL-создателем в них не видна.
		-- Создатель - admin (userID 0), а не активировавший менеджер: акция
		-- служебная, и её владелец не должен зависеть от того, кто активировал
		-- агитацию первым (иначе статистика по менеджерам получит чужие выпуски)
		INSERT INTO [Action] (firmID, userID, startDate, finishDate, isConfirmed)
		VALUES (@serviceFirmID, 0, GETDATE(), '99991231', 1)
		SET @serviceActionID = SCOPE_IDENTITY()
	END

	-- границы цепочки окон (windowPrevId/windowNextId); в ~99% случаев цепочки нет
	SET @first = @windowID
	WHILE 1 = 1
	BEGIN
		SELECT @prev = windowPrevId FROM TariffWindow WHERE windowId = @first
		IF @prev IS NULL BREAK
		SET @first = @prev
	END
	SET @last = @windowID
	WHILE 1 = 1
	BEGIN
		SELECT @next = windowNextId FROM TariffWindow WHERE windowId = @last
		IF @next IS NULL BREAK
		SET @last = @next
	END

	-- служебная кампания станции: get-or-create
	SELECT @svcCampaignID = campaignID FROM Campaign
	WHERE actionID = @serviceActionID AND massmediaID = @mmID AND campaignTypeID = 1

	IF @svcCampaignID IS NULL
	BEGIN
		-- paymentTypeID/agencyID обязательны, но без FK; для служебной кампании
		-- содержательного смысла не несут
		INSERT INTO Campaign (actionID, massmediaID, campaignTypeID, paymentTypeID,
			agencyID, modUser, startDate, finishDate)
		VALUES (@serviceActionID, @mmID, 1, 1,
			(SELECT MIN(agencyID) FROM Agency), @loggedUserID, GETDATE(), '99991231')
		SET @svcCampaignID = SCOPE_IDENTITY()
	END

	-- три кандидата на вставку: 44 в первое окно цепочки, 7 в окно агитации,
	-- 55 в последнее; ручные 4/5 не дублируем (семейства 4/44 и 5/55)
	DECLARE cur_ins CURSOR LOCAL FOR
		SELECT v.rollerID, v.windowID
		FROM (VALUES (@rLocal, @first, 44), (@rAnnounce, @windowID, 7), (@rFederal, @last, 55)) v(rollerID, windowID, slotType)
		WHERE (@isExcluded = 0 OR v.slotType = 7)
			AND NOT EXISTS (
			SELECT 1 FROM Issue i2
				INNER JOIN Roller r2 ON r2.rollerID = i2.rollerID
			WHERE i2.actualWindowID = v.windowID
				AND ((v.slotType = 44 AND r2.rolActionTypeID IN (4, 44))
					OR (v.slotType = 7 AND r2.rolActionTypeID = 7)
					OR (v.slotType = 55 AND r2.rolActionTypeID IN (5, 55)))
		)

	OPEN cur_ins
	FETCH NEXT FROM cur_ins INTO @insRollerID, @insWindowID
	WHILE @@FETCH_STATUS = 0
	BEGIN
		INSERT INTO Issue (rollerID, originalWindowID, actualWindowID, campaignID,
			positionId, isConfirmed, activationDate, tariffPrice)
		VALUES (@insRollerID, @insWindowID, @insWindowID, @svcCampaignID, 0, 1, GETDATE(), 0)

		-- обвязка занимает время окна как обычный выпуск; минус допустим by design
		UPDATE tw SET
			timeInUseConfirmed = CASE WHEN tw.maxCapacity = 0
				THEN tw.timeInUseConfirmed + r.duration ELSE tw.timeInUseConfirmed END,
			capacityInUseConfirmed = CASE WHEN tw.maxCapacity > 0
				THEN tw.capacityInUseConfirmed + 1 ELSE tw.capacityInUseConfirmed END
		FROM TariffWindow tw, Roller r
		WHERE tw.windowId = @insWindowID AND r.rollerID = @insRollerID

		FETCH NEXT FROM cur_ins INTO @insRollerID, @insWindowID
	END
	CLOSE cur_ins
	DEALLOCATE cur_ins
END
ELSE IF @actionName = 'InsertForAction'
BEGIN
	DECLARE @wID int

	DECLARE cur_windows CURSOR LOCAL FOR
		SELECT DISTINCT i.actualWindowID
		FROM Issue i
			INNER JOIN Campaign c ON c.campaignID = i.campaignID
			INNER JOIN Roller r ON r.rollerID = i.rollerID
		WHERE c.actionID = @actionID AND i.isConfirmed = 1 AND r.rolActionTypeID = 6

	OPEN cur_windows
	FETCH NEXT FROM cur_windows INTO @wID
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC AgitationFraming
			@actionName = 'InsertForWindow',
			@windowID = @wID,
			@loggedUserID = @loggedUserID

		FETCH NEXT FROM cur_windows INTO @wID
	END
	CLOSE cur_windows
	DEALLOCATE cur_windows
END
ELSE IF @actionName = 'CleanupWindow'
BEGIN
	IF @serviceFirmID IS NULL RETURN
	IF NOT EXISTS (SELECT 1 FROM [Action]
		WHERE firmID = @serviceFirmID AND deleteDate IS NULL) RETURN

	-- границы цепочки
	DECLARE @cFirst int = @windowID, @cPrev int, @cNext int, @cur int
	WHILE 1 = 1
	BEGIN
		SELECT @cPrev = windowPrevId FROM TariffWindow WHERE windowId = @cFirst
		IF @cPrev IS NULL BREAK
		SET @cFirst = @cPrev
	END

	-- Если где-то в цепочке осталась ПОДТВЕРЖДЁННАЯ агитация - обвязку не трогаем.
	-- Учитываются только подтверждённые выпуски: обвязка существует ради эфира,
	-- поэтому агитация, вернувшаяся в черновик (деактивация акции), обвязку не
	-- удерживает - она будет создана заново при повторной активации.
	-- Заодно собираем окна цепочки в @chainWindows: тот же набор, что даёт
	-- fn_AgitationChainFirst(x) = @cFirst, но без скалярной UDF в WHERE курсора
	-- удаления ниже (RBAR-функция сканировала все выпуски обвязки на каждый вызов
	-- CleanupWindow; при деактивации акции их сотни - см. ActionDeactivate).
	DECLARE @chainWindows TABLE (windowID int PRIMARY KEY)
	SET @cur = @cFirst
	WHILE @cur IS NOT NULL
	BEGIN
		IF EXISTS (SELECT 1 FROM Issue i
				INNER JOIN Roller r ON r.rollerID = i.rollerID
			WHERE i.actualWindowID = @cur AND r.rolActionTypeID = 6 AND i.isConfirmed = 1)
			RETURN
		INSERT INTO @chainWindows (windowID) VALUES (@cur)
		SELECT @cNext = windowNextId FROM TariffWindow WHERE windowId = @cur
		SET @cur = @cNext
	END

	-- агитации в цепочке нет: снять служебную обвязку по всем окнам цепочки.
	-- Прямое удаление (не через IssueIUD): проверки PastIssue/DeadLineViolationDelete
	-- не должны блокировать системную операцию; лог удалений для служебных выпусков
	-- не ведём. Ручные 4/5 не трогаем - удаляются только выпуски служебной акции.
	-- Чистим по ВСЕМ живым акциям служебной фирмы, а не по одной: если руками
	-- завели вторую служебную акцию, обвязка окна могла осесть в любой из них,
	-- и резолвинг единственной actionID пропустил бы "чужой" ролик в эфире.
	DECLARE @delIssueID int, @delWindowID int, @delDuration int

	DECLARE cur_del CURSOR LOCAL FOR
		SELECT i.issueID, i.actualWindowID, r.duration
		FROM Issue i
			INNER JOIN Campaign c ON c.campaignID = i.campaignID
			INNER JOIN Roller r ON r.rollerID = i.rollerID
		WHERE c.actionID IN (SELECT actionID FROM [Action]
				WHERE firmID = @serviceFirmID AND deleteDate IS NULL)
			AND r.rolActionTypeID IN (7, 44, 55)
			AND i.actualWindowID IN (SELECT windowID FROM @chainWindows)

	OPEN cur_del
	FETCH NEXT FROM cur_del INTO @delIssueID, @delWindowID, @delDuration
	WHILE @@FETCH_STATUS = 0
	BEGIN
		UPDATE tw SET
			timeInUseConfirmed = CASE WHEN tw.maxCapacity = 0
				THEN tw.timeInUseConfirmed - @delDuration ELSE tw.timeInUseConfirmed END,
			capacityInUseConfirmed = CASE WHEN tw.maxCapacity > 0
				THEN tw.capacityInUseConfirmed - 1 ELSE tw.capacityInUseConfirmed END
		FROM TariffWindow tw
		WHERE tw.windowId = @delWindowID

		DELETE FROM Issue WHERE issueID = @delIssueID

		FETCH NEXT FROM cur_del INTO @delIssueID, @delWindowID, @delDuration
	END
	CLOSE cur_del
	DEALLOCATE cur_del
END
GO

/* ── Проверка ─────────────────────────────────────────────────────────── */
SET NOEXEC OFF;
GO
SELECT
    [процедура]         = o.name,
    [quoted_identifier] = m.uses_quoted_identifier,   -- ожидается 1
    [ansi_nulls]        = m.uses_ansi_nulls,          -- ожидается 1
    [есть_chainWindows] = CASE WHEN m.definition LIKE '%@chainWindows%' THEN 1 ELSE 0 END,     -- 1
    [нет_UDF_в_cur_del] = CASE WHEN m.definition LIKE '%fn_AgitationChainFirst(i.actualWindowID)%' THEN 0 ELSE 1 END, -- 1
    [изменена]          = o.modify_date
FROM sys.sql_modules m
JOIN sys.objects o ON o.object_id = m.object_id
WHERE o.name = 'AgitationFraming';
GO
PRINT 'Готово. Ожидается: quoted_identifier=1, ansi_nulls=1, есть_chainWindows=1, нет_UDF_в_cur_del=1.';
GO
