-- Stored Procedure

-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 19.09.2008
-- Description:	List of special studio order actions
-- =============================================
CREATE procedure [dbo].[SpecialStudioOrderActions] 
(
	@actionID int = null,
	@agencyID smallint = null, 
	@firmID int = null,
	@startDate datetime = null, 
	@endDate datetime = null,
	@userID smallint = null,
	@paymentTypeID tinyint = null,
	@loggedUserID smallint
)
as 
begin 
	set nocount on;

	declare @isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignSOActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupSOActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

    select 
		a.actionID,
		'Остаток №' + cast(a.actionID as varchar) as [name],
		a.firmID,
		a.finishDate as date,
		a.userID,
		so.agencyID,
		a.tariffPrice as price,
		f.[name] as firm,
		ag.name as agency,
		isnull(u.lastname, '') + space(1) + isnull(left(u.firstname, 1), '') + '.' + isnull(left(u.secondname, 1), '') as manager,
		pt.paymentTypeID,
		pt.name as paymentType
    from StudioOrderAction a 
		inner join StudioOrder so on a.actionID = so.actionID
		inner join Firm f on a.firmID = f.firmID
		inner join Agency ag on so.agencyID = ag.agencyID
		inner join [User] u on so.userID = u.userID
		inner join PaymentType pt on so.paymentTYpeID = pt.paymentTypeID 
		left join GroupMember gm on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	where a.isSpecial = 1
		and (a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) 
end
