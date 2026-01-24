
CREATE PROCEDURE [dbo].[CampaignModuleIssuesRetrieve]
(
	@issueDate DATETIME = NULL,
	@campaignId INT,
	@moduleId INT
)
AS
begin
set nocount on
	SELECT DISTINCT
		(CONVERT(varchar(10), mi.[issueDate], 104) + ' - ' + r.[name])  as name,
		mi.[issueDate],
		@campaignID  as [campaignID],
		@moduleId  as moduleId,
		m.[massmediaID] AS massmediaID,
		mi.moduleIssueID,
		r.[name] as roller,
		r.advertTypeName,
		a.deleteDate
	FROM [ModuleIssue] mi 
		INNER JOIN [Module] m ON mi.[moduleID] = m.[moduleID] 
		inner join vRoller r on mi.rollerID = r.rollerID
		Inner Join Campaign c On c.campaignID = mi.campaignID
		Inner Join Action a On a.actionID = c.actionID
	WHERE mi.[campaignID] = @campaignId 
		AND mi.[moduleID] = @moduleId
		AND mi.[issueDate] = COALESCE(@issueDate, mi.[issueDate])
	order by 
	mi.[issueDate]
END

