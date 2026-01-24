

CREATE    PROC [dbo].[StudioTariffList]
(
@pricelistID smallint = null,
@tariffID int = null
)
as
SET NOCOUNT ON
SELECT 
	st.*,
	tt.name as tariffTypeName,
	rs.name,
	rs.name as rolStyleName
FROM 
	[StudioTariff] st
	INNER JOIN iStudioTariffType tt ON tt.tariffTypeID = st.tariffTypeID
	INNER JOIN RolStyle rs ON rs.rolStyleID = st.rolStyleID
WHERE
	st.pricelistID = COALESCE(@pricelistID, st.pricelistID)
	AND st.tariffID = COALESCE(@tariffID, st.tariffID)
ORDER BY
	rolStyleName

