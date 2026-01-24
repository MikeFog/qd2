CREATE        PROC [dbo].[stat_RollerStatistic]
(
@startDate datetime,
@finishDate datetime,
@splitByManager bit = 0,
@splitByDays bit = 0,
@massmediaString varchar(8000),
@userID smallint = NULL,
@isHideWhite BIT = 0,
@isHideBlack BIT = 0,
@showBlack bit = 1,
@showWhite bit = 1,
@loggedUserID smallint,
@firmID int = null,
@headCompanyID int = null,
@advertTypeID int = null
)
AS
SET NOCOUNT ON

Declare @rollerStat TABLE(
rollerID int, 
date datetime, 
userID smallint, 
firmID smallint)


declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select x.massmediaID, x.myMassmedia, x.foreignMassmedia 
from dbo.fn_GetMassmediasForUser(@loggedUserID) x
	inner join dbo.fn_CreateTableFromString(@massmediaString) y on x.massmediaID = y.ID

Declare @res1 TABLE(
rollerID int, 
date datetime, 
userID smallint, 
firmID smallint,
massmediaID smallint)

Insert Into @res1(rollerID, date, userID, firmID, massmediaID)
Select 
	i.rollerID,
	tw.dayOriginal,
	a.userID,
	a.firmID,
	tw.massmediaID
FROM 
	Issue i
	inner join TariffWindow tw on i.originalWindowID = tw.windowID
	INNER JOIN Campaign c ON c.campaignID = i.campaignID
	INNER JOIN [Action] a ON a.actionID = c.actionID
	INNER JOIN Firm f ON f.firmID = a.firmID
	INNER JOIN PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
	INNER JOIN Roller r On r.rollerID = i.rollerID
	LEFT JOIN AdvertType adt on adt.advertTypeID = r.advertTypeID
	inner join 
	(
		select distinct u.userID 
		from [User] u
			left join [GroupMember] gm on u.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id	
		where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
	) as x on a.userID = x.userID
	--inner join @massmedias mm on tw.massmediaID = mm.massmediaID
where
	--(a.userID = @loggedUserID and mm.myMassmedia = 1 
	--	or a.userID <> @loggedUserID and mm.foreignMassmedia = 1) and
	tw.dayOriginal between @startDate and @finishDate and 
	a.userID = Coalesce(@userID, a.userID) 
	AND a.[isConfirmed] = 1
	and a.firmID = coalesce(@firmID, a.firmID)
	and f.headCompanyID = coalesce(@headCompanyID, f.headCompanyID)
	and (@advertTypeID Is Null Or r.advertTypeID = @advertTypeID Or adt.parentID = @advertTypeID)
	AND (pt.isHidden = 0 or @isHideWhite = 0) And
		(pt.isHidden = 1 or @isHideBlack = 0) and
	((pt.IsHidden = 1 and @showBlack = 1)  or
	(pt.IsHidden = 0 and @showWhite = 1)) 
group by i.rollerID, tw.dayOriginal, a.userID, a.firmID, tw.massmediaID, i.issueID

--for optimize
insert into @rollerStat
        ( rollerID, date, userID, firmID )
select r.rollerID, r.date, r.userID, r.firmID from @res1 r 
	inner join @massmedias mm on r.massmediaID = mm.massmediaID
where (r.userID = @loggedUserID and mm.myMassmedia = 1 
		or r.userID <> @loggedUserID and mm.foreignMassmedia = 1)
	
IF @splitByManager = 0
	select
		row_number() over(order by quantity desc) as RowNum,
		rollerID,
		rollerDescription,
		quantity,
		firmDescription,
		firmID,
		dbo.fn_BrandListByRollerId(rollerID) as brandList,
		dbo.fn_ProductListByRollerId(rollerID) as productList,
		compositionAuthor,
		compositionName,
		duration,
		dbo.fn_Int2Time(duration) as durationString,
		dbo.fn_Int2Time(quantity * duration) as sumDuration,
		advertTypeName,
		isCommon,
		isMute,
		parentID
	FROM (
		SELECT 
			r.rollerID,
			r.name as rollerDescription,
			count(*) as quantity,
			f.name as firmDescription,
			f.firmID,
			r.compositionAuthor,
			r.compositionName,
			r.duration,
			at.name as advertTypeName,
			r.isCommon,
			r.isMute,
			r.parentID
		FROM 
			@rollerStat rs
			INNER JOIN Roller r ON r.rollerID = rs.rollerID
			INNER JOIN Firm f ON f.firmID = rs.firmID
			LEFT JOIN AdvertType at ON r.advertTypeID = at.advertTypeID
		GROUP BY
			r.rollerID,
			r.name,
			f.name,
			f.firmID,
			r.duration,
			r.compositionAuthor,
			r.compositionName,
			at.name,
			r.isCommon,
			r.isMute,
			r.parentID
	) ss
	ORDER BY 
		quantity desc
ELSE
	select
		row_number() over(order by quantity desc) as RowNum,
		rollerID,
		rollerDescription,
		quantity,
		firmDescription,
		firmID,
		dbo.fn_BrandListByRollerId(rollerID) as brandList,
		dbo.fn_ProductListByRollerId(rollerID) as productList,
		userName,
		userID,
		compositionAuthor,
		compositionName,
		duration,
		dbo.fn_Int2Time(duration) as durationString,
		dbo.fn_Int2Time(quantity * duration) as sumDuration,
		advertTypeName,
		isCommon,
		isMute,
		parentID
	FROM (
		SELECT 
			r.rollerID,
			r.name as rollerDescription,
			count(*) as quantity,
			f.firmID,
			f.name as firmDescription,
			u.lastName + Space(1) + u.firstName as userName,
			u.userID,
			r.compositionAuthor,
			r.compositionName,
			r.duration,
			at.name as advertTypeName,
			r.isCommon,
			r.isMute,
			r.parentID
		FROM 
			@rollerStat rs
			INNER JOIN Roller r ON r.rollerID = rs.rollerID
			INNER JOIN [User] u ON rs.userID = u.userID
			INNER JOIN Firm f ON f.firmID = rs.firmID
			LEFT JOIN AdvertType at ON r.advertTypeID = at.advertTypeID
		GROUP BY
			r.rollerID,
			r.name,
			f.name,
			f.firmID,
			u.lastName + Space(1) + u.firstName,
			u.userID,
			r.compositionAuthor,
			r.compositionName,
			r.duration,
			at.name,
			r.isCommon,
			r.isMute,
			r.parentID
	) ss
	ORDER BY 
		quantity desc

-- Add additional data in case when day by day data is required
IF @splitByDays = 1 Begin
	SELECT * FROM @rollerStat
	SELECT DISTINCT date FROM @rollerStat ORDER BY date
END
ELSE BEGIN -- Return 2 fake recordsets
	SELECT 1
	SELECT 1
END
