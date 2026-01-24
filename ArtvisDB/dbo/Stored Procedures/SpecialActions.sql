-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 17.09.2008
-- Description:	List Special Actions
-- Modification: Denis Gladkikh (dgladkikh@fogsoft.ru) 18.09.2008 - Need agency with payment Type
-- =============================================
CREATE PROCEDURE [dbo].[SpecialActions] 
(
	@actionID int = null,
	@startDate datetime = null, 
	@endDate datetime = null,
	@firmID int = null,
	@userID smallint = null,
	@paymentTypeID tinyint = null,
	@agencyID smallint = null,
	@loggedUserID smallint
)
as 
begin 
	set nocount on;

declare 
	@isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

select 
	@isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

    select distinct
		a.actionID, 
		f.[name] as firm, 
		u.userName as manager,
		a.startDate as date,
		a.totalPrice as price,
		a.userID,
		a.firmID,
		('Остаток № ' + cast(a.actionID as varchar)) as [name],
		c.agencyID,
		c.paymentTypeID,
		ag.name as agency,
		pt.[name] as paymenttype
    from [Action] a 
		inner join Campaign c on a.actionID = c.actionID
		inner join Firm f on a.firmID = f.firmID
		inner join [User] u on a.userID = u.userID
		inner join Agency ag on c.agencyID = ag.agencyID
		inner join PaymentType pt on c.paymentTypeID = pt.paymentTypeID
		left join GroupMember gm on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	where a.isSpecial = 1 
		and (a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) 
		and a.actionID = coalesce(@actionID, a.actionID)
		and ((@startDate is null or a.startDate >= @startDate)
		and (@endDate is null or a.startDate <= @endDate)
		and a.firmID = coalesce(@firmID, a.firmID)
		and a.userID = coalesce(@userID, a.userID)
		and c.agencyID = coalesce(@agencyID, c.agencyID)
		and c.paymentTypeID = coalesce(@paymentTypeID, c.paymentTypeID))
	order by 1
end
