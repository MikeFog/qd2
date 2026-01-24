
CREATE   PROCEDURE [dbo].[UserAdditionalMenus]
    @userID smallint,
    @languageCode NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @isAdmin bit;
    SET @isAdmin = dbo.f_IsAdmin(@userID);

    SELECT 
        m.menuID,
        @userID AS userID, 
        CASE 
            WHEN um.isGrant IS NOT NULL THEN 'tick.png' 
            ELSE 'tick_circle.png' 
        END AS img,
        CAST(
            CASE
                WHEN @isAdmin = 0 AND (
                    (gMenus.isSelected IS NULL AND (um.isGrant IS NULL OR um.isGrant = 0))
                    OR (gMenus.isSelected IS NOT NULL AND um.isGrant = 0)
                ) THEN 0
                ELSE 1
            END AS Bit
        ) AS isObjectSelected,
        CASE 
            WHEN @languageCode = 'es' AND m.name_es IS NOT NULL THEN m.name_es
            ELSE m.name
        END AS name
    FROM iMenu m 
    LEFT JOIN UserAdditionMenu um ON m.menuID = um.menuID AND um.userID = @userID
    LEFT JOIN (
        SELECT gmen.menuID, 
               CAST(CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END AS bit) AS isSelected 
        FROM GroupMenu gmen 
        INNER JOIN GroupMember gm ON gmen.groupID = gm.groupID
        WHERE gm.userID = @userID
        GROUP BY gmen.menuID
    ) AS gMenus ON m.menuID = gMenus.menuID
    WHERE @isAdmin = 1 OR gMenus.isSelected IS NOT NULL OR um.isGrant IS NOT NULL;
END;
