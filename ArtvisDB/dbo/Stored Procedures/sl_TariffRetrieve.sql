

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
-- при одинаковом времени первым идёт тариф, действующий раньше в неделе (будни выше выходных):
-- дни тарифов в одно время не пересекаются, поэтому первый день недели однозначен.
ORDER BY
	t.[time] asc,
	CASE WHEN t.monday = 1 THEN 1 WHEN t.tuesday = 1 THEN 2 WHEN t.wednesday = 1 THEN 3
		WHEN t.thursday = 1 THEN 4 WHEN t.friday = 1 THEN 5 WHEN t.saturday = 1 THEN 6
		WHEN t.sunday = 1 THEN 7 ELSE 8 END asc,
	t.tariffID asc
Drop Table #Tariff




