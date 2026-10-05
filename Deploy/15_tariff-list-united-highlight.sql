-- Список тарифов прайс-листа: объединённые тарифы (TariffUnion, «Объединить с блоком») подсвечены.
--
-- Раньше увидеть объединение можно было только в карточке каждого тарифа. Теперь sl_TariffRetrieve
-- (её вызывает Tariffs — список тарифов, сущность 81, и ответ TariffIUD после сохранения) отдаёт служебную
-- колонку row_style = 'united' для тарифа, объединённого любой стороной; список красит строку тем же
-- бледно-зелёным, что ячейку объединённого тарифа в трафик-менеджменте. Колонки нет в метаданных — в
-- таблице она не видна. Строки объединённых тарифов бывают не подряд (будни/выходные на одно время).
--
-- Идемпотентен (CREATE OR ALTER), данные не трогает. Подсветку рисуют новые Merlin.exe и веб; старый
-- клиент колонку игнорирует.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 15_tariff-list-united-highlight.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO


CREATE OR ALTER PROCEDURE [dbo].[sl_TariffRetrieve]
AS
SET NOCOUNT ON

SELECT 
	t.*,
	CONVERT(varchar(5), t.[time], 108) as timeString, 
	dbo.fn_Int2Time(t.[duration]) as durationString, 
	dbo.fn_Int2Time(t.[duration_total]) as durationString_total, 
	'Тариф ' + CONVERT(varchar(5), t.[time], 108) as name ,
	cast(case when tu.tariffUnionID is null then 0 else 1 end as bit) as isUnionEnable,
	tu.tariffUnionID as tariffUnionID,
	-- тариф объединён (TariffUnion) любой стороной — строка подсвечивается в списке
	-- тем же цветом, что ячейка в трафик-менеджменте (TrafficGrid.MarkCellAsUnited)
	case when tu.tariffID is not null
		or exists (select 1 from TariffUnion tp where tp.tariffUnionID = t.tariffID)
		then 'united' end as row_style
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
GO
