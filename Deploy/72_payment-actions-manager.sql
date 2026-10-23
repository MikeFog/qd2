-- Журнал оплат (сущность 146, «Оплата акции»): колонка «Менеджер» — менеджер акции (Action.userID → [User].userName,
-- «Имя Фамилия», как «Менеджер» в «Остатке» и статистике), после «Фирма». Решение владельца 22.10.2026 (вариант 1,
-- docs/tasks/web-menu-inventory.md «Журнал оплат по менеджерам»): вместо десктопного диалога выбора менеджеров и окна на
-- каждого — один журнал 146 за период, чьи оплаты — видно колонкой. Колонка появляется во всех таблицах сущности 146 и в
-- десктопе, и в вебе: нижняя таблица журнала оплат, оба «Журнала оплат по менеджерам», платежи баланса фирмы при выбранном
-- менеджере. Испанский «Менеджер» → «Gerente» уже есть (Deploy/08).
--
-- PaymentCommonActions: left join [User] (Action.userID допускает NULL — строки не теряются и не размножаются), полный
-- текст процедуры, включая № и дату счёта Deploy/21. Метаданные: строка iEntityAttribute (ordinal 12). Параметры прежние.
--
-- После Deploy/08 (fn_Translate, @languageCode) — без него «Ничего не изменено». Идемпотентен. Клиент для колонки не
-- нужен (новый веб — только для пункта «Бухгалтерия → Журнал оплат по менеджерам»); после наката перезапустить qd2 и веб
-- (метаданные читаются при старте). Не перезапускать после 72: 21, 08 и ArtvisDB/Scripts/web-i18n-sql-deploy.sql (несут
-- старый текст PaymentCommonActions — колонка молча исчезнет); если перезапустили — повторить 72.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 72_payment-actions-manager.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.fn_Translate') IS NULL
BEGIN
    RAISERROR(N'72: сначала Deploy/08 (fn_Translate). Ничего не изменено.', 16, 1);
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
	u.userName as manager
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
-- Метаданные: колонка «Менеджер» сущности 146
-- =====================================================================
IF NOT EXISTS (SELECT * FROM dbo.iEntityAttribute WHERE entityID = 146 AND name = 'manager')
    INSERT INTO dbo.iEntityAttribute (entityID, alias, name, ordinal_position, selector, dataType)
    VALUES (146, N'Менеджер', 'manager', 12, 0, NULL);
GO

-- =====================================================================
-- Проверка
-- =====================================================================
SELECT entityID, alias, name, ordinal_position, dataType FROM dbo.iEntityAttribute WHERE entityID = 146 ORDER BY ordinal_position;
IF CHARINDEX(N'userName as manager', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.PaymentCommonActions')), N'')) > 0
   AND EXISTS (SELECT * FROM dbo.iEntityAttribute WHERE entityID = 146 AND name = 'manager')
    PRINT N'ГОТОВО: колонка «Менеджер» в журнале оплат (146) на месте.';
ELSE
    RAISERROR(N'72: колонка «Менеджер» журнала оплат не записана.', 16, 1);
GO
SET NOEXEC OFF;
GO
