CREATE PROC [dbo].[IssuesByDate]
(
  @massmediaID smallint = NULL,
  @campaignID int = NULL,
  @rollerID int = NULL,
  @issueDate datetime = NULL,
  @issueId int = NULL,
  @moduleIssueID int = NULL,
  @packModuleIssueID int = NULL
)
AS
BEGIN
  SET NOCOUNT ON;

  IF @issueId IS NOT NULL
  BEGIN
    SELECT 
      i.*,
      r.[name],
      r.duration,
      tw.massmediaID,
      tw.tariffId,
      dbo.fn_Int2Time(r.duration) as durationString,
      c.actionID,
      ip.[description] as issuePosition,
      tw.windowDateActual as issueDate,
      tw.windowDateOriginal as issueDateOriginal,
      r.advertTypeName,
      a.deleteDate
    FROM dbo.Issue i
    JOIN dbo.TariffWindow tw ON i.actualWindowID = tw.windowId   -- окно выхода
    JOIN dbo.Campaign c      ON c.campaignID = i.campaignID
    JOIN dbo.[Action] a      ON a.actionID = c.actionID
    JOIN dbo.iIssuePosition ip ON ip.positionId = i.positionId
    JOIN dbo.vRoller r       ON r.rollerID = i.rollerID
    WHERE i.issueID = @issueId;
    RETURN;
  END;

  -- ВАЖНО: если massmediaID не задан, текущая логика и так вернёт пусто.
  -- Если нужно "все massmedia", скажи — сделаем корректно.
  IF @massmediaID IS NULL
    RETURN;

  SELECT 
    i.*,
    r.[name],
    r.duration,
    tw.massmediaID,
    tw.tariffId,
    dbo.fn_Int2Time(r.duration) as durationString,
    c.actionID,
    ip.[description] as issuePosition,
    tw.windowDateActual as issueDate,
    tw.windowDateOriginal as issueDateOriginal,
    r.advertTypeName,
    a.deleteDate
  FROM dbo.TariffWindow tw
  JOIN dbo.Issue i            ON i.actualWindowID = tw.windowId   -- окно выхода
  JOIN dbo.Campaign c         ON c.campaignID = i.campaignID
  JOIN dbo.[Action] a         ON a.actionID = c.actionID
  JOIN dbo.iIssuePosition ip  ON ip.positionId = i.positionId
  JOIN dbo.vRoller r          ON r.rollerID = i.rollerID
  LEFT JOIN dbo.ModuleIssue mi       ON mi.moduleIssueID = i.moduleIssueID
  LEFT JOIN dbo.PackModuleIssue pmi  ON pmi.packModuleIssueID = i.packModuleIssueID
  WHERE
      -- день выпуска: модуль/пакет — свой день, остальные — день окна выхода (как IssuesDays)
      (@issueDate IS NULL OR coalesce(mi.issueDate, pmi.issueDate, tw.dayOriginal) = @issueDate)
  AND c.massmediaID = @massmediaID
  AND (@campaignID IS NULL OR c.campaignID = @campaignID)
  AND (@rollerID   IS NULL OR i.rollerID = @rollerID)
  AND (@moduleIssueID IS NULL OR i.moduleIssueID = @moduleIssueID)
  AND (@packModuleIssueID IS NULL OR i.packModuleIssueID = @packModuleIssueID)
  ORDER BY tw.windowDateActual
  OPTION (RECOMPILE);  -- важно при куче optional-параметров
END