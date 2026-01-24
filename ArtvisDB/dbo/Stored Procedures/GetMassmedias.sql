
CREATE PROCEDURE [dbo].[GetMassmedias]
(
	@actionID int = NULL,
	@agencyID int = NULL
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	SELECT 
		mm.prefix as massmediaPrefix,
		mm.[name] as massmediaName, 
		mm.[roltypeID] AS massmediaRolTypeId
	FROM vMassMedia mm 
	WHERE (mm.[massmediaID] IN (
			SELECT cp.[massmediaID]
			FROM [Campaign] cp 
			WHERE cp.[campaignTypeID] <> 4 
				AND cp.[actionID] = @actionID 
				AND cp.[agencyID] = @agencyID) 
		OR mm.[massmediaID] IN (
			SELECT m.[massmediaID]
			FROM 
				[Campaign] cp
				INNER JOIN [PackModuleIssue] i ON cp.[campaignID] = i.[campaignID]
				INNER JOIN [PackModuleContent] pmc ON i.[pricelistID] = pmc.[priceListID]
				INNER JOIN [Module] m ON pmc.[moduleID] = m.[moduleID]
			WHERE 
				cp.[actionID] = @actionID AND cp.[agencyID] = @agencyID ))
END
