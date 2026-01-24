

CREATE   PROC dbo.BrandFirms
(
@brandID smallint,
@firmID smallint = null
)
AS
SET NOCOUNT ON
SELECT 
	f.*,
	fb.brandID
FROM
	Firm f
	INNER JOIN FirmBrand fb ON fb.firmID = f.firmID
WHERE
	fb.brandID = @brandID
	And fb.firmID = Coalesce(@firmID, fb.firmID)
ORDER BY
	f.name


