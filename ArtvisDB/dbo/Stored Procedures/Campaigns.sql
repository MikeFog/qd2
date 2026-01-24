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
			WHEN 4 THEN CAST(1 AS DECIMAL(5,2)) 
			ELSE CAST(a.[discount] AS DECIMAL(5,2)) 
		END AS packDiscount,
		CASE cm.[campaignTypeID]
			WHEN 4 THEN CAST(cm.[finalPrice] AS MONEY) 
			ELSE CAST((cm.[finalPrice] * a.[discount]) AS MONEY) 
		END AS fullPrice,
		mg.name as groupName,
		a.deleteDate
	FROM 
		[Campaign] cm
		INNER JOIN [Action] a ON cm.[actionID] = a.[actionID]
		INNER JOIN [Firm] f ON a.[firmID] = f.[firmID]
		LEFT JOIN vMassMedia mm ON mm.massmediaID = cm.massmediaID
			AND cm.massmediaID = COALESCE(@massmediaID, cm.massmediaID)
		LEFT JOIN MassmediaGroup mg on mg.massmediaGroupID = mm.massmediaGroupID
		INNER JOIN iCampaignType ct ON ct.campaignTypeID = cm.campaignTypeID
		INNER JOIN PaymentType pt ON pt.paymentTypeID = cm.paymentTypeID
		LEFT JOIN Agency ag ON ag.agencyId = cm.agencyId
		LEFT OUTER JOIN [User] u ON u.userId = cm.modUser
	WHERE
		cm.actionID = COALESCE(@actionID, cm.actionID) AND
		cm.campaignID = COALESCE(@campaignID, cm.campaignID)
	ORDER BY
		cm.campaignID
end
ELSE 
begin 
	declare @isRightToViewForeignActions bit,	@isRightToViewGroupActions bit

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
		CAST(a.[discount] AS DECIMAL(5,2)) AS packDiscount,
		CAST((cm.[finalPrice] * a.[discount]) AS MONEY) AS fullPrice,
		mg.name as groupName,
		a.deleteDate
	FROM 
		[Campaign] cm
		INNER JOIN [Action] a ON cm.[actionID] = a.[actionID]
		INNER JOIN [Firm] f ON a.[firmID] = f.[firmID]
		INNER JOIN vMassMedia mm ON mm.massmediaID = cm.massmediaID
			AND cm.massmediaID = COALESCE(@massmediaID, cm.massmediaID)
		LEFT JOIN MassmediaGroup mg on mg.massmediaGroupID = mm.massmediaGroupID
		INNER JOIN iCampaignType ct ON ct.campaignTypeID = cm.campaignTypeID
		INNER JOIN PaymentType pt ON pt.paymentTypeID = cm.paymentTypeID
		LEFT JOIN Agency ag ON ag.agencyId = cm.agencyId
		LEFT OUTER JOIN [User] u ON u.userId = cm.modUser
		left join GroupMember gm on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	where
		--(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
		cm.actionID = COALESCE(@actionID, cm.actionID) AND
		cm.campaignID = COALESCE(@campaignID, cm.campaignID) AND
		a.isConfirmed = 1 AND cm.[campaignTypeID] <> 4
	union all
	-- Догружаем пакетно модульные кампании
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
		CAST(1 AS DECIMAL(5,2)) AS packDiscount,
		CAST((cm.[finalPrice]) AS MONEY) AS fullPrice,
		mg.name as groupName,
		a.deleteDate
	FROM 
		[Campaign] cm
		INNER JOIN [Action] a ON cm.[actionID] = a.[actionID]
		INNER JOIN [Firm] f ON a.[firmID] = f.[firmID]
		INNER JOIN iCampaignType ct ON ct.campaignTypeID = cm.campaignTypeID
			AND cm.[campaignTypeID] = 4
		INNER JOIN PaymentType pt ON pt.paymentTypeID = cm.paymentTypeID
		LEFT JOIN Agency ag ON ag.agencyId = cm.agencyId
		LEFT OUTER JOIN [User] u ON u.userId = cm.modUser
		INNER JOIN [PackModuleIssue] pmi ON pmi.campaignID = cm.campaignID
		INNER JOIN [PackModuleContent] pmc ON pmc.pricelistID = pmi.pricelistID
		INNER JOIN Module m ON m.moduleID = pmc.moduleID
		INNER JOIN vMassMedia mm ON mm.massmediaID = m.massmediaID
			AND m.massmediaID = COALESCE(@massmediaID, m.massmediaID)
		LEFT JOIN MassmediaGroup mg on mg.massmediaGroupID = mm.massmediaGroupID
		left join GroupMember gm on a.userID = gm.userID
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
