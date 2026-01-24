



CREATE     PROC [dbo].[ModuleTariffs]
(
@modulePriceListID smallint = NULL,
@pricelistID smallint = NULL,
@moduleID smallint = NULL 
)
AS
SET NOCOUNT ON
IF NOT @modulePriceListID IS NULL 
	SELECT 
		mt.[modulePriceListID], 
		t.pricelistID,
		mt.[tariffID],
		CONVERT(varchar(5), [time], 108) as timeString, 
		t.[time],
		t.[monday], 
		t.[tuesday], 
		t.[wednesday], 
		t.[thursday], 
		t.[friday], 
		t.[saturday], 
		t.[sunday], 
		t.[price], 
		dbo.fn_Int2Time([duration]) as durationString, 
		t.[duration],
		t.duration_total,
		t.[comment],
		isForModuleOnly,
		CONVERT(varchar(5), [time], 108) as NAME,
		t.[needExt],
		t.[needInJingle],
		t.[needOutJingle],
		t.[maxCapacity],
		t.[suffix],
		cast(case when tu.tariffUnionID is null then 0 else 1 end as bit) as isUnionEnable,
		tu.tariffUnionID as tariffUnionID
	FROM 
		[ModuleTariff] mt
		INNER JOIN Tariff t ON t.tariffID = mt.tariffID
		left join TariffUnion tu on t.tariffID = tu.tariffID
	WHERE
		mt.modulePriceListID = @modulePriceListID
	ORDER BY
		[time]
ELSE	
	SELECT 
		mt.[modulePriceListID], 
		t.pricelistID,
		mt.[tariffID],
		CONVERT(varchar(5), t.[time], 108) as timeString, 
		t.[time],
		t.[monday], 
		t.[tuesday], 
		t.[wednesday], 
		t.[thursday], 
		t.[friday], 
		t.[saturday], 
		t.[sunday], 
		t.[price], 
		dbo.fn_Int2Time(t.[duration]) as durationString, 
		t.[duration],
		t.[comment],
		t.isForModuleOnly,
		CONVERT(varchar(5), t.[time], 108) as NAME,
		t.[needExt],
		t.[needInJingle],
		t.[needOutJingle],
		t.[maxCapacity],
		t.[suffix],
		cast(case when tu.tariffUnionID is null then 0 else 1 end as bit) as isUnionEnable,
		tu.tariffUnionID as tariffUnionID
	FROM 
		[ModuleTariff] mt
		INNER JOIN Tariff t ON t.tariffID = mt.tariffID
		INNER JOIN ModulePricelist mps ON mps.modulePriceListID = mt.modulePriceListID
		left join TariffUnion tu on t.tariffID = tu.tariffID
	WHERE
		mps.moduleID = @moduleID
		AND mps.pricelistID = @pricelistID
	ORDER BY
		[time]




