/*
Modified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - error resolved
Modified: Denis Gladkikh (dgladkikh@fogsoft.ru) 18.09.2008 - replace @moduleIssueID and @packModuleIssueID on @moduleID and @packModuleID
Modified: Denis Gladkikh (dgladkikh@fogsoft.ru) 14.10.2008 - some optimization + add substitude for only one issue
*/
CREATE  PROC [dbo].[RollerSubstitute]
(
@campaignID int,
@campaignTypeID tinyint,
@oldRollerID int,
@oldDuration int,
@newRollerID int,
@newDuration int,
@loggedUserId smallint,
@moduleID int = null,
@packModuleID int = null,
@originalWindowID int = null,
@issueID int = null
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
    if (object_id('tempdb..#days') is null and (@originalWindowID is null or @issueID is null))
    begin 
		print('Для работы процедуры необходима таблица #days (windowID int, issueDate datetime)')
		print('create table #days (windowID int, issueDate datetime)')
		return 
	end 
	else if @originalWindowID is not null
	begin 
		if object_id('tempdb..#days') is null
		begin 
			create table #days (windowID int, issueDate datetime)
			insert into [#days] (windowID,issueDate) 
			select @originalWindowID, tw.dayOriginal from TariffWindow tw where tw.windowId = @originalWindowID
		end 
	end 

declare
	@rightForMinus bit,
	@RightToGoBack bit,
	@windowID int,
	@timeBonus int,
	@issuesDuration int,
	@deadline datetime,
	@position float,
	@newPrice money,
	@extraChargeFirst int, @extraChargeSecond int, @extraChargeLast int,
	@tariffWindowPrice money, 
	@diffDuration int,
	@isConfirmed bit,
	@date datetime,
	@msgError varchar(128),
	@timeadded int,
	@isAdmin bit,
	@isTrafficManager bit,
	@newRollerActionTypeID int

set @diffDuration = @newDuration - @oldDuration
select @isConfirmed = a.isConfirmed From Action a Inner Join Campaign c On a.actionID = c.actionID Where c.campaignID = @campaignID
select @newRollerActionTypeID = rolActionTypeID From Roller where rollerID = @newRollerID

If @newDuration = 0 Begin
	RAISERROR('Roller_NullDuration', 16, 1)
	RETURN 
End

-- в активированных акциях нельзя заменить на ролик без предмета рекламы
If Exists (Select 1 From Roller where rollerID = @newRollerID And advertTypeID Is Null And @isConfirmed = 1) Begin
	RAISERROR('WrongRollerForSubstitution', 16, 1)
	RETURN 
End

Exec hlp_GetMainUserCredentials
	@loggedUserId = @loggedUserId,
	@rightToGoBack = @rightToGoBack out,
	@isAdmin = @isAdmin out,
	@isTrafficManager = @isTrafficManager out,
	@rightForMinus = @rightForMinus OUT

declare @issues table (issueID int primary key, 
	newPrice money, 
	actualWindowID int, 
	date smalldatetime,
	isConfirmed bit, 
	msgError varchar(256))

declare cur_issues cursor local fast_forward
for
select
	i.issueID,
	i.actualWindowID,
	i.positionId,
	c.timeBonus,
	c.issuesDuration,
	pl.extraChargeFirstRoller, 
	pl.extraChargeSecondRoller, 
	pl.extraChargeLastRoller,
	mm.deadline,
	tw.price,
	tw.dayOriginal
from
	Issue i
	inner join TariffWindow tw on i.originalWindowID = tw.windowId
	inner join Tariff t on t.tariffID = tw.tariffId
	inner join Pricelist pl on pl.pricelistID = t.pricelistID
	inner join Campaign c on i.campaignID = c.campaignID
	inner join MassMedia mm on tw.massmediaID = mm.massmediaID
	left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
	left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
	left join PackModulePriceList pmpl on pmi.priceListID = pmpl.priceListID
	inner join #days d on tw.dayOriginal = d.issueDate and (@campaignTypeID not in (1,2) or d.windowID = i.originalWindowID)
where
	i.campaignID = @campaignID
	and i.rollerID = @oldRollerID 
	and	(@moduleID is null or mi.moduleID = @moduleID) 
	and (@packModuleID is null or pmpl.packModuleID = @packModuleID)
	and i.issueID = coalesce(@issueID, i.issueID)
	
open cur_issues
fetch next from cur_issues 
into @issueID, @windowID, @position, @timeBonus, @issuesDuration, @extraChargeFirst, @extraChargeSecond, @extraChargeLast,
	@deadline, @tariffWindowPrice,@date

set @timeadded = 0

while @@fetch_status = 0 
begin
	set @msgError = null

-- В окне может быть только 1 ролик с типом 4 и 1 ролик с типом 5
	If @newRollerActionTypeID In (4, 5) And Exists (
		Select 1 
		From 
			Issue i 
			Inner Join Roller r on r.rollerID = i.rollerID
		Where
			i.originalWindowID = @windowID
			And r.rolActionTypeID = @newRollerActionTypeID
			And i.issueID != @issueID
		)
		Begin
			If @newRollerActionTypeID = 4
				RAISERROR('RolType4AlreadyExistInWindow', 16, 1)
			Else
				RAISERROR('RolType5AlreadyExistInWindow', 16, 1)
			RETURN 1
		End

	If @isAdmin = 0 And	@isConfirmed = 1 And @isTrafficManager = 0 And @date <= IsNull(@deadLine, Convert(datetime, '19000101',112))
	begin
		select @msgError = 'DeadLineViolation'
	end 

	If	@isConfirmed = 1 And  @date <= dbo.ToShortDate(getdate()) And @RightToGoBack <> 1  And @isTrafficManager = 0
	begin
		select @msgError = 'DateInThePast'
	end 

	if @diffDuration > 0 begin
		if @rightForMinus <> 1 and exists(
			Select * From TariffWindow 
			Where windowId = @windowId 
				And [timeInUseConfirmed] + (@diffDuration) > duration) 
		begin
			select @msgError = 'WindowOverflow'
		end
			
		-- For sponsor campaign Time Bonus shouldn't be exceed
		IF @campaignTypeID = 2 
		begin		
			IF @timeBonus - @timeadded < @issuesDuration + @diffDuration 
				select @msgError = 'TimeBonusExceed'
			else 
				set @timeadded = @timeadded + @diffDuration
		end 
	end 

	set @newPrice = 0

	if @campaignTypeID = 1 and @msgError is null
	begin
		select @newPrice = dbo.fn_GetIssuePrice(
			@newDuration, @tariffWindowPrice, 1, @position, @extraChargeFirst, @extraChargeSecond, @extraChargeLast)
	end
			
	insert into @issues (issueID,newPrice,actualWindowID, isConfirmed, msgError,date)
	values (@issueID,@newPrice,@windowID, @isConfirmed, @msgError,@date) 

	fetch next from cur_issues 
	into @issueID, @windowID, @position, @timeBonus, @issuesDuration, @extraChargeFirst, @extraChargeSecond, @extraChargeLast,@deadline, @tariffWindowPrice,@date
end 

close cur_issues
deallocate cur_issues

if @moduleID is not null or @packModuleID is not null 
begin 
	update i set i.msgError = 'Dependence' from @issues i 
		inner join @issues i2 on i.date = i2.date and i2.msgError is not null 
	where i.msgError is null 
end 

IF @campaignTypeID = 3
BEGIN 
	DECLARE cur_missues CURSOR local fast_forward
	FOR
	SELECT mi.[moduleIssueID], mpl.[price], mi.positionId,
		mpl.extraChargeFirstRoller, mpl.extraChargeSecondRoller, mpl.extraChargeLastRoller 
	FROM [ModuleIssue] mi 
		INNER JOIN [ModulePriceList] mpl ON mi.modulePricelistID = mpl.modulePricelistID
		inner join Pricelist pl on mpl.priceListID = pl.pricelistID
		inner join #days d on mi.issueDate = d.issueDate
		WHERE mi.[campaignID] = @campaignID
			and (@moduleID is null or  mi.moduleID = @moduleID) and mi.rollerID = @oldRollerID
			and not exists(select * from @issues i where i.date = mi.issueDate and i.msgError is not null)
			
	DECLARE @moduleIssueID int

	OPEN cur_missues
	FETCH NEXT FROM cur_missues 
	INTO @moduleIssueID, @tariffWindowPrice, @position, @extraChargeFirst, @extraChargeSecond, @extraChargeLast
	
	WHILE @@fetch_status = 0 BEGIN
		SELECT @newPrice = dbo.fn_GetIssuePrice(
			@newDuration, @tariffWindowPrice, 1, @position, @extraChargeFirst, @extraChargeSecond, @extraChargeLast)

		UPDATE [ModuleIssue] 
		SET [rollerID] = @newRollerID, [tariffPrice] = @newPrice 
		WHERE [moduleIssueID] = @moduleIssueID
	
		FETCH NEXT FROM cur_missues 
		INTO @moduleIssueID, @tariffWindowPrice, @position, @extraChargeFirst, @extraChargeSecond, @extraChargeLast
	end
	
	close cur_missues
	deallocate cur_missues
END

IF @campaignTypeID = 4
BEGIN
	DECLARE cur_pmissues CURSOR LOCAL  fast_forward
	FOR 
	SELECT pmi.[packModuleIssueID], pmpl.[price], pmi.positionId, pmpl.extraChargeFirstRoller, pmpl.extraChargeSecondRoller, pmpl.extraChargeLastRoller FROM [PackModuleIssue] pmi 
		INNER JOIN [PackModulePriceList] pmpl ON pmi.[pricelistID] = pmpl.[priceListID]
		inner join #days d on pmi.issueDate = d.issueDate
		WHERE pmi.[campaignID] = @campaignID
			and (@packModuleID is null or  pmpl.packModuleID = @packModuleID) and pmi.rollerID = @oldRollerID
			and not exists(select * from @issues i where i.date = pmi.issueDate and i.msgError is not null)
		
	DECLARE @packModuleIssueID INT
	
	OPEN cur_pmissues
	FETCH NEXT FROM cur_pmissues
	INTO @packModuleIssueID, @tariffWindowPrice, @position, @extraChargeFirst, @extraChargeSecond, @extraChargeLast
	WHILE @@fetch_status = 0 BEGIN
		SELECT @newPrice = dbo.fn_GetIssuePrice(
			@newDuration, @tariffWindowPrice, 1, @position, @extraChargeFirst, @extraChargeSecond, @extraChargeLast)

		UPDATE [PackModuleIssue] 
		SET [rollerID] = @newRollerID, [tariffPrice] = @newPrice 
		WHERE [packModuleIssueID] = @packModuleIssueID
			
		FETCH NEXT FROM cur_pmissues 
		INTO @packModuleIssueID, @tariffWindowPrice, @position, @extraChargeFirst, @extraChargeSecond, @extraChargeLast
	end
	
	close cur_pmissues
	deallocate cur_pmissues
END

update i set i.rollerID = @newRollerID,
	i.tariffPrice = ii.newPrice
from Issue i 
	inner join @issues ii on i.issueID = ii.issueID and ii.msgError is null

Update 
	TariffWindow
Set
	timeInUseConfirmed = 
		Case 
			When [maxCapacity] = 0 
				Then timeInUseConfirmed + coalesce(res.durC,0)
			Else timeInUseConfirmed
		End,
	timeInUseUnconfirmed = 
		Case 
			When [maxCapacity] = 0
				Then timeInUseUnconfirmed + coalesce(res.durU,0)
			Else timeInUseUnconfirmed
		End
from
	(select i.actualWindowID, 
			sum(case when i.isConfirmed = 1 then @diffDuration else 0 end) as durC,
			sum(case when i.isConfirmed = 0 then @diffDuration else 0 end) as durU
		from @issues i	where i.msgError is null
	 group by i.actualWindowID) as res
where
	TariffWindow.windowId = res.actualWindowId
	
select row_number() over(order by tw.windowDateOriginal) as RowNum, tw.windowDateOriginal, msg.message
from @issues i
	inner join iMessageToSubtitute msg on i.msgError = msg.msgError
	inner join Issue ii on i.issueID = ii.issueID
	inner join TariffWindow tw on ii.originalWindowID = tw.windowId
where i.msgError is not null
order by tw.windowDateOriginal
