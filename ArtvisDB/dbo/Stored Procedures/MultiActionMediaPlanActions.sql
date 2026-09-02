-- Проверка списка акций для сводного медиаплана «График размещения по
-- нескольким акциям». Возвращает строку на каждую существующую
-- (не удалённую) акцию из @actionIDString вместе с её фирмой-заказчиком.
-- Вызывающий код сверяет число строк с числом введённых номеров: если
-- меньше — какие-то акции не найдены, операция прерывается.
CREATE PROCEDURE [dbo].[MultiActionMediaPlanActions]
(
    @actionIDString varchar(8000)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.actionID,
        a.firmID,
        LTRIM(RTRIM(ISNULL(f.prefix, N'') + N' ' + f.[name])) AS firmName
    FROM fn_CreateTableFromString(@actionIDString) s
        INNER JOIN [dbo].[Action] a ON a.actionID = CAST(s.[ID] AS int) AND a.deleteDate IS NULL
        INNER JOIN [dbo].[Firm] f ON f.firmID = a.firmID
    ORDER BY a.actionID;
END
