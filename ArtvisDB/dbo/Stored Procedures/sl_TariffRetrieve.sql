

CREATE     PROCEDURE [dbo].[sl_TariffRetrieve]
AS
SET NOCOUNT ON

SELECT 
	t.*,
	CONVERT(varchar(5), t.[time], 108) as timeString, 
	dbo.fn_Int2Time(t.[duration]) as durationString, 
	dbo.fn_Int2Time(t.[duration_total]) as durationString_total, 
	'Тариф ' + CONVERT(varchar(5), t.[time], 108) as name ,
	cast(case when tu.tariffUnionID is null then 0 else 1 end as bit) as isUnionEnable,
	tu.tariffUnionID as tariffUnionID
FROM 
	[Tariff] t
	Inner Join #Tariff t2 On t.tariffId = t2.tariffId
	left join TariffUnion tu on t.tariffID = tu.tariffID
ORDER BY 
	t.[time] asc
Drop Table #Tariff




