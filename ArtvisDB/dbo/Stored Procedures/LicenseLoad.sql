-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 24.09.2008
-- Description:	License Load
-- =============================================
CREATE PROCEDURE [dbo].[LicenseLoad] 
(
	@license binary(500),
	@licenseLenght int
)
AS
BEGIN
	SET NOCOUNT ON;
	
	declare @variablename varchar(50), @variablelenghtname varchar(50)
	select @variablename = 'License', @variablelenghtname = 'LicenseLenght'

    if exists(select * from iInternalVariable where [name] = @variablename)
		update iInternalVariable set [value] = @license where [name] = @variablename
	else
		insert into iInternalVariable ([name], [value]) values (@variablename, @license)

    if exists(select * from iInternalVariable where [name] = @variablelenghtname)
		update iInternalVariable set [value] = cast(@licenseLenght as binary(500)) where [name] = @variablelenghtname
	else
		insert into iInternalVariable ([name], [value]) values (@variablelenghtname, cast(@licenseLenght as binary(500)))
END


