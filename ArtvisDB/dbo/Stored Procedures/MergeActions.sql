CREATE PROCEDURE [dbo].[MergeActions]
(
@firstActionID INT,
@secondActionID INT,
@liveActionID INT OUT,
@loggedUserID int
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

	IF @firstActionID = @secondActionID
		RETURN
		
	IF NOT EXISTS(
		SELECT * 
		FROM [Action] a1 
			INNER JOIN [Action] a2 ON a1.[firmID] = a2.[firmID]
						 AND a1.[userID] = a2.[userID] 
						 AND a1.[actionID] <> a2.[actionID]
		WHERE a1.[actionID] = @firstActionID 
			AND a2.[actionID] = @secondActionID)
		RETURN

	if not exists(select * from [Action] a1 
						inner join [Action] a2 on a1.isConfirmed = a2.isConfirmed 
				where a1.[actionID] = @firstActionID 
					and a2.[actionID] = @secondActionID)
	begin 
		raiserror('MergeInvalidAction', 16, 1)
		return
	end 

	DECLARE @deletedActionID INT
	
	IF @firstActionID < @secondActionID
	BEGIN
		SET @liveActionID = @firstActionID
		SET @deletedActionID = @secondActionID
	END
	ELSE
	BEGIN
		SET @liveActionID = @secondActionID
		SET @deletedActionID = @firstActionID
	END
	
	DECLARE cur_Campaigns CURSOR local fast_forward
	FOR
	SELECT c.[campaignID], c.[campaignTypeID]
	FROM [Campaign] c
	WHERE c.[actionID] = @deletedActionID

	DECLARE @campaignID INT, @campaignNewID INT, @campaignType SMALLINT
	OPEN cur_Campaigns
	FETCH NEXT FROM cur_Campaigns INTO @campaignID, @campaignType

	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @campaignNewID = 0
		SELECT @campaignNewID = c1.[campaignID]
				FROM [Campaign] c1 
					INNER JOIN [Campaign] c2 ON 
						coalesce(c1.[massmediaID], -1) = coalesce(c2.[massmediaID], -1)
						AND c1.[agencyID] = c2.[agencyID]
						AND c1.[paymentTypeID] = c2.[paymentTypeID]
						AND c1.[campaignTypeID] = c2.[campaignTypeID]
				WHERE c1.[actionID] = @liveActionID 
					AND c2.[campaignID] = @campaignID
		
		IF @campaignNewID > 0
		BEGIN
			UPDATE [Issue] SET [campaignID] = @campaignNewID WHERE [campaignID] = @campaignID
			IF @campaignType = 3
				UPDATE [ModuleIssue] SET [campaignID] = @campaignNewID WHERE [campaignID] = @campaignID
			IF @campaignType = 4
				UPDATE [PackModuleIssue] SET [campaignID] = @campaignNewID WHERE [campaignID] = @campaignID
			IF @campaignType = 2
				UPDATE [ProgramIssue] SET [campaignID] = @campaignNewID WHERE [campaignID] = @campaignID
			DELETE FROM [Campaign] WHERE [campaignID] = @campaignID
		END
		
		FETCH NEXT FROM cur_Campaigns INTO @campaignID, @campaignType
	end
	
	close cur_Campaigns
	deallocate cur_Campaigns
		
	UPDATE [Campaign] SET [actionID] = @liveActionID WHERE [actionID] = @deletedActionID
	DELETE FROM ACTION WHERE [actionID] = @deletedActionID

	Exec [dbo].[ActionRecalculate] @actionID = @liveActionID, @needShow = 0, @loggedUserID = @loggedUserID

END
