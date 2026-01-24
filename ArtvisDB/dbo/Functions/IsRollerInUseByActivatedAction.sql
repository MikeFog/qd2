CREATE FUNCTION [dbo].[IsRollerInUseByActivatedAction]
(
@rollerId int
)
RETURNS BIT
AS
BEGIN
	Declare @res bit
	IF Exists(Select 1 from Issue i Where i.rollerID = @rollerId And Not i.activationDate is Null)
		Set @res = 1
	Else
		Set @res = 0

	Return @res
END