CREATE PROC [dbo].[sl_Rollers]
	@showMuteRollers bit = 0
AS
SET NOCOUNT ON

SELECT 
	r.*, 
	dbo.fn_Int2Time(r.[duration]) as durationString, 
	f.name as firmName,
	advt.name as advertTypeName
FROM 
	[Roller] r
	INNER JOIN #Roller r2 ON r.rollerID = r2.rollerID
	LEFT JOIN Firm f ON f.firmID = r.firmID
	LEFT JOIN AdvertType advt ON advt.advertTypeID = r.advertTypeID
where 
	(@showMuteRollers = 1 or r.isMute = 0)
ORDER BY
	r.name

