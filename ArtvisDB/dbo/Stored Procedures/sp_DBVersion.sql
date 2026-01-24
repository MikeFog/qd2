

--
-- if @isSet = 1 than incrementing DB if @version is current DB version 
--
CREATE PROCEDURE [dbo].[sp_DBVersion]
(
	@version int,
	@isSet bit = 0
)
AS
BEGIN
	SET NOCOUNT ON;
	
	declare @PARAM_NAME char(9)
	select @PARAM_NAME = 'VersionDB'
	
	declare @currentVersion int 
	select top 1 @currentVersion = [value] from iInternalVariable where [name] = @PARAM_NAME

	if @currentVersion is null 
		set @currentVersion = 0

	if @currentVersion <> @version or (@currentVersion = 0 and @isSet = 0)
	begin 
		if @isSet = 1
			raiserror('Not correct DB version', 16, 1)
		else 
			select 0
		return
	end

    if @isSet = 1
    begin 
		declare @versionB binary(500)
		select @versionB = cast ((@version + 1) as binary(500))

		if exists(select * from iInternalVariable where [name] = @PARAM_NAME)
			update iInternalVariable set [value] = @versionB where [name] = @PARAM_NAME
		else
			insert into iInternalVariable values (@PARAM_NAME, @versionB)
    end 
    
    if @isSet = 0
		select 1
end


