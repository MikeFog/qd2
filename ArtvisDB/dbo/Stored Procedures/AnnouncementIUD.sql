

CREATE PROCEDURE [dbo].[AnnouncementIUD]
(
@announcementID int = null,
@dateCreated datetime = null,
@isConfirmationRequired bit = null,
@dateConfirmed datetime = null,
@fromUserID smallint = null,
@toUserID smallint = null,
@subject nvarchar(256) = null,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
IF @actionName = 'AddItem' 
begin
	insert into Announcement (announcementID, dateCreated, isConfirmationRequired, dateConfirmed, fromUserID, toUserID, subject) 
	values (@announcementID, @dateCreated, @isConfirmationRequired, @dateConfirmed,	@fromUserID, @toUserID, @subject) 
END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM Announcement WHERE announcementID = @announcementID
ELSE IF @actionName = 'UpdateItem' Begin
	UPDATE	
		Announcement
	SET
		dateCreated = @dateCreated,
		isConfirmationRequired = @isConfirmationRequired,
		dateConfirmed = @dateConfirmed,
		fromUserID = @fromUserID,
		toUserID = @toUserID,
		subject = @subject
	WHERE		
		announcementID = @announcementID
		
	exec AnnouncementList
		@announcementID = @announcementID

End



