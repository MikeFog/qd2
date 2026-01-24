CREATE PROC [dbo].[sl_LookupAgency]
as
SET NOCOUNT ON
SELECT agencyID as id, name FROM Agency where isActive = 1 ORDER BY name


