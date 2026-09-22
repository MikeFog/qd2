CREATE   PROCEDURE [dbo].[PaymentAction_CorrectByActionTotalPrice]
	@actionID int,
	@targetTotalPrice decimal(18, 2) = NULL
AS
BEGIN
	-- =============================================================================
	-- Обрезает оплаты акции (PaymentAction) до новой цены акции.
	--
	-- Кто вызывает: клиент, ActionOnMassmedia.CorrectPaymentAction() — после сохранения
	-- ПОДТВЕРЖДЁННОЙ акции, если её цена уменьшилась (старая TotalPrice > новой).
	-- Параметры: @actionID; @targetTotalPrice — целевая цена (NULL = текущая Action.totalPrice).
	--
	-- Что делает: излишек = SUM(PaymentAction.summa) по акции минус цена. Если излишка нет —
	-- ничего не делает. Иначе идёт по платежам акции от самых свежих (paymentDate DESC,
	-- paymentID DESC): если доля платежа в акции не больше оставшегося излишка — строка
	-- PaymentAction удаляется целиком, иначе её summa уменьшается на остаток излишка.
	-- Сами платежи (Payment) не трогаются — только их привязка к акции.
	--
	-- Транзакцию открывает вызывающий: клиент зовёт ExecuteNonQuery(..., isTransactionRequired = true),
	-- поэтому BEGIN/COMMIT внутри закомментированы.
	--
	-- История: создана прямо в БД 26.05.2026, правилась там же 31.05.2026 (клиентский вызов добавлен
	-- коммитом 997d08f от 27.05.2026, файл процедуры в него не попал). В репозиторий добавлена
	-- 20.09.2026 — текст взят из определения в базе (копия прода).
	-- =============================================================================
	SET NOCOUNT ON;
	SET XACT_ABORT ON;
	DECLARE @currentTotalPrice decimal(18, 2);
	DECLARE @paidSum decimal(18, 2);
	DECLARE @excessSum decimal(18, 2);
	DECLARE @paymentID int;
	DECLARE @paymentActionSum decimal(18, 2);
	DECLARE @delta decimal(18, 2);
	SELECT @currentTotalPrice = a.totalPrice
	FROM dbo.[Action] a --WITH (UPDLOCK, HOLDLOCK)
	WHERE a.actionID = @actionID;
	IF @currentTotalPrice IS NULL
	BEGIN
		RAISERROR('Action not found.', 16, 1);
		RETURN;
	END;
	IF @targetTotalPrice IS NULL
		SET @targetTotalPrice = @currentTotalPrice;
	IF @targetTotalPrice < 0
	BEGIN
		RAISERROR('Target total price cannot be negative.', 16, 1);
		RETURN;
	END;
	--BEGIN TRANSACTION;
	SELECT @paidSum = ISNULL(SUM(pa.summa), 0)
	FROM dbo.PaymentAction pa --WITH (UPDLOCK, HOLDLOCK)
	WHERE pa.actionID = @actionID;
	SET @excessSum = @paidSum - @targetTotalPrice;
	IF @excessSum <= 0
	BEGIN
		--COMMIT TRANSACTION;
		RETURN;
	END;
	DECLARE payment_cursor CURSOR LOCAL FAST_FORWARD FOR
	SELECT pa.paymentID, pa.summa
	FROM dbo.PaymentAction pa --WITH (UPDLOCK, READPAST)
	JOIN dbo.Payment p /*WITH (UPDLOCK, READPAST)*/ ON p.paymentID = pa.paymentID
	WHERE pa.actionID = @actionID
	ORDER BY p.paymentDate DESC, p.paymentID DESC;
	OPEN payment_cursor;
	FETCH NEXT FROM payment_cursor INTO @paymentID, @paymentActionSum;
	WHILE @@FETCH_STATUS = 0 AND @excessSum > 0
	BEGIN
		IF @paymentActionSum <= @excessSum
		BEGIN
			DELETE FROM dbo.PaymentAction
			WHERE paymentID = @paymentID
				  AND actionID = @actionID;
			SET @excessSum = @excessSum - @paymentActionSum;
		END
		ELSE
		BEGIN
			SET @delta = @excessSum;
			UPDATE dbo.PaymentAction
			SET summa = summa - @delta
			WHERE paymentID = @paymentID
				  AND actionID = @actionID;
			SET @excessSum = 0;
		END;
		FETCH NEXT FROM payment_cursor INTO @paymentID, @paymentActionSum;
	END;
	CLOSE payment_cursor;
	DEALLOCATE payment_cursor;
	--COMMIT TRANSACTION;
END;
