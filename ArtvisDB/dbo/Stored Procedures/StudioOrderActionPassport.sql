CREATE PROC [dbo].[StudioOrderActionPassport]
(
@actionID int = Null
)
as
SET NOCOUNT ON
select	id = statusID, name = name
from		iStudioOrderActionStatus
