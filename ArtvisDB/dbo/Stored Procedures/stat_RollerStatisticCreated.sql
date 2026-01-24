
CREATE PROCEDURE [dbo].[stat_RollerStatisticCreated]
(
	@startDate DATETIME = NULL,
	@finishDate DATETIME = NULL,
	@userID smallint = NULL,
	@rollerName VARCHAR(32) = NULL
)
AS
BEGIN
	SET NOCOUNT ON;

	Select 
		r.rollerID as RowNum,
		r.NAME AS 'Имя',
		dbo.fn_Int2Time(r.[duration]) as 'Пр-ть', 
		f.name as 'Фирма',
		r.PATH AS 'Путь',
		u.lastName + Space(1) + u.firstName as 'Создатель'
	FROM 
		[StudioOrder] so
		INNER JOIN [Roller] r ON so.[rollerID] = r.[rollerID]
		LEFT JOIN Firm f ON f.firmID = r.firmID
		INNER JOIN [StudioOrderAction] soa ON soa.[actionID] = so.[actionID]
		LEFT JOIN [User] u ON u.userID = soa.userID
	WHERE
		r.[createDate] >= ISNULL(@startDate, r.[createDate]) AND 
		r.[createDate] < ISNULL(@finishDate,r.[createDate])  + 1
		AND soa.userID = Coalesce(@userID, soa.userID) 
		AND so.isComplete = 1
		AND	r.[name] LIKE ISNULL(@rollerName, '%') + '%'
END


