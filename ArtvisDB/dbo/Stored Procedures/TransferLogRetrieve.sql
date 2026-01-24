CREATE       Proc [dbo].[TransferLogRetrieve]
(
@firmID smallint = NULL,
@headCompanyId smallint = Null,
@massmediaID smallint = NULL,
@startOfInterval datetime = NULL,
@endOfInterval  datetime = null,
@userID smallint = NULL,
@massmediaGroupID smallint = NULL,
@loggedUserID smallint
)
AS

SET NOCOUNT ON

declare @isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)


declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

IF NOT @startOfInterval IS NULL
	SET @startOfInterval = dbo.ToShortDate(@startOfInterval)

IF NOT @endOfInterval IS NULL
	SET @endOfInterval = dbo.ToShortDate(@endOfInterval) + 1

select distinct
	tl.*,
	a.actionID,
	f.name as firmName,
	hc.name as headCompanyName,
	m.name as massmediaName,
	m.groupName,
	u.userName as actionCreator
From
	TransferLog tl
	Inner Join Issue i ON i.issueID = tl.issueID
	Inner Join Campaign c ON c.campaignID = i.campaignID
	Inner Join [Action] a ON c.actionID = a.actionID
	Inner Join Firm f on f.firmID = a.firmID
	Inner Join HeadCompany hc on hc.headCompanyID = f.headCompanyID
	Inner Join vMassmedia m ON m.massmediaID = c.massmediaID
	Inner Join [User] u on u.userID = a.userID
	left join GroupMember gm on a.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
where
	(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
	a.firmID = Coalesce(@firmID, a.firmID) AND
	hc.headCompanyID = Coalesce(@headCompanyID, hc.headCompanyID) AND
	a.userID = Coalesce(@userID, a.userID) AND
	m.massmediaGroupID = Coalesce(@massmediaGroupID, m.massmediaGroupID) AND
	c.massmediaID = Coalesce(@massmediaID, c.massmediaID) AND
	tl.transferDate >= Coalesce(@startOfInterval, tl.transferDate) AND
	tl.transferDate < Coalesce(@endOfInterval, tl.transferDate + 1)
ORDER BY
	tl.transferDate desc
