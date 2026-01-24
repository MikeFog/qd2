

CREATE    FUNCTION dbo.fn_IssuesInModule
(
@modulePricelistId int
)
RETURNS smallint
AS
BEGIN

Return (
	Select Count(*) 
	From ModuleTariff
	Where modulePriceListID = @modulePricelistId
)
	
END


