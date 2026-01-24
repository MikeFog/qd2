-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 24.09.2008
-- Description:	License Get
-- =============================================
CREATE PROCEDURE [dbo].[LicenseGet]
AS
BEGIN
	SET NOCOUNT ON;
	
	select top 1 [value] from iInternalVariable where [name] = 'License'
	select top 1 cast([value] as int) from iInternalVariable where [name] = 'LicenseLenght'
END


