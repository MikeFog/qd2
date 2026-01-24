-- Actually, this isn't 'delete'. Unconnfirmed actions just go to special status and special journal.
CREATE   PROCEDURE [dbo].[DeleteUnconfirmedActions]
(
@loggedUserID int
)
WITH EXECUTE AS OWNER
AS
Declare
	@actionId int,
	@lifeTimeInDays int,
	@count int = 0

select @lifeTimeInDays = CAST(value as int) from iInternalVariable where name = 'UnconfirmedActionsLifetime'

-- 0 means that it's not necessary to delete actions
If @lifeTimeInDays > 0
	Begin

	Declare	cur_actions Cursor local fast_forward
	FOR 
	Select actionID From Action where deleteDate Is Null And isConfirmed = 0
	and DATEDIFF(DAY, finishDate, dbo.ToShortDate(GETDATE())) > @lifeTimeInDays

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
Select userID, 0, 'Перемещено макетов в журнал удалённых рекламных акций: ' + Str(@count) From [User] Where IsAdmin = 1 
