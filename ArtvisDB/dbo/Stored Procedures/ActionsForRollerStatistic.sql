CREATE     PROCEDURE [dbo].[ActionsForRollerStatistic]
(
@startDate datetime,
@finishDate datetime,
@rollerID int,
@massmediaString varchar(8000),
@userID smallint = null,
@loggedUserID smallint,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tAction NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Акция №');

	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

SELECT distinct
	ac.*,
	us.userName as creator,
	@tAction + LTRIM(ac.[actionID]) as name,
	f.name as firmName
FROM 
	[Action] ac 
	INNER JOIN [vUser] us ON us.userID = ac.userID
	INNER JOIN [Firm] f ON f.firmID = ac.firmID
	INNER JOIN [Campaign] c ON c.actionID = ac.actionID
	INNER JOIN Issue i ON c.campaignID = i.campaignID
	INNER JOIN TariffWindow tw On tw.windowId = i.originalWindowID
	INNER JOIN dbo.fn_CreateTableFromString(@massmediaString) m on m.ID = tw.massmediaID
	inner join 
	(
		select distinct u.userID 
		from [User] u
			left join [GroupMember] gm on u.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id	
		where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
	) as x on ac.userID = x.userID
where 
	i.rollerID = @rollerID AND
	tw.dayOriginal between @startDate and @finishDate and 
	ac.userID = Coalesce(@userID, ac.userID)	
ORDER BY
	ac.[actionID] DESC
