-- Удаление подтверждённой акции («Удалить» в журнале акций: акция уходит в «Удалённые рекламные акции», десктоп и веб)
-- не писало журнал удалённых выпусков (сущность 193 «Удаленные рекламные выпуски», таблица LogDeletedIssue) и не
-- уведомляло администраторов (SayAdminThatIssuesDelete — «Удалил выпуски, дата выхода которых раньше чем через N дней…»):
-- ActionIUD 'DeleteItem' сначала звал ActionDeactivate, который снимает подтверждение со всех выпусков акции, а запись
-- журнала и проверка для уведомления шли после и отбирали isConfirmed = 1 — пусто. Так было с первой версии процедуры;
-- на копиях прода (Artvis до 28.09, Tumen до 06.10) у удалённых подтверждённых акций — ни одной строки журнала и ни
-- одного уведомления, а по прод-логу Artvis за 17.09–16.10.2026 подтверждённых акций удалено не меньше 19.
-- Теперь журнал и уведомление — до ActionDeactivate; сами блоки не менялись (время выхода — фактическое окно,
-- Deploy/26; день уведомления — tw.dayOriginal, Deploy/24). Параметры и выдача прежние, остальные ветки не тронуты.
-- docs/tasks/side-defects.md №2.
--
-- Что заметят пользователи: после наката каждое удаление подтверждённой акции пишет в «Удаленные рекламные выпуски» по
-- строке на подтверждённый выпуск, а если среди выпусков есть ближе f_SysParamsDaysLog дней (включая прошедшие), все
-- администраторы получают уведомление. Прошлые удаления восстановить нельзя: их выпуски уже неподтверждены.
--
-- Порядок: после Deploy/46 (несёт ActionIUD с @comment) — без него скрипт отказывается: «Ничего не изменено»; клиент
-- qd2 и перезапуск веба не нужны. Не перезапускать после 58: 24, 26 и 46 (несут старый текст ActionIUD); если
-- перезапустили — повторить 58. Идемпотентен (CREATE OR ALTER). Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 58_action-delete-deleted-issues-log.sql

SET NOCOUNT ON;
GO
IF COL_LENGTH('dbo.Action', 'comment') IS NULL
   OR CHARINDEX(N'@comment', OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionIUD'))) = 0
BEGIN
    RAISERROR(N'58: сначала Deploy/46 (ActionIUD с @comment). Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [dbo].[ActionIUD]
(
@actionID int = NULL,
@firmID smallint = NULL,
@userID smallint = NULL,
@newCreatorID smallint = NULL,
@isConfirmed bit = NULL,
@actionName varchar(32),
@loggedUserID smallint,
@comment nvarchar(256) = NULL -- комментарий менеджера; NULL - не менять, пустая строка - очистить
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT on

declare @IsAdmin bit, @isTrafficManager bit

set @IsAdmin = dbo.f_IsAdmin(@loggedUserID)
set @isTrafficManager = [dbo].[f_IsTrafficManager](@loggedUserID)

-- Only admin is allowed to delete ACTIVATED issue which is in the past already
if @actionName = 'DeleteItem' and @IsAdmin <> 1 and @isTrafficManager <> 1
	and exists(select * 
				from TariffWindow tw 
					inner join Issue i on tw.windowId = i.actualWindowID 
					inner join Campaign c on i.campaignID = c.campaignID
				where c.actionID = @actionID and tw.dayOriginal <= dbo.ToShortDate(getdate()) and i.isConfirmed = 1)
begin
	raiserror('PastIssue', 16, 1)
	return
end 

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [Action](firmID, userID, isConfirmed)
	VALUES(@firmID, @userID, @isConfirmed)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @actionID = SCOPE_IDENTITY()

	EXEC Actions1 @actionID = @actionID
END
ELSE IF @actionName = 'DeleteItem' 
	begin
	-- если акция уже находится в журнале удалённых акций, то удалять по настоящему
	If Exists (Select 1 From [Action] WHERE actionID = @actionID And Not deleteDate Is Null)
		Begin

		-- Политическая агитация: выпуски удаляются массово, минуя IssueIUD, поэтому
		-- окна с агитацией запоминаем до удаления и снимаем обвязку после
		Declare @agitWindows table (windowID int primary key)
		Insert Into @agitWindows (windowID)
		Select Distinct i.actualWindowID
		From Issue i
			Inner Join Campaign c On c.campaignID = i.campaignID
			Inner Join Roller r On r.rollerID = i.rollerID
		Where c.actionID = @actionID And r.rolActionTypeID = 6 And i.isConfirmed = 1

		DELETE Issue FROM Campaign c
		WHERE c.campaignID = Issue.campaignID AND c.actionID = @actionID

		DELETE FROM [Action] WHERE actionID = @actionID

		Declare @agitWindowID int
		Declare cur_agit_act Cursor Local For Select windowID From @agitWindows

		Open cur_agit_act
		Fetch Next From cur_agit_act Into @agitWindowID
		While @@FETCH_STATUS = 0
		Begin
			Exec AgitationFraming
				@actionName = 'CleanupWindow',
				@windowID = @agitWindowID,
				@loggedUserID = @loggedUserID

			Fetch Next From cur_agit_act Into @agitWindowID
		End
		Close cur_agit_act
		Deallocate cur_agit_act

		Return

		End

	-- Журнал удалённых и уведомление администраторам - до ActionDeactivate: он снимает подтверждение со всех
	-- выпусков акции, и отбор isConfirmed = 1 после него пуст (так было с первой версии, журнал не писался).
	insert into [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID)
	select @loggedUserID, c.actionID, i.rollerID, tw.windowDateActual, tw.massmediaID
	from Issue i
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
		inner join Campaign c on i.campaignID = c.campaignID
	where c.actionID = @actionID and i.isConfirmed = 1

	if exists(select *
		from Issue i
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
			inner join Campaign c on i.campaignID = c.campaignID
		where c.actionID = @actionID and i.isConfirmed = 1 and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog())
	begin
		exec SayAdminThatIssuesDelete @loggedUserID, @actionID
	end

	Select @isConfirmed = IsConfirmed From Action Where actionID = @actionID
	If @isConfirmed = 1	Begin
		Exec [ActionDeactivate] @actionID = @actionID, @loggedUserID = @loggedUserID
	End

	Update [Action] Set deleteDate = GETDATE() WHERE actionID = @actionID

	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When [maxCapacity] = 0 Then timeInUseConfirmed - t1.duration
				Else timeInUseConfirmed
			End,
		timeInUseUnconfirmed = 
			Case 
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed - t1.durationU
				Else timeInUseUnconfirmed
			End,
		capacityInUseConfirmed = 
			Case 
				When ([maxCapacity] > 0) 
					Then capacityInUseConfirmed - t1.countIssues
				Else capacityInUseConfirmed
			End,
		capacityInUseUnconfirmed = 
			Case  
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed - t1.countIssuesU
				Else capacityInUseUnconfirmed
			end,
		isFirstPositionOccupied = 
			Case 
				When firstCount > 0 Then 0
				Else isFirstPositionOccupied
			End,
		isSecondPositionOccupied = 
			Case 
				When secondCount > 0 Then 0
				Else isSecondPositionOccupied
			End,
		isLastPositionOccupied = 
			Case 
				When lastCount > 0 Then 0
				Else isLastPositionOccupied
			End,
		firstPositionsUnconfirmed = firstPositionsUnconfirmed - firstCountU,
		secondPositionsUnconfirmed = secondPositionsUnconfirmed - secondCountU,
		lastPositionsUnconfirmed = lastPositionsUnconfirmed - lastCountU
	From
		(select i.actualWindowID as windowID, 
			sum(case when i.isConfirmed = 1 then r.duration else 0 end) as duration, 
			sum(case when i.isConfirmed = 0 then r.duration else 0 end) as durationU, 
			sum(case when i.isConfirmed = 1 then 1 else 0 end) as countIssues,
			sum(case when i.isConfirmed = 0 then 1 else 0 end) as countIssuesU,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = -20 then 1 else 0 end, 0)) as firstCount,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = -10 then 1 else 0 end, 0)) as secondCount,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = 10 then 1 else 0 end, 0)) as lastCount,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = -20 then 1 else 0 end, 0)) as firstCountU,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = -10 then 1 else 0 end, 0)) as secondCountU,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = 10 then 1 else 0 end, 0)) as lastCountU
		 from 
			Issue i 
			Inner Join Roller r On r.rollerId = i.rollerId
			inner join Campaign c on i.campaignID = c.campaignID
		where c.actionID = @actionID
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID
END
ELSE IF @actionName = 'UpdateItem' 
	BEGIN
	DECLARE @oldFirmId INT, @oldIsConfirmed BIT 
	SELECT @oldFirmId = a.[firmID], @oldIsConfirmed = a.isConfirmed 
	FROM [Action] a 
	WHERE a.[actionID] = @actionID
	
	-- карточка «Свойства» присылает не все поля: чего нет - остаётся как было
	SET @firmID = ISNULL(@firmID, @oldFirmId)
	SET @isConfirmed = ISNULL(@isConfirmed, @oldIsConfirmed)

	IF (@oldFirmId <> @firmID)
		DELETE FROM [PaymentAction] WHERE [actionID] = @actionID

	IF (@oldIsConfirmed = 1 
		AND @isConfirmed = 0 AND EXISTS(SELECT * FROM dbo.[PaymentAction] pa WHERE pa.actionID = @actionID))
	BEGIN
		RAISERROR('ActionWithPaymentsCannotDeactivate',16,1)
		RETURN
	END

	UPDATE	
		[Action]
	SET			
		firmID = @firmID,
		userID = COALESCE(@newCreatorID, @userID, userID),
		isConfirmed = @isConfirmed,
		[comment] = CASE WHEN @comment IS NULL THEN [comment] ELSE NULLIF(LTRIM(RTRIM(@comment)), N'') END
	WHERE		
		actionID = @actionID

	EXEC Actions1 @actionID = @actionID
END
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[ActionIUD] TO PUBLIC
    AS [dbo];
GO

-- Проверка: журнал удалённых пишется до ActionDeactivate.
IF CHARINDEX(N'insert into [LogDeletedIssue]', OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionIUD'))) > 0
   AND CHARINDEX(N'insert into [LogDeletedIssue]', OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionIUD')))
       < CHARINDEX(N'Exec [ActionDeactivate]', OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionIUD')))
    PRINT N'ГОТОВО: ActionIUD — журнал удалённых и уведомление до деактивации.';
ELSE
    RAISERROR(N'58: новая версия ActionIUD не применена.', 16, 1);
GO

-- Снять режим «не выполнять», если его включила защита в начале (иначе окно SSMS остаётся в NOEXEC).
SET NOEXEC OFF;
GO
