CREATE         PROC [dbo].[StudioOrderActionsForPayment]
(
@paymentID int,
@loggedUserID smallint
)
AS
SET NOCOUNT ON

declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignSOActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupSOActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

DECLARE @paidUP TABLE(actionID INT, paidUp MONEY, paymentIsHidden BIT)

INSERT INTO @paidUP
SELECT distinct
	soa.actionID,
	IsNull(sum(psoa.summa), 0),
	pt.isHidden
FROM 
	[StudioOrderAction] soa
	INNER JOIN PaymentStudioOrder pso ON pso.firmID = soa.firmID 
	LEFT JOIN PaymentStudioOrderAction psoa ON psoa.actionID = soa.actionID 
		AND pso.[paymentID] = psoa.[paymentID]
	LEFT JOIN [PaymentType] pt ON pso.[paymentTypeID] = pt.[paymentTypeID]
	left join GroupMember gm on soa.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
where (soa.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null))
GROUP BY 
	soa.actionID, pt.isHidden

SELECT 
	soa.actionID,
	SUM(so.finalPrice) AS finalPrice,
	pu.paidUp as paidUp
FROM 
	[StudioOrderAction] soa
	INNER JOIN @paidUP pu ON soa.[actionID] = pu.[actionID]
	INNER JOIN [StudioOrder] so ON so.actionID = soa.actionID
		And so.isComplete = 1 
	INNER JOIN PaymentStudioOrder pso ON pso.firmID = soa.firmID
		And pso.agencyID = so.agencyID
	INNER JOIN [PaymentType] pt2 ON pso.[paymentTypeID] = pt2.[paymentTypeID]
		AND pt2.[isHidden] = pu.paymentIsHidden
	INNER JOIN [PaymentType] pt ON so.[paymentTypeID] = pt.[paymentTypeID]
		AND pt.[isHidden] = pu.paymentIsHidden
WHERE
	pso.paymentID = @paymentID
GROUP BY 
	soa.actionID, pu.paidUp
HAVING
	pu.paidUp < Sum(so.finalPrice)
