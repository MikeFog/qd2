CREATE PROC [dbo].[PaymentsCommon]
(
@paymentID int = Null,
@startOfInterval datetime = Null,
@endOfInterval datetime = Null,
@firmID smallint = Null,
@headCompanyId smallint = Null,
@paymentTypeID smallint = Null,
@agencyID smallint  = Null,
@isNotClosedOnly bit = 0,
@agenciesIDString varchar(1024) = null,
@actionID INT = NULL,
@isHideWhite BIT = 0,
@isHideBlack BIT = 0,
@filterAgencies bit = 0,
@loggedUserID int ,
@showBlack bit = 1,
@showWhite bit = 1
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
CREATE TABLE #PaymentsCommon(paymentID int)

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

IF @agenciesIDString is not Null begin -- баланс для фирмы
	CREATE TABLE #Agency (agencyID smallint)

	Exec hlp_PopulateTableFromCommaSeparatedString '#Agency', @agenciesIDString
	
	INSERT INTO #PaymentsCommon(paymentID)
	select distinct 
		p.paymentID
	FROM
		[Payment] p
		INNER JOIN firm f ON f.firmID = p.firmID
		Inner Join HeadCompany hc on hc.headCompanyID = f.headCompanyID
		INNER JOIN agency a ON a.agencyID = p.agencyID
		INNER JOIN paymentType pt ON pt.paymentTypeID = p.paymentTypeID
		inner JOIN [user] u ON u.userID = p.userID 
		LEFT JOIN [PaymentAction] pa ON pa.[paymentID] = p.[paymentID]
			AND pa.[actionID] = COALESCE(@actionID, pa.[actionID])
		left join 
			(
				select distinct am.agencyID from AgencyMassmedia am 
					inner join @massmedias mm on am.massmediaID = mm.massmediaID and mm.foreignMassmedia = 1
			) x on p.agencyID = x.agencyID
	where
		p.firmID = Coalesce(@firmID, p.firmID)
		And f.headCompanyID = Coalesce(@headCompanyId, f.headCompanyID)
		AND p.paymentDate >= Coalesce(@startOfInterval, p.paymentDate)
		AND p.paymentDate <= Coalesce(@endOfInterval, p.paymentDate)
		AND p.agencyID IN (SELECT agencyID From #Agency)
		AND (pt.isHidden = 0 or @isHideWhite = 0) And
			(pt.isHidden = 1 or @isHideBlack = 0) and
			((pt.IsHidden = 1 and @showBlack = 1)  or
			(pt.IsHidden = 0 and @showWhite = 1)) 
		and ((@filterAgencies = 0 and x.agencyID is not null) or 
			(exists(select * from Campaign cam inner join [Action] act on cam.actionID = act.actionID where a.agencyID = cam.agencyID and @loggedUserID = act.userID ) 
				or exists(select * from Payment pay where a.agencyID = pay.agencyID and @loggedUserID = pay.userID)))
	End
else 
	INSERT INTO #PaymentsCommon(paymentID)
	select distinct
		p.paymentID
	FROM
		[Payment] p
		INNER JOIN firm f ON f.firmID = p.firmID
		Inner Join HeadCompany hc on hc.headCompanyID = f.headCompanyID
		INNER JOIN agency a ON a.agencyID = p.agencyID
		INNER JOIN paymentType pt ON pt.paymentTypeID = p.paymentTypeID
		INNER JOIN [user] u ON u.userID = p.userID
		LEFT JOIN [PaymentAction] pa ON pa.[paymentID] = p.[paymentID]
		left join 
			(
				select distinct am.agencyID from AgencyMassmedia am 
					inner join @massmedias mm on am.massmediaID = mm.massmediaID and mm.foreignMassmedia = 1
			) x on p.agencyID = x.agencyID
	WHERE
		p.firmID = Coalesce(@firmID, p.firmID)
		And f.headCompanyID = Coalesce(@headCompanyId, f.headCompanyID)
		AND p.paymentTypeID = Coalesce(@paymentTypeID, p.paymentTypeID)
		AND p.agencyID = Coalesce(@agencyID, p.agencyID)
		AND(@isNotClosedOnly = 0 or p.summa > ISNULL((SELECT SUM(pa.[summa]) FROM [PaymentAction] pa WHERE pa.[paymentID] = p.[paymentID]), 0))
		AND p.paymentDate >= Coalesce(@startOfInterval, p.paymentDate)
		AND p.paymentDate <= Coalesce(@endOfInterval, p.paymentDate)
		AND p.paymentID = Coalesce(@paymentID, p.paymentID)
		AND (@actionID IS NULL OR pa.[actionID] = @actionID)
		AND (pt.isHidden = 0 or @isHideWhite = 0) And
			(pt.isHidden = 1 or @isHideBlack = 0) and 
			((pt.IsHidden = 1 and @showBlack = 1)  or
			(pt.IsHidden = 0 and @showWhite = 1))
		and ((@filterAgencies = 0 and x.agencyID is not null) or 
			( exists(select * from Campaign cam inner join [Action] act on cam.actionID = act.actionID where a.agencyID = cam.agencyID and @loggedUserID = act.userID) 
				or exists(select * from Payment pay where a.agencyID = pay.agencyID and @loggedUserID = pay.userID)))

Exec sl_PaymentsCommon
