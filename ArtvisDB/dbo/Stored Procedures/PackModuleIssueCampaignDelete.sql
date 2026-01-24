
CREATE PROCEDURE [dbo].[PackModuleIssueCampaignDelete]
(
	@campaignId INT = NULL,
	@packModuleId SMALLINT = NULL,
	@loggedUserId SMALLINT = NULL,
	@actionName varchar(32)	
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;
	
	exec CampaignPackDayDelete
		@campaignId = @campaignId, --  int
		@loggedUserID = @loggedUserId, --  smallint
		@packModuleId = @packModuleId,
		@actionName = @actionName --  varchar(32)

END



