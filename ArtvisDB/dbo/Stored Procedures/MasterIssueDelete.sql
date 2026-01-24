-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 18.05.2009
-- Description:	Delete Master Issue
-- =============================================
CREATE PROCEDURE [dbo].[MasterIssueDelete] 
(
	@issueDate datetime, 
	@actionID int,
	@positionID int,
	@rollerID int,
	@grantorID smallint = null,
	@loggedUserId smallint
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

	declare cur_massmedias cursor local fast_forward for
	select c.massmediaID, c.campaignID from dbo.Campaign c where c.actionID = @actionID
	
	declare @massmediaID smallint, @campaignID int, @issueID int
	
	open cur_massmedias
	fetch next from cur_massmedias into @massmediaID, @campaignID
	
	while @@fetch_status = 0
	begin 
		select @issueID = null
		
		select top 1 @issueID = i.issueID
		from Issue i 
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
		where tw.massmediaID = @massmediaID and i.campaignID = @campaignID and i.positionID = @positionID and i.rollerID = @rollerID
			and tw.windowDateOriginal between @issueDate and dateadd(second, -1, dateadd(minute, 30, @issueDate))
		order by case when (tw.duration - tw.timeInUseConfirmed) > 0 then 0 else 1 end, tw.windowDateOriginal
	
		exec dbo.IssueIUD
			@rollerID = @rollerID,
			@campaignID = @campaignID,
			@positionId = @positionId, 
			@loggedUserId = @loggedUserId,
			@massmediaID = @massmediaID,
			@actionName = 'DeleteItem',
			@grantorID = @grantorID,
			@issueID = @issueID

	
		if @@error <> 0 
			return 
	
		fetch next from cur_massmedias into @massmediaID, @campaignID
	end 
END
