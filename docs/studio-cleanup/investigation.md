# Зачистка модуля «Производство роликов» (Studio) — исследование

**Статус:** исследование завершено. Ничего не удалено. Скрипты подготовлены в
`docs/studio-cleanup/drop-plan.sql` — **не запускать до согласования**.

**Дата:** 2026-09-08. Источник фактов по БД: `localhost\ArtvisDev` — это копия
боевой базы, снятая с прода ~2026-09-01, поэтому инвентаризация полная. По
мёртвому модулю расхождений с текущим продом за неделю практически быть не может;
достаточно один раз сверить РАЗДЕЛ 0 из `drop-plan.sql` непосредственно перед
применением.

---

## 1. Что это за модуль

Исторический модуль учёта **производства (записи) рекламных роликов** сторонними
студиями: студии, их прайс-листы и тарифы по «стилям ролика», заказы/наряды на
производство, счета, платежи за производство, балансы и статистика. Отдельная
ветка меню «Производство роликов» (menuID 156) и бухгалтерская ветка (menuID 110).

Сборка десктопа называется `Merlin` (см. `Client/Client.csproj`,
`<AssemblyName>Merlin</AssemblyName>`), поэтому в метаданных классы прописаны как
`Merlin.Classes.*`.

### Признаки того, что модуль мёртв

| Признак | Факт |
|---|---|
| Меню | Вся ветка `miProductionStudio`/156 и `miPaymentStudioOrder…`/110 помечены `iMenu.isObsolete = 1` (≈25 пунктов) |
| Сущности | Почти все `iEntity.isObsolete = 1` (см. §4) |
| Данные | Все транзакционные таблицы **пустые** на dev: `StudioOrder`, `StudioOrderAction`, `StudioOrderBill`, `PaymentStudioOrder`, `PaymentStudioOrderAction` — 0 строк |
| Остались только справочники | `Studio` 3, `StudioAgency` 1, `StudioPricelist` 15, `StudioTariff` 23, `iStudioOrderActionStatus` 3, `iStudioTariffType` 2, `RolStyle` 25 |
| Код | Ключевые формы/отчёты/доменные классы модуля **не подключены** к сборке (`Client.csproj`), лежат в дереве мёртвым грузом (см. §5) |

`RolStyle` («стиль ролика») формально общий с `Roller`, но фактически тоже часть
этого модуля — см. §6.

---

## 2. Таблицы БД (13)

Порядок — от листьев к корню (порядок удаления).

| # | Таблица | Строк (dev) | Назначение |
|---|---|---|---|
| 1 | `PaymentStudioOrderAction` | 0 | распределение платежа по акциям производства |
| 2 | `PaymentStudioOrder` | 0 | платёж фирмы за производство роликов |
| 3 | `StudioOrderBill` | 0 | счёт по акции производства |
| 4 | `StudioOrder` | 0 | наряд на производство одного ролика |
| 5 | `StudioOrderAction` | 0 | акция (пакет нарядов) на производство |
| 6 | `StudioAgency` | 1 | связь студия↔агентство |
| 7 | `StudioTariff` | 23 | тариф: прайс-лист × стиль ролика × тип тарифа |
| 8 | `StudioPricelist` | 15 | период действия прайс-листа студии |
| 9 | `Studio` | 3 | студия (подтип `Agency`: `Studio.studioID = Agency.agencyID`) |
| 10 | `iStudioOrderActionStatus` | 3 | справочник статусов акции |
| 11 | `iStudioTariffType` | 2 | справочник типов тарифа (за секунду / фиксированный) |
| 12 | `RolStyle` | 25 | справочник «стилей ролика» — см. §6 (удалять опционально, отдельным шагом) |

### Внешние ключи, приходящие в домен извне

Ничего постороннего в домен не ссылается, кроме:

- `Studio.studioID` — 1:1 с `Agency` по значению PK (не FK). Строка `Studio` —
  это «признак» того, что агентство является студией. Каскадное удаление
  `Studio` при удалении `Agency` уже зашито в `AgencyIUD` (см. §3).
- FK внутри домена и наружу — на `Agency`, `Firm`, `PaymentType`, `User`,
  `RolStyle`. Обратных FK (снаружи в домен) нет.

`RolStyle` входящие FK: только `FK_StudioTariff_RolStyle`,
`FK_StudioOrder_RolStyle`. `Roller.rolStyleID` — **без FK**.

---

## 3. Программные объекты БД

### 3a. В домене, есть в репозитории `ArtvisDB/` (24)

Функции: `f_GetStudioTariffId`, `f_OrderPrice`.
Вью: `vStudio`.
Процедуры: `rpt_StudioOrderAct`, `rpt_OrderActionBill`, `sl_StudioOrderActions`,
`sl_PaymentStudioOrders`, `stat_BalanceStudioOrder`, `stat_BalanceManagerOrder`,
`BalanceStudioOrderFilter`, `FirmBalanceStudioOrderOnLoad`, `FirmStudioOrderManagers`,
`PaymentStudioOrders`, `PaymentStudioOrderFilter`, `PaymentStudioOrderPassport`,
`PaymentStudioOrderIUD`, `PaymentStudioOrderActions`, `PaymentStudioOrderActionID`,
`PaymentStudioOrderActionFilter`, `PaymentStudioOrderActionUsers`,
`SpecialStudioOrderActions`, `SpecialStudioOrderActionIUD`, `SpecialSOActionPassport`.

### 3b. В домене, но в репозитории `ArtvisDB/` ОТСУТСТВУЮТ (23) — «db-only»

SQL-проект `ArtvisDB` неполон: боевые/dev процедуры CRUD, списков и паспортов
модуля в него никогда не добавляли.

`Studios`, `StudioIUD`, `StudioPassport`, `StudioAgencyID`,
`StudioPricelists`, `StudioPricelistIUD`,
`StudioTariffList`, `StudioTariffPassport`, `StudioTariffIUD`,
`StudioOrders`, `StudioOrderIUD`, `StudioOrderPassport`, `StudioOrderFilter`,
`StudioOrderAgencies`,
`StudioOrderActions`, `StudioOrderActionIUD`, `StudioOrderActionPassport`,
`StudioOrderActionsFilter`, `StudioOrderActionsForPayment`,
`StudioOrderActionPriceForAgency`,
`StudioOrderBills`, `StudioOrderBillIUD`,
`FirmWithOrder` (проц журнала для сущности 119).

> Перед удалением снять их определения (шаг 0 в `drop-plan.sql`) — на случай
> отката и чтобы понять, не тянут ли они что-то ещё (по обратным зависимостям —
> не тянут, кроме общих процедур из 3c).

### 3c. Общие процедуры/функции с «студийной» примесью — **править, не удалять** (9)

| Объект | Что студийного | Что делать при зачистке |
|---|---|---|
| `AgencyIUD` | `DELETE FROM [Studio] WHERE StudioID = @agencyID` в ветке `DeleteItem` | убрать строку `DELETE FROM [Studio]` |
| `agencyPassport` | 2-й результат-сет: список студий (`vStudio` + `StudioAgency`) для селектора на карточке агентства | убрать 2-й SELECT; синхронно убрать `iTableAlias` (proc `agencyPassport`, position 2, alias `studio`) и страницу-селектор студий из паспорта сущности `Agency` (entityID 8) + студийный код в `Client/Classes/Agency.cs` (класс `StudioAgency`, ветки `ProductionStudio` в `Update`/`Delete`) |
| `agencies` | закомментированный `union … StudioOrder` и `@needStudioID` | почистить мёртвый комментарий (косметика) |
| `sl_Agencies` | закомментированный `LEFT JOIN StudioAgency` | косметика |
| `LookupUsedAgency` | `union select distinct o.agencyID from StudioOrder o` — **активно** | убрать `union … StudioOrder` |
| `UserListByRights` | ветка `@forStudioOrders = 1` → `fn_IsRightТо…SOActions` | убрать параметр `@forStudioOrders` и ветку (сверить всех вызывающих!) |
| `GroupListByRights` | то же | то же |
| `RollerIUD` | параметр `@studioOrderID`, `SELECT … FROM StudioOrder` для подстановки | убрать параметр и связанный `SELECT`/`IF` |
| `RollerPassport` | возвращает результат-сет `RolStyle` | убрать, если решено удалять `RolStyle` (§6) |

### 3d. Отчётные/статистические процедуры, целиком про студию, но на общих сущностях (3 + фильтры)

`stat_VolumeOfRealizationForRollers` (+ `statVolumeOfRealizationForRollersFilter`) —
сущность 159 «Статистика::Объём реализации по роликам».
`stat_RollerStatisticCreated` — сущность 186 «Статистика::Созданные ролики».
`stat_BalanceManagerOrder` — сущность 169.

Читают только `StudioOrder*`/`PaymentStudioOrder*`. Меню (91, 116, 104) —
`isObsolete = 1`. Сущности 159 и 186 при этом `isObsolete = 0` (недочистили).
Удаляются вместе с модулем.

### 3e. Функции с зависимостями

- `f_OrderPrice` → `StudioTariff` — студийная, удалить.
- `f_GetStudioTariffId` → `StudioPricelist`, `StudioTariff` — студийная, удалить.

Внешние объекты, ссылающиеся на `vStudio`: `agencyPassport`, плюс студийные
`StudioOrderFilter`, `StudioOrderPassport`, `StudioOrders`, `Studios`.
После правки `agencyPassport` вью можно удалять.

---

## 4. Метаданные (таблицы `i*`, права, меню)

### 4a. `iEntity` — сущности модуля

| entityID | name | tableName | className | isObsolete |
|---|---|---|---|---|
| 19 | Продакшен-студия | vStudio | Merlin.Classes.ProductionStudio | 1 |
| 113 | Прайс-лист (производство рекламных роликов) | StudioPricelist | Merlin.Classes.StudioPricelist | 1 |
| 115 | Тариф на производство роликов | StudioTariff | …PresentationObject | 1 |
| 116 | Акция на производство роликов | StudioOrderAction | Merlin.Classes.Domain.StudioOrder.StudioOrderAction | 1 |
| 117 | Заказ на производство ролика | StudioOrder | Merlin.Classes.Domain.StudioOrder.StudioOrder | 1 |
| 119 | Фирма (журнал заказов на ролики) | firm | …ObjectContainer | 1 |
| 123 | Агентсто для Студии | StudioAgency | …PresentationObject | 1 |
| 124 | Платеж за производство роликов | PaymentStudioOrder | Merlin.Classes.PaymentStudioOrder | 1 |
| 125 | Оплата акции (производство роликов) | PaymentStudioOrderAction | …PresentationObject | 1 |
| 126 | Кандидаты на оплату (производство роликов) | StudioOrderAction | …PresentationObject | 1 |
| 127 | Баланс (производство роликов) | — | Merlin.Classes.FirmBalanceStudioOrder | 1 |
| 128 | Счёт (производство роликов) | StudioOrderBill | …PresentationObject | 1 |
| 159 | Статистика::Объём реализации по роликам | — | …PresentationObject | **0** |
| 169 | Статистика::Журнал задолженностей по менеджерам (заказы) | — | …PresentationObject | 1 |
| 186 | Статистика::Созданные ролики | — | …PresentationObject | **0** |
| 200 | Акт выполненных работ (Производство роликов) | StudioOrder | …PresentationObject | 1 |
| 208 | Остаток (Производство роликов) | StudioOrderAction | …PresentationObject | 1 |
| 4 | Стиль ролика | RolStyle | …PresentationObject | 1 | ← см. §6 |

Общие сущности с модульной привязкой (сами не трогаем, чистим только их
студийные строки в `iModuleProcedure`): `107` Пользователь, `199` Менеджер
(модуль 210 «Select For Studio Order»), `8` Agency (селектор студий в паспорте).

### 4b. Прочие метаданные, привязанные к этим сущностям / процедурам

Объём на dev (замерено сухим прогоном `drop-plan.sql`):

| Метатаблица | Строк | Комментарий |
|---|---|---|
| `iEntity` | 17 | сущности из §4a (без entity 4) |
| `iEntityAction` | 66 | действия этих сущностей |
| `iEntityAttribute` | 87 | колонки журналов |
| `iEntityRelation` | 4 | сценарии 16, 17 |
| `iStoredProcedure` | 41 | id 31–32, 115–161, 254, 307–308, 324 + отчёты/статистика |
| `iModuleProcedure` | 64–66 | маппинг сущность/модуль/действие → процедура (в т.ч. модуль 210) |
| `iTableAlias` | 43 | псевдонимы результат-сетов паспортов/фильтров |
| `GroupRight` | 73 | выданные права (реально ненулевые по сущностям 116/117/124/125/127) |
| `iMenu` | 17+ | ветки 156 и 110 целиком + отдельные пункты; все `isObsolete = 1` |
| `GroupMenu` | 10 | права групп на эти пункты меню |
| `iModules` | 1 | `210` «Select For Studio Order» |
| `iRelationScenario` | 1–2 | `17` `ProductionAction`; `16` (19→113) — проверить, что не переиспользуется |
| `UserAdditionMenu` / `UserAdditionRight` | 0 | проверено на dev |

Пункты меню: `miProductionStudio`, `miStudioTariff`, `miRolStyle`,
`miCreateProductionAction`, `miProductionActionsStudio`, `miPaymentStudioOrder*`,
`miBalanceStudioOrder*`, `miFirmBalanceStudioOrder`, `miStudioOrderActPrint`,
`miSpecialStudioOrderActions`, `miStats.VolumeOfRealization4Roll`,
`miStats.RollersCreated`, `miStats.BalanceManagerOrder` и вся ветка 156/110.

---

## 5. Код C#

**Проверка рабочих ссылок на удаляемые классы выполнена** (2026-09-08, поиск по
всему решению + сверка с `Client.csproj` / `FogSoft.Core.csproj`). Итоги — в §5a–5e.
Резолюция сущностей идёт только через метаданные (`Entity.CreateObject` →
`Activator.CreateInstance(assemblyName, className)`, `FogSoft.WinForm/Classes/Entity.cs`);
C#-фабрик/`switch` по студийным типам нет — после удаления строк `iEntity`
классы становятся полностью недостижимыми.

### 5a. Скомпилировано в сборку (есть в `Client.csproj`) — подлежит удалению

| Файл | Роль | Кто на него ссылается |
|---|---|---|
| `Client/Classes/ProductionStudio.cs` | доменный класс студии (+ вложенный `struct ParamNames`) | только `Agency.cs` (§5b) и метаданные (entity 19) |
| `Client/Classes/StudioPricelist.cs` | доменный класс прайс-листа студии | **никто** в C# — только метаданные (entity 113) |
| `Client/Classes/PaymentStudioOrder.cs` + `.WinForms.cs` | платёж за производство | `MDIForm.cs`, `FrmFirmStudioOrderBalance.cs`, метаданные (entity 124). **⚠ см. §5e — здесь же лежит базовый `abstract class Payment`** |
| `Client/Classes/FirmBalanceStudioOrder.cs` + `.WinForms.cs` | баланс по производству | `FrmFirmStudioOrderBalance.cs`, метаданные (entity 127) |
| `Client/Forms/FrmFirmStudioOrderBalance.cs` (+ `.Designer`, `.resx`) | форма баланса по фирме | `MDIForm.ShowFirmBalanceStudioOrders()`, `FirmBalanceStudioOrder.WinForms.cs` |

Все внешние ссылки — либо внутри самого набора на удаление, либо в файлах §5b,
которые всё равно правятся. **Живых ссылок из непричастного кода нет** (кроме
общего `Payment`, §5e).

`FogSoft.Core/FogSoft.Core.csproj` (веб) дополнительно линкует
`FirmBalanceStudioOrder.cs`, `PaymentStudioOrder.cs`, `ProductionStudio.cs`,
`StudioPricelist.cs` (строки 120, 148, 152, 164) — эти
`<Compile Include="..\Client\…" Link="Domain\…">` тоже убрать. `.WinForms.cs`
половинки в веб не линкуются. В самом `FogSoft.Web/` студийные классы напрямую
не создаются (только enum в `MenuRoutes.cs`).

### 5b. Общие файлы со студийными вставками — **править точечно**

| Файл | Что студийного |
|---|---|
| `Client/Classes/InternalConstants.cs` | значения enum `Entities` (RolStyle=4, ProductionStudio=19, StudioPricelist=113, StudioOrderAction=116, StudioOrder=117, StudioAgency=123, PaymentStudioOrder=124, PaymentStudioOrderAction=125, StudioOrderActionPaymentCandidate=126, BalanceStudioOrder=127, StudioOrderBill=128, StudioOrderActJournal=200, SpecialStudioOrderAction=208, FirmWithOrders=119); константа `RelationScenarios.StudioTariff`; `StatsVolumeofRealization4Rollers=159`, `StatsRollersCreated=186`, `StatsBalanceManagerOrder=169` |
| `Client/Forms/MDIForm.cs` | ветки роутинга меню и методы `ShowStudioJournal`, `ShowStudioTariff`, `ShowStudioOrders`, `ShowBalanceStudioOrders`, `ShowFirmBalanceStudioOrders`, `ShowPaymentStudioOrders`, `ShowPaymentStudioOrderByManager*`, `ShowSpecialStudioOrderAction`, `ShowRolStyles` |
| `Client/Classes/Agency.cs` | класс `StudioAgency : PresentationObject`; ветки `po as ProductionStudio` в `Update()`/`Delete()`; `procParameters["needStudioID"]` |
| `FogSoft.WinForm/Classes/Constants.cs` | `InterfaceObjects.SelectForStudioOrder = 210` |
| `FogSoft.Web/Infrastructure/MenuRoutes.cs` | 7 записей студийных `mi*` → `Entities.*` |
| `FogSoft.Web/Infrastructure/DomainAssemblyResolver.cs` | только в комментарии-примере; трогать не нужно |
| `Client/Classes/PaymentCommon.cs` (`PaymentCandidatesForm.cs`) | закомментированная ссылка на `PaymentStudioOrderAction` — косметика |

Ложные срабатывания (не студия): `*/Resources.Designer.cs`, `Settings.Designer.cs`
(строка «Microsoft Visual Studio»), `VectorBoxExportDocument.cs`
(`<StudioId>0</StudioId>` — чужой формат плейлиста), `DataAccessor.cs`
(«Management Studio» в комментарии), `PresentationObject.cs` /
`FirmBalance.WinForms.cs` (упоминания в комментариях).

### 5c. Мёртвый груз — файлы в дереве, но **НЕ в сборке** (проверено по `Client.csproj`)

Их можно просто удалить как файлы, на компиляцию не влияет:

- `Client/Forms/StudioOrderActionForm.cs` (+ `.Designer.cs`, `.resx`)
- `Client/Classes/Domain/StudioOrder/StudioOrder.cs`
- `Client/Classes/Domain/StudioOrder/StudioOrderAction.cs`
- `Client/Reports/StudioOrderAct.cs`, `StudioOrderActReport.cs`,
  `StudioOrderActionBill.cs`, `StudioOrderActionBillReport.cs`,
  `StudioOrderAgreement.cs`, `StudioOrderContract.cs`, `StudioOrderContractReport.cs`
- `Client/Reports/StudioOrderAct.rpt`, `StudioOrderActionBill.rpt`,
  `StudioOrderAgreement.rpt`, `StudioOrderContract.rpt`

**Проверено:** на эти типы ссылаются только файлы из этого же списка (namespace
`Merlin.Classes.Domain.StudioOrder` используется в 3 отчётах + `StudioOrderActionForm.cs`,
все — не в сборке). `.rpt` в `Client.csproj` как `EmbeddedResource` не подключены
(только `Icons/*.png`). Рефлексии по строковым именам этих типов нигде нет.
Метаданные сущностей 116/117 указывают `className =
Merlin.Classes.Domain.StudioOrder.*`, которого **нет в сборке** → эти журналы уже
падают при открытии (лишнее подтверждение, что модуль мёртв). Кластер закрыт —
файлы можно удалить.

### 5d. Иконки (`Client.csproj`, `EmbeddedResource`)

`Icons/StudioOrder.png`, `Icons/StudioOrderAction.png`, `Icons/JournalStudioOrder.png`,
`Icons/AddStudioOrder.png` — убрать `EmbeddedResource` и файлы.

### 5e. Базовый класс `Payment` — вынесен в отдельный файл ✅ (2026-09-08)

`Client/Classes/PaymentStudioOrder.cs` содержал **два** типа: `abstract class Payment`
(база живого не-студийного функционала — `PaymentCommon`, `PaymentCandidatesForm`,
линкуется и в веб) и студийный `PaymentStudioOrder : Payment`.

Сделано:
- `abstract class Payment` (+ `Payment.ParamNames`) вынесен в новый
  `Client/Classes/Payment.cs`;
- добавлен в `Client.csproj` (перед `PaymentStudioOrder.cs`) и в
  `FogSoft.Core.csproj` (перед `PaymentCommon.cs`);
- `PaymentStudioOrder.cs` теперь содержит только `PaymentStudioOrder`.
- Собрано: `qd2.sln`, `FogSoft.Core`, `FogSoft.Web` — 0 ошибок.

Теперь `PaymentStudioOrder.cs` + `.WinForms.cs` можно удалять вместе с модулем без
последствий для общих платежей.

Остальные базовые классы удаляемых типов лежат в своих файлах и не затронуты:
`Pricelist` (для `StudioPricelist`), `FirmBalance` (для `FirmBalanceStudioOrder`),
`FrmFirmBalance` (для `FrmFirmStudioOrderBalance`), `ObjectContainer` (для
`ProductionStudio`).

---

## 6. `RolStyle` («стиль ролика») — отдельное решение

Сейчас `RolStyle` используется:

- FK от `StudioTariff`, `StudioOrder` (уходят вместе с модулем);
- колонка `Roller.rolStyleID` — **без FK**, заполнена у 2845 из 21523 роликов,
  но в паспорте ролика (entity 20) поля стиля **нет** — не редактируется и не
  показывается;
- `RollerPassport` возвращает мёртвый результат-сет со списком `RolStyle`;
- entity 4, меню `miRolStyle` (`isObsolete = 1`), процедуры `RollerStyles`,
  `RolStyleIUD`, форма — `MDIForm.ShowRolStyles`.

**Вывод:** `RolStyle` — часть того же мёртвого модуля, просто «протекла» в `Roller`.
Удаление возможно, но это чуть больший объём правок (`Roller.rolStyleID`, `vRoller`,
`RollerIUD`, `RollerPassport`, enum, меню). Вынесено в **отдельный необязательный
шаг** — приоритет ниже, чем у основной зачистки `Studio*`.

---

## 7. Порядок зачистки (когда согласуем)

1. **Бэкап**: `BACKUP DATABASE` + выгрузка определений всех db-only процедур (§3b) и
   студийных строк метаданных.
2. **БД, схема**:
   1. поправить общие процедуры §3c (`AgencyIUD`, `agencyPassport`, `LookupUsedAgency`,
      `UserListByRights`, `GroupListByRights`, `RollerIUD`, при желании `RollerPassport`);
   2. `DROP PROCEDURE` — все из §3a + §3b + §3d;
   3. `DROP FUNCTION f_OrderPrice, f_GetStudioTariffId`;
   4. `DROP VIEW vStudio`;
   5. `DROP TABLE` в порядке §2 (1→11; `RolStyle` — отдельно, §6).
3. **БД, метаданные** (§4): удалить строки `GroupRight` → `GroupMenu` → `iMenu` →
   `iModuleProcedure` → `iTableAlias` → `iEntityAction` → `iEntityAttribute` →
   `iEntityRelation` → `iRelationScenario` → `iStoredProcedure` → `iEntity` →
   `iModules` (210). Порядок — по FK между `i*` таблицами (проверить на конкретной БД).
4. **Код** (§5):
   1. ~~вынести `abstract class Payment` из `PaymentStudioOrder.cs` в
      `Client/Classes/Payment.cs`~~ — **сделано 2026-09-08** (§5e);
   2. удалить файлы 5a + 5c + иконки 5d; вычистить вставки 5b;
   3. убрать `<Compile>`/`<EmbeddedResource>` из `Client.csproj` и `FogSoft.Core.csproj`;
   4. собрать `qd2.sln` и веб (`FogSoft.Core`/`FogSoft.Web`).
5. **Регрессия**: логин; карточка агентства (селектор студий должен исчезнуть без
   ошибок); журнал роликов + паспорт ролика; журнал платежей (общий);
   `LookupUsedAgency` дергается из фильтров платежей/баланса — проверить эти формы;
   права/меню у не-админа.
6. **`ArtvisDB` sqlproj**: удалить `.sql`-файлы студийных объектов из проекта,
   чтобы сравнение схемы не пыталось их воссоздать.

## 8. Риски

- **`ArtvisDB` sqlproj неполон** — в БД объектов больше, чем в репозитории (§3b).
  Список §3b собран с копии прода (dev), т.е. полный; отдельной сверки на проде
  не требует, кроме РАЗДЕЛА 0 перед применением.
- **`agencyPassport` + паспорт агентства** — самое связное место. Правка
  процедуры, `iTableAlias`, XML паспорта сущности 8 и `Agency.cs` должны идти
  вместе, иначе карточка агентства сломается. Метаданные кешируются — рестарт
  клиента после правок `iEntity`/`iTableAlias`.
- **`UserListByRights` / `GroupListByRights`** — параметр `@forStudioOrders`
  используется и не-студийными вызовами со значением по умолчанию 0; удаление
  параметра требует сверки всех вызовов (C# + метаданные).
- **`LookupUsedAgency`** — активная процедура (не комментарий); используется в
  общих фильтрах. После правки проверить, что список агентств в фильтрах платежей
  не «схлопнулся».
- **Порядок удаления строк в `i*`** — между метатаблицами есть свои FK; на
  конкретной БД проверить и при необходимости переставить шаги §7.3.
- **Рефлексия по формам/отчётам** (§5c) — проверено, отсутствует.
- **Базовый `Payment` в удаляемом файле** (§5e) — если удалить `PaymentStudioOrder.cs`
  «в лоб», отвалятся `PaymentCommon` и `PaymentCandidatesForm` (живой функционал
  «Журнал оплат», в т.ч. веб). Порядок правок — в §7.4.1.

---

## Приложение

- `docs/studio-cleanup/drop-plan.sql` — черновик скриптов (снятие бэкапа
  определений, правки общих процедур, DROP-ы, очистка метаданных). Обёрнут в
  `BEGIN TRAN … ROLLBACK` — в текущем виде это сухой прогон. **Проверено на
  `ArtvisDev`: проходит целиком без ошибок и откатывается** (2026-09-08). Для
  боевого применения — сверить РАЗДЕЛ 0 на `Artvis`, снять `BACKUP DATABASE`,
  заменить `ROLLBACK` на `COMMIT`.
