CREATE DEFAULT [dbo].[DF_false]
    AS 0;


GO
EXECUTE sp_bindefault @defname = N'[dbo].[DF_false]', @objname = N'[dbo].[iRolType].[isLoadable]';


GO
EXECUTE sp_bindefault @defname = N'[dbo].[DF_false]', @objname = N'[dbo].[Roller].[isCommon]';


GO
EXECUTE sp_bindefault @defname = N'[dbo].[DF_false]', @objname = N'[dbo].[MassMedia].[isCheckDeadline]';


GO
EXECUTE sp_bindefault @defname = N'[dbo].[DF_false]', @objname = N'[dbo].[PaymentType].[isHidden]';


GO
EXECUTE sp_bindefault @defname = N'[dbo].[DF_false]', @objname = N'[dbo].[AdvertType].[isParent]';


GO
EXECUTE sp_bindefault @defname = N'[dbo].[DF_false]', @objname = N'[dbo].[AdvertType].[isHidden]';


GO
EXECUTE sp_bindefault @defname = N'[dbo].[DF_false]', @objname = N'[dbo].[SponsorTariff].[isAlive]';


GO
EXECUTE sp_bindefault @defname = N'[dbo].[DF_false]', @objname = N'[dbo].[iEntityAction].[isHidden]';


GO
EXECUTE sp_bindefault @defname = N'[dbo].[DF_false]', @objname = N'[dbo].[Tariff].[isForModuleOnly]';

