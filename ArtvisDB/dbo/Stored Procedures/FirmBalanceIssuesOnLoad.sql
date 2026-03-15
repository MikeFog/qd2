CREATE  PROCEDURE [dbo].[FirmBalanceIssuesOnLoad]
(
	@loggedUserID smallint
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON;

	-- 1. firms
	Create Table #firm(firmID int)
	Insert Into #firm(firmID)
	(Select Distinct [firmID] From Action 
	UNION
	Select Distinct [firmID] From [Payment])
	Exec sl_Firms

	-- 2. Managers
	CREATE TABLE #User(userID smallint)
	Insert Into #User(userID)
	(Select Distinct [userID] From ACTION
	UNION
	Select Distinct [userID] From [Payment])
	Exec sl_Users null

	-- 3. Payment types
	((SELECT DISTINCT
		pt.*
	FROM 
		[PaymentType] pt
		INNER JOIN [Campaign] c ON c.[paymentTypeID] = pt.paymentTypeID
	)
	UNION
	(SELECT DISTINCT
		pt.*
	FROM 
		[PaymentType] pt
		INNER JOIN [Payment] c ON c.[paymentTypeID] = pt.paymentTypeID)) 
	ORDER BY name

	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	-- 4. Agencies
	CREATE TABLE #Agency(agencyID smallint)

	Insert Into #Agency(agencyID)
	select x.agencyID 
	from 
	(Select Distinct agencyID From [Campaign]
	UNION
	SELECT DISTINCT agencyID FROM [Payment]) as x

	Exec sl_Agencies