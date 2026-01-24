CREATE                   PROC [dbo].[StudioOrders]
(
@actionID smallint = null,
@studioOrderID int = null,
@studioID smallint = null,
@userID smallint = null,
@isReadyOnly bit = 0,
@orderFinishDate datetime = null,
@startOfInterval datetime = null,
@endOfInterval datetime = null,
@startOfIntervalCompleted datetime = null,
@endOfIntervalCompleted datetime = null,
@agenciesIDString varchar(1024) = null,
@firmID smallint = null,
@isHideWhite BIT = 0,
@isHideBlack BIT = 0,
@showBlack bit = 1,
@showWhite bit = 1,
@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

CREATE TABLE #Agency(agencyID smallint)
CREATE Table #PaymentType (paymentTypeID smallint)

-- Populate temporary tables with Agency and Payment types
IF @agenciesIDString Is Null
	INSERT INTO #Agency 
	SELECT agencyID FROM Agency
Else
	Exec dbo.hlp_PopulateTableFromCommaSeparatedString '#Agency', @agenciesIDString 

declare @isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignSOActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupSOActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

SELECT distinct
	f.name as firmName,
	ISNULL(s.prefix + ' ', '') + s.name as studioName,
	ag.name as agencyName,
	pt.name as paymentTypeName,
	rs.name as rolStyleName,
	o.*,
	dbo.fn_Int2Time(r.duration) as sRollerDuration,
	rs.name,
	o.actionID,
	LTrim(rs.rolTypeID) + '_' + LTrim(s.studioID) as rolTypeID,
	us.firstName + Space(1) + us.lastName as creator,
	a.firmID as actionFirmID
FROM 
	[StudioOrder] o
	INNER JOIN StudioOrderAction a ON a.actionID = o.actionID	
	INNER JOIN Firm f ON f.firmID = a.firmID
	INNER JOIN vStudio s ON s.studioID = o.studioID
	INNER JOIN Agency ag ON ag.agencyID = o.agencyID
	INNER JOIN PaymentType pt ON pt.paymentTypeID = o.paymentTypeID
	INNER JOIN RolStyle rs ON rs.rolStyleID = o.rolStyleID
	LEFT JOIN [User] us ON o.userID = us.[userID]
	left join Roller r on o.rollerID = r.rollerID 
	left join GroupMember gm on us.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
where
	(us.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
	a.actionID = Coalesce(@actionID, a.actionID) And
	a.firmID = Coalesce(@firmID, a.firmID) And
	o.studioOrderID = Coalesce(@studioOrderID, o.studioOrderID) AND
	o.studioID = Coalesce(@studioID, o.studioID) AND
	(o.finishDate = @orderFinishDate OR @orderFinishDate Is Null) AND
	(o.isComplete = @isReadyOnly OR @isReadyOnly = 0) AND
	o.createDate >= Coalesce(@startOfInterval, o.createDate) And 
	o.createDate <= Coalesce(@endOfInterval, o.createDate) And
	ag.agencyID IN (Select agencyID From #Agency) AND
	-- Change a.userID on o.userID (нужно для фильтра - акт выполненных работ)
	o.userID = Coalesce(@userID, o.userID)	And
	(pt.isHidden = 1 or @isHideWhite = 0) And
	(pt.isHidden = 0 or @isHideBlack = 0) and
	((pt.IsHidden = 1 and @showBlack = 1)  or
	(pt.IsHidden = 0 and @showWhite = 1)) and
	((@startOfIntervalCompleted is null and @endOfIntervalCompleted is null) or
	(o.finishDate is not null  and 
	o.finishDate >= coalesce(@startOfIntervalCompleted, o.finishDate) and
	o.finishDate <= coalesce(@endOfIntervalCompleted, o.finishDate) ) )
ORDER BY
	o.finishDate Desc
