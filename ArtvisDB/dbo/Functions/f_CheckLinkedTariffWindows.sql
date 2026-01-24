

CREATE  FUNCTION [dbo].[f_CheckLinkedTariffWindows]
(
@windowDate datetime,
@massmediaId int
)
RETURNS bit
AS
BEGIN

Declare @flag int
Select TOP 1 @flag = [windowNextId] From TariffWindow where massmediaID = @massmediaID And  [windowDateOriginal] < @windowDate Order by [windowDateOriginal] DESC
IF not @flag Is Null
begin		
	return 1
end 

RETURN 0	

END


