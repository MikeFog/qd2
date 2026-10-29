-- 1, если в акции есть кампании и с типом оплаты «Не включать в счет на предоплату»
-- (PaymentType.isHidden = 1), и без него. В одной акции так быть не должно (с 29.10.2026,
-- PaymentKindMixedInAction); старые смешанные акции не правились.
CREATE FUNCTION [dbo].[fn_ActionPaymentKindsMixed] (@actionID INT)
RETURNS BIT
AS
BEGIN
    RETURN CASE WHEN EXISTS (SELECT 1 FROM dbo.Campaign c INNER JOIN dbo.PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
                             WHERE c.actionID = @actionID AND pt.isHidden = 1)
                 AND EXISTS (SELECT 1 FROM dbo.Campaign c INNER JOIN dbo.PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
                             WHERE c.actionID = @actionID AND pt.isHidden = 0)
                THEN 1 ELSE 0 END;
END
