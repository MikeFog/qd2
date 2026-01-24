/*
Получает данные для показа дней и выпусков в виде дерева для формы изменения позиционирования
*/
CREATE   PROC [dbo].[IssueChangePositioningPassport]
(
@campaignID int,
@campaignTypeID tinyint,
@objectID int = NULL,
@positionID int
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON


Exec CampaignDaysTreePassport @campaignID, @campaignTypeID, @objectID, @positionID = @positionID

--select positionId as Id, description as name from iIssuePosition Where positionId In(-20, -10, 0, 10)

