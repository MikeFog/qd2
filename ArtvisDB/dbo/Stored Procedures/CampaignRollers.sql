-- Created by GitHub Copilot in SSMS - review carefully before executing

/*
Modified by Denis Gladkikh (dgladkikh@fogsoft.ru) - add moduleID and packModuleID for roller subtitude
Performance refactoring by GitHub Copilot:
  - removed redundant SELECT DISTINCT (GROUP BY already ensures uniqueness)
  - replaced vRoller view with direct JOINs to Roller + AdvertType
  - added OPTION (RECOMPILE) to eliminate catch-all parameter sniffing issues
*/

CREATE PROC [dbo].[CampaignRollers]
(
    @campaignID         int,
    @issueDate          datetime = null,
    @moduleIssueID      int = null,
    @packModuleIssueID  int = null,
    @rollerId           int = null
)
AS
SET NOCOUNT ON;

SELECT
    i.campaignID,
    i.rollerID,
    c.massmediaID,
    @issueDate                                                              AS issueDate,
    r.name,
    dbo.fn_Int2Time(r.duration)                                             AS durationString,
    COUNT(i.issueID)                                                        AS [count],
    r.duration,
    r.path,
    CASE WHEN @moduleIssueID     IS NULL THEN NULL ELSE i.moduleIssueID     END AS moduleIssueID,
    CASE WHEN @packModuleIssueID IS NULL THEN NULL ELSE i.packModuleIssueID END AS packModuleIssueID,
    mi.moduleID,
    pmpl.packModuleID,
    r.isMute,
    at.name                                                                 AS advertTypeName,
    mi.modulePricelistID,
    a.deleteDate
FROM
    dbo.Issue i
    INNER JOIN dbo.TariffWindow        tw   ON i.originalWindowID    = tw.windowId
    INNER JOIN dbo.Campaign            c    ON c.campaignID          = i.campaignID
    INNER JOIN dbo.Action              a    ON a.actionID            = c.actionID
    INNER JOIN dbo.Roller              r    ON r.rollerID            = i.rollerID
    LEFT  JOIN dbo.AdvertType          at   ON at.advertTypeID       = r.advertTypeID
    LEFT  JOIN dbo.ModuleIssue         mi   ON mi.moduleIssueID      = i.moduleIssueID
    LEFT  JOIN dbo.PackModuleIssue     pmi  ON pmi.packModuleIssueID = i.packModuleIssueID
    LEFT  JOIN dbo.PackModulePriceList pmpl ON pmi.pricelistID       = pmpl.priceListID
                                           AND tw.dayOriginal BETWEEN pmpl.startDate AND pmpl.finishDate
WHERE
    i.campaignID = @campaignID
    AND (@moduleIssueID     IS NULL OR i.moduleIssueID     = @moduleIssueID)
    AND (@packModuleIssueID IS NULL OR i.packModuleIssueID = @packModuleIssueID)
    AND (@issueDate         IS NULL OR tw.dayOriginal      = @issueDate)
    AND (@rollerId          IS NULL OR i.rollerID          = @rollerId)
GROUP BY
    i.campaignID,
    i.rollerID,
    c.massmediaID,
    r.name,
    r.duration,
    r.path,
    CASE WHEN @moduleIssueID     IS NULL THEN NULL ELSE i.moduleIssueID     END,
    CASE WHEN @packModuleIssueID IS NULL THEN NULL ELSE i.packModuleIssueID END,
    mi.moduleID,
    pmpl.packModuleID,
    r.isMute,
    at.name,
    mi.modulePricelistID,
    a.deleteDate
ORDER BY
    r.name
OPTION (RECOMPILE);
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[CampaignRollers] TO PUBLIC
    AS [dbo];

