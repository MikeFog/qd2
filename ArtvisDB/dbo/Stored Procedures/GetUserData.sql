CREATE    PROC [dbo].[GetUserData]
(
@loginName nvarchar(32) = null,
@password binary(16) = null,
@userID smallint = null
)
AS
SET NOCOUNT on

if @userID is null
	select top 1 @userID = [userID] from [User] where [loginName] = @loginName AND passwordHash = @password and isActive = 1

SELECT 
	[userID], 
	[firstName], 
	[lastName], 
	phone,
	email,
	dbo.f_IsAdmin(userID) as isAdmin,
	dbo.f_IsGrantor(userID) as isGrantor,
	dbo.f_IsTrafficManager(userID) as isTrafficManager,
	dbo.fn_IsRightToEditForeignActions(userID) as isRightToEditForeignActions,
	dbo.fn_IsRightToEditGroupActions(userID) as isRightToEditGroupActions,
	dbo.fn_IsRightToViewForeignActions(userID) as isRightToViewForeignActions,
	dbo.fn_IsRightToViewGroupActions(userID) as isRightToViewGroupActions,
	dbo.fn_IsRightToEditForeignSOActions(userID) as isRightToEditForeignSOActions,
	dbo.fn_IsRightToEditGroupSOActions(userID) as isRightToEditGroupSOActions,
	dbo.fn_IsRightToViewForeignSOActions(userID) as isRightToViewForeignSOActions,
	dbo.fn_IsRightToViewGroupSOActions(userID) as isRightToViewGroupSOActions,
	isBookKeeper
FROM 
	[User]
WHERE
	[userID] = @userID
	and isActive = 1

-- user Groups
select g.groupID, g.[name] from [Group] g 
	inner join GroupMember gm on g.groupID = gm.groupID
	inner join [User] u on gm.userID = u.userID
where gm.userID = @userID and u.isActive = 1
