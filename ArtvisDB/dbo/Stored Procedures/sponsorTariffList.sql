
CREATE      PROC [dbo].[sponsorTariffList]
(
@pricelistID smallint = Null,
@tariffID smallint = Null,
@time smalldatetime = Null,
@monday bit = Null,
@tuesday bit = Null,
@wednesday bit = Null,
@thursday bit = Null,
@friday bit = Null,
@saturday bit = Null,
@sunday bit = Null
)
as
SET NOCOUNT ON
SELECT 
	st.*,
	Convert(varchar(5), [time], 108) as timeString,
	dbo.fn_Int2Time([duration]) as tariffDuration,
	'Тариф ' + 	Convert(varchar(5), [time], 108) as [name] 
FROM 
	[SponsorTariff] st
WHERE
	st.[pricelistID] = Coalesce(@pricelistID, st.pricelistID)
	And st.tariffID = Coalesce(@tariffID, st.tariffID)
	And st.time = Coalesce(@time, st.time)
	And st.monday = Coalesce(@monday, st.monday)
	And st.tuesday = Coalesce(@tuesday, st.tuesday)
	And st.wednesday = Coalesce(@wednesday, st.wednesday)
	And st.thursday = Coalesce(@thursday, st.thursday)
	And st.friday = Coalesce(@friday, st.friday)
	And st.saturday = Coalesce(@saturday, st.saturday)
	And st.sunday = Coalesce(@sunday, st.sunday)
ORDER BY
	st.[time]






