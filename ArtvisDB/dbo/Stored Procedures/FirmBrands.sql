

CREATE   PROC [dbo].[FirmBrands]
(
@firmID smallint
)
as
set nocount on
SELECT 
	b.[brandID], 
	b.[name],
	fb.firmID 
FROM 
	[Brand] b
	INNER JOIN FirmBrand fb ON fb.brandID = b.brandID
WHERE
	fb.firmID = @firmID
ORDER BY 
	b.[name]



