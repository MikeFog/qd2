CREATE procedure [dbo].[AgencyPainting] 
(
@agencyID int
)
AS
SET NOCOUNT ON

select painting as dirPainting from Agency where agencyID = @agencyID