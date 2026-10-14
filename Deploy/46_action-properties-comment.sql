-- Свойства рекламной акции: карточка со всеми данными для просмотра и одним правимым полем «Комментарий».
--
-- У акции (iEntity 77) не было паспорта; «Редактировать» открывает ActionForm (цены и кампании), а это другое окно.
-- Теперь у акции есть пункт «Свойства» (Properties) — стандартный паспорт: номер, фирма, создатель, статус, период,
-- даты создания/изменения (только просмотр) и «Комментарий» менеджера (правится).
-- Двойной щелчок и «Редактировать» по-прежнему открывают ActionForm.
--
-- Что делает скрипт:
--   1. Action.comment [doubleString] NULL (nvarchar(256), в паспорте — многострочное поле); журналы столбец не показывают,
--      Actions1 отдаёт его сам (select a.*);
--   2. ActionIUD: необязательный @comment (NULL — не менять, '' — очистить); UpdateItem не затирает firmID/isConfirmed/userID,
--      если их не прислали (карточка присылает не всё). Прочие вызовы ActionIUD не меняются;
--   3. iEntityAction: у сущности 77 действие «Свойства» (Properties) — последним пунктом меню, через разделитель, как у фирмы. ВНИМАНИЕ: веб проверяет право «Свойства» на любом
--      UpdateItem этой сущности (WebActionAuthorization), поэтому при ПЕРВОМ создании действия оно выдаётся всем группам и
--      персональным выдачам, у которых есть «Редактировать», «Сменить фирму-заказчика» или «Сменить создателя». Повторный
--      запуск права не трогает;
--   4. паспорт сущности 77;
--   5. испанские подписи паспорта (iTranslation);
--   6. колонка «Комментарий» в журналах акций (77) и удалённых акций (1236) — только метаданные.
--
-- Нужны новый Merlin.exe и веб (старый клиент пункта не покажет, но работает). После наката — перезапустить qd2 и веб
-- (паспорта читаются при старте). Идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 46_action-properties-comment.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

-- =====================================================================
-- 1. Колонка
-- =====================================================================
IF COL_LENGTH('dbo.[Action]', 'comment') IS NULL
BEGIN
    ALTER TABLE dbo.[Action] ADD [comment] [dbo].[doubleString] NULL;
    PRINT N'Колонка Action.comment добавлена.';
END
ELSE
    PRINT N'Колонка Action.comment уже есть.';
GO

-- =====================================================================
-- 2. ActionIUD
-- =====================================================================
CREATE OR ALTER PROCEDURE [dbo].[ActionIUD]
(
@actionID int = NULL,
@firmID smallint = NULL,
@userID smallint = NULL,
@newCreatorID smallint = NULL,
@isConfirmed bit = NULL,
@actionName varchar(32),
@loggedUserID smallint,
@comment nvarchar(256) = NULL -- комментарий менеджера; NULL - не менять, пустая строка - очистить
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT on

declare @IsAdmin bit, @isTrafficManager bit

set @IsAdmin = dbo.f_IsAdmin(@loggedUserID)
set @isTrafficManager = [dbo].[f_IsTrafficManager](@loggedUserID)

-- Only admin is allowed to delete ACTIVATED issue which is in the past already
if @actionName = 'DeleteItem' and @IsAdmin <> 1 and @isTrafficManager <> 1
	and exists(select * 
				from TariffWindow tw 
					inner join Issue i on tw.windowId = i.actualWindowID 
					inner join Campaign c on i.campaignID = c.campaignID
				where c.actionID = @actionID and tw.dayOriginal <= dbo.ToShortDate(getdate()) and i.isConfirmed = 1)
begin
	raiserror('PastIssue', 16, 1)
	return
end 

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [Action](firmID, userID, isConfirmed)
	VALUES(@firmID, @userID, @isConfirmed)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @actionID = SCOPE_IDENTITY()

	EXEC Actions1 @actionID = @actionID
END
ELSE IF @actionName = 'DeleteItem' 
	begin
	-- если акция уже находится в журнале удалённых акций, то удалять по настоящему
	If Exists (Select 1 From [Action] WHERE actionID = @actionID And Not deleteDate Is Null)
		Begin

		-- Политическая агитация: выпуски удаляются массово, минуя IssueIUD, поэтому
		-- окна с агитацией запоминаем до удаления и снимаем обвязку после
		Declare @agitWindows table (windowID int primary key)
		Insert Into @agitWindows (windowID)
		Select Distinct i.actualWindowID
		From Issue i
			Inner Join Campaign c On c.campaignID = i.campaignID
			Inner Join Roller r On r.rollerID = i.rollerID
		Where c.actionID = @actionID And r.rolActionTypeID = 6 And i.isConfirmed = 1

		DELETE Issue FROM Campaign c
		WHERE c.campaignID = Issue.campaignID AND c.actionID = @actionID

		DELETE FROM [Action] WHERE actionID = @actionID

		Declare @agitWindowID int
		Declare cur_agit_act Cursor Local For Select windowID From @agitWindows

		Open cur_agit_act
		Fetch Next From cur_agit_act Into @agitWindowID
		While @@FETCH_STATUS = 0
		Begin
			Exec AgitationFraming
				@actionName = 'CleanupWindow',
				@windowID = @agitWindowID,
				@loggedUserID = @loggedUserID

			Fetch Next From cur_agit_act Into @agitWindowID
		End
		Close cur_agit_act
		Deallocate cur_agit_act

		Return

		End
	
	Select @isConfirmed = IsConfirmed From Action Where actionID = @actionID
	If @isConfirmed = 1	Begin
		Exec [ActionDeactivate] @actionID = @actionID, @loggedUserID = @loggedUserID
	End

	Update [Action] Set deleteDate = GETDATE() WHERE actionID = @actionID

	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When [maxCapacity] = 0 Then timeInUseConfirmed - t1.duration
				Else timeInUseConfirmed
			End,
		timeInUseUnconfirmed = 
			Case 
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed - t1.durationU
				Else timeInUseUnconfirmed
			End,
		capacityInUseConfirmed = 
			Case 
				When ([maxCapacity] > 0) 
					Then capacityInUseConfirmed - t1.countIssues
				Else capacityInUseConfirmed
			End,
		capacityInUseUnconfirmed = 
			Case  
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed - t1.countIssuesU
				Else capacityInUseUnconfirmed
			end,
		isFirstPositionOccupied = 
			Case 
				When firstCount > 0 Then 0
				Else isFirstPositionOccupied
			End,
		isSecondPositionOccupied = 
			Case 
				When secondCount > 0 Then 0
				Else isSecondPositionOccupied
			End,
		isLastPositionOccupied = 
			Case 
				When lastCount > 0 Then 0
				Else isLastPositionOccupied
			End,
		firstPositionsUnconfirmed = firstPositionsUnconfirmed - firstCountU,
		secondPositionsUnconfirmed = secondPositionsUnconfirmed - secondCountU,
		lastPositionsUnconfirmed = lastPositionsUnconfirmed - lastCountU
	From
		(select i.actualWindowID as windowID, 
			sum(case when i.isConfirmed = 1 then r.duration else 0 end) as duration, 
			sum(case when i.isConfirmed = 0 then r.duration else 0 end) as durationU, 
			sum(case when i.isConfirmed = 1 then 1 else 0 end) as countIssues,
			sum(case when i.isConfirmed = 0 then 1 else 0 end) as countIssuesU,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = -20 then 1 else 0 end, 0)) as firstCount,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = -10 then 1 else 0 end, 0)) as secondCount,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = 10 then 1 else 0 end, 0)) as lastCount,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = -20 then 1 else 0 end, 0)) as firstCountU,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = -10 then 1 else 0 end, 0)) as secondCountU,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = 10 then 1 else 0 end, 0)) as lastCountU
		 from 
			Issue i 
			Inner Join Roller r On r.rollerId = i.rollerId
			inner join Campaign c on i.campaignID = c.campaignID
		where c.actionID = @actionID
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID

	insert into [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
	select @loggedUserID, c.actionID, i.rollerID, tw.windowDateActual, tw.massmediaID 
	from Issue i 
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
		inner join Campaign c on i.campaignID = c.campaignID 
	where c.actionID = @actionID and i.isConfirmed = 1

	if exists(select *
		from Issue i 
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
			inner join Campaign c on i.campaignID = c.campaignID 
		where c.actionID = @actionID and i.isConfirmed = 1 and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog())
	begin 
		exec SayAdminThatIssuesDelete @loggedUserID, @actionID
	end 
END
ELSE IF @actionName = 'UpdateItem' 
	BEGIN
	DECLARE @oldFirmId INT, @oldIsConfirmed BIT 
	SELECT @oldFirmId = a.[firmID], @oldIsConfirmed = a.isConfirmed 
	FROM [Action] a 
	WHERE a.[actionID] = @actionID
	
	-- карточка «Свойства» присылает не все поля: чего нет - остаётся как было
	SET @firmID = ISNULL(@firmID, @oldFirmId)
	SET @isConfirmed = ISNULL(@isConfirmed, @oldIsConfirmed)

	IF (@oldFirmId <> @firmID)
		DELETE FROM [PaymentAction] WHERE [actionID] = @actionID

	IF (@oldIsConfirmed = 1 
		AND @isConfirmed = 0 AND EXISTS(SELECT * FROM dbo.[PaymentAction] pa WHERE pa.actionID = @actionID))
	BEGIN
		RAISERROR('ActionWithPaymentsCannotDeactivate',16,1)
		RETURN
	END

	UPDATE	
		[Action]
	SET			
		firmID = @firmID,
		userID = COALESCE(@newCreatorID, @userID, userID),
		isConfirmed = @isConfirmed,
		[comment] = CASE WHEN @comment IS NULL THEN [comment] ELSE NULLIF(LTRIM(RTRIM(@comment)), N'') END
	WHERE		
		actionID = @actionID

	EXEC Actions1 @actionID = @actionID
END
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[ActionIUD] TO PUBLIC
    AS [dbo];
GO

-- =====================================================================
-- 3. Действие «Свойства» у акции и права
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 77 AND tableName = 'Action')
    RAISERROR(N'Остановлено: сущность 77 (Рекламная акция) не найдена. Ничего не изменено.', 16, 1);
ELSE IF NOT EXISTS (SELECT 1 FROM dbo.iEntityAction WHERE entityID = 77 AND name = 'Properties')
BEGIN
    -- как у других сущностей (фирма: «Обновить», разделитель, «Свойства»): последним пунктом, через разделитель
    IF NOT EXISTS (SELECT 1 FROM dbo.iEntityAction WHERE entityID = 77 AND alias = N'-' AND ordinal_position = 700)
        INSERT INTO dbo.iEntityAction (entityID, alias, name, ordinal_position, isHidden, isGrantingAllowed, imgResourceName, parentID)
        VALUES (77, N'-', NULL, 700, 0, 0, NULL, NULL);
    INSERT INTO dbo.iEntityAction (entityID, alias, name, ordinal_position, isHidden, isGrantingAllowed, imgResourceName, parentID)
    VALUES (77, N'Свойства', 'Properties', 710, 0, 1, 'Icons.Properties.png', NULL);

    DECLARE @prop smallint = (SELECT entityActionID FROM dbo.iEntityAction WHERE entityID = 77 AND name = 'Properties');
    DECLARE @src TABLE (entityActionID smallint PRIMARY KEY);
    INSERT INTO @src SELECT entityActionID FROM dbo.iEntityAction
    WHERE entityID = 77 AND name IN ('Edit', 'ChangeFirm', 'ChangeCreator');

    INSERT INTO dbo.GroupRight (groupID, entityActionID)
    SELECT DISTINCT gr.groupID, @prop FROM dbo.GroupRight gr WHERE gr.entityActionID IN (SELECT entityActionID FROM @src);

    INSERT INTO dbo.UserAdditionRight (userID, entityActionID, isGrant)
    SELECT DISTINCT ur.userID, @prop, 1 FROM dbo.UserAdditionRight ur
    WHERE ur.entityActionID IN (SELECT entityActionID FROM @src) AND ur.isGrant = 1;

    DECLARE @msg nvarchar(400) = N'Действие «Свойства» (77/Properties) создано; выдано: групп '
        + CAST((SELECT COUNT(*) FROM dbo.GroupRight WHERE entityActionID = @prop) AS nvarchar(10))
        + N', персональных ' + CAST((SELECT COUNT(*) FROM dbo.UserAdditionRight WHERE entityActionID = @prop) AS nvarchar(10)) + N'.';
    PRINT @msg;
END
ELSE
BEGIN
    -- действие уже есть (ранняя редакция скрипта ставила его перед «Отметить как удалённую») — переносим в конец
    IF EXISTS (SELECT 1 FROM dbo.iEntityAction WHERE entityID = 77 AND name = 'Properties' AND ordinal_position = 465)
    BEGIN
        UPDATE dbo.iEntityAction SET ordinal_position = 710, imgResourceName = 'Icons.Properties.png' WHERE entityID = 77 AND name = 'Properties';
        IF NOT EXISTS (SELECT 1 FROM dbo.iEntityAction WHERE entityID = 77 AND alias = N'-' AND ordinal_position = 700)
            INSERT INTO dbo.iEntityAction (entityID, alias, name, ordinal_position, isHidden, isGrantingAllowed, imgResourceName, parentID)
            VALUES (77, N'-', NULL, 700, 0, 0, NULL, NULL);
        PRINT N'«Свойства» акции перенесены в конец меню.';
    END
    PRINT N'Действие «Свойства» у акции уже есть — права не трогаем.';
END
GO

-- =====================================================================
-- 4. Паспорт сущности 77
-- =====================================================================
DECLARE @xml nvarchar(max) = N'<passport>
	<page caption="Общие">
		<label caption="Номер акции:" name="actionID" />
		<label caption="Фирма:" name="firmName" />
		<label caption="Создатель:" name="creator" />
		<field caption="Активирована:" name="isConfirmed" disabled="true"/>
		<separator />
		<field caption="Начало:" name="startDate" disabled="true"/>
		<field caption="Окончание:" name="finishDate" disabled="true"/>
		<separator />
		<field caption="Создана:" name="createDate" type="df_datetime" disabled="true"/>
		<field caption="Изменена:" name="modDate" type="df_datetime" disabled="true"/>
		<separator />
		<field caption="Комментарий:" name="comment" height="80" maxlength="256"/>
	</page>
</passport>';

IF TRY_CAST(@xml AS xml) IS NULL
    RAISERROR(N'Остановлено: XML паспорта невалиден. Ничего не изменено.', 16, 1);
ELSE IF EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 77 AND CAST(passport AS nvarchar(max)) LIKE N'%name="comment"%')
BEGIN
    PRINT N'Паспорт акции уже содержит поле «Комментарий».';
    -- ранняя редакция скрипта записывала в паспорт и «Пакетную скидку» — убираем (она есть в журнале акций)
    IF EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 77 AND CAST(passport AS nvarchar(max)) LIKE N'%name="packageDiscountName"%')
    BEGIN
        UPDATE dbo.iEntity
        SET passport = REPLACE(CAST(passport AS nvarchar(max)), N'<label caption="Пакетная скидка:" name="packageDiscountName" />', N'')
        WHERE entityID = 77;
        PRINT N'Из паспорта акции убрана строка «Пакетная скидка».';
    END
END
ELSE IF EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 77 AND LEN(CAST(passport AS nvarchar(max))) > 0)
    RAISERROR(N'Остановлено: у сущности 77 уже есть другой паспорт — смотрите вручную. Ничего не изменено.', 16, 1);
ELSE
BEGIN
    UPDATE dbo.iEntity SET passport = @xml WHERE entityID = 77;
    PRINT N'Паспорт акции записан.';
END
GO

-- =====================================================================
-- 5. Переводы подписей паспорта (es)
-- =====================================================================
IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (VALUES
    (N'Номер акции:', N'Número de campaña:'),
    (N'Создатель:', N'Creador:'),
    (N'Активирована:', N'Activada:'),
    (N'Создана:', N'Creada:'),
    (N'Изменена:', N'Modificada:'),
    (N'Комментарий:', N'Comentario:')
    ) AS src ([source], [text])
       ON dst.lang = 'es' AND dst.context = ''
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES ('es', '', src.[source], src.[text]);
GO

-- =====================================================================
-- 6. Колонка «Комментарий» в журналах: акции (77) и удалённые акции (1236), селектор 0.
--    Actions1 отдаёт столбец сам (select a.*), процедуры не менялись.
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 77 AND name = 'comment' AND selector = 0)
    INSERT INTO dbo.iEntityAttribute (entityID, alias, name, ordinal_position, selector, dataType)
    VALUES (77, N'Комментарий', 'comment', 55, 0, NULL);   -- после «Активирована» (50)
IF NOT EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 1236 AND name = 'comment' AND selector = 0)
    INSERT INTO dbo.iEntityAttribute (entityID, alias, name, ordinal_position, selector, dataType)
    VALUES (1236, N'Комментарий', 'comment', 55, 0, NULL); -- после «Дата удаления» (50)
GO

-- =====================================================================
-- Проверка
-- =====================================================================
IF COL_LENGTH('dbo.[Action]', 'comment') IS NOT NULL
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionIUD')) LIKE N'%@comment%'
   AND EXISTS (SELECT 1 FROM dbo.iEntityAction WHERE entityID = 77 AND name = 'Properties')
   AND (SELECT CAST(passport AS nvarchar(max)) FROM dbo.iEntity WHERE entityID = 77) LIKE N'%name="comment"%'
   AND (SELECT COUNT(*) FROM dbo.iEntityAttribute WHERE entityID IN (77, 1236) AND name = 'comment' AND selector = 0) = 2
    PRINT N'ГОТОВО: свойства акции — колонка, процедура, действие, паспорт.';
ELSE
    RAISERROR(N'46: свойства акции применены не полностью (колонка/процедура/действие/паспорт).', 16, 1);
GO

-- Кому выдано «Свойства» (проверьте глазами)
SELECT N'группа' AS [кому], CAST(gr.groupID AS nvarchar(20)) AS [id]
FROM dbo.GroupRight gr
    JOIN dbo.iEntityAction a ON a.entityActionID = gr.entityActionID AND a.entityID = 77 AND a.name = 'Properties'
UNION ALL
SELECT N'пользователь', CAST(ur.userID AS nvarchar(20))
FROM dbo.UserAdditionRight ur
    JOIN dbo.iEntityAction a ON a.entityActionID = ur.entityActionID AND a.entityID = 77 AND a.name = 'Properties'
WHERE ur.isGrant = 1;
GO
