
CREATE       PROC [dbo].[RollerStyles]
(
@RolStyleID smallint = NULL,
@ShowActive bit = 0
)
as
SET NOCOUNT ON
SELECT 
	rs.[rolStyleID], 
	rs.[name], 
	rs.IsActive
FROM 
	[RolStyle] rs
WHERE
	rs.[rolStyleID] = COALESCE(@RolStyleID, rs.[rolStyleID])
	and dbo.f_IsActiveFilter(rs.IsActive, @ShowActive) = 1
ORDER BY 
	rs.[name]

