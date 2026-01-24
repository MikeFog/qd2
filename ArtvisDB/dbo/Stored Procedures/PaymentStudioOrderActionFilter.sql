CREATE  PROC [dbo].[PaymentStudioOrderActionFilter]
(
	@loggedUserID int
)
WITH EXECUTE AS OWNER
as
set nocount on 
-- 1. Manager
exec [UserListByRights]
	@loggedUserID = @loggedUserID, @forStudioOrders = 1 --  smallint

-- 2. Agency
select distinct a.agencyID as id, a.name
from Agency a
	left join PaymentStudioOrder pso on pso.agencyID = a.agencyID
where pso.agencyID is not null

-- 3. Payment Type
select p.paymentTypeID as id, p.name
from PaymentType p
where Exists (
	SELECT * FROM PaymentStudioOrder pso WHERE pso.paymentTypeID = p.paymentTypeID
)
