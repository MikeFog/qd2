CREATE    PROC [dbo].[PaymentStudioOrderActionUsers]
(
@startDate datetime = Null,
@finishDate datetime = NULL,
@agencyID INT = null,
@loggedUserID int = null
)
WITH EXECUTE AS OWNER
as
set nocount on 

declare @isRightToViewForeignActions bit,
		@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignSOActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupSOActions(@loggedUserID)
		
declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

CREATE TABLE #User(userID smallint)
INSERT INTO #User(userID)
SELECT DISTINCT
	soa.[userID] 
FROM 
	[StudioOrderAction] soa
	INNER JOIN PaymentStudioOrderAction psoa ON psoa.actionID = soa.actionID
	INNER JOIN PaymentStudioOrder pso ON pso.paymentID = psoa.paymentID
	left join GroupMember gm on soa.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
where
	(soa.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) 
	and	pso.paymentDate BETWEEN Coalesce(@startDate, pso.paymentDate) 
	And Coalesce(@finishDate + 1, pso.paymentDate)
	AND pso.[agencyID] = COALESCE(@agencyID, pso.[agencyID])

if @loggedUserID is not null and not exists(select * from #user where userID = @loggedUserID)
	insert into [#User] values (@loggedUserID)

EXEC sl_Users NULL
