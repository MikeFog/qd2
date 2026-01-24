

CREATE PROC [dbo].[CheckPackModuleTariffWithMaxCapacity]
(
@pricelistID int, 
@level int
)
as
If Exists(
	Select 1 
	From
		PackModuleContent pmc
		Inner Join ModuleTariff mt On mt.modulePriceListID = pmc.modulePriceListID
		Inner Join Tariff t On t.tariffID = mt.tariffID
	Where
		pmc.pricelistID = @pricelistID
		And t.maxCapacity > 0 And t.maxCapacity < @level
	)
	Begin
	Select 1
	Return
	End
Select 0