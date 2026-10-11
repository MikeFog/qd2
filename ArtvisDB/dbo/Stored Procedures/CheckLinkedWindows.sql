CREATE   procedure [dbo].[CheckLinkedWindows]
(
@startDate DATETIME, 
@finishDate DATETIME,
@massmediaId int,
@loggedUserId int
)
WITH EXECUTE AS OWNER
AS

Declare @windowId int, @windowNextId int, @windowDate1 datetime, @windowDate2 datetime

-- Этап 7 (docs/tasks/window-actual-switch.md, Р-8): объединение окон A → B рвётся, если между ними в эфире выходит
-- чужое окно станции (A.выход < C.выход < B.выход, интервал открытый). Раньше смотрели по времени по расписанию:
-- следующее после головы окно не было хвостом. Уведомление — во времени выхода.
DECLARE cur_windows CURSOR LOCAL STATIC FOR
Select a.windowId, a.windowNextId, a.windowDateActual, b.windowDateActual
From TariffWindow a
	inner join TariffWindow b on b.windowId = a.windowNextId
Where a.massmediaID = @massmediaId And a.windowDateActual >= @startDate And a.windowDateActual < @finishDate
	And a.dayActual >= convert(date, @startDate) And a.dayActual <= @finishDate And a.windowNextId is not null
	And exists (
		select 1 from TariffWindow c
		where c.massmediaID = @massmediaId
			and c.windowDateActual > a.windowDateActual
			and c.windowDateActual < b.windowDateActual
			and c.windowId <> a.windowId and c.windowId <> b.windowId)

Open cur_windows
Fetch Next From  cur_windows Into @windowId, @windowNextId, @windowDate1, @windowDate2
While @@FETCH_STATUS = 0
	Begin
	-- удаляем связь между окнами и отправляем уведомление траффик-менеджеру
	Update [dbo].[TariffWindow] Set windowNextId = Null Where windowId = @windowId
	Update [dbo].[TariffWindow] Set [windowPrevId] = Null Where windowId = @windowNextId

	Exec SayTrafficThatWindowLinkDeleted @loggedUserId, @massmediaId, @windowDate1, @windowDate2
	Fetch Next From  cur_windows Into @windowId, @windowNextId, @windowDate1, @windowDate2
	End

Close cur_windows
Deallocate cur_windows
