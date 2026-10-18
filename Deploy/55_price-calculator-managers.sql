-- Калькулятор цены для веба, этап К.2 (docs/tasks/web-price-calculator.md, ВК-3 а): список «Менеджер:».
-- Новая процедура dbo.pc_CalculatorManagers — активные пользователи, кому пункт «Калькулятор цены» выдан группой или
-- лично (правило UserMenuItems без isPublic), кроме администраторов и «admin». Существующие объекты не меняются, десктоп
-- её не зовёт.
--
-- Порядок: в любое время до веб-калькулятора, независимо от 53 и 54; клиент qd2 не нужен.
-- Идемпотентен: CREATE OR ALTER.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 55_price-calculator-managers.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [dbo].[pc_CalculatorManagers]
AS
BEGIN
    -- «Менеджер:» веб-калькулятора цены (docs/tasks/web-price-calculator.md, ВК-3 а, этап К.2): активные пользователи,
    -- кому пункт «Калькулятор цены» (miPriceCalculator) выдан группой или лично — правило UserMenuItems без isPublic, — кроме
    -- администраторов и «admin» (userID 0). Администраторы — служебные учётки вроде «Импортёр в 1-С» (состоит в группе с
    -- этим пунктом) и те, кто сам выбирает менеджера (список видит только администратор); себя экран добавляет сам.
    -- Личный запрет (UserAdditionMenu.isGrant = 0) перебивает группу, как в меню.
    SET NOCOUNT ON;

    SELECT u.userID, name = u.lastName + SPACE(1) + u.firstName
    FROM [User] u
        JOIN iMenu m ON m.codeName = N'miPriceCalculator' AND m.isObsolete = 0
        LEFT JOIN UserAdditionMenu um ON um.menuID = m.menuID AND um.userID = u.userID
    WHERE u.userID <> 0
        AND u.isActive = 1
        AND u.isAdmin = 0
        AND (um.isGrant = 1
            OR (um.isGrant IS NULL AND EXISTS (
                SELECT 1
                FROM GroupMenu gmen
                    JOIN GroupMember gm ON gm.groupID = gmen.groupID
                WHERE gmen.menuID = m.menuID AND gm.userID = u.userID)))
    ORDER BY u.lastName, u.firstName;
END
GO

-- Проверка: процедура на месте.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.pc_CalculatorManagers')) LIKE N'%miPriceCalculator%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.pc_CalculatorManagers')) LIKE N'%u.isAdmin = 0%'
    PRINT N'ГОТОВО: процедура pc_CalculatorManagers.';
ELSE
    RAISERROR(N'55: процедура не создана.', 16, 1);
GO
