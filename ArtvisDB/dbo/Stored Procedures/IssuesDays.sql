-- День выпуска: у линейных и спонсорских — день окна выхода (actualWindowID, куда выпуск перенёс трафик),
-- у модулей и пакетов — день модуля/пакета (правило 07.10.2026, docs/tasks/window-actual-switch.md).
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
	Convert(varchar(10), d.issueDay, 104) as name,
	d.issueDay as issueDate,
	a.[userID] AS userID,
	a.deleteDate
FROM 
	Issue i 
	inner join TariffWindow tw on i.actualWindowID = tw.windowId
	left join ModuleIssue mi on mi.moduleIssueID = i.moduleIssueID
	left join PackModuleIssue pmi on pmi.packModuleIssueID = i.packModuleIssueID
	cross apply (select issueDay = coalesce(mi.issueDate, pmi.issueDate, tw.dayOriginal)) d
	inner join Campaign c on i.campaignID = c.campaignID
	inner join [Action] a on c.actionID = a.actionID
WHERE 
	i.[campaignID] = @campaignID AND
	i.rollerID = Coalesce(@rollerID, i.rollerID) and 
	(@issueDate is null or d.issueDay = @issueDate)
ORDER BY
	d.issueDay
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[IssuesDays] TO PUBLIC
    AS [dbo];

