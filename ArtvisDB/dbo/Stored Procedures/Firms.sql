CREATE PROCEDURE [dbo].[Firms]
(
@firmID           SMALLINT    = NULL,
@headCompanyID    INT         = NULL,   -- новый параметр
@ShowActive       BIT         = 1,
@ShowInactive     BIT         = 0,
@lastDateBefore   DATETIME    = NULL,
@lastDateAfter    DATETIME    = NULL,
@userId           INT         = NULL,
@ShowWithAction   BIT         = 1,
@ShowWithoutAction BIT        = 1,
@name varchar(256) = null
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

	If @firmID Is Not Null
		Select @ShowActive = isIdle, @ShowInactive = ~isIdle From Firm where firmID = @firmID

    SELECT 
        f.*,
        ai.finishDate AS lastDate,
        u.userName   AS lastManager,
		hc.name as headCompanyName
    FROM 
        [Firm] f
		Inner Join HeadCompany hc on hc.headCompanyID = f.headCompanyID
        LEFT JOIN 
        (
            SELECT ActionId, FirmID, userID, finishDate
            FROM (
                SELECT
                    ActionId,
                    userID,
                    FirmID,
                    finishDate,
                    ROW_NUMBER() OVER (PARTITION BY FirmID ORDER BY finishDate DESC) AS rn
                FROM Action 
                WHERE userID = ISNULL(@userId, userID)
            ) AS RankedActions
            WHERE rn = 1
        ) AS ai 
            ON f.firmID = ai.firmID
        LEFT JOIN [User] u 
            ON u.userID = ai.userID
    WHERE
        f.firmID = COALESCE(@firmID, f.firmID)
        AND (@headCompanyID IS NULL OR f.HeadCompanyID = @headCompanyID)   -- фильтрация по новой колонке
        AND ((f.isIdle = 1 AND @ShowActive   = 1) OR (f.isIdle = 0 AND @ShowInactive = 1))
        AND ((ai.userID IS NULL AND @ShowWithoutAction = 1) OR (ai.userID IS NOT NULL AND @ShowWithAction = 1))
        AND (@lastDateBefore IS NULL OR @lastDateBefore > finishDate)
        AND (@lastDateAfter  IS NULL OR @lastDateAfter  < finishDate)
        AND (@userId IS NULL OR ai.userID = ISNULL(@userId, ai.userID))
		AND (@name IS NULL OR f.name LIKE '%' + @name + '%') 
    ORDER BY
        [name];
END
