




CREATE      VIEW dbo.vStudio
AS
SELECT
	st.studioID,
	IsActive = st.IsActive,
	a.agencyID,
	a.name,
	a.address,
	a.phone,
	a.fax,
	a.account,
	a.inn,
	a.okpo,
	a.okonh,
	a.bankID,
	a.director,
	a.bookkeeper,
	a.prefix,
	a.directorSignature,
	a.bookkeeperSignature,
	a.fullPrefix,
	a.reportString,
	a.registration,
	a.egrn,
	a.kpp,
	a.okved,
	a.email
FROM
	dbo.Studio st
	INNER JOIN dbo.Agency a ON a.agencyID = st.studioID







