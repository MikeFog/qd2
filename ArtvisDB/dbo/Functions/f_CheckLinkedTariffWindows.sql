

CREATE  FUNCTION [dbo].[f_CheckLinkedTariffWindows]
(
@windowDate datetime,
@massmediaId int
)
RETURNS bit
AS
BEGIN

-- Этап 7 (docs/tasks/window-actual-switch.md, Р-8): время @windowDate попадает внутрь эфирного промежутка
-- объединённой пары окон станции — голова A выходит раньше, хвост B позже (интервал открытый: та же минута, что у
-- головы или хвоста, — не вторжение). Раньше смотрели по времени по расписанию: «последнее окно до @windowDate
-- имеет продолжение». Поиск — в пределах суток до @windowDate (связи внутри дня); условие по dayActual держит план на
-- индексе (massmediaID, dayActual) при любом первом вызове — без него план мог читать всю историю станции.
IF EXISTS (
	select 1
	from TariffWindow a
		inner join TariffWindow b on b.windowId = a.windowNextId
	where a.massmediaID = @massmediaId
		and a.dayActual >= dateadd(day, -1, convert(date, @windowDate))
		and a.dayActual <= convert(date, @windowDate)
		and a.windowNextId is not null
		and a.windowDateActual < @windowDate
		and a.windowDateActual >= dateadd(day, -1, @windowDate)
		and b.windowDateActual > @windowDate)
	RETURN 1

RETURN 0

END

