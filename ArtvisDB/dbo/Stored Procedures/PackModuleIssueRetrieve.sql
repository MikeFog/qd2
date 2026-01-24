/*
Modified by Denis Gladkikh (dgladkikh@fogsoft.ru) 18.09.2008 add packModuleID for roller subtitude
*/
CREATE          PROC [dbo].[PackModuleIssueRetrieve]
(
@campaignId INT = NULL,
@packModuleId INT = NULL,
@packModuleIssueID INT = NULL,
@pricelistId INT = NULL,
@startDate DATETIME = NULL,
@finishDate DATETIME = NULL,
@showUnconfirmed BIT = 1,
@issueDate DATETIME = NULL
)
AS

SET NOCOUNT ON
SET DATEFIRST 1

IF @issueDate IS NOT NULL
	BEGIN
		SET @startDate = @issueDate
		SET @finishDate = @issueDate
	END
	
SELECT 
	pmi.[packModuleIssueID],
	pm.[name] + ' - ' + CONVERT(VARCHAR(10), pmi.[issueDate], 104) + ' - ' + r.name AS name,
	pm.[name] AS packModuleName,
	pmi.[issueDate],
	pmi.[campaignID],
	r.[name] AS rollerName,
	pmi.[isConfirmed],
	c.[actionID],
	r.[rollerID],
	r.[path],
	r.duration,
	DatePart(dw, pmi.issueDate)  as [weekday],
	pl.packModuleID,
	r.isMute,
	pmi.positionId,
	pmi.pricelistID,
	ip.[description] as issuePosition,
	r.advertTypeName,
	a.deleteDate
FROM 
	[PackModuleIssue] pmi
	INNER JOIN [Campaign] c ON pmi.[campaignID] = c.[campaignID]
	INNER JOIN [PackModulePriceList] pl ON pmi.[pricelistID] = pl.[priceListID]
	INNER JOIN [PackModule] pm ON pl.[packModuleID] = pm.[packModuleID]
	INNER JOIN [vRoller] r ON pmi.[rollerID] = r.[rollerID]
	Inner Join iIssuePosition ip On ip.positionId = pmi.positionId
	Inner Join Action a On a.actionID = c.actionID
WHERE
	pmi.[campaignID] = COALESCE(@campaignId, pmi.[campaignID])	
	AND pm.[packModuleID] = COALESCE(@packModuleId, pm.[packModuleID])
	AND pmi.[packModuleIssueID] = COALESCE(@packModuleIssueID, pmi.[packModuleIssueID])
	AND pmi.[pricelistID] = COALESCE(@pricelistId, pmi.[pricelistID])
	AND pmi.[issueDate] >= COALESCE(@startDate, pmi.[issueDate])
	AND pmi.[issueDate] <= COALESCE(@finishDate, pmi.[issueDate])
	AND (@showUnconfirmed = 1 OR pmi.[isConfirmed] = 1)
ORDER BY 
	pm.[name], pmi.[issueDate]
