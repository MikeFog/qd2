CREATE PROCEDURE [dbo].[PaymentCommonActionUsers]
(
@startDate datetime = Null,
@finishDate datetime = NULL,
@agencyID INT = null,
@loggedUserID smallint
)
WITH EXECUTE AS OWNER
as
set nocount on 

declare @isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

CREATE TABLE #User(userID smallint)
INSERT INTO #User(userID)
SELECT DISTINCT
	a.[userID] 
FROM 
	[Action] a
	INNER JOIN PaymentAction pa ON pa.actionID = a.actionID
	INNER JOIN Payment p ON p.paymentID = pa.paymentID
	left join GroupMember gm on a.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
WHERE	
	(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) 
	and	p.paymentDate BETWEEN Coalesce(@startDate, p.paymentDate) 
	And Coalesce(dateadd(ss, -1, dateadd(day, 1, @finishDate)), p.paymentDate)
	AND p.[agencyID] = COALESCE(@agencyID, p.[agencyID])

EXEC sl_Users NULL
