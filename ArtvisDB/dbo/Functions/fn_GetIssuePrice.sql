







CREATE          FUNCTION fn_GetIssuePrice
(
@rollerDuration int,
@tariffWindowPrice money,
@issueRatio float,
@position float,
@extraChargeFirst int,
@extraChargeSecond int,
@extraChargeLast int
)
RETURNS money
AS
Begin
/*
Positions:
-20 - first
-10 - second
0 - undefined
10 - last
*/

Declare @price money
Set @price = 
	(@rollerDuration * @tariffWindowPrice * @issueRatio +
	case		
		when @position IN (-20, -15) 	then (Convert(float, @rollerDuration) * @tariffWindowPrice * @issueRatio * @extraChargeFirst / 100)
		when @position IN (-10, -5) then (Convert(float, @rollerDuration) * @tariffWindowPrice * @issueRatio * @extraChargeSecond / 100)
		when @position IN (5, 10) then (Convert(float, @rollerDuration) * @tariffWindowPrice * @issueRatio * @ExtraChargeLast / 100)
		else  0
	end)
Return IsNull(@price, 0)

End









