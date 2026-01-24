CREATE PROC [dbo].[ActionRollerSetAdvertType] 
(
@rollerID int,
@advertTypeID int,
@isCommon bit,
@isMute bit,
@actionID int = null, 
@firmID int = null, 
@changeFlag bit = 0,
@duration int,
@newRollerId int output
)
AS

If @firmID Is Null And @actionID Is Null
	Begin
	Raiserror('FirmAndActionAreNull', 16, 1)
	Return
	End

If @firmID Is Null
	Select @firmID = firmID From Action Where actionID = @actionID

-- Если это ролик "для всех фирм", который используется в модульных тарифах, то надо сделать клон этого ролика и уже ему назначить предмет рекламы
-- Усли клон уже есть - вернуть его
-- Это может быть уже "клон", на котором нажали назначить или сменить предмет рекламы

If @isCommon = 1 Or Exists (Select 1 From Roller Where rollerID = @rollerID And parentID Is Not Null)
	Begin
	If @isCommon = 1
		Begin
		Select @newRollerId = rollerID From Roller Where parentID = @rollerID And advertTypeID = @advertTypeID --And firmID = @firmID
		If @newRollerId Is Null
			Begin
			INSERT INTO [Roller]([name],[duration],[rolStyleID],[path],[isEnabled],[rolActionTypeID],[isCommon],[isMute],[advertTypeID],[parentID])
			Select [name],[duration],[rolStyleID],[path],[isEnabled],[rolActionTypeID],0,0,@advertTypeID,@rollerID From Roller Where rollerID = @rollerID
	
			Set @newRollerId = @@IDENTITY
			End
		End
	Else
		Begin
		Select 
			@newRollerId = r1.rollerID 
		From 
			Roller r1
			Inner Join Roller r2 On r1.parentID = r2.parentID And r1.firmID = r2.firmID
		Where 
			r2.rollerID = @rollerID And r1.advertTypeID = @advertTypeID 

		If @newRollerId Is Null
			Begin
			INSERT INTO [Roller]([name],[duration],[rolStyleID],[path],[isEnabled],[rolActionTypeID],[isCommon],[isMute],[advertTypeID],[parentID])
			Select [name],[duration],[rolStyleID],[path],[isEnabled],[rolActionTypeID],0,0,@advertTypeID,parentID From Roller Where rollerID = @rollerID
	
			Set @newRollerId = @@IDENTITY
			End
		End	
	End
Else If @isMute = 1
	Begin
	-- это случай ролика-пустышки. Тут надо поискать, вдруг у данной фирмы уже есть пустышка с таким временем и предметом рекламы
	Select @newRollerId = rollerID From Roller Where firmID = @firmID And isMute = 1 And advertTypeID = @advertTypeID And duration = @duration

	If @newRollerId Is Null
		Begin
		Update Roller Set advertTypeID = @advertTypeID Where rollerID = @rollerID
		Set @newRollerId = @rollerID
		End
	End
Else -- это обычный ролик
	Begin
	Update Roller Set advertTypeID = @advertTypeID, firmID = @firmID Where rollerID = @rollerID
	Set @newRollerId = @rollerID
	End

If @changeFlag = 1 And @newRollerId <> @rollerID
	Begin
		Update 
			[PackModuleIssue] Set [rollerID] = @newRollerId
		From 
			PackModuleIssue pmi Inner Join Campaign c On c.campaignID = pmi.campaignID
		Where 
			c.actionID = @actionID 
			And pmi.rollerID = @rollerID

		Update 
			[ModuleIssue] Set [rollerID] = @newRollerId
		From 
			ModuleIssue mi Inner Join Campaign c On c.campaignID = mi.campaignID
		Where 
			c.actionID = @actionID 
			And mi.rollerID = @rollerID 

		Update 
			[Issue] Set [rollerID] = @newRollerId
		From 
			Issue i Inner Join Campaign c On c.campaignID = i.campaignID
		Where 
			c.actionID = @actionID 
			And i.rollerID = @rollerID 
	End