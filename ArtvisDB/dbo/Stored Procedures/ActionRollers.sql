




CREATE PROC [dbo].[ActionRollers]
(
@actionID int = null,
@rollerID int = null
)
WITH EXECUTE AS OWNER
AS
Begin

SET NOCOUNT ON

If @rollerID Is Null And @actionID Is Null
	BEGIN
		RAISERROR('RollerId And ActionId Is Null', 16, 1)
		RETURN
	END

If @rollerID Is Not Null
	Exec [dbo].[Rollers] @rollerID = @rollerID
Else
	SELECT DISTINCT
	--	i.[campaignID], 
		i.[rollerID],
	--	c.massmediaID,
		r.NAME,
		dbo.fn_Int2Time(r.[duration]) as durationString,
		COUNT(i.[issueID]) AS [count],
		r.duration,
		r.isCommon,
		r.isMute,
		r.parentID,
		advt.name as advertTypeName,
		@actionID as actionID
	FROM 
		[Issue] i
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
		INNER JOIN Campaign c ON c.campaignID = i.campaignID
		inner join Roller r on i.rollerID = r.rollerID
		left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID 
		left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
		left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
											and	tw.dayOriginal between pmpl.startDate and pmpl.finishDate
		LEFT JOIN AdvertType advt ON advt.advertTypeID = r.advertTypeID
	WHERE
		c.actionID = @actionID
	GROUP BY 
		--i.[campaignID], i.[rollerID], c.[massmediaID], r.[name], r.[duration], r.isCommon, r.isMute, advt.name
		i.[rollerID], r.[name], r.[duration], r.isCommon, r.isMute, r.parentID, advt.name
	ORDER BY
		r.name

End