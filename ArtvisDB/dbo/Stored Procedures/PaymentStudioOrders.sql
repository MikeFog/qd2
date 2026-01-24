CREATE            PROC [dbo].[PaymentStudioOrders]
(
@paymentID int = Null,
@startOfInterval datetime = Null,
@endOfInterval datetime = Null,
@firmID smallint = Null,
@paymentTypeID smallint = Null,
@agencyID smallint  = Null,
@isNotClosedOnly bit = 0,
@agenciesIDString varchar(1024) = null,
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
CREATE TABLE #PaymentStudioOrder(paymentID int)

IF @agenciesIDString is not Null 
Begin
	CREATE TABLE #Agency (agencyID smallint)

	Exec hlp_PopulateTableFromCommaSeparatedString '#Agency', @agenciesIDString

	INSERT INTO #PaymentStudioOrder(paymentID)
	SELECT 
		p.paymentID
	FROM 
		[PaymentStudioOrder] p
		INNER JOIN firm f ON f.firmID = p.firmID
		INNER JOIN agency a ON a.agencyID = p.agencyID
		INNER JOIN paymentType pt ON pt.paymentTypeID = p.paymentTypeID
		INNER JOIN [user] u ON u.userID = p.userID
	where
		p.firmID = Coalesce(@firmID, p.firmID)
		AND p.paymentDate >= Coalesce(@startOfInterval, p.paymentDate)
		AND p.paymentDate <= Coalesce(@endOfInterval, p.paymentDate)
		AND p.agencyID IN (SELECT agencyID From #Agency) AND 
		((pt.IsHidden = 1 and @showBlack = 1)  or
		(pt.IsHidden = 0 and @showWhite = 1)) and
		(pt.isHidden = 0 or @isHideWhite = 0) And
		(pt.isHidden = 1 or @isHideBlack = 0) and (@filterAgencies = 0 
			or exists(select * from StudioOrder so inner join StudioOrderAction act on so.actionID = act.actionID where a.agencyID = so.agencyID and (@loggedUserID = act.userID or so.userID = @loggedUserID) ) 
			or exists(select * from [PaymentStudioOrder] pay where a.agencyID = pay.agencyID and @loggedUserID = pay.userID))
End
Else
	INSERT INTO #PaymentStudioOrder(paymentID)
	SELECT 
		p.paymentID
	FROM 
		[PaymentStudioOrder] p
		INNER JOIN firm f ON f.firmID = p.firmID
		INNER JOIN agency a ON a.agencyID = p.agencyID
		INNER JOIN paymentType pt ON pt.paymentTypeID = p.paymentTypeID
		INNER JOIN [user] u ON u.userID = p.userID
	WHERE
		p.firmID = Coalesce(@firmID, p.firmID)
		AND p.paymentTypeID = Coalesce(@paymentTypeID, p.paymentTypeID)
		AND p.agencyID = Coalesce(@agencyID, p.agencyID)
		AND(@isNotClosedOnly = 0 or p.summa > ISNULL((SELECT SUM(pa.[summa]) FROM [PaymentStudioOrderAction] pa WHERE pa.[paymentID] = p.[paymentID]), 0))
		AND p.paymentDate >= Coalesce(@startOfInterval, p.paymentDate)
		AND p.paymentDate <= Coalesce(@endOfInterval, p.paymentDate)
		AND p.paymentID = Coalesce(@paymentID, p.paymentID) AND 
		((pt.IsHidden = 1 and @showBlack = 1)  or
		(pt.IsHidden = 0 and @showWhite = 1)) and
		(pt.isHidden = 0 or @isHideWhite = 0) And
		(pt.isHidden = 1 or @isHideBlack = 0) and (@filterAgencies = 0 
			or exists(select * from StudioOrder so inner join StudioOrderAction act on so.actionID = act.actionID where a.agencyID = so.agencyID and (@loggedUserID = act.userID or so.userID = @loggedUserID) ) 
			or exists(select * from [PaymentStudioOrder] pay where a.agencyID = pay.agencyID and @loggedUserID = pay.userID))

Exec sl_PaymentStudioOrders
