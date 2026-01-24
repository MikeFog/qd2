




CREATE     FUNCTION [dbo].[fn_ProductListByRollerId]
(
@rollerId int
)
RETURNS varchar(4000)
AS
BEGIN

Declare @productList varchar(1000)
Set @productList = ''

Select	
	@productList = at.name
From 
	AdvertType at
	Inner Join Roller ra ON ra.advertTypeID = at.advertTypeID
WHERE	
	ra.rollerID = @rollerId

Return @productList

END





