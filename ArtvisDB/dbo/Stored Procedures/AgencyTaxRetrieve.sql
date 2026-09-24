CREATE   PROC [dbo].[AgencyTaxRetrieve]
(
@agencyId smallint = null,
@agencyTaxId smallint = null,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tAgencyTax NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Налог для агентства ''');

Select 
	a.*,
	t.name as taxName,
	@tAgencyTax + ag.name + '''' as name
From
	AgencyTax a
	Inner Join iTax t On a.taxId = t.taxId
	Inner Join Agency ag On a.agencyId = ag.agencyId
Where
	a.agencyId = Coalesce(@agencyId, a.agencyId) And
	a.agencyTaxId = Coalesce(@agencyTaxId, a.agencyTaxId)
Order by
	a.startDate desc
