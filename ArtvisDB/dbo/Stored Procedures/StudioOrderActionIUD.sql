








CREATE          PROCEDURE [dbo].[StudioOrderActionIUD]
(
@actionID int Out,
@firmID smallint = NULL,
@userID int = NULL,
@finishDate datetime = NULL,
@priceChange money = NULL,
@finalPriceChange money = NULL,
@actionName varchar(32),
@contacts VARCHAR(256) = null,
@orderStatusID int = NULL,
@loggedUserId SMALLINT = NULL
)
AS
SET NOCOUNT ON

IF @actionName = 'AddItem' begin

	INSERT INTO [StudioOrderAction](firmID, userID, [contacts])
	VALUES(@firmID, @userID, @contacts)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @actionID = SCOPE_IDENTITY()
	
	-- create message for media director from manager with this [StudioOrderAction]
	insert into Announcement ( dateConfirmed, fromRoleID, toRoleID, fromUserID, toUserID, subject) 
	values ( 	null, 4 /* менеджер */, 3 /* Режиссер */, @userID, NULL, N'Создана акция №' + cast(@actionID as varchar) + '.') 
	
	END
ELSE IF @actionName = 'DeleteItem' begin

	DELETE FROM [StudioOrderAction] WHERE actionID = @actionID

	-- delete messages for media director from manager for this [StudioOrderAction]
	delete	from Announcement
	where	fromRoleID = 1 /* Администратор */
				and subject like '%' + cast(@actionID as varchar) + '%'
	
	end
ELSE IF @actionName = 'UpdateItem' begin

	declare	@oldStatus INT, @oldUserID SMALLINT 
	select	top 1 @oldStatus = orderStatusID, @oldUserID = [userID]
	from		[StudioOrderAction] where actionID = @actionID

	IF @userID IS NULL 
		SET @userID = @oldUserID
		
	UPDATE	
		[StudioOrderAction]
	SET		
		firmID = @firmID,
		[contacts] = @contacts,
		[userID] = @userID,
		orderStatusID = isnull(@orderStatusID, 1)
	WHERE		
		actionID = @actionID
		
	SELECT @userID = soa.[userID] FROM [StudioOrderAction] soa WHERE soa.[actionID] = @actionID
	
	if (isnull(@oldStatus, -1) != @orderStatusID and @orderStatusID in (1))
	-- create message for media director from manager with this [StudioOrderAction]
	insert into Announcement ( dateConfirmed, fromRoleID, toRoleID, fromUserID, toUserID, subject) 
	values ( 	null, 4 /* менеджер */, 3 /* Режиссер */, @userID, NULL, N'Акция №' + cast(@actionID as varchar) + ' обновлена.' ) 
			
	if (isnull(@oldStatus, -1) != @orderStatusID and @orderStatusID in (3))
	-- create message for media director from manager with this [StudioOrderAction]
	insert into Announcement ( dateConfirmed, fromRoleID, toRoleID, fromUserID, toUserID, subject) 
	values ( 	null, 3 /* Режиссер */, 4 /* менеджер */, @loggedUserId, @userID, N'Акция №' + cast(@actionID as varchar) + ' обсуждена.' )
	
	if (isnull(@oldStatus, -1) != @orderStatusID and @orderStatusID in (2))
	-- create message for media director from manager with this [StudioOrderAction]
	insert into Announcement ( dateConfirmed, fromRoleID, toRoleID, fromUserID, toUserID, subject) 
	SELECT DISTINCT NULL, 4/* менеджер */, 3 /* Режиссер */, @userID, so.userID, N'Акция №' + cast(@actionID as varchar) + ' готова к производству роликов.' 
		FROM [StudioOrder] so WHERE so.actionID = @actionID
		
	end
ELSE IF @actionName = 'UpdatePrice'
	UPDATE	
		[StudioOrderAction]
	SET		
		tariffPrice = tariffPrice + @priceChange, 
		totalPrice = totalPrice + @finalPriceChange,
		finishDate = @finishDate
	WHERE		
		actionID = @actionID



















