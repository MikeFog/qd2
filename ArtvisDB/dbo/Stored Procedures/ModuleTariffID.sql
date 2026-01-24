

CREATE  PROCEDURE [dbo].[ModuleTariffID]
(
@modulePriceListID smallint,
@tariffID int,
@actionName varchar(32),
@isEditTarrifs BIT = 0,
@pricelistID smallint = NULL,
@time smalldatetime = NULL,
@monday tinyint = NULL,
@tuesday tinyint = NULL,
@wednesday tinyint = NULL,
@thursday tinyint = NULL,
@friday tinyint = NULL,
@saturday tinyint = NULL,
@sunday tinyint = NULL,
@price money = null,
@duration smallint = NULL,
@duration_total smallint = NULL,
@comment nvarchar(32) = NULL,
@isForModuleOnly bit = NULL,
@suffix NVARCHAR(16) = NULL,
@needExt BIT = NULL,
@maxCapacity SMALLINT = NULL,
@needInJingle bit = NULL,
@needOutJingle BIT = NULL
)
WITH EXECUTE AS OWNER
as
begin 
	SET NOCOUNT on
	
	if (@actionName in ('AddItem', 'DeleteItem') OR (@actionName = 'UpdateItem' AND @isEditTarrifs = 1)) 
		and exists(select * from ModuleIssue where modulePricelistID = @modulePriceListID)
	begin 
		raiserror('CannotChangeModuleContent', 16, 1)
		return
	end 
	
	declare @isCapacityTariff bit 
	select @isCapacityTariff = case when t.maxCapacity > 0 then 1 else 0 end from Tariff t where t.tariffID = @tariffID

	IF @actionName = 'AddItem' OR (@actionName = 'UpdateItem' AND @isEditTarrifs = 1)
	begin 
		if exists(select * 
			from ModuleTariff mt 
				inner join Tariff t on t.tariffID = mt.tariffID 
			where mt.modulePriceListID = @modulePriceListID and ((t.maxCapacity > 0 and @isCapacityTariff = 0) or (t.maxCapacity = 0 and @isCapacityTariff = 1)))
		begin 
			raiserror('TariffMustBeOneTypeInModule', 16, 1)
			return
		end 
				
		INSERT INTO [ModuleTariff](modulePriceListID, tariffID)
		VALUES(@modulePriceListID, @tariffID)
	end 
	ELSE IF @actionName = 'DeleteItem'
		DELETE FROM [ModuleTariff] 
		WHERE modulePriceListID = @modulePriceListID AND tariffID = @tariffID
	ELSE IF @actionName = 'UpdateItem'
	begin
		exec [TariffIUD]
			@tariffID = @tariffID, --  int
			@pricelistID = @pricelistID, --  smallint
			@time = @time, --  smalldatetime
			@monday = @monday, --  tinyint
			@tuesday = @tuesday, --  tinyint
			@wednesday = @wednesday, --  tinyint
			@thursday = @thursday, --  tinyint
			@friday = @friday, --  tinyint
			@saturday = @saturday, --  tinyint
			@sunday = @sunday, --  tinyint
			@price = @price, --  money
			@duration = @duration, --  smallint
			@duration_total = @duration_total,
			@comment = @comment, --  nvarchar(32)
			@isForModuleOnly = @isForModuleOnly, --  bit
			@actionName = @actionName, --  varchar(32)
			@suffix = @suffix, --  nvarchar(16)
			@needExt = @needExt, --  bit
			@maxCapacity = @maxCapacity, --  smallint
			@needInJingle = @needInJingle, --  bit
			@needOutJingle = @needOutJingle --  bit
	END
end 




