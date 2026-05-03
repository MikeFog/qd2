CREATE PROC [dbo].[Campaigns]
(
@actionID int = null,
@campaignID int = null,
@massmediaID smallint = null,
@loggedUserID smallint = null
)
as
set nocount on

IF (@actionID IS NOT NULL OR @campaignID IS NOT NULL /* (@campaignID IS NOT NULL AND @massmediaID IS NULL AND @actionID IS NULL)*/)
begin
	SELECT
		cm.*,
		CASE cm.[campaignTypeID]
			WHEN 4 THEN 'Пакетная модульная кампания'
			ELSE mm.NAME + isnull(' (' + mg.name +')', '')
		END AS name	,
		mm.name as massmediaName,
		f.[name] AS firmName,
		ct.name as campaignTypeName,
		pt.name as paymentTypeName,
		ag.name as agencyName,
		dbo.fn_Int2Time(cm.issuesDuration) as issuesDurationString,
		u.lastName + ' ' + u.firstName as modUserName,
		CASE cm.[campaignTypeID]
			WHEN 1 THEN 91
			WHEN 2 THEN 93
			WHEN 3 THEN 92
			WHEN 4 THEN 171
		END AS entityId,
		CASE cm.[campaignTypeID]
			WHEN 4 THEN CAST(1 AS DECIMAL(9,4))
			ELSE a.[discount]
		END AS packDiscount,
		CASE cm.[campaignTypeID]
			WHEN 4 THEN cm.[finalPrice]
			ELSE CAST(cm.[finalPrice] * a.[discount] AS DECIMAL(9,2))
		END AS fullPrice,
		mg.name as groupName,
		a.deleteDate
	FROM
		[Campaign] cm WITH (NOLOCK)
		INNER JOIN [Action] a WITH (NOLOCK) ON cm.[actionID] = a.[actionID]
		INNER JOIN [Firm] f WITH (NOLOCK) ON a.[firmID] = f.[firmID]
		LEFT JOIN vMassMedia mm WITH (NOLOCK) ON mm.massmediaID = cm.massmediaID
		LEFT JOIN MassmediaGroup mg WITH (NOLOCK) on mg.massmediaGroupID = mm.massmediaGroupID
		INNER JOIN iCampaignType ct WITH (NOLOCK) ON ct.campaignTypeID = cm.campaignTypeID
		INNER JOIN PaymentType pt WITH (NOLOCK) ON pt.paymentTypeID = cm.paymentTypeID
		LEFT JOIN Agency ag WITH (NOLOCK) ON ag.agencyId = cm.agencyId
		LEFT OUTER JOIN [User] u WITH (NOLOCK) ON u.userId = cm.modUser
	WHERE
		cm.actionID = COALESCE(@actionID, cm.actionID) AND
		cm.campaignID = COALESCE(@campaignID, cm.campaignID)
	ORDER BY
		cm.campaignID
end
ELSE
begin
	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id)
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

	SELECT distinct
		cm.*,
		CASE cm.[campaignTypeID]
			WHEN 4 THEN 'Пакетная модульная кампания'
			ELSE mm.NAME + isnull(' (' + mg.name +')', '')
		END AS name	,
		mm.name as massmediaName,
		f.[name] AS firmName,
		ct.name as campaignTypeName,
		pt.name as paymentTypeName,
		ag.name as agencyName,
		dbo.fn_Int2Time(cm.issuesDuration) as issuesDurationString,
		u.lastName + ' ' + u.firstName as modUserName,
		CASE cm.[campaignTypeID]
			WHEN 1 THEN 91
			WHEN 2 THEN 93
			WHEN 3 THEN 92
			WHEN 4 THEN 171
		END AS entityId,
		NULL AS packmodulemassmediaID,
		a.[discount] AS packDiscount,
		CAST(cm.[finalPrice] * a.[discount] AS DECIMAL(9,2)) AS fullPrice,
		mg.name as groupName,
		a.deleteDate
	FROM
		[Campaign] cm WITH (NOLOCK)
		INNER JOIN [Action] a WITH (NOLOCK) ON cm.[actionID] = a.[actionID]
		INNER JOIN [Firm] f WITH (NOLOCK) ON a.[firmID] = f.[firmID]
		INNER JOIN vMassMedia mm WITH (NOLOCK) ON mm.massmediaID = cm.massmediaID
			AND cm.massmediaID = COALESCE(@massmediaID, cm.massmediaID)
		LEFT JOIN MassmediaGroup mg WITH (NOLOCK) on mg.massmediaGroupID = mm.massmediaGroupID
		INNER JOIN iCampaignType ct WITH (NOLOCK) ON ct.campaignTypeID = cm.campaignTypeID
		INNER JOIN PaymentType pt WITH (NOLOCK) ON pt.paymentTypeID = cm.paymentTypeID
		LEFT JOIN Agency ag WITH (NOLOCK) ON ag.agencyId = cm.agencyId
		LEFT OUTER JOIN [User] u WITH (NOLOCK) ON u.userId = cm.modUser
		left join GroupMember gm WITH (NOLOCK) on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	where
		cm.actionID = COALESCE(@actionID, cm.actionID) AND
		cm.campaignID = COALESCE(@campaignID, cm.campaignID) AND
		a.isConfirmed = 1 AND cm.[campaignTypeID] <> 4
	union all
	(SELECT DISTINCT
		cm.*,
		'Пакетная модульня кампания' AS name,
		mm.name as massmediaName,
		f.[name] AS firmName,
		ct.name as campaignTypeName,
		pt.name as paymentTypeName,
		ag.name as agencyName,
		dbo.fn_Int2Time(cm.issuesDuration) as issuesDurationString,
		u.lastName + ' ' + u.firstName as modUserName,
		171 AS entityId,
		mm.massmediaID AS packmodulemassmediaID,
		CAST(1 AS DECIMAL(9,4)) AS packDiscount,
		CAST(cm.[finalPrice] AS DECIMAL(9,2)) AS fullPrice,
		mg.name as groupName,
		a.deleteDate
	FROM
		[Campaign] cm WITH (NOLOCK)
		INNER JOIN [Action] a WITH (NOLOCK) ON cm.[actionID] = a.[actionID]
		INNER JOIN [Firm] f WITH (NOLOCK) ON a.[firmID] = f.[firmID]
		INNER JOIN iCampaignType ct WITH (NOLOCK) ON ct.campaignTypeID = cm.campaignTypeID
			AND cm.[campaignTypeID] = 4
		INNER JOIN PaymentType pt WITH (NOLOCK) ON pt.paymentTypeID = cm.paymentTypeID
		LEFT JOIN Agency ag WITH (NOLOCK) ON ag.agencyId = cm.agencyId
		LEFT OUTER JOIN [User] u WITH (NOLOCK) ON u.userId = cm.modUser
		INNER JOIN [PackModuleIssue] pmi WITH (NOLOCK) ON pmi.campaignID = cm.campaignID
		INNER JOIN [PackModuleContent] pmc WITH (NOLOCK) ON pmc.pricelistID = pmi.pricelistID
		INNER JOIN Module m WITH (NOLOCK) ON m.moduleID = pmc.moduleID
		INNER JOIN vMassMedia mm WITH (NOLOCK) ON mm.massmediaID = m.massmediaID
			AND m.massmediaID = COALESCE(@massmediaID, m.massmediaID)
		LEFT JOIN MassmediaGroup mg WITH (NOLOCK) on mg.massmediaGroupID = mm.massmediaGroupID
		left join GroupMember gm WITH (NOLOCK) on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	where
		(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
		cm.actionID = COALESCE(@actionID, cm.actionID) AND
		cm.campaignID = COALESCE(@campaignID, cm.campaignID) AND
		a.isConfirmed = 1 AND cm.[campaignTypeID] = 4
	)
	ORDER BY
		cm.campaignID
end