CREATE Proc [dbo].[MoveIssues2NewCampaign]
(
@oldCampaignId int,
@newCampaignId int,
@rollerId int = null,
@splitDate datetime = null,
@splitType int
)
AS

If @splitType = 1 -- split by period
	Update 
		Issue
	Set 
		campaignID = @newCampaignId
	From 
		Issue i
		Inner Join TariffWindow tw on i.originalWindowID = tw.windowId
	Where 
		campaignID = @oldCampaignId
		and tw.dayOriginal >= @splitDate
Else -- split by rollers
	Update 
		Issue
	Set 
		campaignID = @newCampaignId
	Where 
		campaignID = @oldCampaignId
		and rollerID = @rollerId