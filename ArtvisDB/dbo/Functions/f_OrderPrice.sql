
CREATE      FUNCTION dbo.f_OrderPrice
(
@duration int,
@tariffId int
)
RETURNS MONEY
AS
BEGIN

-- Get Pricelist for give Studio and Date -----------------
Declare	@price Money, @tariffType smallint

Select	@price = summa,
		@tariffType = tariffTypeID
From	StudioTariff
Where	tariffId = @tariffId

If	@tariffType = 1 Return @duration * IsNull(@price, 0)
Return	IsNull(@price, 0)

END



