CREATE PROC [dbo].[massmediaList]
(
@massmediaID smallint = null,
@ShowActive bit = 1,
@startDate DATETIME = NULL,
@finishDate DATETIME = null,
@massmediaGroupID int = null,
@loggedUserID smallint = null,
@mediaPlusMassmediaID smallint = null,
@checkCanAdd bit = 0,
@agencyID smallint = null
)
AS
SET NOCOUNT ON 

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

IF @startDate IS NOT NULL OR @finishDate IS NOT NULL 
	SELECT DISTINCT
		mm.*,
		mmg.[name] as groupname2,
		mmg.massmediaGroupID as groupID		
	FROM 
		[vMassmedia] mm
		inner join @massmedias umm on mm.massmediaID = umm.massmediaID
			and (@checkCanAdd = 0 or umm.myMassmedia = 1)
		INNER JOIN [Pricelist] pl ON pl.[massmediaID] = mm.[massmediaID]
		INNER JOIN [Tariff] t ON pl.[pricelistID] = t.[pricelistID]
		INNER JOIN [TariffWindow] tw ON t.[tariffID] = tw.[tariffId]
		INNER JOIN [Issue] i ON tw.[windowId] = i.originalWindowID
		INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			AND ((@startDate IS NULL OR c.finishDate >= @startDate) 
				AND (@finishDate IS NULL OR c.[startDate] <= @finishDate) 
					AND c.finishDate IS NOT NULL AND c.[startDate] IS NOT NULL)
		left join MassmediaGroup mmg on mmg.massmediaGroupID = mm.massmediaGroupID
	WHERE
		mm.[massmediaID] = Coalesce(@massmediaID, mm.[massmediaID])
		and dbo.f_IsActiveChildFilter(@massmediaID, mm.isActive, @ShowActive) = 1
		and (@massmediaGroupID is null or (mm.massmediaGroupID = @massmediaGroupID))
		and (@mediaPlusMassmediaID is null or mm.mediaPlusMassmediaID = @mediaPlusMassmediaID)
		--and (@checkCanAdd = 0 or exists(select * from AgencyMassmedia am where am.massmediaID = mm.massmediaID))
		and (@agencyID is null or exists(select * from dbo.AgencyMassmedia am where am.massmediaID = mm.massmediaID and am.agencyID = @agencyID))
	ORDER BY 
		mm.[name]
ELSE
	SELECT --DISTINCT
		mm.*,
		mmg.[name] as groupname2,
		mmg.massmediaGroupID as groupID,
		m.painting
	FROM 
		[vMassmedia] mm
		INNER JOIN MassMedia m On m.massmediaID = mm.massmediaID
		inner join @massmedias umm on mm.massmediaID = umm.massmediaID
			and (@checkCanAdd = 0 or umm.myMassmedia = 1)
		left join MassmediaGroup mmg on mmg.massmediaGroupID = mm.massmediaGroupID
	WHERE
		mm.[massmediaID] = Coalesce(@massmediaID, mm.[massmediaID])
		and dbo.f_IsActiveChildFilter(@massmediaID, mm.isActive, @ShowActive) = 1
		and (@massmediaGroupID is null or (mm.massmediaGroupID = @massmediaGroupID))
		and (@mediaPlusMassmediaID is null or mm.mediaPlusMassmediaID = @mediaPlusMassmediaID)
		--and (@checkCanAdd = 0 or exists(select * from AgencyMassmedia am where am.massmediaID = mm.massmediaID))
		and (@agencyID is null or exists(select * from dbo.AgencyMassmedia am where am.massmediaID = mm.massmediaID and am.agencyID = @agencyID))
	ORDER BY 
		mm.[name]

