CREATE PROC [dbo].[SetAdvertTypeForCommmonRoller] 
(
@rollerID int,
@advertTypeID int,
@issueDate datetime,
@moduleID int = null,
@pricelistID int = null, -- это packModulePricelistID
@campaignID int
)
AS

If @moduleID Is Null and @pricelistID Is Null
	Begin
	Raiserror('Для выполнения операции необходима информация о модуле или пакетном модуле', 16, 1)
	Return
	End

-- Если это ролик "для всех фирм", который используется в модульных тарифах, то надо сделать клон этого ролика и уже ему назначить предмет рекламы
-- Усли клон уже есть - вернуть его



Declare @newRollerId int, @isCommon bit
Select @isCommon = isCommon From Roller Where rollerID = @rollerID

-- Это либо ролик "для всех фирм, либо его клон"

If @isCommon = 1
	Begin
	Select @newRollerId = rollerID From Roller Where parentID = @rollerID And advertTypeID = @advertTypeID
	If @newRollerId Is Null
		Begin
		INSERT INTO [Roller]([name],[duration],[rolStyleID],[path],[isEnabled],[rolActionTypeID],[isCommon],[isMute],[advertTypeID],[parentID])
		Select [name],[duration],[rolStyleID],[path],[isEnabled],[rolActionTypeID],0,0,@advertTypeID,@rollerID From Roller Where rollerID = @rollerID
	
		Set @newRollerId = @@IDENTITY
		End
	End
Else
	Begin
	Select @newRollerId = r1.rollerID 
	From 
		Roller r1
		Inner Join Roller r2 On r1.parentID = r2.parentID
	Where 
		r2.rollerID = @rollerID And r1.advertTypeID = @advertTypeID
	If @newRollerId Is Null
		Begin
		INSERT INTO [Roller]([name],[duration],[rolStyleID],[path],[isEnabled],[rolActionTypeID],[isCommon],[isMute],[advertTypeID],[parentID])
		Select [name],[duration],[rolStyleID],[path],[isEnabled],[rolActionTypeID],0,0,@advertTypeID,parentID From Roller Where rollerID = @rollerID
	
		Set @newRollerId = @@IDENTITY
		End
	End	

If @pricelistID Is Not Null
	Begin
	Update 
		[Issue] Set [rollerID] = @newRollerId
	From 
		PackModuleIssue pmi 
	Where 
		pmi.packModuleIssueID = Issue.packModuleIssueID
		and pmi.campaignID = @campaignID
		and pmi.rollerID = @rollerID
		and pmi.issueDate = @issueDate
		and pmi.pricelistID = @pricelistID

	Update 
		[PackModuleIssue] Set [rollerID] = @newRollerId
	Where 
		campaignID = @campaignID
		and rollerID = @rollerID
		and issueDate = @issueDate
		and pricelistID = @pricelistID
	End

If @moduleID Is Not Null
	Begin
	Update 
		[Issue] Set [rollerID] = @newRollerId
	From 
		ModuleIssue mi 
	Where 
		mi.moduleIssueID = Issue.moduleIssueID
		and mi.campaignID = @campaignID
		and mi.rollerID = @rollerID 
		and mi.issueDate = @issueDate
		and mi.moduleID = @moduleID

	Update 
		[ModuleIssue] Set [rollerID] = @newRollerId
	Where 
		campaignID = @campaignID
		and rollerID = @rollerID 
		and issueDate = @issueDate
		and moduleID = @moduleID
	End