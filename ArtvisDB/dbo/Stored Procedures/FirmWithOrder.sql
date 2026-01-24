/*
Modified by Denis Gladkikh (dgladkikh@fogsoft.ru) 22.09.2008 - add special order action logic
*/
CREATE PROC [dbo].[FirmWithOrder]
(
@firmID smallint = null,
@paymentTypeID smallint = null,
@startOfInterval datetime = null,
@endOfInterval datetime = null,
@rolTypeID smallint = null,
@actionFinishDate datetime = null,
@userID smallint = null,
@studioID smallint = null,
@agencyID smallint = null,
@actionID int = null,
@isHideBlack bit = 0,
@isHideWhite bit = 0,
@isShowComplete BIT = 0, -- Не фильтруется если @firmID задан
@isShowUncomplete BIT = 0, -- Не фильтруется если @firmID задан
@showBlack bit = 1,
@showWhite bit = 1,
@groupID smallint = null,
@loggedUserID smallint = null
)
AS
SET NOCOUNT ON

	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignSOActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupSOActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

declare @firm table (firmID int primary key)

insert into @firm (firmID) 
select distinct act.firmID FROM StudioOrderAction act 
		LEFT JOIN StudioOrder so ON so.actionID = act.actionID 
			AND so.paymentTypeID = Coalesce(@paymentTypeID, so.paymentTypeID)
			AND so.studioID = Coalesce(@studioID, so.studioID)
			AND so.agencyID = Coalesce(@agencyID, so.agencyID)
		LEFT JOIN RolStyle rs ON rs.rolStyleID = so.rolStyleID 
			AND rs.rolTypeID = Coalesce(@rolTypeID, rs.rolTypeID)
		LEFT JOIN PaymentType pt ON pt.paymentTypeID = so.paymentTypeID
			AND (
				(@isHideBlack = 0 And pt.isHidden = 1) Or 
				(@isHideWhite = 0 And pt.isHidden = 0)
				)  and
				((pt.IsHidden = 1 and @showBlack = 1)  or
				(pt.IsHidden = 0 and @showWhite = 1)) 
		left join GroupMember gm on act.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	where 
		(@groupID is null or gm.groupID = @groupID) 
		and (act.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null))
		and act.isSpecial = 0 
		and (@agencyID is null or so.agencyID = @agencyID)
		AND act.createDate >= Coalesce(@startOfInterval, act.createDate)
		AND (act.createDate < @endOfInterval Or @endOfInterval Is Null)
		AND (act.finishDate >= @actionFinishDate or @actionFinishDate Is Null)
--		AND (act.finishDate = @actionFinishDate Or (act.finishDate Is Null And @actionFinishDate Is Null))
		AND act.userID = Coalesce(@userID, act.userID)
		AND act.actionID = Coalesce(@actionID, act.actionID)
		AND ((@isShowComplete = 1 AND @isShowUncomplete = 1) 
			OR (@isShowComplete = 1 AND @isShowUncomplete = 0 AND so.isComplete = 1) 
			OR (@isShowComplete = 0 AND @isShowUncomplete = 1 AND so.isComplete = 0)
			OR @firmID IS NOT NULL)

SELECT 
	f.*
FROM 
	[Firm] f
	inner join @firm t on f.firmID = t.firmID
WHERE f.firmID = Coalesce(@firmID, f.firmID)
ORDER BY
	f.[name]
