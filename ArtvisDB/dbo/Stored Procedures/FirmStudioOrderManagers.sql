CREATE PROCEDURE [dbo].[FirmStudioOrderManagers]
(
	@firmID int,
	@loggedUserID smallint
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

	declare @isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignSOActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupSOActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

    CREATE TABLE #User(userID smallint)
	INSERT INTO #User(userID)
	SELECT DISTINCT 
		u.userID
	FROM 
		[User] u
		left join GroupMember gm on u.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	WHERE 
		(u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) 
		and u.userID <> 0	
		and (u.userID in (select a.userID from StudioOrderAction a where a.firmID = @firmID)
			or u.userID in (select p.userID from PaymentStudioOrder p where p.firmID = @firmID))
		
		
	EXEC sl_Users null
END
