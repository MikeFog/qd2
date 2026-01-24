CREATE                 PROCEDURE [dbo].[ProgramIssueIUD]
(
@issueID int = NULL OUT,
@campaignID int = NULL,
@programID smallint = NULL,
@tariffID int = NULL,
@issueDate datetime = NULL,
@tariffPrice money = NULL,
@bonus smallint = NULL,
@loggedUserID smallint,
@isConfirmed bit = null,
@advertTypeID smallint = null,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
set datefirst 1
DECLARE	
	@massmediaID smallint,
	@RightToGoBack bit,
	@IsAdmin bit,
	@IsTrafficManager bit,
	@RightForMinus bit, 
	@finishDate datetime,
	@timeBonus int,
	@issuesDuration int

SELECT 
	@massmediaID = massmediaID,
	@finishDate = finishDate,
	@timeBonus = timeBonus,
	@issuesDuration = issuesDuration	
FROM 
	Campaign 
WHERE 
	campaignID = @campaignID

EXEC hlp_GetMainUserCredentials
	@loggedUserId, @rightToGoBack out, @isAdmin out, @IsTrafficManager out, @rightForMinus out

IF @actionName = 'AddItem' 
	BEGIN
	-- Verify data against few rules
	-- 1. Disabled window
	IF dbo.fn_IsDisabledWindow(@massmediaID, @issueDate) = 1 BEGIN
		RAISERROR('DisabledWindowInsertProgram', 16, 1)
		RETURN
		END

	-- 2. impossible to add issues with date less than today 
	If	dbo.ToShortDate(@issueDate) <= dbo.ToShortDate(getdate()) And @IsAdmin <> 1 And @IsTrafficManager <> 1  BEGIN
		RAISERROR('IncorrectProgramIssueDate', 16, 1)
		RETURN
		END

	-- 3. if company has already finished, refuse changes ------
	if	@finishDate < dbo.ToShortDate(getdate()) And @IsAdmin <> 1 And @IsTrafficManager <> 1  BEGIN
		RAISERROR('CampaignAlreadyFinished', 16, 1)
		RETURN
		END

	declare @datepart tinyint 
	set @datepart = datepart(dw, @issueDate)

	if not exists(select * 
		from SponsorTariff t 
			inner join SponsorProgramPricelist sppl on t.pricelistID = sppl.pricelistID
		where t.tariffID = @tariffID and sppl.sponsorProgramID = @programID
			and ((@issueDate >= dbo.ToShortDate(@issueDate) + sppl.broadcastStart 
				and ((t.monday = 1 and @datepart = 1)
				or (t.tuesday = 1 and @datepart = 2)
				or (t.wednesday = 1 and @datepart = 3)
				or (t.thursday = 1 and @datepart = 4)
				or (t.friday = 1 and @datepart = 5)
				or (t.saturday = 1 and @datepart = 6)
				or (t.sunday = 1 and @datepart = 7)))
			or (@issueDate < dbo.ToShortDate(@issueDate) + sppl.broadcastStart 
				and ((t.monday = 1 and @datepart = 7)
				or (t.tuesday = 1 and @datepart = 1)
				or (t.wednesday = 1 and @datepart = 2)
				or (t.thursday = 1 and @datepart = 3)
				or (t.friday = 1 and @datepart = 4)
				or (t.saturday = 1 and @datepart = 5)
				or (t.sunday = 1 and @datepart = 6)))))
	begin 
		raiserror('ProgramNotExists',16,1)
		return
	end 

	if exists(select * from ProgramIssue where programID = @programID and tariffID = @tariffID and @issueDate = issueDate and isConfirmed = 1)
	begin 
		raiserror('UIX_ProgramIssue_program_issueDate',16,1)
		return
	end 

	INSERT INTO [ProgramIssue](campaignID, programID, tariffID, issueDate, [tariffPrice], isConfirmed, advertTypeID)
	select @campaignID, @programID, @tariffID, @issueDate, @tariffPrice, @isConfirmed, @advertTypeID
	
	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @issueID = SCOPE_IDENTITY()
		
	EXEC dbo.[ProgramIssues] @issueID= @issueID
END
ELSE IF @actionName = 'DeleteItem' BEGIN
	-- Only admin is allowed to delete issue which is in the past already
	If	@issueDate < GetDate() And @IsAdmin <> 1 BEGIN
		RAISERROR('PastIssue', 16, 1)
		RETURN
	END	
	
	if @bonus is null 
		select @bonus = pl.bonus
			FROM ProgramIssue i 
				inner join SponsorTariff st on i.tariffID = st.tariffID
				INNER JOIN [SponsorProgramPricelist] pl ON st.[pricelistID] = pl.[pricelistID]
			where i.issueID = @issueID

	-- Should be balance between time bonus and roller issues duration
	If	@issuesDuration > @timeBonus - @bonus BEGIN
		RAISERROR('ProgramIssueDeleteBonusError', 16, 1)
		RETURN
	END		
	
	DELETE FROM [ProgramIssue] WHERE issueID = @issueID
	END
ELSE IF @actionName = 'UpdateItem'
	Begin
	UPDATE	
		[ProgramIssue]
	SET			 
		issueDate = @issueDate,
		advertTypeID = @advertTypeID
	WHERE		
		issueID = @issueID

	EXEC dbo.[ProgramIssues] @issueID= @issueID
	End

