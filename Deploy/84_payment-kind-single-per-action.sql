-- Вид оплаты в рекламной акции один (29.10.2026, Миша): либо все кампании акции с типами оплаты с признаком
-- «Не включать в счет на предоплату» (PaymentType.isHidden = 1: прочее, промо, бонус, одежда эфира), либо все без него
-- (Безналичные, Наличные, Взаимозачёт).
-- 1. fn_ActionPaymentKindsMixed (новая): 1, если в акции есть кампании обоих видов.
-- 2. CampaignIUD: отказ PaymentKindMixedInAction
--    - AddItem: в акции есть кампании другого вида и ни одной того же;
--    - UpdateItem: кампания меняет вид оплаты или уходит в другую акцию (разделение), а в акции есть кампании другого вида.
--    Смена типа в пределах вида не проверяется. Новый параметр @checkPaymentKind bit = 1: 0 передаёт только массовая смена
--    типа оплаты акции (меняет все кампании в одной транзакции, вид проверяет заранее).
-- 3. MergeActions: отказ PaymentKindMixedInAction, если объединение двух акций одного вида каждая даёт смешанную.
-- 4. paymentTypeIUD UpdateItem: отказ PaymentTypeKindInUse — признак не меняется, пока тип стоит в акции рядом с другим типом.
-- 5. iMessage + испанский (iTranslation): PaymentKindMixedInAction, PaymentKindChangeNeedsAllCampaigns,
--    PaymentKindChangeRolledBack, PaymentTypeKindInUse.
-- Старые смешанные акции не правятся (на копии Artvis 91, на Tumen 8 на 29.10.2026): в них можно добавлять кампании того
-- вида, что в них уже есть, менять тип в пределах вида и привести акцию к одному виду; сделать смешанной новую — нельзя.
-- Клиент: новые Merlin.exe и веб (массовая смена «Сменить тип оплаты» у акции, проверки до клона и разделения). Старый клиент
-- тоже защищён сервером, но массовая смена вида у него не пройдёт (по одной кампании — отказ на первой).
-- После наката перезапустить qd2 и веб (новые сообщения). Параметры и выдача процедур прежние, новый параметр с умолчанием.
-- Идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 84_payment-kind-single-per-action.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO
-- 1, если в акции есть кампании и с типом оплаты «Не включать в счет на предоплату»
-- (PaymentType.isHidden = 1), и без него. В одной акции так быть не должно (с 29.10.2026,
-- PaymentKindMixedInAction); старые смешанные акции не правились.
CREATE OR ALTER FUNCTION [dbo].[fn_ActionPaymentKindsMixed] (@actionID INT)
RETURNS BIT
AS
BEGIN
    RETURN CASE WHEN EXISTS (SELECT 1 FROM dbo.Campaign c INNER JOIN dbo.PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
                             WHERE c.actionID = @actionID AND pt.isHidden = 1)
                 AND EXISTS (SELECT 1 FROM dbo.Campaign c INNER JOIN dbo.PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
                             WHERE c.actionID = @actionID AND pt.isHidden = 0)
                THEN 1 ELSE 0 END;
END

GO

CREATE OR ALTER PROCEDURE [dbo].[CampaignIUD]
(
@campaignID int OUT,
@actionID int = NULL,
@campaignTypeID tinyint = NULL,
@massmediaID smallint = NULL,
@paymentTypeID smallint = NULL,
@agencyID smallint = NULL,
@loggedUserId smallint,
@actionName varchar(32),
@needShow bit = 1,
@managerDiscount decimal(18, 10) = 1,
-- 0 — только массовая смена типа оплаты акции (ActionOnMassmedia.ApplyPaymentTypeChangeMass):
-- она меняет все кампании в одной транзакции, а вид проверяет заранее
@checkPaymentKind bit = 1
)
WITH EXECUTE AS OWNER
as
set nocount on

declare @IsAdmin bit, @IsTraffic bit
set @IsAdmin = dbo.f_IsAdmin(@loggedUserID)
set @IsTraffic = dbo.f_IsTrafficManager(@loggedUserID)

-- Only admin is allowed to delete issue which is in the past already
if @actionName = 'DeleteItem' and @IsAdmin <> 1 and @IsTraffic <> 1
	and exists(select * 
				from TariffWindow tw 
					inner join Issue i on tw.windowId = i.actualWindowID 
				where i.campaignID = @campaignID and tw.dayOriginal <= dbo.ToShortDate(getdate()))
begin
	raiserror('PastIssue', 16, 1)
	return
end 

if @actionID is null and @campaignID is not null
	select @actionID = c.actionID from Campaign c where c.campaignID = @campaignID

IF @actionName = 'AddItem' begin
	if @agencyID is null 
	begin 
		raiserror('CannotAddCampaignWithoutAgency', 16, 1)
		return
	end

	if @IsAdmin = 0 and @IsTraffic = 0 and exists(select * 
				from [Action] a 
					inner join Campaign c on a.actionID = c.actionID
				where a.actionID = @actionID and a.finishDate < getdate() and a.isConfirmed = 1)
	begin 
		raiserror('CannotAddCampaignInConfirmedFinishedAction', 16, 1)
		return 
	end 

	-- В акции все кампании одного вида оплаты: либо все с «Не включать в счет на предоплату»
	-- (PaymentType.isHidden), либо все без. В старую смешанную акцию можно добавить кампанию
	-- того вида, который в ней уже есть.
	if @checkPaymentKind = 1
		and exists(select * from Campaign c inner join PaymentType pt on pt.paymentTypeID = c.paymentTypeID
					where c.actionID = @actionID
						and pt.isHidden <> (select isHidden from PaymentType where paymentTypeID = @paymentTypeID))
		and not exists(select * from Campaign c inner join PaymentType pt on pt.paymentTypeID = c.paymentTypeID
					where c.actionID = @actionID
						and pt.isHidden = (select isHidden from PaymentType where paymentTypeID = @paymentTypeID))
	begin
		raiserror('PaymentKindMixedInAction', 16, 1)
		return
	end

	select @managerDiscount = [dbo].[fn_GetMaxUserDiscount](@loggedUserId, GETDATE(), [dbo].[GetMaxDate]())

	INSERT INTO [Campaign](actionID, campaignTypeID, massmediaID, paymentTypeID, agencyID, modUser, managerDiscount)
	VALUES(@actionID, @campaignTypeID, @massmediaID, @paymentTypeID, @agencyID, @loggedUserId, @managerDiscount)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @campaignID = SCOPE_IDENTITY()

	if @needShow = 1
		EXEC Campaigns @CampaignID = @CampaignID, @actionID = @actionID
END
ELSE IF @actionName = 'DeleteItem' begin
	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When [maxCapacity] = 0 
					Then timeInUseConfirmed - t1.duration
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
		where i.campaignID = @campaignID
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID

	insert into [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
	select @loggedUserID, @actionID, i.rollerID, tw.windowDateActual, tw.massmediaID 
	from Issue i 
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
	where i.campaignID = @CampaignID and i.isConfirmed = 1

	if exists(select *
		from Issue i 
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
		where i.campaignID = @CampaignID and i.isConfirmed = 1 and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog())
	begin 
		exec SayAdminThatIssuesDelete @loggedUserID, @actionID
	end 

	-- Политическая агитация: выпуски удаляются массово, минуя IssueIUD, поэтому
	-- окна с агитацией запоминаем до удаления и снимаем обвязку после
	DECLARE @agitWindows table (windowID int primary key)
	INSERT INTO @agitWindows (windowID)
	SELECT DISTINCT i.actualWindowID
	FROM Issue i INNER JOIN Roller r ON r.rollerID = i.rollerID
	WHERE i.campaignID = @CampaignID AND r.rolActionTypeID = 6 AND i.isConfirmed = 1

	DELETE Issue WHERE CampaignID = @CampaignID
	DELETE FROM [Campaign] WHERE CampaignID = @CampaignID

	DECLARE @agitWindowID int
	DECLARE cur_agit_camp CURSOR LOCAL FOR SELECT windowID FROM @agitWindows

	OPEN cur_agit_camp
	FETCH NEXT FROM cur_agit_camp INTO @agitWindowID
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC AgitationFraming
			@actionName = 'CleanupWindow',
			@windowID = @agitWindowID,
			@loggedUserID = @loggedUserID

		FETCH NEXT FROM cur_agit_camp INTO @agitWindowID
	END
	CLOSE cur_agit_camp
	DEALLOCATE cur_agit_camp
END
ELSE IF @actionName = 'UpdateItem' BEGIN

    -- Read current values before update
    DECLARE @currentAgencyID     SMALLINT;
    DECLARE @currentPaymentTypeID SMALLINT;

    SELECT
        @currentAgencyID      = agencyID,
        @currentPaymentTypeID = paymentTypeID
    FROM dbo.Campaign
    WHERE CampaignID = @CampaignID;

    -- Check agencyID change: block if a linked payment belongs to the old agency
    IF @currentAgencyID <> @agencyID
    BEGIN
        DECLARE @conflictingPaymentID INT;

        SELECT TOP 1 @conflictingPaymentID = pa.paymentID
        FROM dbo.Campaign c
        INNER JOIN dbo.PaymentAction pa ON pa.actionID  = c.actionID
        INNER JOIN dbo.Payment p        ON p.paymentID  = pa.paymentID
        WHERE c.CampaignID = @CampaignID
          AND p.agencyID   = @currentAgencyID
          AND p.isEnabled  = 1;

        IF @conflictingPaymentID IS NOT NULL
            RAISERROR('CannotChangeAgency_PaymentExists', 16, 1);
    END

    -- Check paymentTypeID change: block if a linked payment has the old paymentTypeID
    IF @currentPaymentTypeID <> @paymentTypeID
    BEGIN
        DECLARE @conflictingPaymentID2 INT;

        SELECT TOP 1 @conflictingPaymentID2 = pa.paymentID
        FROM dbo.Campaign c
        INNER JOIN dbo.PaymentAction pa ON pa.actionID      = c.actionID
        INNER JOIN dbo.Payment p        ON p.paymentID      = pa.paymentID
        WHERE c.CampaignID        = @CampaignID
          AND p.paymentTypeID     = @currentPaymentTypeID
          AND p.isEnabled         = 1;

        IF @conflictingPaymentID2 IS NOT NULL
            RAISERROR('CannotChangePaymentType_PaymentExists', 16, 1);
    END

    -- Вид оплаты (PaymentType.isHidden) кампании сменился или она уходит в другую акцию
    -- (разделение акции): остальные кампании акции должны быть того же вида. Смена типа
    -- в пределах вида старую смешанную акцию не задевает.
    IF @checkPaymentKind = 1
    BEGIN
        DECLARE @newIsHidden BIT = (SELECT isHidden FROM dbo.PaymentType WHERE paymentTypeID = @paymentTypeID);

        IF EXISTS (SELECT 1 FROM dbo.Campaign c INNER JOIN dbo.PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
                   WHERE c.campaignID = @campaignID AND (pt.isHidden <> @newIsHidden OR c.actionID <> @actionID))
           AND EXISTS (SELECT 1 FROM dbo.Campaign c INNER JOIN dbo.PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
                       WHERE c.actionID = @actionID AND c.campaignID <> @campaignID AND pt.isHidden <> @newIsHidden)
        BEGIN
            RAISERROR('PaymentKindMixedInAction', 16, 1);
            RETURN;
        END
    END

    UPDATE dbo.Campaign
    SET
        paymentTypeID = @paymentTypeID,
        agencyID      = @agencyID,
        modUser       = @loggedUserId,
        actionID      = @actionID
    WHERE CampaignID = @CampaignID;

    IF @needShow = 1
        EXEC Campaigns @CampaignID = @CampaignID, @actionID = @actionID;
END
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[CampaignIUD] TO PUBLIC
    AS [dbo];



GO

CREATE OR ALTER PROCEDURE [dbo].[MergeActions]
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

	-- Объединение не должно делать акцию смешанной по виду оплаты (PaymentType.isHidden):
	-- две акции одного вида — можно, разных — нельзя. Если одна уже смешанная (старые данные),
	-- не мешаем.
	if dbo.fn_ActionPaymentKindsMixed(@firstActionID) = 0
		and dbo.fn_ActionPaymentKindsMixed(@secondActionID) = 0
		and exists(select * from Campaign c inner join PaymentType pt on pt.paymentTypeID = c.paymentTypeID
					where c.actionID in (@firstActionID, @secondActionID) and pt.isHidden = 1)
		and exists(select * from Campaign c inner join PaymentType pt on pt.paymentTypeID = c.paymentTypeID
					where c.actionID in (@firstActionID, @secondActionID) and pt.isHidden = 0)
	begin
		raiserror('PaymentKindMixedInAction', 16, 1)
		return
	end

	DECLARE @deletedActionID INT, @managerDiscount DECIMAL(18, 10), @managerDiscount2 DECIMAL(18, 10)
	
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
	SELECT c.[campaignID], c.[campaignTypeID], c.managerDiscount
	FROM [Campaign] c
	WHERE c.[actionID] = @deletedActionID

	DECLARE @campaignID INT, @campaignNewID INT, @campaignType SMALLINT
	OPEN cur_Campaigns
	FETCH NEXT FROM cur_Campaigns INTO @campaignID, @campaignType, @managerDiscount

	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @campaignNewID = 0
		SELECT @campaignNewID = c1.[campaignID], @managerDiscount2 = c1.managerDiscount
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
		
			UPDATE [Campaign] 
			SET managerDiscount = GREATEST(@managerDiscount, @managerDiscount2) 
			WHERE campaignID = @campaignNewID
		END
		
		FETCH NEXT FROM cur_Campaigns INTO @campaignID, @campaignType, @managerDiscount
	end
	
	close cur_Campaigns
	deallocate cur_Campaigns
		
	UPDATE [Campaign] SET [actionID] = @liveActionID WHERE [actionID] = @deletedActionID
	DELETE FROM ACTION WHERE [actionID] = @deletedActionID

	Exec [dbo].[ActionRecalculate] @actionID = @liveActionID, @loggedUserID = @loggedUserID

END
GO

CREATE OR ALTER PROC [dbo].[paymentTypeIUD]
(
@paymentTypeID int = NULL OUT,
@name nvarchar(32) = NULL,
@isHidden bit = NULL,
@actionName varchar(32),
@isActive bit = 0
)
AS

SET NOCOUNT ON
IF @actionName = 'AddItem' BEGIN
	INSERT INTO  paymentType(name, isHidden, isActive) 
	VALUES(@name, @isHidden, @isActive)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @paymentTypeID = SCOPE_IDENTITY()
	END
ELSE IF @actionName = 'DeleteItem' BEGIN
	DELETE FROM paymentType WHERE paymentTypeID = @paymentTypeID
	END
ELSE IF @actionName = 'UpdateItem' BEGIN
	-- «Не включать в счет на предоплату» (isHidden) — вид оплаты, в акции все кампании одного
	-- вида. Пока тип стоит в акции рядом с другим типом оплаты, признак не меняется.
	IF EXISTS (SELECT 1 FROM paymentType WHERE paymentTypeID = @paymentTypeID AND isHidden <> @isHidden)
		AND EXISTS (SELECT 1 FROM Campaign c
						INNER JOIN Campaign c2 ON c2.actionID = c.actionID AND c2.paymentTypeID <> c.paymentTypeID
					WHERE c.paymentTypeID = @paymentTypeID)
	BEGIN
		RAISERROR('PaymentTypeKindInUse', 16, 1)
		RETURN
	END

	UPDATE paymentType
	SET 	name = @name, 
			isActive = @isActive,
			isHidden = @isHidden
	WHERE paymentTypeID = @paymentTypeID

	END
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[paymentTypeIUD] TO PUBLIC
    AS [dbo];
GO

-- Сообщения об отказе (+ испанский, где есть многоязычность веба)
DECLARE @msg1 NVARCHAR(4000) = N'В одной рекламной акции все кампании должны быть с типами оплаты одного вида: либо все с признаком «Не включать в счет на предоплату», либо все без него. Операция прервана.';
DECLARE @es1  NVARCHAR(4000) = N'En una misma campaña publicitaria todas las pautas deben tener tipos de pago del mismo grupo: o todos con la marca «No incluir en la factura de anticipo», o todos sin ella. Operación cancelada.';
IF EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'PaymentKindMixedInAction')
    UPDATE dbo.iMessage SET [message] = @msg1 WHERE name = 'PaymentKindMixedInAction';
ELSE
    INSERT INTO dbo.iMessage (name, [message]) VALUES ('PaymentKindMixedInAction', @msg1);
IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (SELECT 'es' AS lang, '' AS context, @msg1 AS [source], @es1 AS [text]) AS src
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);

DECLARE @msg2 NVARCHAR(4000) = N'У выбранного типа оплаты признак «Не включать в счет на предоплату» другой, чем у остальных кампаний акции, а в одной акции все кампании должны быть одного вида. Отметьте все кампании акции. Операция прервана.';
DECLARE @es2  NVARCHAR(4000) = N'El tipo de pago elegido tiene la marca «No incluir en la factura de anticipo» distinta de la de las demás pautas de la campaña publicitaria, y en una misma campaña publicitaria todas las pautas deben ser del mismo grupo. Marque todas las pautas de la campaña. Operación cancelada.';
IF EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'PaymentKindChangeNeedsAllCampaigns')
    UPDATE dbo.iMessage SET [message] = @msg2 WHERE name = 'PaymentKindChangeNeedsAllCampaigns';
ELSE
    INSERT INTO dbo.iMessage (name, [message]) VALUES ('PaymentKindChangeNeedsAllCampaigns', @msg2);
IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (SELECT 'es' AS lang, '' AS context, @msg2 AS [source], @es2 AS [text]) AS src
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);

DECLARE @msg3 NVARCHAR(4000) = N'Тип оплаты не изменён ни у одной кампании: при смене признака «Не включать в счет на предоплату» кампании акции меняются только все вместе.';
DECLARE @es3  NVARCHAR(4000) = N'El tipo de pago no se cambió en ninguna pauta: al cambiar la marca «No incluir en la factura de anticipo», las pautas de la campaña publicitaria solo se cambian todas juntas.';
IF EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'PaymentKindChangeRolledBack')
    UPDATE dbo.iMessage SET [message] = @msg3 WHERE name = 'PaymentKindChangeRolledBack';
ELSE
    INSERT INTO dbo.iMessage (name, [message]) VALUES ('PaymentKindChangeRolledBack', @msg3);
IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (SELECT 'es' AS lang, '' AS context, @msg3 AS [source], @es3 AS [text]) AS src
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);

DECLARE @msg4 NVARCHAR(4000) = N'Признак «Не включать в счет на предоплату» нельзя изменить: этот тип оплаты стоит в рекламных акциях вместе с другими типами оплаты, а в одной акции все кампании должны быть одного вида. Операция прервана.';
DECLARE @es4  NVARCHAR(4000) = N'No se puede cambiar la marca «No incluir en la factura de anticipo»: este tipo de pago se usa en campañas publicitarias junto con otros tipos de pago, y en una misma campaña publicitaria todas las pautas deben ser del mismo grupo. Operación cancelada.';
IF EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'PaymentTypeKindInUse')
    UPDATE dbo.iMessage SET [message] = @msg4 WHERE name = 'PaymentTypeKindInUse';
ELSE
    INSERT INTO dbo.iMessage (name, [message]) VALUES ('PaymentTypeKindInUse', @msg4);
IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (SELECT 'es' AS lang, '' AS context, @msg4 AS [source], @es4 AS [text]) AS src
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);

GO
