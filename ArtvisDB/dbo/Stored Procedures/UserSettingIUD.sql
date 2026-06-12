CREATE PROCEDURE [dbo].[UserSettingIUD]
(
    @userID       SMALLINT,
    @settingName  VARCHAR(128),
    @settingValue NVARCHAR(500)
)
AS
SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM [dbo].[UserSetting] WHERE [userID] = @userID AND [settingName] = @settingName)
    UPDATE [dbo].[UserSetting]
    SET    [settingValue] = @settingValue
    WHERE  [userID]      = @userID
      AND  [settingName] = @settingName;
ELSE
    INSERT INTO [dbo].[UserSetting] ([userID], [settingName], [settingValue])
    VALUES (@userID, @settingName, @settingValue);
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[UserSettingIUD] TO PUBLIC
    AS [dbo];
