CREATE proc [dbo].[ReportPartTextU]
(
@reportPartTextID int,
@reportText ntext
)
as
SET NOCOUNT ON
UPDATE 
	[dbo].[ReportPartText]
SET 
	[reportText] = @reportText
WHERE
	reportPartTextID = @reportPartTextID
