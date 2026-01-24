CREATE   PROCEDURE [dbo].[PaymentCommonActions]
(
@paymentID int = null,
@managerID smallint = null,
@startOfInterval datetime = null,
@endOfInterval datetime = null,
@agencyID smallint = null,
@paymentTypeID smallint = null,
@firmID smallint = null,
--@paymentTypesIDString varchar(1024) = null,
@agenciesIDString varchar(1024) = NULL,
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
	IF @agencyID IS NULL
		INSERT INTO #Agency SELECT agencyID FROM Agency
	ELSE
		INSERT INTO #Agency VALUES(@agencyID)
Else
	Exec dbo.hlp_PopulateTableFromCommaSeparatedString '#Agency', @agenciesIDString

	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

select distinct
	psoa.*,
	'Оплата акции №' + LTrim(psoa.actionID) as name,
	f.name as firmName,
	a.name as agencyName,
	pt.name as paymentTypeName
FROM
	[PaymentAction] psoa
	INNER JOIN [Action] soa ON soa.actionID = psoa.actionID
	inner join [Campaign] c on soa.[actionID] = c.[actionID]
	INNER JOIN Payment pso ON pso.paymentID = psoa.paymentID
	INNER JOIN Firm f ON f.firmID = pso.firmID
	INNER JOIN Agency a ON a.agencyID = pso.agencyID
	INNER JOIN PaymentType pt ON pt.paymentTypeID = pso.paymentTypeID
	left join @massmedias umm on c.massmediaID = umm.massmediaID
	left join GroupMember gm on soa.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
where
	(soa.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
			(soa.isSpecial = 1 or (c.campaignTypeID <> 4 and umm.massmediaID is not null and ((soa.userID = @loggedUserID and umm.myMassmedia = 1) or (soa.userID <> @loggedUserID and umm.foreignMassmedia = 1) )) 
				or (c.campaignTypeID = 4 and not exists(select * 
														from PackModuleIssue pmi 
															inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
															inner join Module m on pmc.moduleID = m.moduleID
															left join @massmedias ummm on m.massmediaID = ummm.massmediaID
														where pmi.campaignID = c.campaignID and (ummm.massmediaID is null or 
															(soa.userID = @loggedUserID and umm.myMassmedia = 0) or
															 (soa.userID <> @loggedUserID and umm.foreignMassmedia = 0) )))) and	
	psoa.[paymentID] = Coalesce(@paymentID, psoa.[paymentID]) And
	pso.paymentDate BETWEEN Coalesce(@startOfInterval, pso.paymentDate)
		And Coalesce(dateadd(ss, -1, dateadd(day, 1, @endOfInterval)), pso.paymentDate)	And
	soa.userID = Coalesce(@managerID, soa.[userID]) And
	pso.agencyID IN (Select agencyID From #Agency) And
	soa.firmID = Coalesce(@firmID, soa.firmID) AND 
	(pt.isHidden = 0 or @isHideWhite = 0) And
	(pt.isHidden = 1 or @isHideBlack = 0) and
	((pt.IsHidden = 1 and @showBlack = 1)  or
	(pt.IsHidden = 0 and @showWhite = 1)) 
ORDER BY
	psoa.actionID desc
