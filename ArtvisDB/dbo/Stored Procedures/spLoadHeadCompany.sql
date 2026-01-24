-- Хранимая процедура для загрузки данных из таблицы [HeadCompany]
-- По аналогии с PROC [dbo].[banks]
CREATE PROCEDURE [dbo].[spLoadHeadCompany]
    @HeadCompanyID INT = NULL,           -- Идентификатор головной организации (NULL для без фильтра)
    @HeadCompanyName NVARCHAR(200) = NULL -- Название головной организации (NULL для без фильтра)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [HeadCompanyID] AS [ID],
        [Name] AS [Name]
    FROM [HeadCompany]
    WHERE (@HeadCompanyID IS NULL OR [HeadCompanyID] = @HeadCompanyID)
      AND (@HeadCompanyName IS NULL OR [Name] LIKE @HeadCompanyName + '%')
    ORDER BY [Name];
END
