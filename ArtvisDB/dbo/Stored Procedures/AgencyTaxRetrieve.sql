CREATE   PROC [dbo].[AgencyTaxRetrieve]
(
@agencyId smallint = null,
@agencyTaxId smallint = null
)
AS
SET NOCOUNT ON

Select 
	a.*,
	t.name as taxName,
	'Налог для агентства ''' + ag.name + ''''
From
	AgencyTax a
	Inner Join iTax t On a.taxId = t.taxId
	Inner Join Agency ag On a.agencyId = ag.agencyId
Where
	a.agencyId = Coalesce(@agencyId, a.agencyId) And
	a.agencyTaxId = Coalesce(@agencyTaxId, a.agencyTaxId)
Order by
	a.startDate desc
