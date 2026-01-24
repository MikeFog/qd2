CREATE              PROCEDURE [dbo].[rpt_ActionPrograms]
(
@actionID int = NULL
)
AS
SET NOCOUNT ON

select distinct m.name from
	Campaign c
    inner join Issue i on c.campaignID = i.campaignID
	inner join TariffWindow tw on i.actualWindowID = tw.windowId
	inner join Tariff t on tw.tariffId = t.tariffID
	inner join ModuleTariff mt on t.tariffID = mt.tariffID
	inner join ModulePriceList mpl on mt.modulePriceListID = mpl.modulePriceListID
	inner join Module m on mpl.moduleID = m.moduleID 
	inner join vMassmedia mm on m.massmediaID = mm.massmediaID
where c.actionID = @actionID 

