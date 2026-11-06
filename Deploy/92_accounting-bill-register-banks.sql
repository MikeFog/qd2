-- «Бухгалтерия»: пункты «Реестр счетов» и «Банки» (06.11.2026, Миша: всё нужное бухгалтерии — в одном месте).
-- Это копии пунктов «Рекламного отдела» (miBillRegister, miBank) со своими codeName — miBillRegisterBuh и miBankBuh,
-- как уже сделано для «Журнала оплат» (miPaymentCommon / miPaymentFRS): у пункта меню один родитель, права выдаются на пункт.
-- Пункты в «Рекламном отделе» остаются.
--   1. iMenu: «Реестр счетов» — позиция 5 (после «Журнала оплат»), «Банки» — позиция 150 (после «Журнала подтверждённых
--      рекламных акций», перед «Специальными отчётами»). Название, испанское название и значок — из исходных пунктов.
--   2. Права (GroupMenu, UserAdditionMenu): как у «Журнала оплат» Бухгалтерии (miPaymentCommon) — администраторы и бухгалтерия.
-- Реестр из «Бухгалтерии» открывается без менеджера по умолчанию (все акции), в «Рекламном отделе» по-прежнему — вошедший.
-- Клиент: нужны новые Merlin.exe (ветки miBillRegisterBuh/miBankBuh в MDIForm) и веб (MenuRoutes). Старый клиент на щелчок
-- по новым пунктам ничего не открывает. После наката — перезапустить qd2 и веб (меню читается при старте).
-- Нужен Deploy/80 (miBillRegister). Идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 92_accounting-bill-register-banks.sql

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.iMenu WHERE codeName = 'miBillRegister')
   OR NOT EXISTS (SELECT 1 FROM dbo.iMenu WHERE codeName = 'miBank')
   OR NOT EXISTS (SELECT 1 FROM dbo.iMenu WHERE codeName = 'miAccounting')
   OR NOT EXISTS (SELECT 1 FROM dbo.iMenu WHERE codeName = 'miPaymentCommon')
BEGIN
    RAISERROR(N'92: не найден один из пунктов miBillRegister (Deploy/80), miBank, miAccounting, miPaymentCommon. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO

BEGIN TRANSACTION;

DECLARE @accounting smallint = (SELECT menuID FROM dbo.iMenu WHERE codeName = 'miAccounting');

INSERT INTO dbo.iMenu (name, parentID, position, codeName, hotKey, align, imgResourcePath, isPublic, isObsolete, name_es)
SELECT m.name, @accounting, n.position, n.newCode, NULL, m.align, m.imgResourcePath, m.isPublic, m.isObsolete, m.name_es
FROM (VALUES ('miBillRegister', 'miBillRegisterBuh', 5),
             ('miBank',         'miBankBuh',         150)) AS n (sourceCode, newCode, position)
    INNER JOIN dbo.iMenu m ON m.codeName = n.sourceCode
WHERE NOT EXISTS (SELECT 1 FROM dbo.iMenu x WHERE x.codeName = n.newCode);

DECLARE @payments smallint = (SELECT menuID FROM dbo.iMenu WHERE codeName = 'miPaymentCommon');

INSERT INTO dbo.GroupMenu (groupID, menuID)
SELECT gm.groupID, m.menuID
FROM dbo.GroupMenu gm
    CROSS JOIN dbo.iMenu m
WHERE gm.menuID = @payments AND m.codeName IN ('miBillRegisterBuh', 'miBankBuh')
  AND NOT EXISTS (SELECT 1 FROM dbo.GroupMenu x WHERE x.groupID = gm.groupID AND x.menuID = m.menuID);

INSERT INTO dbo.UserAdditionMenu (userID, menuID, isGrant)
SELECT um.userID, m.menuID, um.isGrant
FROM dbo.UserAdditionMenu um
    CROSS JOIN dbo.iMenu m
WHERE um.menuID = @payments AND m.codeName IN ('miBillRegisterBuh', 'miBankBuh')
  AND NOT EXISTS (SELECT 1 FROM dbo.UserAdditionMenu x WHERE x.userID = um.userID AND x.menuID = m.menuID);

COMMIT TRANSACTION;
GO

-- сверка: пункты в Бухгалтерии; права — столько же, сколько у miPaymentCommon
SELECT m.codeName, m.name, m.position, p.codeName AS parent,
    (SELECT COUNT(*) FROM dbo.GroupMenu g WHERE g.menuID = m.menuID) AS groups,
    (SELECT COUNT(*) FROM dbo.GroupMenu g JOIN dbo.iMenu s ON s.menuID = g.menuID WHERE s.codeName = 'miPaymentCommon') AS groups_expected,
    (SELECT COUNT(*) FROM dbo.UserAdditionMenu u WHERE u.menuID = m.menuID) AS users,
    (SELECT COUNT(*) FROM dbo.UserAdditionMenu u JOIN dbo.iMenu s ON s.menuID = u.menuID WHERE s.codeName = 'miPaymentCommon') AS users_expected
FROM dbo.iMenu m
    LEFT JOIN dbo.iMenu p ON p.menuID = m.parentID
WHERE m.codeName IN ('miBillRegisterBuh', 'miBankBuh');
GO
SET NOEXEC OFF;
GO
