CREATE PROC [dbo].[ManagerDiscountHistoryRetrieve]
(
    @showDifferentUsersOnly BIT = 1,
    @massmediaGroupId INT = NULL,
    @grantorId SMALLINT = NULL,
    @creatorId SMALLINT = NULL,
    @startOfInterval DATETIME = NULL,
    @endOfInterval DATETIME = NULL
)
AS
SET NOCOUNT ON

SELECT 
    u1.userName AS grantorName,
    cm.actionID,
    u2.userName AS creatorName,
    md.discountSetTime,
    md.managerDiscount,
    mm.name AS massmediaName,
    mm.groupName,
    a.createDate
FROM 
    ManagerDiscountHistory md 
    INNER JOIN [vUser] u1 ON md.userID = u1.userID
    INNER JOIN Campaign cm ON cm.campaignID = md.campaignID
    INNER JOIN [Action] a ON cm.[actionID] = a.[actionID]
    INNER JOIN [vUser] u2 ON a.userID = u2.userID
    LEFT JOIN vMassMedia mm ON mm.massmediaID = cm.massmediaID
WHERE
    (@showDifferentUsersOnly = 0 OR u1.userID <> u2.userID)
    AND (@massmediaGroupId IS NULL OR mm.massmediaGroupID = @massmediaGroupId)
    AND (@grantorId IS NULL OR md.userID = @grantorId)
    AND (@creatorId IS NULL OR a.userID = @creatorId)
    AND (@startOfInterval IS NULL OR md.discountSetTime >= @startOfInterval)
    AND (@endOfInterval IS NULL OR md.discountSetTime <= @endOfInterval)
ORDER BY 
    md.[discountSetTime]