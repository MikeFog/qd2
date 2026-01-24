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
Declare @flag int

DECLARE cur_windows CURSOR FOR
Select windowId, windowNextId, windowDateOriginal
From TariffWindow 
Where massmediaID = @massmediaId And windowDateOriginal >= @startDate And windowDateOriginal < @finishDate And not [windowNextId] Is Null

Open cur_windows
Fetch Next From  cur_windows Into @windowId, @windowNextId, @windowDate1
While @@FETCH_STATUS = 0
	Begin
	
	Select TOP 1 @flag = [windowPrevId] From TariffWindow where massmediaID = @massmediaID And  [windowDateOriginal] > @windowDate1 Order by [windowDateOriginal] ASC
	If @flag Is Null
	Begin
		-- удаляем связь между окнами и отправляем уведомление траффик-менеджеру
		Update [dbo].[TariffWindow] Set windowNextId = Null Where windowId = @windowId
		Update [dbo].[TariffWindow] Set [windowPrevId] = Null Where windowId = @windowNextId
		
		Select @windowDate2 = windowDateOriginal From TariffWindow where windowId = @windowNextId
		Exec SayTrafficThatWindowLinkDeleted @loggedUserId, @massmediaId, @windowDate1, @windowDate2
	End
	Fetch Next From  cur_windows Into @windowId, @windowNextId, @windowDate1
	End

Close cur_windows
Deallocate cur_windows
