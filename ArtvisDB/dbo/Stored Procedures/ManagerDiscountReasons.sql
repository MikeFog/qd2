





CREATE        PROC [dbo].[ManagerDiscountReasons]
(
@managerDiscountReasonID smallint = NULL
)
as
SET NOCOUNT ON
SELECT 
	*
FROM 
	ManagerDiscountReason
WHERE
	managerDiscountReasonID = IsNull(@managerDiscountReasonID, managerDiscountReasonID)
ORDER BY 
	name