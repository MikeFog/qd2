
CREATE   PROC [dbo].[hlp_CompanyDiscountCalculate]
(
@massMediaID smallint,
@campaignTypeID tinyint,
@startDate datetime,
@tariffPrice money,
@discountValue float output
)
as
SET NOCOUNT on
select @discountValue = NULL

Select	
	@discountValue = dv.discount
From		
	DiscountRelease dr 
	Inner Join DiscountValue dv On dv.discountReleaseId = dr.discountReleaseId
Where	
	dr.[massmediaID] = @massMediaID and
	@startDate >= dr.startDate AND 
	(dr.finishDate IS NULL OR @startDate < dr.finishDate) and
	dv.summa <= @tariffPrice 
	AND 
	(
	(dr.[isForType1] = 1 And @campaignTypeID = 1)
	Or 	(dr.[isForType2] = 1 And @campaignTypeID = 2)
	Or 	(dr.[isForType3] = 1 And @campaignTypeID = 3)
	)
Set	@DiscountValue = IsNull(@DiscountValue, 1)


