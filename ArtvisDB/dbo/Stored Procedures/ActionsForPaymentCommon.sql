CREATE  PROC [dbo].[ActionsForPaymentCommon]
(
@paymentID int,
@loggedUserID smallint
)
AS
SET NOCOUNT ON

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

declare @isRightToViewForeignActions bit,@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

DECLARE @paidUP TABLE(actionID INT, paidUp MONEY, paymentIsHidden bit, agencyID smallint)

INSERT INTO @paidUP
SELECT distinct
	a.actionID,
	IsNull(sum(pa.summa), 0),
	pt.isHidden,
	p.agencyID
FROM 
	(
		select distinct a.actionID, a.firmID 
		from [Action] a 
			inner join Campaign c on a.actionID = c.actionID
			inner join 
			(
				select distinct u.userID 
				from [User] u
					left join [GroupMember] gm on u.userID = gm.userID
					left join @ugroups ug on gm.groupID = ug.id
				where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
			) as x on a.userID = x.userID
			left join @massmedias umm on c.massmediaID = umm.massmediaID
			where (dbo.f_IsAdmin(@loggedUserID) = 1 and a.isSpecial = 1) or
		 		((c.campaignTypeID <> 4 and umm.massmediaID is not null and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1) )) 
					or (c.campaignTypeID = 4 and not exists(select * 
														from PackModuleIssue pmi 
															inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
															inner join Module m on pmc.moduleID = m.moduleID
															left join @massmedias ummm on m.massmediaID = ummm.massmediaID
														where pmi.campaignID = c.campaignID and (ummm.massmediaID is null or 
															(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
															 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0))))) 
	) a
	INNER JOIN Payment p ON p.firmID = a.firmID  
	LEFT JOIN PaymentAction pa ON pa.actionID = a.actionID 
		AND p.[paymentID] = pa.[paymentID] 
	LEFT JOIN [PaymentType] pt ON p.[paymentTypeID] = pt.[paymentTypeID]
GROUP BY 
	a.actionID, pt.isHidden,p.agencyID
	

SELECT 
	a.actionID,
	SUM(case when c.campaignTypeID = 4 then c.finalPrice else c.finalPrice * a.discount end) AS finalPrice,
	pu.paidUp as paidUp
FROM 
	[Action] a
	INNER JOIN @paidUP pu ON a.[actionID] = pu.[actionID]
	INNER JOIN [Campaign] c ON c.actionID = a.actionID
	INNER JOIN Payment p ON p.firmID = a.firmID
		And p.agencyID = c.[agencyID] and pu.agencyID = p.agencyID
	INNER JOIN [PaymentType] pt2 ON p.[paymentTypeID] = pt2.[paymentTypeID]
		AND pt2.[isHidden] = pu.paymentIsHidden
	INNER JOIN [PaymentType] pt ON c.[paymentTypeID] = pt.[paymentTypeID]
		AND pt.[isHidden] = pu.paymentIsHidden
WHERE
	p.paymentID = @paymentID AND a.[isConfirmed] = 1
GROUP BY 
	a.actionID, pu.paidUp
HAVING
	cast(SUM(case when c.campaignTypeID = 4 then c.finalPrice else c.finalPrice * a.discount end)*100 as int) - cast(pu.paidUp * 100 as int) > 0

