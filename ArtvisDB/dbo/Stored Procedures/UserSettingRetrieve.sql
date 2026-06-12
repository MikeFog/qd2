CREATE PROCEDURE [dbo].[UserSettingRetrieve]
(
    @userID      SMALLINT,
    @settingName VARCHAR(128)
)
AS
SET NOCOUNT ON;

SELECT [settingValue]
FROM   [dbo].[UserSetting]
WHERE  [userID]      = @userID
  AND  [settingName] = @settingName;
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[UserSettingRetrieve] TO PUBLIC
    AS [dbo];
