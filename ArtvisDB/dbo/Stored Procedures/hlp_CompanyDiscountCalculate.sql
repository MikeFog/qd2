
CREATE   PROC [dbo].[hlp_CompanyDiscountCalculate]
(
@massMediaID smallint,
@campaignTypeID tinyint,
@startDate datetime,
@tariffPrice decimal(18,2),
@discountValue decimal(9,4) output,
@discountReleaseID smallint = NULL output -- какой набор скидок дал скидку (NULL — ни один порог не пройден)
)
as
SET NOCOUNT on
select @discountValue = NULL, @discountReleaseID = NULL

-- Порог — наибольшая сумма, до которой дотягивает кампания
Select TOP 1
	@discountValue = dv.discount,
	@discountReleaseID = dr.discountReleaseID
From		
	DiscountRelease dr 
	Inner Join DiscountValue dv On dv.discountReleaseId = dr.discountReleaseId
Where	
	dr.[massmediaID] = @massMediaID and
	@startDate >= dr.startDate AND 
	@startDate < DATEADD(DAY, 1, dr.finishDate) and
	dv.summa <= @tariffPrice 
	AND 
	(
	(dr.[isForType1] = 1 And @campaignTypeID = 1)
	Or 	(dr.[isForType2] = 1 And @campaignTypeID = 2)
	Or 	(dr.[isForType3] = 1 And @campaignTypeID = 3)
	)
Order By
	dv.summa DESC
Set	@DiscountValue = IsNull(@DiscountValue, 1)