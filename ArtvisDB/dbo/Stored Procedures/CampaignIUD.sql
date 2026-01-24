CREATE PROCEDURE [dbo].[CampaignIUD]
(
@campaignID int OUT,
@actionID int = NULL,
@campaignTypeID tinyint = NULL,
@massmediaID smallint = NULL,
@paymentTypeID smallint = NULL,
@agencyID smallint = NULL,
@loggedUserId smallint,
@actionName varchar(32),
@needShow bit = 1,
@managerDiscount float = 1
)
WITH EXECUTE AS OWNER
as
set nocount on

declare @IsAdmin bit, @IsTraffic bit
set @IsAdmin = dbo.f_IsAdmin(@loggedUserID)
set @IsTraffic = dbo.f_IsTrafficManager(@loggedUserID)

-- Only admin is allowed to delete issue which is in the past already
if @actionName = 'DeleteItem' and @IsAdmin <> 1 and @IsTraffic <> 1
	and exists(select * 
				from TariffWindow tw 
					inner join Issue i on tw.windowId = i.originalWindowID 
				where i.campaignID = @campaignID and tw.dayOriginal <= dbo.ToShortDate(getdate()))
begin
	raiserror('PastIssue', 16, 1)
	return
end 

if @actionID is null and @campaignID is not null
	select @actionID = c.actionID from Campaign c where c.campaignID = @campaignID

IF @actionName = 'AddItem' begin
	if @agencyID is null 
	begin 
		raiserror('CannotAddCampaignWithoutAgency', 16, 1)
		return
	end

	if @IsAdmin = 0 and @IsTraffic = 0 and exists(select * 
				from [Action] a 
					inner join Campaign c on a.actionID = c.actionID
				where a.actionID = @actionID and a.finishDate < getdate() and a.isConfirmed = 1)
	begin 
		raiserror('CannotAddCampaignInConfirmedFinishedAction', 16, 1)
		return 
	end 

	INSERT INTO [Campaign](actionID, campaignTypeID, massmediaID, paymentTypeID, agencyID, modUser, managerDiscount)
	VALUES(@actionID, @campaignTypeID, @massmediaID, @paymentTypeID, @agencyID, @loggedUserId, @managerDiscount)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @campaignID = SCOPE_IDENTITY()

	if @needShow = 1
		EXEC Campaigns @CampaignID = @CampaignID, @actionID = @actionID
END
ELSE IF @actionName = 'DeleteItem' begin
	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When [maxCapacity] = 0 
					Then timeInUseConfirmed - t1.duration
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
		where i.campaignID = @campaignID
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID

	insert into [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
	select @loggedUserID, @actionID, i.rollerID, tw.windowDateOriginal, tw.massmediaID 
	from Issue i 
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
	where i.campaignID = @CampaignID and i.isConfirmed = 1

	if exists(select *
		from Issue i 
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
		where i.campaignID = @CampaignID and i.isConfirmed = 1 and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog())
	begin 
		exec SayAdminThatIssuesDelete @loggedUserID, @actionID
	end 

	DELETE Issue WHERE CampaignID = @CampaignID
	DELETE FROM [Campaign] WHERE CampaignID = @CampaignID
END
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE	
		[Campaign]
	SET			
		paymentTypeID = @paymentTypeID, 
		agencyID = @agencyID,
		modUser = @loggedUserId,
		actionID = @actionID
	WHERE		
		CampaignID = @CampaignID

	if @needShow = 1
		EXEC Campaigns @CampaignID = @CampaignID, @actionID = @actionID
END
