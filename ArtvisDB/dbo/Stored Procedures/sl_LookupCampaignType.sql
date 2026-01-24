
CREATE PROC [dbo].[sl_LookupCampaignType]
as
SET NOCOUNT ON
select CampaignTypeId as Id, name from iCampaignType order by name

