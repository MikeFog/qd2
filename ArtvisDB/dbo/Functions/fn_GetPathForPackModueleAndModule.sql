


CREATE FUNCTION [dbo].[fn_GetPathForPackModueleAndModule]
(
	@packModuleId int,
	@packModulePriceListId int,
	@moduleId int,
	@massmediaId int
)
RETURNS VARCHAR(256)
AS
BEGIN

Declare 
	@path varchar(256)

If @packModuleId Is Not Null
	Begin

	Select @path = Trim(path) from PackModule where packModuleID = @packModuleId
	if @path Is Not Null And Len(@path) > 0 return @path

	Select @path = m.path 
	from 
		Module m inner join PackModuleContent pmc on pmc.moduleID = m.moduleID And m.massmediaID = @massmediaId
	where 
		pmc.pricelistID = @packModulePriceListId
	return @path

	End

If @moduleId Is Not Null
	Begin
	Select @path = path from Module where moduleID = @moduleId
	return @path
	End

return null

END
