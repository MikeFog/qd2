



CREATE     PROC [dbo].[disabledWindows]
(
@massmediaID smallint = null,
@disabledWindowID int = null
)
as
set nocount on
SELECT 
	dw.*, 
	mm.name as massmediaName,
	'Время профилактики ' + Convert(varchar(10), dw.[startDate], 104) as name
FROM 
	[DisabledWindow] dw
	INNER JOIN vMassMedia mm ON mm.massmediaID = dw.massmediaID
WHERE
	dw.[massmediaID] = COALESCE(@massmediaID, dw.[massmediaID]) AND
	dw.[disabledWindowID] = COALESCE(@disabledWindowID, dw.[disabledWindowID])
ORDER BY
	dw.startDate desc









