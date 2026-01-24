CREATE PROC [dbo].[rpt_OrderActionBill]
(
@actionId int,
@agencyId smallint
)
AS
SET NOCOUNT ON

Declare @billDate datetime
Select @billDate = billDate
From StudioOrderBill
Where	actionID = @actionId And agencyID = @agencyId

SELECT
	aa.[name],
	aa.quantity,
	CAST(aa.quantity * tax AS MONEY) AS tax,
	aa.quantity * price AS price
FROM (
Select	
	rs.name,
	count( distinct so.studioOrderID ) as quantity,
	Case 
		when coalesce(avg(at.divisor), 0) < 0.0000001 Then 0
		Else so.finalPrice / avg(at.divisor) -- it can be only one
	End	 as tax,
	so.finalPrice as price
From		
	[StudioOrder] so
	INNER Join RolStyle rs On so.RolStyleID = rs.RolStyleID
	inner join PaymentType pt on so.paymentTypeID = pt.paymentTypeID
		and pt.IsHidden = 0
	left join dbo.AgencyTax at on so.agencyID = at.agencyID
		and ((so.finishDate is not null and so.finishDate between at.startDate and at.finishDate)
			or (so.finishDate is null and @billDate between at.startDate and at.finishDate))
Where	
	so.actionID = @actionId And
	so.agencyID = @agencyId
Group by 
	rs.name,
	so.finalPrice
) aa
