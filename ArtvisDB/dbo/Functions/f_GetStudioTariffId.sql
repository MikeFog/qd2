

CREATE	 FUNCTION dbo.f_GetStudioTariffId
(
@studioId smallint,
@rolstyleId int,
@theDate datetime
)
RETURNS INT
AS
BEGIN
Declare	@pricelistId int, @tariffId int

Select	@pricelistId = pricelistID
From	StudioPricelist
Where	studioID = @studioId and
		@theDate between startDate And finishDate

-- Get tariff ---------------------------------------------
Select	@tariffId = tariffID
From	StudioTariff
Where	pricelistID = @pricelistId and
		rolStyleID = @rolStyleId

RETURN @tariffId
END

