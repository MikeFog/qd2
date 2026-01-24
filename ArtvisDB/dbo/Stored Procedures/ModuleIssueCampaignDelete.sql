
CREATE PROCEDURE [dbo].[ModuleIssueCampaignDelete]
(
	@moduleID SMALLINT,
	@campaignId INT,
	@loggedUserId SMALLINT,
	@actionName VARCHAR(32)
)
WITH EXECUTE AS OWNER
AS
begin
SET NOCOUNT on
	if @actionName <> 'DeleteItem'
		return
		
	exec CampaignModuleIssueDelete
			@campaignID = @campaignId, --  int
			@moduleId = @moduleID, --  int
			@loggedUserId = @loggedUserId, --  int
			@actionName = @actionName --  varchar(32)

end 




