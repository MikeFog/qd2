CREATE  PROCEDURE [dbo].[ActionIUD]
(
@actionID int = NULL,
@firmID smallint = NULL,
@userID smallint = NULL,
@newCreatorID smallint = NULL,
@isConfirmed bit = NULL,
@actionName varchar(32),
@loggedUserID smallint
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
					inner join Issue i on tw.windowId = i.originalWindowID 
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

		DELETE Issue FROM Campaign c 
		WHERE c.campaignID = Issue.campaignID AND c.actionID = @actionID

		DELETE FROM [Action] WHERE actionID = @actionID

		Return

		End
	
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

	insert into [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
	select @loggedUserID, c.actionID, i.rollerID, tw.windowDateOriginal, tw.massmediaID 
	from Issue i 
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
		inner join Campaign c on i.campaignID = c.campaignID 
	where c.actionID = @actionID and i.isConfirmed = 1

	if exists(select *
		from Issue i 
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
			inner join Campaign c on i.campaignID = c.campaignID 
		where c.actionID = @actionID and i.isConfirmed = 1 and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog())
	begin 
		exec SayAdminThatIssuesDelete @loggedUserID, @actionID
	end 
END
ELSE IF @actionName = 'UpdateItem' 
	BEGIN
	DECLARE @oldFirmId INT, @oldIsConfirmed BIT 
	SELECT @oldFirmId = a.[firmID], @oldIsConfirmed = a.isConfirmed 
	FROM [Action] a 
	WHERE a.[actionID] = @actionID
	
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
		userID = IsNull(@newCreatorID, @userID),
		isConfirmed = @isConfirmed
	WHERE		
		actionID = @actionID

	EXEC Actions1 @actionID = @actionID
END
