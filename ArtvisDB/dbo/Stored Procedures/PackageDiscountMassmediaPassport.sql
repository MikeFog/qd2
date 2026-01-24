-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 31.01.2008
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[PackageDiscountMassmediaPassport]
AS
BEGIN
	SET NOCOUNT ON;

	SELECT mm.[massmediaID] AS id, mm.nameWithGroup as name
	FROM [vMassmedia] mm WHERE mm.[isActive] = 1 ORDER BY mm.[name]
END
