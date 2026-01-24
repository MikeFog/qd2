-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 07.02.2008
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[RollersForModule]
(
	@startDate DATETIME,
	@finishDate DATETIME,
	@moduleID INT = NULL,
	@packModuleID INT = NULL,
	@campaignID int
)
AS
BEGIN
	SET NOCOUNT ON;

	CREATE TABLE #Roller(rollerID int)
	Declare @firmId int, @actionId int
	Select @firmId = a.firmID, @actionId = a.actionID From Campaign c Inner Join Action a on a.actionID = c.actionID Where c.campaignID = @campaignID
	
    IF @moduleID IS NOT NULL 
		Begin
			INSERT INTO #Roller
			select 
				distinct mpl.[rollerID]
			FROM 
				Module m
				INNER JOIN [ModulePriceList] mpl ON m.[moduleID] = mpl.[moduleID]
			WHERE 
				m.[moduleID] = @moduleID 
				AND mpl.[startDate] <= @finishDate AND mpl.[finishDate] >= @startDate
		End
	ELSE IF @packModuleID IS NOT NULL
		Begin
			INSERT INTO [#Roller] 
			select distinct pmpl.[rollerID]
			FROM [PackModule] pm 
				INNER JOIN [PackModulePriceList] pmpl ON pm.[packModuleID] = pmpl.[packModuleID]
			WHERE pm.[packModuleID] = @packModuleID AND pmpl.[startDate] <= @finishDate AND pmpl.[finishDate] >= @startDate
		End

	SELECT 
		r.*, 
		dbo.fn_Int2Time(r.[duration]) as durationString, 
		f.name as firmName,
		@actionId as actionId
	FROM 
		[vRoller] r
		INNER JOIN #Roller r2 ON r.rollerID = r2.rollerID
		LEFT JOIN Firm f ON f.firmID = r.firmID
	ORDER BY
		r.name
END

