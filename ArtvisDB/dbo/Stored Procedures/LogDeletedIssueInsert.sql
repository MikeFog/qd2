-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 15.02.2008
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[LogDeletedIssueInsert]
(
	@loggedUserId SMALLINT,
	@actionId INT,
	@rollerID INT,
	@issueDate datetime,
	@massmediaID smallint 
)
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) VALUES (@loggedUserId, @actionId, @rollerID, @issueDate, @massmediaID) 
END

