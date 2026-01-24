CREATE PROCEDURE [dbo].[PaymentStudioOrderActions]
(
@paymentID int = null,
@managerID smallint = null,
@startOfInterval datetime = null,
@endOfInterval datetime = null,
@agencyID smallint = null,
@paymentTypeID smallint = null,
@firmID smallint = null,
@agenciesIDString varchar(1024) = NULL,
@isHideBlack int = 0,
@isHideWhite int = 0,
@showBlack bit = 1,
@showWhite bit = 1,
@loggedUserID smallint
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
CREATE TABLE #Agency(agencyID smallint)

-- Populate temporary tables with Agency and Payment types
IF @agenciesIDString Is Null
	IF @agencyID IS NULL
		INSERT INTO #Agency SELECT agencyID FROM Agency
	ELSE
		INSERT INTO #Agency VALUES(@agencyID)
Else
	Exec dbo.hlp_PopulateTableFromCommaSeparatedString '#Agency', @agenciesIDString 

	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignSOActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupSOActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

select distinct
	psoa.*,
	'Оплата акции №' + LTrim(psoa.actionID) as name,
	f.name as firmName,
	a.name as agencyName,
	pt.name as paymentTypeName,
	u.[lastName] + ' ' + u.[firstName] AS userName
FROM 
	[PaymentStudioOrderAction] psoa
	INNER JOIN StudioOrderAction soa ON soa.actionID = psoa.actionID
	INNER JOIN [USER] u ON u.[userID] = soa.[userID]
	INNER JOIN PaymentStudioOrder pso ON pso.paymentID = psoa.paymentID
	INNER JOIN Firm f ON f.firmID = pso.firmID
	INNER JOIN Agency a ON a.agencyID = pso.agencyID
	INNER JOIN PaymentType pt ON pt.paymentTypeID = pso.paymentTypeID
	left join GroupMember gm on u.[userID] = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
WHERE 	
	(soa.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null))
	and	psoa.[paymentID] = Coalesce(@paymentID, psoa.[paymentID]) And
	pso.paymentDate BETWEEN Coalesce(@startOfInterval, pso.paymentDate) 
		And Coalesce(@endOfInterval, pso.paymentDate)	and -- remove + 1 так как проблемы в журнале оплат по менеджерам
	soa.userID = Coalesce(@managerID, soa.userID) And
	pso.agencyID IN (Select agencyID From #Agency) And
	soa.firmID = Coalesce(@firmID, soa.firmID) AND 
	(pt.isHidden = 0 or @isHideWhite = 0) And
	(pt.isHidden = 1 or @isHideBlack = 0) and 
	((pt.IsHidden = 1 and @showBlack = 1)  or
	(pt.IsHidden = 0 and @showWhite = 1))
ORDER BY
	psoa.actionID desc
