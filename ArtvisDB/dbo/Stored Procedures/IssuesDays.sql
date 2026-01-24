-- Logic Mast be on original days. Names leave old
CREATE             PROC [dbo].[IssuesDays]
(
@campaignID int,
@massmediaID smallint,
@rollerID int = NULL,
@issueDate DATETIME = NULL
)
AS
SET NOCOUNT ON

SELECT DISTINCT	
	@massmediaID as [massmediaID],
	@campaignID  as [campaignID], 	
	@rollerID  as [rollerID],
	Convert(varchar(10), tw.dayOriginal, 104) as name,
	tw.dayOriginal as issueDate,
	a.[userID] AS userID,
	a.deleteDate
FROM 
	Issue i 
	inner join TariffWindow tw on i.originalWindowID = tw.windowId
	inner join Campaign c on i.campaignID = c.campaignID
	inner join [Action] a on c.actionID = a.actionID
WHERE 
	i.[campaignID] = @campaignID AND
	i.rollerID = Coalesce(@rollerID, i.rollerID) and 
	(@issueDate is null or tw.dayOriginal = @issueDate)
ORDER BY
	tw.dayOriginal

