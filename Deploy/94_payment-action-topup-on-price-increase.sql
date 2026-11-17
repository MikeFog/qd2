-- Привязки оплат к акции (PaymentAction) при изменении цены подтверждённой акции (17.11.2026).
-- 1) Доливка при подорожании — жалоба заказчика «работает только в сторону уменьшения». С 27.05.2026 после пересчёта
--    подтверждённой акции, если цена уменьшилась, PaymentAction_CorrectByActionTotalPrice урезала привязки до цены; при
--    обратном росте ничего не происходило. Теперь, если привязано меньше цены, процедура доливает в уже существующие
--    привязки акции из свободного остатка их платежей (от свежих платежей; не больше долга акции и долга группы
--    «агентство + вид оплаты» платежа; новых привязок не создаёт; выключенные платежи не трогает).
-- 2) Урезание по агентству. Раньше излишек снимался с самых свежих платежей любого агентства: подешевела кампания
--    агентства А — мог урезаться платёж агентства Б. Теперь сначала снимается с переплаченных групп «агентство + вид
--    оплаты» (баланс как в журнале оплат, ActionsForPaymentCommon), остаток — как раньше. Всего снимается столько же.
-- Клиент (qd2 и веб) теперь вызывает процедуру при любом изменении цены, а не только при уменьшении.
--
-- В любое время, на Artvis и Tumen. Урезание по агентству действует сразу и со старым клиентом; доливка — с новыми qd2 и
-- вебом. Уже недоплаченные акции поправятся при следующем изменении их цены. Идемпотентен (CREATE OR ALTER).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 94_payment-action-topup-on-price-increase.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [dbo].[PaymentAction_CorrectByActionTotalPrice]
	@actionID int,
	@targetTotalPrice decimal(18, 2) = NULL
AS
BEGIN
	-- =============================================================================
	-- Подгоняет оплаты акции (PaymentAction) под новую цену акции — в обе стороны.
	--
	-- Кто вызывает: клиент, ActionOnMassmedia.CorrectPaymentAction() — после пересчёта
	-- ПОДТВЕРЖДЁННОЙ акции, если её цена изменилась (до Deploy/94 — только если уменьшилась).
	-- Параметры: @actionID; @targetTotalPrice — целевая цена (NULL = текущая Action.totalPrice).
	--
	-- Излишек = SUM(PaymentAction.summa) по акции минус цена.
	-- Группа — «агентство + вид оплаты (PaymentType.isHidden)» платежа; баланс группы = сумма finalPrice
	-- кампаний акции этого агентства и вида минус привязки платежей этого агентства и вида (так долг
	-- акции считает журнал оплат, ActionsForPaymentCommon). Баланс < 0 — группа переплачена.
	--
	-- Излишек > 0 (акция подешевела). Всего снимается ровно излишек, как и до Deploy/94; меняется
	-- только, с каких привязок (Deploy/94, раньше — с самых свежих платежей любого агентства):
	--   1) по платежам от самых свежих (paymentDate DESC, paymentID DESC) — только привязки
	--      переплаченных групп, не больше переплаты группы;
	--   2) если излишек остался (сумма finalPrice кампаний больше цены акции или задан
	--      @targetTotalPrice) — как раньше, с самых свежих платежей любой группы.
	--   Привязка, снятая целиком, удаляется, иначе её summa уменьшается.
	-- Излишек < 0 (акция подорожала или недоплачена) — Deploy/94: доливает в те же строки
	-- PaymentAction (summa > 0) из свободного остатка их платежей (Payment.summa минус все его
	-- привязки), от самых свежих платежей, не больше долга акции и долга группы платежа.
	-- Новых строк не создаёт: платёж, чья привязка была удалена целиком, обратно не привязывается.
	-- Выключенные платежи (isEnabled = 0) не используются. Свободный остаток читается под UPDLOCK
	-- на Payment. Сами платежи (Payment) не меняются — только их привязка к акции.
	--
	-- Транзакцию открывает вызывающий: клиент зовёт ExecuteNonQuery(..., isTransactionRequired = true),
	-- поэтому BEGIN/COMMIT внутри закомментированы.
	--
	-- История: создана прямо в БД 26.05.2026, правилась там же 31.05.2026 (клиентский вызов добавлен
	-- коммитом 997d08f от 27.05.2026, файл процедуры в него не попал). В репозиторий добавлена
	-- 20.09.2026 — текст взят из определения в базе (копия прода). Deploy/94 (17.11.2026) — группы
	-- при урезании и доливка.
	-- =============================================================================
	SET NOCOUNT ON;
	SET XACT_ABORT ON;
	DECLARE @currentTotalPrice decimal(18, 2);
	DECLARE @paidSum decimal(18, 2);
	DECLARE @excessSum decimal(18, 2);
	DECLARE @paymentID int;
	DECLARE @paymentActionSum decimal(18, 2);
	DECLARE @agencyID smallint;
	DECLARE @isHidden bit;
	DECLARE @groupBalance decimal(18, 2);
	DECLARE @freeSum decimal(18, 2);
	DECLARE @moveSum decimal(18, 2);
	DECLARE @groups TABLE (
		agencyID smallint NOT NULL,
		isHidden bit NOT NULL,
		balance decimal(18, 2) NOT NULL,
		PRIMARY KEY (agencyID, isHidden));
	DECLARE @links TABLE (
		ord int IDENTITY(1, 1) PRIMARY KEY,
		paymentID int NOT NULL,
		agencyID smallint NOT NULL,
		isHidden bit NOT NULL,
		isEnabled bit NOT NULL);
	DECLARE @ord int;
	DECLARE @pass tinyint;
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
	IF @excessSum = 0
	BEGIN
		--COMMIT TRANSACTION;
		RETURN;
	END;

	-- Привязки акции от самых свежих платежей и балансы их групп.
	INSERT INTO @links (paymentID, agencyID, isHidden, isEnabled)
	SELECT pa.paymentID, p.agencyID, pt.isHidden, p.isEnabled
	FROM dbo.PaymentAction pa
	JOIN dbo.Payment p ON p.paymentID = pa.paymentID
	JOIN dbo.PaymentType pt ON pt.paymentTypeID = p.paymentTypeID
	WHERE pa.actionID = @actionID
	ORDER BY p.paymentDate DESC, p.paymentID DESC;

	INSERT INTO @groups (agencyID, isHidden, balance)
	SELECT g.agencyID, g.isHidden,
		ISNULL((SELECT SUM(c.finalPrice)
				FROM dbo.Campaign c
				JOIN dbo.PaymentType ptc ON ptc.paymentTypeID = c.paymentTypeID
				WHERE c.actionID = @actionID AND c.agencyID = g.agencyID AND ptc.isHidden = g.isHidden), 0)
		- ISNULL((SELECT SUM(pa.summa)
				FROM dbo.PaymentAction pa
				JOIN @links l ON l.paymentID = pa.paymentID
				WHERE pa.actionID = @actionID AND l.agencyID = g.agencyID AND l.isHidden = g.isHidden), 0)
	FROM (SELECT DISTINCT agencyID, isHidden FROM @links) g;

	IF @excessSum > 0
	BEGIN
		-- Проход 1 — только переплаченные группы; проход 2 — остаток излишка с любых привязок.
		SET @pass = 1;
		WHILE @pass <= 2 AND @excessSum > 0
		BEGIN
			SET @ord = 0;
			WHILE @excessSum > 0
			BEGIN
				SELECT TOP (1) @ord = l.ord, @paymentID = l.paymentID, @agencyID = l.agencyID, @isHidden = l.isHidden
				FROM @links l WHERE l.ord > @ord ORDER BY l.ord;
				IF @@ROWCOUNT = 0 BREAK;

				SET @paymentActionSum = NULL;
				SELECT @paymentActionSum = summa FROM dbo.PaymentAction
				WHERE paymentID = @paymentID AND actionID = @actionID;
				IF ISNULL(@paymentActionSum, 0) <= 0 CONTINUE;

				SET @moveSum = CASE WHEN @paymentActionSum < @excessSum THEN @paymentActionSum ELSE @excessSum END;
				IF @pass = 1
				BEGIN
					SELECT @groupBalance = balance FROM @groups WHERE agencyID = @agencyID AND isHidden = @isHidden;
					IF -@groupBalance < @moveSum SET @moveSum = -@groupBalance;
				END;
				IF @moveSum <= 0 CONTINUE;

				IF @moveSum >= @paymentActionSum
					DELETE FROM dbo.PaymentAction
					WHERE paymentID = @paymentID AND actionID = @actionID;
				ELSE
					UPDATE dbo.PaymentAction
					SET summa = summa - @moveSum
					WHERE paymentID = @paymentID AND actionID = @actionID;
				UPDATE @groups SET balance = balance + @moveSum WHERE agencyID = @agencyID AND isHidden = @isHidden;
				SET @excessSum = @excessSum - @moveSum;
			END;
			SET @pass = @pass + 1;
		END;
		--COMMIT TRANSACTION;
		RETURN;
	END;

	-- Излишек < 0: доливка из свободного остатка уже привязанных платежей.
	SET @excessSum = -@excessSum; -- дальше это долг акции
	SET @ord = 0;
	WHILE @excessSum > 0
	BEGIN
		SELECT TOP (1) @ord = l.ord, @paymentID = l.paymentID, @agencyID = l.agencyID, @isHidden = l.isHidden
		FROM @links l WHERE l.ord > @ord AND l.isEnabled = 1 ORDER BY l.ord;
		IF @@ROWCOUNT = 0 BREAK;

		SET @paymentActionSum = NULL;
		SELECT @paymentActionSum = summa FROM dbo.PaymentAction
		WHERE paymentID = @paymentID AND actionID = @actionID;
		IF ISNULL(@paymentActionSum, 0) <= 0 CONTINUE;

		SELECT @freeSum = p.summa - ISNULL(
			(SELECT SUM(x.summa) FROM dbo.PaymentAction x WHERE x.paymentID = p.paymentID), 0)
		FROM dbo.Payment p WITH (UPDLOCK)
		WHERE p.paymentID = @paymentID;
		SELECT @groupBalance = balance FROM @groups WHERE agencyID = @agencyID AND isHidden = @isHidden;

		SET @moveSum = @excessSum;
		IF @freeSum < @moveSum SET @moveSum = @freeSum;
		IF @groupBalance < @moveSum SET @moveSum = @groupBalance;
		IF @moveSum <= 0 CONTINUE;

		UPDATE dbo.PaymentAction
		SET summa = summa + @moveSum
		WHERE paymentID = @paymentID AND actionID = @actionID;
		UPDATE @groups SET balance = balance - @moveSum WHERE agencyID = @agencyID AND isHidden = @isHidden;
		SET @excessSum = @excessSum - @moveSum;
	END;
	--COMMIT TRANSACTION;
END;
GO

PRINT N'ГОТОВО';
GO
