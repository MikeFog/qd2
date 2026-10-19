-- Какое сообщение показать при постановке модуля и пакета (hlp_IssueVerify, ветки модуля и пакета; десктоп и веб;
-- docs/tasks/side-defects.md, «Мелочи этапа 6.0»). У модуля и пакета окон за день несколько, проверки считаются для
-- каждого окна, и выбиралось сообщение, наибольшее по алфавиту среди окон: «WindowOverflow» одного окна перебивал «позиция
-- занята», «окно закрыто» и «штучное окно переполнено» соседнего. Теперь — первое по порядку проверок, как в линейной
-- ветке, где окно одно: DeadLineViolation, IncorrectIssueDate, DisabledInsertRoller, DisabledInsertSimpleRoller,
-- FirstLastIssueError, WindowMaxCapacityOverflow, WindowOverflow. У пакета «дата в прошлом» (IncorrectIssueDate, одна на
-- все окна) стоит первой, как у модуля и линейки (было последним WHEN). Отказ (код возврата) не меняется нигде — меняется
-- только текст, когда ошибки есть в нескольких окнах. Выборка окон и TOP 1 прежние (окно без ошибки — последним: иначе
-- затёрся бы выставленный раньше CampaignAlreadyFinished). Параметры прежние.
--
-- Порядок: после Deploy/50 (несёт текст процедуры с окнами модуля и пакета по dayOriginal) — без него скрипт
-- отказывается: «Ничего не изменено». В любое время; клиент qd2 и перезапуск веба не нужны. Не перезапускать после 62:
-- 26, 37, 38, 50 и ArtvisDB/Scripts promo-position-forbidden-deploy.sql, local-promo-roller-types-deploy.sql (несут
-- старый текст hlp_IssueVerify); если перезапустили — повторить 62. Идемпотентен (CREATE OR ALTER). Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 62_issue-verify-module-message-order.sql

SET NOCOUNT ON;
GO
IF CHARINDEX(N'And tw.dayOriginal = @issueDate', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_IssueVerify')), N'')) = 0
   OR CHARINDEX(N'AgitationServiceRollerManual', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_IssueVerify')), N'')) = 0
BEGIN
    RAISERROR(N'62: сначала Deploy/50 (hlp_IssueVerify с окнами модуля и пакета по dayOriginal). Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROC [dbo].[hlp_IssueVerify]
(
@issueID int,
@actionName varchar(32),
@massmediaID smallint,
@DeadLine datetime,
@windowID int,
@issueDate datetime,
@rollerDuration int,
@rightToGoBack bit,
@isAdmin bit,
@isTrafficManager bit,
@rightForMinus bit,
@campaignFinishDate datetime,
@campaignTypeID tinyint,
@isConfirmed bit,
@positionId smallint,
@timeBonus int,
@issuesDuration int,
@sumCapacity INT, 
@rollerActionTypeID TINYINT,
@msgError varchar(64) out,
@modulePricelistID int = null,
@needByTypeVerify bit = 0,
@packModulePriceListId int = null
)
AS
SET NOCOUNT ON

declare @tomorrow datetime
select @msgError = null, @tomorrow = dateadd(day, 1, Convert(datetime, Convert(varchar(8),getdate(), 112), 112)), @issueDate = Convert(datetime, Convert(varchar(8),@issueDate, 112), 112)

IF @sumCapacity IS NULL 
 set @sumCapacity = 1
 
-- check Disabled Windows 
if ((@needByTypeVerify = 0 or @campaignTypeID in (1,2,3)) 
		and exists (select * 
						from DisabledWindow dw 
							inner join TariffWindow tw on tw.windowID = @windowID and tw.massmediaID = @MassMediaId
								and dw.massmediaID = @MassMediaId and tw.windowDateActual between dw.startDate And dw.finishDate)
	or (@needByTypeVerify = 1 and @campaignTypeID = 4 and exists(select * from [PackModuleContent]  pmc
																	INNER JOIN [ModulePriceList] mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
																	INNER JOIN [ModuleTariff] mt ON mpl.[modulePriceListID] = mt.[modulePriceListID]
																	INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID] and tw.dayOriginal = @issueDate
																	inner join DisabledWindow dw on tw.windowDateActual between dw.startDate And dw.finishDate and tw.massmediaID = dw.massmediaID
																	where pmc.pricelistID = @packModulePriceListId ) ) )
begin
	set @msgError = 'DisabledWindowInsert'
	return 1
end


--set @msgError = @rollerActionTypeID
--RETURN 1

-- «Отбивка политической агитации» (7), «Локальное СМИ (агитация)» (44) и «Федеральное СМИ (агитация)» (55)
-- ставит только система (AgitationFraming пишет Issue напрямую, мимо этой проверки); человеку - отказ
-- (заказчик, 11.10.2026)
If @rollerActionTypeID In (7, 44, 55)
	Begin
		set @msgError = 'AgitationServiceRollerManual'
		RETURN 1
	End

-- В окне может быть только один открывающий (тип 4 или 44) и один закрывающий (тип 5 или 55)
-- идентификатор СМИ; 44/55 - авто-обрамление политической агитации, пары к ручным 4/5
If @rollerActionTypeID In (4, 44) And Exists (
	Select 1
	From
		Issue i
		Inner Join Roller r on r.rollerID = i.rollerID
	Where
		i.actualWindowID = @windowID
		And r.rolActionTypeID In (4, 44)
		And i.issueID != IsNull(@issueID, -1)
	)
	Begin
		set @msgError = 'RolType4AlreadyExistInWindow'
		RETURN 1
	End

If @rollerActionTypeID In (5, 55) And Exists (
	Select 1
	From
		Issue i
		Inner Join Roller r on r.rollerID = i.rollerID
	Where
		i.actualWindowID = @windowID
		And r.rolActionTypeID In (5, 55)
		And i.issueID != IsNull(@issueID, -1)
	)
	Begin
		set @msgError = 'RolType5AlreadyExistInWindow'
		RETURN 1
	End

-- Размещение агитации сразу подтверждённым выпуском (в уже активированной акции)
-- требует заполненных роликов обвязки в карточке станции - иначе обвязку не создать.
-- В черновике проверка не нужна: её выполнит активация (ActionActivate).
If @rollerActionTypeID = 6 And @isConfirmed = 1
	And Exists (
		Select 1 From MassMedia mm
		Where mm.massmediaID = @MassMediaId
			And (mm.agitationLocalRollerID Is Null
				Or mm.agitationAnnounceRollerID Is Null
				Or mm.agitationFederalRollerID Is Null)
	)
	Begin
		set @msgError = 'AgitationStationRollersNotSet'
		RETURN 1
	End

-- Анонс политической агитации (тип 7) в окне может быть только один
If @rollerActionTypeID = 7 And Exists (
	Select 1
	From
		Issue i
		Inner Join Roller r on r.rollerID = i.rollerID
	Where
		i.actualWindowID = @windowID
		And r.rolActionTypeID = 7
		And i.issueID != IsNull(@issueID, -1)
	)
	Begin
		set @msgError = 'RolType7AlreadyExistInWindow'
		RETURN 1
	End

-- Для политической агитации и её авто-обрамления спецпозиционирование не применяется
If @rollerActionTypeID In (6, 7, 44, 55) And IsNull(@positionId, 0) <> 0
	Begin
		set @msgError = 'AgitationPositionForbidden'
		RETURN 1
	End

-- Локальное промо (8 - со спонсором, 9 - без спонсора) не позиционируется:
-- место в блоке ему задаёт тип ролика при выгрузке
If @rollerActionTypeID In (8, 9) And IsNull(@positionId, 0) <> 0
	Begin
		set @msgError = 'PromoPositionForbidden'
		RETURN 1
	End

-- «Локальное СМИ» (4) и «Федеральное СМИ» (5) не позиционируются (заказчик, 11.10.2026):
-- место в блоке им задаёт тип ролика при выгрузке
If @rollerActionTypeID In (4, 5) And IsNull(@positionId, 0) <> 0
	Begin
		set @msgError = 'RolType45PositionForbidden'
		RETURN 1
	End

if	@campaignFinishDate < @tomorrow and @campaignTypeID <> 2 And @RightToGoBack <> 1 And @isTrafficManager = 0
	set @msgError = 'CampaignAlreadyFinished'
else if @campaignTypeID = 2 
	Begin
	Declare
		@extraChargeFirst tinyint,
		@extraChargeSecond tinyint,
		@extraChargeLast tinyint

	Select
		@extraChargeFirst = IsNull(extraChargeFirstRoller, 0),
		@extraChargeSecond = IsNull(extraChargeSecondRoller, 0),
		@extraChargeLast = IsNull(extraChargeLastRoller, 0)
	From 
		Pricelist p
		Inner Join Tariff t on p.pricelistID = t.pricelistID
		Inner Join TariffWindow tw on tw.tariffId = t.tariffID
	Where 
		tw.windowId = @windowID

	If @timeBonus < @issuesDuration + dbo.f_GetSponsorDuration(@rollerDuration, @positionId, @extraChargeFirst, @extraChargeSecond, @extraChargeLast)
		Begin
		set @msgError = 'TimeBonusExceed'
		return 1
		End
	End
if (@needByTypeVerify = 0 or @campaignTypeID in (1,2))
	select top 1 @msgError = 
		case
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And @deadLine is not null and tw.dayActual <= @deadLine And @isAdmin = 0 And @isTrafficManager = 0 then 'DeadLineViolation' 
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And tw.dayOriginal < @tomorrow And @RightToGoBack <> 1 And @isTrafficManager = 0 then 'IncorrectIssueDate'
			when tw.isDisabled = 1 then 'DisabledInsertRoller'
			when @rollerActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
			when (( tw.isFirstPositionOccupied = 1 And @positionId = -20) 
					or (tw.isSecondPositionOccupied = 1	And @positionId = -10)
					or (tw.isLastPositionOccupied = 1	And @positionId = 10)) then 'FirstLastIssueError'
			when @rightForMinus = 0 AND @isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + @sumCapacity)) < 0) then 'WindowMaxCapacityOverflow'
			when @rightForMinus = 0 AND @isConfirmed = 1 and tw.[timeInUseConfirmed] + @rollerDuration > tw.duration then 'WindowOverflow'
			else null 
		end 
	from [TariffWindow] tw
	where tw.[windowId] = @windowID 
else if @needByTypeVerify = 1 and @campaignTypeID = 3
	Begin
	-- окон у модуля за день несколько: сообщение - первой сработавшей проверки по порядку CASE, как в линейной ветке
	-- (окно одно), а не наибольший по алфавиту ключ среди окон (WindowOverflow одного окна перебивал
	-- FirstLastIssueError и закрытое окно соседнего); строки и TOP 1 прежние, окно без ошибки - последним
	select top 1 @msgError = e.msgError
	from [TariffWindow] tw
		Inner Join ModuleTariff mt On mt.tariffId = tw.tariffId
		Inner Join ModulePriceList mpl On mpl.modulePriceListID = mt.modulePriceListID
		Cross Apply (Select
		case 
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And @deadLine is not null and tw.dayActual <= @deadLine And @isAdmin = 0 And @isTrafficManager = 0 then 'DeadLineViolation' 
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And tw.dayOriginal < @tomorrow And @RightToGoBack <> 1 And @isTrafficManager = 0 then 'IncorrectIssueDate'
			when tw.isDisabled = 1 then 'DisabledInsertRoller'
			when @rollerActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
			when (( tw.isFirstPositionOccupied = 1 And @positionId = -20) 
					or (tw.isSecondPositionOccupied = 1	And @positionId = -10)
					or (tw.isLastPositionOccupied = 1	And @positionId = 10)) then 'FirstLastIssueError'
			when @rightForMinus = 0 AND @isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + @sumCapacity)) < 0) then 'WindowMaxCapacityOverflow'
			when @rightForMinus = 0 AND @isConfirmed = 1 and tw.[timeInUseConfirmed] + @rollerDuration > tw.duration then 'WindowOverflow'
			else null 
		end As msgError) e
	where 
		mt.modulePriceListID = @modulePricelistID
		-- день модуля и пакета - по dayOriginal, как вставка выпусков (ModuleIssueIUD, PackModuleIssueID)
		-- и IsModuleExist; индекс IX_TariffWindow_TariffID_DayOriginal; время окна переносится только
		-- внутри дня, так что dayOriginal = dayActual
		And tw.dayOriginal = @issueDate
	order by
		case e.msgError
			when 'DeadLineViolation' then 1
			when 'IncorrectIssueDate' then 2
			when 'DisabledInsertRoller' then 3
			when 'DisabledInsertSimpleRoller' then 4
			when 'FirstLastIssueError' then 5
			when 'WindowMaxCapacityOverflow' then 6
			when 'WindowOverflow' then 7
			else 8 -- окно без ошибки - последним (NULL при ASC встал бы первым)
		end
	if @msgError Is Null And @positionId <> 0
	Begin
	Select @msgError = 'MaxCapacityModuleSetPositionError'
	From
		ModuleTariff mt
		Inner Join Tariff t On t.tariffID = mt.tariffID
	Where
		mt.modulePriceListID = @modulePriceListId
		And t.maxCapacity > 0 And t.maxCapacity < 4
	End
	End 
else if @needByTypeVerify = 1 and @campaignTypeID = 4
	Begin
	If (@actionName = 'AddItem' Or @isConfirmed = 1) And @isAdmin = 0 And @isTrafficManager = 0 And 
		Exists
		(
		Select 1 
		from 
			[PackModuleContent]  pmc
			INNER JOIN [ModuleTariff] mt ON pmc.[modulePriceListID] = mt.[modulePriceListID]
			INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID]
			inner join MassMedia mm on tw.massmediaID = mm.massmediaID
		WHERE 
			pmc.[pricelistID] = @packModulePriceListId
			AND tw.dayOriginal = @issueDate
			AND @issueDate <= mm.deadLine
		)
		Set @msgError = 'DeadLineViolation'

	If @msgError Is Null
		-- окон у пакета за день несколько: сообщение - по порядку проверок, как в модульной ветке (не по алфавиту)
		select top 1 @msgError = e.msgError
		from [PackModuleContent]  pmc
			INNER JOIN [ModulePriceList] mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
			INNER JOIN [ModuleTariff] mt ON mpl.[modulePriceListID] = mt.[modulePriceListID]
			INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID]
			inner join MassMedia mm on mm.massmediaID = tw.massmediaID
			Cross Apply (Select
			case 
				-- дата в прошлом - одна на все окна пакета: первой, как в линейной и модульной ветках
				when (@actionName = 'AddItem' Or @isConfirmed = 1) And @issueDate < @tomorrow And @RightToGoBack <> 1 And @isTrafficManager = 0 then 'IncorrectIssueDate'
				when tw.isDisabled = 1 then 'DisabledInsertRoller'
				when @rollerActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
				when @positionId <> 0 and (( tw.isFirstPositionOccupied = 1 And @positionId = -20) 
						or (tw.isSecondPositionOccupied = 1	And @positionId = -10)
						or (tw.isLastPositionOccupied = 1	And @positionId = 10)) then 'FirstLastIssueError'
				when @rightForMinus = 0 AND @isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + @sumCapacity)) < 0) then 'WindowMaxCapacityOverflow'
				when @rightForMinus = 0 AND @isConfirmed = 1 and tw.[timeInUseConfirmed] + @rollerDuration > tw.duration then 'WindowOverflow'
				else null 
			end As msgError) e
		WHERE 
			pmc.[pricelistID] = @packModulePriceListId
			AND tw.dayOriginal = @issueDate
		order by
			case e.msgError
				when 'DeadLineViolation' then 1
				when 'IncorrectIssueDate' then 2
				when 'DisabledInsertRoller' then 3
				when 'DisabledInsertSimpleRoller' then 4
				when 'FirstLastIssueError' then 5
				when 'WindowMaxCapacityOverflow' then 6
				when 'WindowOverflow' then 7
				else 8 -- окно без ошибки - последним (NULL при ASC встал бы первым)
			end
	if @msgError Is Null And @positionId <> 0
		Begin
		Select @msgError = 'MaxCapacityPackModuleSetPositionError'
		From
			PackModuleContent pmc
			Inner Join ModuleTariff mt On mt.modulePriceListID = pmc.modulePriceListID
			Inner Join Tariff t On t.tariffID = mt.tariffID
		Where
			pmc.pricelistID = @packModulePriceListId
			And t.maxCapacity > 0 And t.maxCapacity < 4
		End
	End
if @msgError is not null 
	return 1

return 0
GO

-- Проверка: сообщение модуля и пакета выбирается по порядку проверок.
IF CHARINDEX(N'order by 1 desc', OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_IssueVerify'))) = 0
   AND CHARINDEX(N'when ''WindowOverflow'' then 7', OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_IssueVerify'))) > 0
   AND CHARINDEX(N'And tw.dayOriginal = @issueDate', OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_IssueVerify'))) > 0
    PRINT N'ГОТОВО: hlp_IssueVerify — сообщение модуля и пакета по порядку проверок.';
ELSE
    RAISERROR(N'62: новая версия hlp_IssueVerify не применена.', 16, 1);
GO

-- Снять режим «не выполнять», если его включила защита в начале (иначе окно SSMS остаётся в NOEXEC).
SET NOEXEC OFF;
GO
