-- Секунды прайма по станциям для порога «Максимальный процент прайма» пакетной скидки в калькуляторе цены и оценке
-- Шаблона №3 (pc_PackageDiscountCalculateModel, pc_CalcDiscountsModel; docs/tasks/package-discount-prime-limit.md).
-- Отдельный необязательный параметр, а не колонка pc_SelectedMassmedia/pc_CalcRow: старые qd2 и веб его не передают и
-- считают как раньше, без порога.
CREATE TYPE [dbo].[pc_MassmediaPrime] AS TABLE (
    [massmediaID]      SMALLINT NOT NULL PRIMARY KEY,
    [primeDurationSec] INT      NOT NULL);
