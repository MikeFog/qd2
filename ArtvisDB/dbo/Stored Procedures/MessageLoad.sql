CREATE PROC [dbo].[MessageLoad]
(
@name varchar(64) = null
)
as
SET NOCOUNT on
SELECT m.[message], m.name as messageName FROM [iMessage] m WHERE [name] = coalesce(@name, m.[name])

SELECT 
	mp.[name], m.name as messageName
FROM 
	[iMessageParameter] mp
	INNER JOIN [iMessage] m ON m.messageID = mp.messageID
WHERE
	m.name = coalesce(@name, m.[name])
ORDER BY
	ordinal

