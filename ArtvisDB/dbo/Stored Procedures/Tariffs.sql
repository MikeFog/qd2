













CREATE               PROC [dbo].[Tariffs]
(
@pricelistID smallint = NULL,
@tariffID int = NULL,
@excludeModuleTariffs bit = 0
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

Create Table #Tariff(tariffId int)

IF @tariffID IS NULL BEGIN

	Insert Into #Tariff(tariffId)

	SELECT 
		t.tariffId
	FROM 
		[Tariff] t
	WHERE
		pricelistID = COALESCE(@pricelistID, t.pricelistID)
		AND (@excludeModuleTariffs = 0 Or t.isForModuleOnly = 0)

END
ELSE BEGIN
	Insert Into #Tariff(tariffId) Values(@tariffID)
END

Exec sl_TariffRetrieve














