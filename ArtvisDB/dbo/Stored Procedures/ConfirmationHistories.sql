


CREATE    PROC [dbo].[ConfirmationHistories]
(
@confirmationID int = null,
@userID smallint = null,
@grantorID smallint = null,
@confirmationTypeID  tinyint = null,
@startOfInterval datetime = null,
@endOfInterval datetime = null
)
AS
SET NOCOUNT ON
select top 1000 
	ch.*,
	ct.name as confirmationTypeName,
	ct.name,
	u.LastName + Space(1) + u.FirstName as manager,
	g.LastName + Space(1) + g.FirstName as grantor
FROM 
	[ConfirmationHistory] ch
	INNER JOIN iConfirmationType ct ON ct.confirmationTypeID = ch.confirmationTypeID
	INNER JOIN [User] u ON u.userID = ch.userID
	INNER JOIN [User] g ON g.userID = ch.grantorID
WHERE
	ch.confirmationID = Coalesce(@confirmationID, ch.confirmationID) And
	ch.userID = Coalesce(@userID, ch.userID) And
	ch.grantorID = Coalesce(@grantorID, ch.grantorID) And
	ch.confirmationTypeID = isnull(@confirmationTypeID, ch.confirmationTypeID) And
	ch.dateCreated >= Coalesce(@startOfInterval, ch.dateCreated) And
	ch.dateCreated < Coalesce(@endOfInterval, ch.dateCreated) + 1
ORDER BY
	ch.dateCreated Desc





