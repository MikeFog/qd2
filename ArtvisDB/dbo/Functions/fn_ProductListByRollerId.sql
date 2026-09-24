




CREATE     FUNCTION [dbo].[fn_ProductListByRollerId]
(
@rollerId int
)
RETURNS nvarchar(4000)
AS
BEGIN

Declare @productList nvarchar(1000)
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





