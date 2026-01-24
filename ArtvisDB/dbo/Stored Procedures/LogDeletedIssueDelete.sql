-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 15.02.2008
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[LogDeletedIssueDelete]
(
	@logId INT 
)
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM [LogDeletedIssue] WHERE [logId] = @logId 
END
