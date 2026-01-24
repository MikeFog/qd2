
CREATE   PROC [dbo].[banks]
(
@bankId int = NULL,
@bik varchar(32) = NULL,
@bankName nvarchar(255) = NULL
)
as
set nocount on
SELECT 
	id = bankId,
	bankId,
	name,
	bik,
	corAccount
FROM
	bank
WHERE 
	bankId = COALESCE(@bankId, bankId)
	and bik = COALESCE(@bik, bik)
	and (@bankName Is Null or CHARINDEX(@bankName, name) > 0)
ORDER BY 
	name



