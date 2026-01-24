
CREATE PROCEDURE [dbo].[MassmediaGroups]
(
	@massmediaGroupID int = null
)
AS
BEGIN
	SET NOCOUNT ON;

    select * from MassmediaGroup mg where mg.massmediaGroupID = isnull(@massmediaGroupID, mg.massmediaGroupID)
END

