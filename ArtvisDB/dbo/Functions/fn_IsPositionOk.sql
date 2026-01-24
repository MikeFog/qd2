



CREATE     FUNCTION dbo.fn_IsPositionOk
(
@positionId smallint,
@windowID int
)
RETURNS BIT
AS
BEGIN

Declare	@isFirstOccupied bit,	@isSecondOccupied bit,	@isLastOccupied bit

Select 
	@isFirstOccupied = isFirstPositionOccupied,
	@isSecondOccupied = isSecondPositionOccupied,
	@isLastOccupied = isLastPositionOccupied
From 
	TariffWindow 
Where 
	windowId = @windowID


If @isFirstOccupied = 1	And @positionId = -20 Return 0
If @isSecondOccupied = 1	And @positionId = -10 Return 0
If @isLastOccupied = 1	And @positionId = 10 Return 0

Return 1

END




