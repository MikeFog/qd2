CREATE PROCEDURE [dbo].[sp_GetAgencyPaintings]
	@agencyID int = null,
	@massmediaID int = null
AS
BEGIN
	SET NOCOUNT ON;

    select ap.painting, ap.paintingTypeID
	from AgencyPainting ap
	where ap.agencyID = coalesce(@agencyID, @massmediaID)
END
