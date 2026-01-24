CREATE PROCEDURE [dbo].[LogsDeletedIssue]
(
	@startDate DATETIME = NULL,
	@finishDate DATETIME = NULL,
	@userId SMALLINT = null,
	@actionID int = null,
	@rollerCheckName nvarchar(64) = null
)
AS
BEGIN
	SET NOCOUNT ON;

	select 
		'Лог ' + CAST(l.[logId] AS VARCHAR(20)) AS name, 
		l.actionID, 
		r.[name] AS rollerName, 
		l.issueDate , 
		u.userName AS [user], 
		l.[date], 
		l.[logId] , 
		mm.[name] as mmName,
		mm.groupName
	FROM [LogDeletedIssue] l 
		left JOIN [User] u ON l.[userId] = u.[userID]
		left JOIN Roller r ON r.[rollerID] = l.rollerID
		left join vMassMedia mm on mm.massmediaID = l.massmediaID
	WHERE (@startDate IS NULL OR dbo.[ToShortDate](l.[date]) >= dbo.[ToShortDate](@startDate)) AND 
		(@finishDate IS NULL OR dbo.[ToShortDate](l.[date]) <= dbo.[ToShortDate](@finishDate)) AND
		u.[userID] = ISNULL(@userId, u.[userID])
		and l.actionId = coalesce(@actionID, l.actionId)
		and (@rollerCheckName is null or r.[name] like '%' + @rollerCheckName + '%')
	ORDER BY l.[date] desc
END
