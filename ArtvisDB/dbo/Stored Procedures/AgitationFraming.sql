-- Авто-обвязка политической агитации (см. docs/tasks/political-agitation-ads.md).
-- Обвязка (типы 44/7/55) принадлежит окну, а не акции менеджера: выпуски создаются
-- в служебной акции (по одной служебной кампании на радиостанцию), чтобы не менять
-- инвариант Issue.campaignID NOT NULL и не попадать в клиентские медиапланы.
-- Служебные сущности создаются лениво (get-or-create): job_DeleteEmptyActions может
-- удалить пустую служебную акцию между агитационными периодами - это штатно.
CREATE PROCEDURE [dbo].[AgitationFraming]
(
@actionName varchar(32),	-- 'InsertForAction' | 'CleanupWindow'
@actionID int = null,		-- для InsertForAction: акция менеджера, только что активированная
@windowID int = null,		-- для CleanupWindow: окно, из которого удалили ролик типа 6
@loggedUserID smallint
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DECLARE @SERVICE_FIRM_NAME nvarchar(64) = N'(Служебная) Обвязка политагитации'

DECLARE @serviceFirmID smallint, @serviceActionID int

SELECT @serviceFirmID = firmID FROM Firm WHERE [name] = @SERVICE_FIRM_NAME

IF @actionName = 'InsertForAction'
BEGIN
	IF @serviceFirmID IS NULL
	BEGIN
		INSERT INTO Firm ([name]) VALUES (@SERVICE_FIRM_NAME)
		SET @serviceFirmID = SCOPE_IDENTITY()
	END

	SELECT @serviceActionID = actionID FROM [Action]
	WHERE firmID = @serviceFirmID AND deleteDate IS NULL

	IF @serviceActionID IS NULL
	BEGIN
		INSERT INTO [Action] (firmID, startDate, finishDate, isConfirmed)
		VALUES (@serviceFirmID, GETDATE(), '99991231', 1)
		SET @serviceActionID = SCOPE_IDENTITY()
	END

	DECLARE @wID int, @mmID smallint, @first int, @last int, @prev int, @next int,
		@rLocal int, @rAnnounce int, @rFederal int, @svcCampaignID int, @insRollerID int, @insWindowID int

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
		SELECT @mmID = tw.massmediaID FROM TariffWindow tw WHERE tw.windowId = @wID
		SELECT @rLocal = agitationLocalRollerID, @rAnnounce = agitationAnnounceRollerID, @rFederal = agitationFederalRollerID
		FROM MassMedia WHERE massmediaID = @mmID

		-- страховка: активация обязана была это проверить (AgitationStationRollersNotSet)
		IF @rLocal IS NULL OR @rAnnounce IS NULL OR @rFederal IS NULL
		BEGIN
			FETCH NEXT FROM cur_windows INTO @wID
			CONTINUE
		END

		-- границы цепочки окон (windowPrevId/windowNextId); в ~99% случаев цепочки нет
		SET @first = @wID
		WHILE 1 = 1
		BEGIN
			SELECT @prev = windowPrevId FROM TariffWindow WHERE windowId = @first
			IF @prev IS NULL BREAK
			SET @first = @prev
		END
		SET @last = @wID
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
			FROM (VALUES (@rLocal, @first, 44), (@rAnnounce, @wID, 7), (@rFederal, @last, 55)) v(rollerID, windowID, slotType)
			WHERE NOT EXISTS (
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

		FETCH NEXT FROM cur_windows INTO @wID
	END
	CLOSE cur_windows
	DEALLOCATE cur_windows
END
ELSE IF @actionName = 'CleanupWindow'
BEGIN
	IF @serviceFirmID IS NULL RETURN
	SELECT @serviceActionID = actionID FROM [Action]
	WHERE firmID = @serviceFirmID AND deleteDate IS NULL
	IF @serviceActionID IS NULL RETURN

	-- границы цепочки
	DECLARE @cFirst int = @windowID, @cPrev int, @cNext int, @cur int
	WHILE 1 = 1
	BEGIN
		SELECT @cPrev = windowPrevId FROM TariffWindow WHERE windowId = @cFirst
		IF @cPrev IS NULL BREAK
		SET @cFirst = @cPrev
	END

	-- если где-то в цепочке ещё осталась агитация (тип 6) - обвязку не трогаем
	SET @cur = @cFirst
	WHILE @cur IS NOT NULL
	BEGIN
		IF EXISTS (SELECT 1 FROM Issue i
				INNER JOIN Roller r ON r.rollerID = i.rollerID
			WHERE i.actualWindowID = @cur AND r.rolActionTypeID = 6)
			RETURN
		SELECT @cNext = windowNextId FROM TariffWindow WHERE windowId = @cur
		SET @cur = @cNext
	END

	-- агитации в цепочке нет: снять служебную обвязку по всем окнам цепочки.
	-- Прямое удаление (не через IssueIUD): проверки PastIssue/DeadLineViolationDelete
	-- не должны блокировать системную операцию; лог удалений для служебных выпусков
	-- не ведём. Ручные 4/5 не трогаем - удаляются только выпуски служебной акции.
	DECLARE @delIssueID int, @delWindowID int, @delDuration int

	DECLARE cur_del CURSOR LOCAL FOR
		SELECT i.issueID, i.actualWindowID, r.duration
		FROM Issue i
			INNER JOIN Campaign c ON c.campaignID = i.campaignID
			INNER JOIN Roller r ON r.rollerID = i.rollerID
			INNER JOIN TariffWindow tw ON tw.windowId = i.actualWindowID
		WHERE c.actionID = @serviceActionID
			AND r.rolActionTypeID IN (7, 44, 55)
			AND (tw.windowId = @cFirst
				OR EXISTS (SELECT 1 FROM TariffWindow tw0 WHERE tw0.windowId = @windowID AND tw0.massmediaID = tw.massmediaID)
					AND dbo.fn_AgitationChainFirst(i.actualWindowID) = @cFirst)

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
