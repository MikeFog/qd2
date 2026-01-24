



CREATE    FUNCTION fn_BrandListByRollerId
(
@rollerId int
)
RETURNS varchar(4000)
AS
BEGIN

DECLARE @brandList varchar(4000)
Set @brandList = ''
Select 
	@brandList  = @brandList + name + ', '
From
	Brand b
	Inner Join RollerBrand rb ON b.brandID = rb.brandID
Where
	rb.rollerID = @rollerId
Return @brandList

END




