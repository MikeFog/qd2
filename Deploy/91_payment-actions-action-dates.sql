-- Журнал оплат (сущность 146, «Оплата акции»): колонки «Начало акции» и «Окончание акции» (Action.startDate/finishDate),
-- после «Менеджер». Просьба Миши 06.11.2026 — для «Журнала оплат по менеджерам». Колонки, как и «Менеджер» (Deploy/72),
-- появляются во всех таблицах сущности 146 и в десктопе, и в вебе: нижняя таблица журнала оплат, оба «Журнала оплат по
-- менеджерам», платежи баланса фирмы. Подписи — как в окне «Изменение затронет акции» (DiscountChangeImpact, Deploy/86);
-- испанский «Inicio de la campaña» / «Fin de la campaña» уже есть (Deploy/86). Тип date2 — как у «Даты счёта»: в вебе и
-- Excel дата без времени.
--
-- PaymentCommonActions: полный текст процедуры Deploy/72 плюс два поля акции (строк не прибавляется — у платежа по акции
-- одна акция). Параметры прежние. Метаданные: две строки iEntityAttribute (ordinal 13, 14).
--
-- После Deploy/72 (и 08). Идемпотентен. Клиент и веб не нужны; после наката перезапустить qd2 и веб (метаданные читаются
-- при старте). Не перезапускать после 91: 21, 08, 72 и ArtvisDB/Scripts/web-i18n-sql-deploy.sql (несут старый текст
-- PaymentCommonActions — колонки молча опустеют); если перезапустили — повторить 91.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 91_payment-actions-action-dates.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.fn_Translate') IS NULL
BEGIN
    RAISERROR(N'91: сначала Deploy/08 (fn_Translate). Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PaymentCommonActions]
(
@paymentID int = null,
@managerID smallint = null,
@startOfInterval datetime = null,
@endOfInterval datetime = null,
@agencyID smallint = null,
@paymentTypeID smallint = null,
@firmID smallint = null,
--@paymentTypesIDString varchar(1024) = null,
@agenciesIDString varchar(1024) = NULL,
@isHideWhite BIT = 0,
@isHideBlack BIT = 0,
@showBlack bit = 1,
@showWhite bit = 1,
@loggedUserID smallint,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
DECLARE @tActionPayment NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Оплата акции №');
CREATE TABLE #Agency(agencyID smallint)
CREATE Table #PaymentType (paymentTypeID smallint)

-- Populate temporary tables with Agency and Payment types
IF @agenciesIDString Is Null
	IF @agencyID IS NULL
		INSERT INTO #Agency SELECT agencyID FROM Agency
	ELSE
		INSERT INTO #Agency VALUES(@agencyID)
Else
	Exec dbo.hlp_PopulateTableFromCommaSeparatedString '#Agency', @agenciesIDString

	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

select distinct
	psoa.*,
	@tActionPayment + LTrim(psoa.actionID) as name,
	f.name as firmName,
	a.name as agencyName,
	pt.name as paymentTypeName,
	b.billNo,
	b.billDate,
	u.userName as manager,
	soa.startDate as actionStartDate,
	soa.finishDate as actionFinishDate
FROM
	[PaymentAction] psoa
	INNER JOIN [Action] soa ON soa.actionID = psoa.actionID
	inner join [Campaign] c on soa.[actionID] = c.[actionID]
	INNER JOIN Payment pso ON pso.paymentID = psoa.paymentID
	INNER JOIN Firm f ON f.firmID = pso.firmID
	INNER JOIN Agency a ON a.agencyID = pso.agencyID
	INNER JOIN PaymentType pt ON pt.paymentTypeID = pso.paymentTypeID
	-- счёт, выписанный по акции агентству платежа (Bill: не больше одного на акцию+агентство)
	left join Bill b on b.actionID = psoa.actionID and b.agencyID = pso.agencyID
	-- менеджер акции; имя — как у колонки «Менеджер» в «Остатке» (SpecialActions) и статистике
	left join [User] u on u.userID = soa.userID
	left join @massmedias umm on c.massmediaID = umm.massmediaID
	left join GroupMember gm on soa.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
where
	(soa.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
			(soa.isSpecial = 1 or (c.campaignTypeID <> 4 and umm.massmediaID is not null and ((soa.userID = @loggedUserID and umm.myMassmedia = 1) or (soa.userID <> @loggedUserID and umm.foreignMassmedia = 1) )) 
				or (c.campaignTypeID = 4 and not exists(select * 
														from PackModuleIssue pmi 
															inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
															inner join Module m on pmc.moduleID = m.moduleID
															left join @massmedias ummm on m.massmediaID = ummm.massmediaID
														where pmi.campaignID = c.campaignID and (ummm.massmediaID is null or 
															(soa.userID = @loggedUserID and umm.myMassmedia = 0) or
															 (soa.userID <> @loggedUserID and umm.foreignMassmedia = 0) )))) and	
	psoa.[paymentID] = Coalesce(@paymentID, psoa.[paymentID]) And
	pso.paymentDate BETWEEN Coalesce(@startOfInterval, pso.paymentDate)
		And Coalesce(dateadd(ss, -1, dateadd(day, 1, @endOfInterval)), pso.paymentDate)	And
	soa.userID = Coalesce(@managerID, soa.[userID]) And
	pso.agencyID IN (Select agencyID From #Agency) And
	soa.firmID = Coalesce(@firmID, soa.firmID) AND 
	(pt.isHidden = 0 or @isHideWhite = 0) And
	(pt.isHidden = 1 or @isHideBlack = 0) and
	((pt.IsHidden = 1 and @showBlack = 1)  or
	(pt.IsHidden = 0 and @showWhite = 1)) 
ORDER BY
	psoa.actionID desc
GO

-- =====================================================================
-- Метаданные: колонки «Начало акции», «Окончание акции» сущности 146
-- =====================================================================
IF NOT EXISTS (SELECT * FROM dbo.iEntityAttribute WHERE entityID = 146 AND name = 'actionStartDate')
    INSERT INTO dbo.iEntityAttribute (entityID, alias, name, ordinal_position, selector, dataType)
    VALUES (146, N'Начало акции', 'actionStartDate', 13, 0, 'date2');
IF NOT EXISTS (SELECT * FROM dbo.iEntityAttribute WHERE entityID = 146 AND name = 'actionFinishDate')
    INSERT INTO dbo.iEntityAttribute (entityID, alias, name, ordinal_position, selector, dataType)
    VALUES (146, N'Окончание акции', 'actionFinishDate', 14, 0, 'date2');
GO

-- =====================================================================
-- Проверка
-- =====================================================================
SELECT entityID, alias, name, ordinal_position, dataType FROM dbo.iEntityAttribute WHERE entityID = 146 ORDER BY ordinal_position;
IF CHARINDEX(N'soa.finishDate as actionFinishDate', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.PaymentCommonActions')), N'')) > 0
   AND CHARINDEX(N'userName as manager', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.PaymentCommonActions')), N'')) > 0
   AND (SELECT COUNT(*) FROM dbo.iEntityAttribute WHERE entityID = 146 AND name IN ('actionStartDate', 'actionFinishDate')) = 2
    PRINT N'ГОТОВО: колонки «Начало акции» и «Окончание акции» в журнале оплат (146) на месте.';
ELSE
    RAISERROR(N'91: колонки дат акции журнала оплат не записаны.', 16, 1);
GO
SET NOEXEC OFF;
GO
