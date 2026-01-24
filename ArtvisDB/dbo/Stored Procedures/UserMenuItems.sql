
CREATE PROC [dbo].[UserMenuItems]
(
    @userID smallint,
    @languageCode NVARCHAR(10)
)
AS
SET NOCOUNT ON;

DECLARE @isAdmin bit;
SET @isAdmin = dbo.f_IsAdmin(@userID);

SELECT 
    m.[menuID], 
    CASE 
        WHEN @languageCode = 'es' AND m.name_es IS NOT NULL THEN m.name_es
        ELSE m.name
    END AS name,
    m.[parentID], 
    m.[position], 
    m.[codeName],
    m.align,
    CAST(CASE 
        WHEN m.isPublic = 1 OR @isAdmin = 1
            OR (um.isGrant IS NOT NULL AND um.isGrant = 1) 
            OR (um.isGrant IS NULL AND gMenus.isSelected = 1) THEN 1 
        ELSE 0 
    END AS bit) AS [enabled],
    m.hotKey,
    m.imgResourcePath
FROM 
    [iMenu] m 
LEFT JOIN (
    SELECT 
        gmen.menuID, 
        CAST(CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END AS bit) AS isSelected 
    FROM 
        GroupMenu gmen 
    INNER JOIN 
        GroupMember gm ON gmen.groupID = gm.groupID
    WHERE 
        gm.userID = @userID
    GROUP BY 
        gmen.menuID
) AS gMenus ON m.menuID = gMenus.menuID
LEFT JOIN 
    UserAdditionMenu um ON m.menuID = um.menuID AND um.userID = @userID
WHERE 
    m.isObsolete = 0
ORDER BY 
    position;
