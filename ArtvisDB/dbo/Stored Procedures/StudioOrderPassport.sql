

CREATE                    PROC [dbo].[StudioOrderPassport]
(
@studioOrderID int = Null
)
AS
SET NOCOUNT ON
-- Studios
SELECT DISTINCT
	s.[studioID] as id, 
	isnull(s.prefix, '') + ' ' + s.NAME AS name
FROM 	
	[vStudio] s
	LEFT JOIN StudioOrder so on so.StudioID = s.StudioID 
WHERE	
	dbo.f_IsActiveChildFilter(so.StudioID, s.isActive, 1) = 1
ORDER BY 
	NAME
	
	
-- Studio Agencies
SELECT 	
	a.[agencyID] as id, 
	a.[name],
	sa.studioID 
FROM 	
	[Agency] a
	INNER JOIN StudioAgency sa ON sa.agencyID = a.agencyID
	LEFT JOIN StudioOrder so on so.agencyID = a.agencyID and so.studioOrderID = @studioOrderID
WHERE	
	dbo.f_IsActiveChildFilter(so.StudioID, a.isActive, 1) = 1
ORDER BY 
	a.name
-- PaymentTypes
SELECT 
	pt.[paymentTypeID] as id, pt.[name] 
FROM 	
	[PaymentType] pt
	LEFT JOIN StudioOrder so on pt.paymentTypeID = so.paymentTypeID and so.studioOrderID = @studioOrderID
WHERE	
	dbo.f_IsActiveChildFilter(so.paymentTypeID, pt.isActive, 1) = 1
ORDER BY 
	pt.name

DECLARE @date datetime

If @studioOrderID Is Null
	Set @date = dbo.ToShortDate(getdate())
Else
	Select @date = createDate From StudioOrder Where studioOrderID = @studioOrderID

-- Select roltypes from latest pricelist for each studio
SELECT  DISTINCT
	spl.studioID, LTrim(rt.rolTypeID) + '_' + LTrim(spl.studioID) as id, 
	rt.name
FROM 
	StudioPricelist spl
	INNER JOIN StudioTariff st ON st.pricelistID = spl.pricelistID
	INNER JOIN RolStyle rs ON rs.rolStyleID = st.RolStyleID
	INNER JOIN iRolType rt ON rt.rolTypeID = rs.rolTypeID
	LEFT JOIN StudioOrder p on p.StudioOrderID = @StudioOrderID and p.rolStyleID = rs.RolStyleID
WHERE	
--	@date >= spl.startDate And @date <= spl.finishDate
	@date between spl.startDate And spl.finishDate
	and dbo.f_IsActiveChildFilter(p.RolStyleID, rt.isActive, 1) = 1
ORDER BY
	rt.name

-- RolStyles
SELECT
	LTrim(rs.rolTypeID) + '_' + LTrim(spl.studioID) as rolTypeID, 
	rs.rolStyleID as id, 
	rs.name
FROM 
	StudioPricelist spl
	INNER JOIN StudioTariff st ON st.pricelistID = spl.pricelistID
	INNER JOIN RolStyle rs ON rs.rolStyleID = st.RolStyleID
	LEFT JOIN StudioOrder p on p.StudioOrderID = @StudioOrderID and p.rolStyleID = rs.RolStyleID
WHERE	
--	@date >= spl.startDate And @date <= spl.finishDate
	@date between spl.startDate And spl.finishDate
	and dbo.f_IsActiveChildFilter(p.RolStyleID, rs.isActive, 1) = 1
ORDER BY
	rs.name


