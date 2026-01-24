/*
Modified by Denis Gladkikh (dgladkikh@fogsoft.ru) 22.09.2008 - add special order action logic
*/
CREATE proc [dbo].[StudioOrderActions]
(
@firmID smallint = Null,
@actionID int = null,
@actionFinishDate datetime = null,
@agencyID smallint = null,
@startOfInterval datetime = null,
@endOfInterval datetime = null,
@isHideBlack bit = 0,
@isHideWhite bit = 0,
@paymentTypeID smallint = null,
@studioID smallint = null,
@userID smallint = NULL,
@statusID INT = null,
@isShowComplete BIT = 1,
@isShowUncomplete BIT = 1,
@showBlack bit = 1,
@showWhite bit = 1,
@groupID smallint = null,
@loggedUserID smallint = null
)
WITH EXECUTE AS OWNER
AS

SET NOCOUNT ON

	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignSOActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupSOActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

CREATE TABLE #StudioOrderAction(actionID int)
INSERT INTO #StudioOrderAction(actionID)
SELECT DISTINCT 
	a.actionId 
FROM 
	[StudioOrderAction] a
	LEFT JOIN StudioOrder so ON so.actionID = a.actionID
	LEFT JOIN PaymentType pt ON pt.paymentTypeID = so.paymentTypeID
	LEFT JOIN RolStyle rs ON rs.rolstyleID = so.rolstyleID	
	left join GroupMember gm on a.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
where  
	(@groupID is null or gm.groupID = @groupID) 
	and (a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null))
	and a.isSpecial = 0 and 
	a.firmID = Coalesce(@firmID, a.firmID)
	and (@agencyID is null or so.agencyID = @agencyID)
	AND a.actionID = Coalesce(@actionID, a.actionID)
	AND a.userID = Coalesce(@userID, a.userID)
	AND a.createDate >= Coalesce(@startOfInterval, a.createDate)
	AND (a.finishDate = Coalesce(@actionFinishDate, a.finishDate) Or (a.finishDate Is Null And @actionFinishDate Is Null))
	AND (a.createDate < @endOfInterval Or @endOfInterval Is Null)
	AND (@agencyID Is Null Or so.agencyID = so.agencyID)
	AND (@studioID Is Null Or so.studioID = so.studioID)
	AND (@paymentTypeID Is Null Or so.paymentTypeID = so.paymentTypeID)
	and (@statusID is null or @statusID = a.orderStatusID)
	AND (
		(@isHideBlack = 0 And IsNull(pt.isHidden, 1) = 1) Or 
		(@isHideWhite = 0 And IsNull(pt.isHidden, 0) = 0)
		) and 
	((IsNull(pt.IsHidden, 1) = 1 and @showBlack = 1)  or
	(IsNull(pt.IsHidden, 0) = 0 and @showWhite = 1)) 		
	AND ((@isShowComplete = 1 AND @isShowUncomplete = 1) 
		OR (@isShowComplete = 1 AND @isShowUncomplete = 0 AND so.isComplete = 1) 
		OR (@isShowComplete = 0 AND @isShowUncomplete = 1 AND so.isComplete = 0))
ORDER BY 
	a.actionID

EXEC sl_StudioOrderActions
