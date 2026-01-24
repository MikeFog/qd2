CREATE      PROC [dbo].[sl_StudioOrderActions]
AS
SET NOCOUNT ON

SELECT DISTINCT
	a.[contacts], a.[createDate], a.[finishDate], a.[firmID], a.[orderStatusID], a.[tariffPrice], a.[totalPrice], 
	--if need go to [StudioOrders] and think what can do with filtered userID
	--a.[userID],
	a.userID as managerID,
	a.actionID,
	f.name as firmName,
	statusName = s.name,
	u.lastName + Space(1) + u.firstName as creator,
	'№' + LTRIM(a.actionID) + Space(1) + f.name as name
FROM 
	StudioOrderAction a
	INNER JOIN #StudioOrderAction a2 ON a.actionID = a2.actionID
	INNER JOIN firm f ON f.firmID = a.firmID
	INNER JOIN [user] u ON u.userID = a.userID
	inner join iStudioOrderActionStatus s on s.statusID = a.orderStatusID
ORDER BY 
	a.actionID DESC
