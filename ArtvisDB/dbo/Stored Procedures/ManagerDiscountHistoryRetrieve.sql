CREATE PROC [dbo].[ManagerDiscountHistoryRetrieve]
(
    @showDifferentUsersOnly  BIT           = 1,
    @showActivatedOnly       BIT           = 1,   -- 1: только активированные (isConfirmed=1); 0: все
    @massmediaGroupId        INT           = NULL,
    @grantorId               SMALLINT      = NULL,
    @creatorId               SMALLINT      = NULL,
    @startOfInterval         DATETIME      = NULL,
    @endOfInterval           DATETIME      = NULL,
    @maxManagerDiscount      DECIMAL(18,4) = NULL, -- показывать только записи, где managerDiscount < @maxManagerDiscount
    @startOfCreateInterval   DATETIME      = NULL, -- начало интервала даты создания акции
    @endOfCreateInterval     DATETIME      = NULL  -- конец интервала даты создания акции
)
AS
SET NOCOUNT ON

SELECT 
    md.ManagerDiscountHistoryID,    
    u1.userName        AS grantorName,
    cm.actionID,
    u2.userName        AS creatorName,
    md.discountSetTime,
    md.managerDiscount,
    mm.name            AS massmediaName,
    mm.groupName,
    a.createDate,
    mdr.name           AS reason
FROM 
    dbo.ManagerDiscountHistory   md
    INNER JOIN dbo.[vUser]             u1  ON md.userID                     = u1.userID
    INNER JOIN dbo.Campaign            cm  ON cm.campaignID                  = md.campaignID
    INNER JOIN dbo.[Action]            a   ON cm.[actionID]                  = a.[actionID]
    INNER JOIN dbo.[vUser]             u2  ON a.userID                       = u2.userID
    LEFT  JOIN dbo.vMassMedia          mm  ON mm.massmediaID                 = cm.massmediaID
    LEFT  JOIN dbo.ManagerDiscountReason mdr ON mdr.managerDiscountReasonID = md.managerDiscountReasonID
WHERE
    (@showDifferentUsersOnly    = 0   OR u1.userID          <> u2.userID)
    AND (@showActivatedOnly     = 0   OR a.isConfirmed       = 1)
    AND (@massmediaGroupId      IS NULL OR mm.massmediaGroupID   = @massmediaGroupId)
    AND (@grantorId             IS NULL OR md.userID             = @grantorId)
    AND (@creatorId             IS NULL OR a.userID              = @creatorId)
    AND (@startOfInterval       IS NULL OR md.discountSetTime   >= @startOfInterval)
    AND (@endOfInterval         IS NULL OR md.discountSetTime   <= @endOfInterval)
    AND (@maxManagerDiscount    IS NULL OR md.managerDiscount    < @maxManagerDiscount)
    AND (@startOfCreateInterval IS NULL OR a.createDate         >= @startOfCreateInterval)
    AND (@endOfCreateInterval   IS NULL OR a.createDate         <= @endOfCreateInterval)
ORDER BY 
    md.[discountSetTime] desc