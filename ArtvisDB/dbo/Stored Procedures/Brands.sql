
CREATE  PROC [dbo].[Brands]
(
@firmID smallint = Null
)
as
set nocount on
IF @firmID Is Null
	SELECT 
		[Brand].*
	FROM 
		[Brand]
	ORDER BY 
		[name]
ELSE
	SELECT DISTINCT
		b.*
	FROM 
		[Brand] b
		INNER JOIN FirmBrand fb ON fb.brandID = b.brandID
	WHERE
		fb.firmID = @firmID
	ORDER BY 
		b.[name]



