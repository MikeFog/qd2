
CREATE PROCEDURE [dbo].[PackIssuesDays]
(
@campaignID INT,
@rollerID int = NULL,
@issueDate DATETIME = NULL
)
AS
SET NOCOUNT ON

SELECT DISTINCT
	@campaignID  as [campaignID], 	
	@rollerID  as [rollerID],
	CONVERT(varchar(10), [issueDate], 104) as name,
	CONVERT(datetime, CONVERT(varchar(10), [issueDate], 104), 104) as issueDate,
	a.deleteDate
FROM 
	[PackModuleIssue] i
	Inner Join Campaign c On c.campaignID = i.campaignID
	Inner Join Action a On a.actionID = c.actionID
WHERE i.[campaignID] = @campaignID 
	AND [rollerID] = ISNULL(@rollerID, rollerID) 
	AND [issueDate] = ISNULL(@issueDate, issueDate)
ORDER BY
	CONVERT(datetime, CONVERT(varchar(10), [issueDate], 104), 104)

