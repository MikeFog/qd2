




CREATE  PROC dbo.NextBillOrContractNo
(
@agencyID smallint, 
@year smallint, 
@type tinyint = 1,
@nextValue smallint out
)
AS
SET NOCOUNT ON
If Not Exists (
	Select * From AgencyBillNo Where agencyId = @agencyID and [year] = @year
	)
	Insert Into AgencyBillNo(agencyId, [year]) Values(@agencyID, @year)

DECLARE @NextVal1 int, @NextVal2 int

SELECT		
	@NextVal1 = billNo,
	@NextVal2 = contractNo
FROM			
	AgencyBillNo
Where 		
	agencyId = @agencyID and [year] = @year

If	@Type = 1 Begin
	UPDATE	AgencyBillNo
	SET		billNo = billNo + 1
	Where 	agencyId = @agencyID and [year] = @year

	Set @nextValue = @NextVal1
	End
Else Begin
	UPDATE	AgencyBillNo
	SET		contractNo = contractNo + 1
	Where 	agencyId = @agencyID and [year] = @year

	Set @nextValue = @NextVal1
	End







