CREATE PROCEDURE [dbo].[DeleteDeletedActions]
(
@loggedUserID int,
@forceFlag bit = 1
)
WITH EXECUTE AS OWNER
AS
Declare
	@actionId int,
	@lifeTimeInDays int,
	@count int = 0

select @lifeTimeInDays = CAST(value as int) from iInternalVariable where name = 'DeletedActionsLifetime'

-- 0 means that it's not necessary to delete actions
If @lifeTimeInDays > 0 Or @forceFlag = 1
	Begin

	Declare	cur_actions Cursor local fast_forward
	FOR 
	Select actionID From Action where deleteDate Is Not Null 
	and (DATEDIFF(DAY, deleteDate, dbo.ToShortDate(GETDATE())) > @lifeTimeInDays /* Or @forceFlag = 1*/)

	Open	cur_actions
	Fetch	next from cur_actions 
	Into	@actionId

	WHILE	@@fetch_status = 0 
		begin
		
		Exec [ActionIUD]
			@actionName = 'DeleteItem',
			@actionId = @actionId,
			@loggedUserID = @loggedUserID
		
		Fetch	next from cur_actions 
		Into	@actionId

		Set @count = @count + 1
	end

	close cur_actions
	deallocate cur_actions

	End

insert into Announcement (toUserID, fromUserID, [subject]) 
Select userID, 0, 'Удалено рекламных акций: ' + Str(@count) From [User] Where IsAdmin = 1 

-- delete 'empty' campaigns and actions

delete
from 
	Campaign
Where 
	Not exists (select 1 from Issue i where i.campaignID = Campaign.campaignID)
	and Not exists (select 1 from ProgramIssue i where i.campaignID = Campaign.campaignID)
	and Not exists (select 1 from Action where isSpecial = 1 and Action.actionID = Campaign.actionID)

delete from Action where isSpecial = 0 and not exists (select 1 from Campaign c where c.actionID = Action.actionID)

insert into Announcement (toUserID, fromUserID, [subject]) 
Select userID, 0, 'Удалено "пустых" рекламных акций: ' + Str(@@ROWCOUNT) From [User] Where IsAdmin = 1 

