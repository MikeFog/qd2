-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 06.11.2008
-- Description:	
-- =============================================
CREATE FUNCTION f_GetSponsorDuration 
(
	@rollerDuration int,
	@positionID smallint,
	@extraChargeFirst tinyint,
	@extraChargeSecond tinyint,
	@extraChargeLast tinyint
)
RETURNS int
AS
BEGIN
	declare @duration int
	select @duration = @rollerDuration + ((case 
											when @positionID in (-20, -15) then @rollerDuration * @extraChargeFirst
											when @positionID in (-10, -5) then @rollerDuration * @extraChargeSecond
											when @positionID in (10, 5) then @rollerDuration * @extraChargeLast
											else 0
										end) / 100 )
	return @duration
END
