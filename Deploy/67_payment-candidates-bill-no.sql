-- Диалог «Акции на оплату» (журнал оплат → «Выбрать акции для оплаты», сущность 147):
-- колонки «№ счёта» и «Дата счёта» сразу после «№ Акции» — видно, к какому счёту идёт оплата.
-- Продолжение Deploy/21 (те же колонки в нижней таблице журнала оплат, сущность 146).
--
-- ActionsForPaymentCommon добавляет billNo/billDate из Bill по акции и агентству ПЛАТЕЖА (left join;
-- ключ Bill — actionID+agencyID, строки не размножаются). Нет счёта этому агентству — пусто.
-- Процедуру зовёт только этот диалог (десктоп PaymentCandidatesForm и веб PaymentCandidatesForm.razor;
-- веб колонки берёт не из метаданных и новые поля игнорирует).
-- Метаданные: две строки iEntityAttribute (ordinal 2 и 3, «№ Акции» = 1); испанский уже есть (Deploy/21).
--
-- Идемпотентен. Клиент для колонок не нужен (после наката — перезапустить qd2, метаданные читаются
-- при старте). Не перезапускать после 67: ArtvisDB/Scripts/campaign-finalprice-with-pack-deploy.sql
-- (несёт старую версию процедуры); если перезапустили — повторить 67.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 67_payment-candidates-bill-no.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROC [dbo].[ActionsForPaymentCommon]
(
@paymentID int,
@loggedUserID smallint
)
AS
SET NOCOUNT ON

-- Проверка роли пользователя
DECLARE @isAdmin bit = 0
DECLARE @isBookKeeper bit = 0

SELECT 
	@isAdmin = IsAdmin,
	@isBookKeeper = IsBookKeeper
FROM [dbo].[user]
WHERE userID = @loggedUserID

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

declare @isRightToViewForeignActions bit,@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

DECLARE @paidUP TABLE(actionID INT, paidUp decimal(18,2), paymentIsHidden bit, agencyID smallint)

INSERT INTO @paidUP
SELECT distinct
	a.actionID,
	IsNull(sum(pa.summa), 0),
	pt.isHidden,
	p.agencyID
FROM 
	(
		select distinct a.actionID, a.firmID 
		from [Action] a 
			inner join Campaign c on a.actionID = c.actionID
			inner join 
			(
				select distinct u.userID 
				from [User] u
					left join [GroupMember] gm on u.userID = gm.userID
					left join @ugroups ug on gm.groupID = ug.id
				where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
			) as x on a.userID = x.userID
			left join @massmedias umm on c.massmediaID = umm.massmediaID
			where ((@isAdmin = 1 or @isBookKeeper = 1) and a.isSpecial = 1) or
		 		((c.campaignTypeID <> 4 and umm.massmediaID is not null and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1) )) 
					or (c.campaignTypeID = 4 and not exists(select * 
														from PackModuleIssue pmi 
															inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
															inner join Module m on pmc.moduleID = m.moduleID
															left join @massmedias ummm on m.massmediaID = ummm.massmediaID
														where pmi.campaignID = c.campaignID and (ummm.massmediaID is null or 
															(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
															 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0))))) 
	) a
	INNER JOIN Payment p ON p.firmID = a.firmID  
	LEFT JOIN PaymentAction pa ON pa.actionID = a.actionID 
		AND p.[paymentID] = pa.[paymentID] 
	LEFT JOIN [PaymentType] pt ON p.[paymentTypeID] = pt.[paymentTypeID]
GROUP BY 
	a.actionID, pt.isHidden,p.agencyID

SELECT 
	a.actionID,
	SUM(c.finalPrice) AS finalPrice,
	pu.paidUp as paidUp,
	b.billNo,
	b.billDate
FROM 
	[Action] a
	INNER JOIN @paidUP pu ON a.[actionID] = pu.[actionID]
	INNER JOIN [Campaign] c ON c.actionID = a.actionID
	INNER JOIN Payment p ON p.firmID = a.firmID
		And p.agencyID = c.[agencyID] and pu.agencyID = p.agencyID
	INNER JOIN [PaymentType] pt2 ON p.[paymentTypeID] = pt2.[paymentTypeID]
		AND pt2.[isHidden] = pu.paymentIsHidden
	INNER JOIN [PaymentType] pt ON c.[paymentTypeID] = pt.[paymentTypeID]
		AND pt.[isHidden] = pu.paymentIsHidden
	-- счёт по акции агентству платежа (Bill: не больше одного на акцию+агентство)
	LEFT JOIN Bill b ON b.actionID = a.actionID AND b.agencyID = p.agencyID
WHERE
	p.paymentID = @paymentID AND a.[isConfirmed] = 1
GROUP BY 
	a.actionID, pu.paidUp, b.billNo, b.billDate
HAVING
	cast(SUM(c.finalPrice)*100 as int) - cast(pu.paidUp * 100 as int) > 0

GO

-- =====================================================================
-- Метаданные: колонки сущности 147
-- =====================================================================
IF NOT EXISTS (SELECT * FROM dbo.iEntityAttribute WHERE entityID = 147 AND name = 'billNo')
    INSERT INTO dbo.iEntityAttribute (entityID, alias, name, ordinal_position, selector, dataType)
    VALUES (147, N'№ счёта', 'billNo', 2, 0, NULL);
IF NOT EXISTS (SELECT * FROM dbo.iEntityAttribute WHERE entityID = 147 AND name = 'billDate')
    INSERT INTO dbo.iEntityAttribute (entityID, alias, name, ordinal_position, selector, dataType)
    VALUES (147, N'Дата счёта', 'billDate', 3, 0, 'date2');
GO

-- =====================================================================
-- Проверка
-- =====================================================================
SELECT entityID, alias, name, ordinal_position, dataType FROM dbo.iEntityAttribute WHERE entityID = 147 ORDER BY ordinal_position;
SELECT o.name, o.modify_date FROM sys.objects o WHERE o.name = 'ActionsForPaymentCommon';
PRINT N'ГОТОВО';
GO
