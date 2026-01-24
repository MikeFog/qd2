CREATE PROC [dbo].[TaxRetrieve]
AS
SET NOCOUNT ON

Select 
	t.*
From
	iTax t
Order by 
	t.name

