
CREATE FUNCTION [dbo].[fn_GetIssuePrice]
(
@rollerDuration int,
@tariffWindowPrice decimal(18, 2),
@issueRatio decimal(18, 10),
@position int,
@extraChargeFirst int,
@extraChargeSecond int,
@extraChargeLast int
)
RETURNS decimal(18, 2)
AS
Begin
	Declare @price decimal(18, 2)
	Set @price = 
		(@rollerDuration * @tariffWindowPrice * @issueRatio +
		case		
			when @position IN (-20, -15) then (@rollerDuration * @tariffWindowPrice * @issueRatio * @extraChargeFirst / 100.0)
			when @position IN (-10, -5)  then (@rollerDuration * @tariffWindowPrice * @issueRatio * @extraChargeSecond / 100.0)
			when @position IN (5, 10)    then (@rollerDuration * @tariffWindowPrice * @issueRatio * @extraChargeLast / 100.0)
			else 0
		end)
	Return IsNull(@price, 0)
End