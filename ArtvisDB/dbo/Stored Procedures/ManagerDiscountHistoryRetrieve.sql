CREATE                PROC [dbo].[ManagerDiscountHistoryRetrieve]
(
@showDifferentUsersOnly bit = 1
)
AS
SET NOCOUNT ON

Select 
	u1.userName as grantorName,
	cm.actionID,
	u2.userName as creatorName,
	md.discountSetTime,
	md.managerDiscount,
	mm.name as massmediaName,
	mm.groupName
From 
	ManagerDiscountHistory md 
	Inner Join [vUser] u1 On md.userID = u1.userID
	Inner Join Campaign cm On cm.campaignID = md.campaignID
	INNER JOIN [Action] a ON cm.[actionID] = a.[actionID]
	Inner Join [vUser] u2 On a.userID = u2.userID
	LEFT JOIN vMassMedia mm ON mm.massmediaID = cm.massmediaID
Where
	(@showDifferentUsersOnly = 0 Or u1.userID <> u2.userID)
Order By 
	md.[discountSetTime]