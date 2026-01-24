
CREATE PROCEDURE [dbo].[CreateNotActivatedActionsAnnouncement]
AS
BEGIN
	SET NOCOUNT ON;
	SET DATEFIRST 1
	
	declare @today datetime, @weekday tinyint 
	set @today = dbo.ToShortDate(getdate())
	Set @weekday = DatePart(dw, @today)
	
	declare @actions table (actionID int primary key, userID smallint)
	
	insert into @actions (actionID,userID)
	select a.actionID, a.userID
	from [Action] a 
		where a.isConfirmed = 0 and a.isAlerted = 0
			and (datediff(day, @today, a.startDate) <= 1
				or (@weekday = 5 and  datediff(day, @today, a.startDate) <= 3 )
				or (@weekday = 6 and  datediff(day, @today, a.startDate) <= 2 ))
	
	insert into Announcement (dateCreated, isConfirmationRequired,fromUserID,toUserID,[subject]) 
	select getdate(), 0, a.userID, a.userID, 'Акция №' + cast(a.actionID as nvarchar) + ' не активирована.'
	from @actions a
						
	update a set a.isAlerted = 1 
	from [Action] a inner join @actions ac on a.actionID = ac.actionID
END



