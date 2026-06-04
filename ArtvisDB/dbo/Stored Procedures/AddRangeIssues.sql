-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 14.04.2009
-- Description:	
-- =============================================
CREATE   procedure [dbo].[AddRangeIssues] 
(
	@issueDate datetime, 
	@rollerID int = NULL,
	@rollerDuration smallint = NULL,
	@positionId int = 0,
	@loggedUserId smallint,
	@actionID int,
	@grantorID SMALLINT = NULL,
	@considerUnconfirmed bit = 0,
	@ignoreWindowsWithTheSameFirmIssue bit = 0
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;

	declare cur_massmedias cursor local fast_forward for
	select c.massmediaID, c.campaignID from dbo.Campaign c where c.actionID = @actionID
	
	declare @massmediaID smallint, @campaignID int, @windowID int, @price decimal(18,2), @windowDateActual datetime, @firmID int
	
	select @firmID = a.firmID
	from dbo.[Action] a
	where a.actionID = @actionID
	
	open cur_massmedias
	fetch next from cur_massmedias into @massmediaID, @campaignID
	
	while @@fetch_status = 0
	begin 
		select @windowID = null, @price = null
		
		select top 1 @windowID = tw.windowId, @price = tw.price, @windowDateActual = tw.windowDateActual
		from TariffWindow tw
			inner join dbo.Tariff t on tw.tariffId = t.tariffID
		where tw.massmediaID = @massmediaID and tw.maxCapacity = 0 and tw.isDisabled = 0 and t.isForModuleOnly = 0
			and tw.windowDateOriginal between @issueDate and dateadd(second, -1, dateadd(minute, 30, @issueDate))
			and (
				@ignoreWindowsWithTheSameFirmIssue = 0
				or not exists
				(
					select 1
					from dbo.Issue i
						inner join dbo.Campaign c on c.campaignID = i.campaignID
						inner join dbo.[Action] a on a.actionID = c.actionID
					where i.actualWindowID = tw.windowId
						and a.firmID = @firmID
						and (i.isConfirmed = 1 or (a.deleteDate is null and @considerUnconfirmed = 1))
				)
			)
		order by 
			CASE
				WHEN @considerUnconfirmed = 0 THEN tw.duration - tw.timeInUseConfirmed
				ELSE tw.duration - tw.timeInUseConfirmed - tw.timeInUseUnconfirmed
			END		 
			desc
	
		if @windowID is null or @price is null 
		begin 
			if @ignoreWindowsWithTheSameFirmIssue = 1
				and exists
				(
					select 1
					from dbo.TariffWindow tw
						inner join dbo.Tariff t on tw.tariffId = t.tariffID
						inner join dbo.Issue i on i.actualWindowID = tw.windowId
						inner join dbo.Campaign c on c.campaignID = i.campaignID
						inner join dbo.[Action] a on a.actionID = c.actionID
					where tw.massmediaID = @massmediaID and tw.maxCapacity = 0 and tw.isDisabled = 0 and t.isForModuleOnly = 0
						and tw.windowDateOriginal between @issueDate and dateadd(second, -1, dateadd(minute, 30, @issueDate))
						and a.firmID = @firmID
						and (i.isConfirmed = 1 or a.deleteDate is null)
				)
				raiserror('IssueWithTheSameFirmExists', 16, 1)
			else
				raiserror('CannotAddRangeIssues', 16, 1)
			return 
		end 
	
		exec dbo.IssueIUD
			@rollerID = @rollerID,
			@rollerDuration = @rollerDuration,
			@issueDate = @windowDateActual,
			@windowID = @windowID, 
			@tariffWindowPrice = @price,
			@campaignID = @campaignID,
			@positionId = @positionId, 
			@ratio = 1,
			@loggedUserId = @loggedUserId,
			@massmediaID = @massmediaID,
			@actionName = 'AddItem',
			@grantorID = @grantorID
	
		if @@error <> 0 
			return 
	
		fetch next from cur_massmedias into @massmediaID, @campaignID
	end 

end