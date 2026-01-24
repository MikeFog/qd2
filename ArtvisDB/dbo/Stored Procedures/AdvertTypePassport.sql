
CREATE PROCEDURE [dbo].[AdvertTypePassport]
(
@parentID int = null,
@actionName varchar(32)
)
AS
BEGIN
	SET NOCOUNT ON;
	SELECT [advertTypeID] as Id, [name] From [AdvertType] Where [parentID] Is Null And @parentID Is NOT NULL Order By [name]
END
