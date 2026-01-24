CREATE     PROC [dbo].[WindowIssuesRetrieve]
(
@windowId int,
@showUnconfirmed bit = 0,
@rollerID int = null,
@firmID int = null
)
AS

SET NOCOUNT ON

if @showUnconfirmed = 0
begin 
	SELECT 
		i.*,
		r.[name],
		r.duration,
		tw.massmediaID,
		tw.tariffId,
		dbo.fn_Int2Time(r.duration) as durationString,
		c.actionID,
		ip.[description] as issuePosition,
		tw.windowDateOriginal as issueDate,
		a.firmID,
		u.userName as actionCreator,
		r.advertTypeName,
		a.deleteDate,
		f.name as firmName
	FROM
		Issue i
		inner join vRoller r on i.rollerID = r.rollerID 
		INNER JOIN Campaign c ON c.campaignID = i.campaignID
		inner join [Action] a on c.actionID = a.actionID
		inner join [User] u on a.userID = u.userID
		Inner Join iIssuePosition ip On ip.positionId = i.positionId
		Inner Join TariffWindow tw On tw.windowId = i.originalWindowID
		Left Join Firm f On f.firmID = r.firmID
	where (i.isConfirmed = 1 Or @showUnconfirmed = 1) and actualWindowId = @windowId
		and r.rollerID = coalesce(@rollerID, r.rollerID)
		and a.firmID = coalesce(@firmID, a.firmID)
	ORDER BY 
		i.positionId
end 
else 
begin 
	SELECT 
		i.*,
		r.[name],
		r.duration,
		tw.massmediaID,
		tw.tariffId,
		dbo.fn_Int2Time(r.duration) as durationString,
		c.actionID,
		ip.[description] as issuePosition,
		tw.windowDateActual as issueDate,
		a.firmID,
		u.userName as actionCreator,
		r.advertTypeName,
		a.deleteDate,
		f.name as firmName
	FROM
		Issue i
		inner join vRoller r on i.rollerID = r.rollerID 
		INNER JOIN Campaign c ON c.campaignID = i.campaignID
		inner join [Action] a on c.actionID = a.actionID
		inner join [User] u on a.userID = u.userID
		Inner Join iIssuePosition ip On ip.positionId = i.positionId
		Inner Join TariffWindow tw On tw.windowId = i.actualWindowId
		Left Join AdvertType advt on advt.advertTypeID = r.advertTypeID
		Left Join Firm f On f.firmID = r.firmID
	where (i.isConfirmed = 1 Or @showUnconfirmed = 1) and actualWindowId = @windowId
		and r.rollerID = coalesce(@rollerID, r.rollerID)
		and a.firmID = coalesce(@firmID, a.firmID)
		and a.deleteDate Is Null
	ORDER BY 
		i.positionId
end
