# Комбо-модули

Комбо-модуль — объединение нескольких модулей в одну сущность. Устроен так же,
как пакетный модуль, но **без уровня прайс-листа**.

| | Пакетный модуль | Комбо-модуль |
|---|---|---|
| Уровень 1 | `PackModule` (`name`, `path`) | `ComboModule` (`name`) |
| Уровень 2 | `PackModulePriceList` (период, цена, наценки, ролик) | *отсутствует* |
| Уровень 3 | `PackModuleContent` (`moduleID`, `modulePriceListID`) | `ComboModuleContent` (`moduleID`) |

Решения, зафиксированные с заказчиком (14.08.2026):

- у комбо-модуля **только название**: ни периода действия, ни цены, ни наценок,
  ни ролика-заставки, ни пути для DJin;
- запись состава ссылается **только на модуль**, без прайс-листа модуля;
- из проверок `PackModuleContentIUD` перенесена **только уникальность модуля**
  внутри комбо-модуля. Единый тип рекламы (`roltypeID`), однородность тарифов по
  `maxCapacity` и покрытие дат прайс-листами модулей — **не переносятся**
  (последнее и невозможно: у комбо-модуля нет дат).

## Шаг 1 (сделано): администрирование

Меню **Администрация → Комбо-модули**. Форма стандартного браузера: слева дерево
комбо-модулей, справа содержимое выбранного узла — ровно как у пакетных модулей.
Создание/редактирование/удаление на обоих уровнях.

Собственных классов C# нет: корень использует
`FogSoft.WinForm.Classes.ObjectContainer`, состав — `PresentationObject`. Всё
остальное описано метаданными.

### Объекты базы

| Объект | Назначение |
|---|---|
| `dbo.ComboModule` | комбо-модуль (`comboModuleID`, `name` уникально) |
| `dbo.ComboModuleContent` | состав; уникальный индекс `(comboModuleID, moduleID)`; FK на `ComboModule` и `Module` с `ON DELETE CASCADE` |
| `dbo.ComboModules` | список для журнала/дерева |
| `dbo.ComboModuleIUD` | CRUD комбо-модуля |
| `dbo.ComboModuleContentRetrieve` | состав комбо-модуля (станция, группа, модуль) |
| `dbo.ComboModuleContentIUD` | CRUD состава + проверка уникальности модуля |
| `dbo.ComboModuleContentPassport` | справочники паспорта: 1 — станции, 2 — модули (`ModuleList`) |

### Метаданные

Скрипт `ArtvisDB/Scripts/combo-modules-seed.sql`, идемпотентный.

| Таблица | Что добавляется |
|---|---|
| `iEntity` | 1270 «Комбо-модуль», 1271 «Модули комбо-модуля» + XML паспортов |
| `iEntityAttribute` | колонки списков: Название; Радиостанция/Группа/Модуль |
| `iEntityAction` | по 5 действий на уровень (Обновить, Удалить, Добавить модуль, Свойства, разделители) |
| `iStoredProcedure` + `iModuleProcedure` | 9 привязок процедур (Fake Module — IUD, 118 — Simple Journal, 119 — Properties Page) |
| `iTableAlias` | `massmedia`, `modules` для `ComboModuleContentPassport` |
| `iRelationScenario` + `iEntityRelation` | сценарий `Combo Modules`: 1270 → 1271 |
| `iMenu` | `miComboModules` в разделе «Администрация», позиция 23 |
| `GroupMenu`, `GroupRight` | права выдаются тем же группам, у которых есть пакетные модули |
| `iMessage` | `ComboModuleContentDuplicate` |

**Идентификаторы сущностей 1270/1271 зашиты в C#** (`Merlin.Classes.Entities`),
поэтому должны совпадать во всех базах. Скрипт прерывается с ошибкой, если эти
номера в целевой базе заняты другими сущностями.

### Изменения в C#

- `Client/Classes/InternalConstants.cs` — `Entities.ComboModule = 1270`,
  `Entities.ComboModuleContent = 1271`, `RelationScenarios.ComboModules`;
- `Client/Forms/MDIForm.cs` — обработчик `miComboModules` → `ShowComboModules`
  (копия `ShowPackModules` со сценарием `Combo Modules`).

## Развёртывание

```
sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i ArtvisDB\Scripts\combo-modules-seed.sql
```

Затем программные объекты (порядок важен — `ComboModuleContentIUD` вызывает
`ComboModuleContentRetrieve`):

1. `dbo\Tables\ComboModule.sql`
2. `dbo\Tables\ComboModuleContent.sql`
3. `dbo\Stored Procedures\ComboModules.sql`
4. `dbo\Stored Procedures\ComboModuleIUD.sql`
5. `dbo\Stored Procedures\ComboModuleContentRetrieve.sql`
6. `dbo\Stored Procedures\ComboModuleContentIUD.sql`
7. `dbo\Stored Procedures\ComboModuleContentPassport.sql`

Таблицы разворачивать до скрипта метаданных не обязательно, но до первого
запуска клиента — обязательно.

После обновления базы — **свежая сборка `Merlin.exe`**. Метаданные сущностей
кэшируются клиентом, так что запущенные копии надо перезапустить.

## Что дальше (вне шага 1)

Комбо-модуль пока ни на что не влияет: он только создаётся и хранится.
Размещение, ценообразование и участие в кампаниях — отдельные шаги.
