# Инвентаризация меню десктопа и статус в вебе

Дата: 2026-09-18. База: `ArtvisDev` (ноутбук разработчика, Windows-аутентификация).
Ветка: `feature/web-passport-lookup`, отправная точка — `bf8565a`.

## Зачем

Первая инвентаризация (этап 1) считала только ветки верхнего уровня
`if/else` в `MDIForm.MenuItemClick`. Из-за этого пропустили семейство
`miStats.*`: оно уходит в одну ветку `ShowStatsJournal`, а внутри у неё свой
`switch` на 15 пунктов (история — `web-migration.md`, «Журналы статистики и
правило ManagerFilter»). Здесь — карта без таких дыр: каждый пункт `iMenu` с
`codeName` прослежен до конца, до экрана или операции, и сверен с вебом.

**Веб и десктоп не запускались.** Статус «перенесён» — это «есть запись в
`MenuRoutes`» (`FogSoft.Web/Infrastructure/MenuRoutes.cs`), а не «открыл и
проверил»; живая проверка перенесённых экранов — в `web-migration.md`. Всё
остальное — чтение кода и запросы к `ArtvisDev`. Прод (`Artvis`) не
запрашивался: что в его `iMenu`, **не установлено**.

## 1. Источники

1. **`iMenu` на `ArtvisDev`**: 90 строк, `isObsolete = 1` нет. Из них 10
   разделителей (`name = '-'`), 10 папок (у папки есть дети) и 70 листьев.
   `codeName` есть у всех 70 листьев и у одной папки — `miAccounting`, то есть
   **71 пункт с `codeName`**. Прежняя формулировка «19 папок без `codeName`» —
   это 9 папок и 10 разделителей.
2. **Процедура `UserMenuItems`** (`ArtvisDB/dbo/Stored Procedures/UserMenuItems.sql`)
   — единственный источник прав; веб грузит меню ею же (`MenuService.Load`).
3. **Десктоп**: `Client/Forms/MDIForm.cs`, `MenuItemClick` (`:128`) — 57 веток
   верхнего уровня (`:137`–`:257`, плюс охранное `:133`), в них 61 отдельный
   `codeName` и префикс `miStats.` (`:218`), за которым `switch` на 15 case
   (`:749`–`:811`). Сверх этого просмотрены все вызываемые методы и формы, на
   которые ветки ведут (см. таблицу и §4). Дерево строит
   `FogSoft.WinForm/Classes/MenuManager.cs`.
4. **Веб**: `MenuRoutes.cs` (30 записей `SimpleJournal` + 8 `Browser`),
   `MenuAccess.cs`, `MenuNodeView.razor`, `Journal.razor`, `Browser.razor`.

Ветки считались так (воспроизводимо; результат 58 включает охранное `if` на
`:133`, то есть 57 веток):

```bash
sed -n '/private void MenuItemClick/,/catch (Exception ex)/p' Client/Forms/MDIForm.cs \
  | grep -cE "^\s*(else )?if ?\("
```

### Старые цифры не сходятся

- В докстроке `MenuRoutes.cs:9-11` и в `web-migration.md` (строки 898 и 1670)
  стоит «70 веток, из них 22 → `ShowSimpleJournal`». На текущем `MDIForm.cs`
  веток 57, а прямо в `ShowSimpleJournal` ведут **15** (`miPaymentType`,
  `miBank`, `miFirm`, `miBalance`, `miBalanceFromRSection`,
  `miPaymentByManagerFromRSection`, `miConfirmationHistory`,
  `miTransferJournal`, `miLog`, `miGroupMassmedia`, `miSpecialActions`,
  `miReportPartText`, `miManagerDiscountHistory`, `miManagerDiscountReason`,
  `miBonusesStat`) плюс 14 внутри `miStats.*`. Сходится с 30 записями
  `SimpleJournal`: 15 + 14 + `miMassMedia` = 30.
- Часть разницы объясняется зачисткой Studio: до коммита `6c3c372` в
  `MenuItemClick` было 68 веток, коммит убрал 11 studio-веток. Откуда взялось
  именно «22» — **не установлено**. Докстрока — код, в этой задаче не правилась.
- Цифра «59 веток» из постановки тоже не воспроизвелась: получилось 57.

## 2. Что происходит по клику (общая механика десктопа)

- Обработчик `MenuItemClick` навешен на **каждый** пункт, включая папки:
  верхний уровень — `MenuManager.cs:148`, дети — `:161`. Ключ — `Tag`, то есть
  `codeName` (`MenuManager.cs:213`, `MDIForm.cs:135`).
- У папок без `codeName` `Tag = DBNull`, `ToString()` даёт пустую строку — ни
  одна ветка не совпадает, ничего не происходит. Разделитель — не
  `ToolStripMenuItem`, обработчик выходит на `:133`.
- Если верхний пункт недоступен (`enabled = 0`), его поддерево в десктопе не
  строится вовсе (`MenuManager.cs:146`–`:150`). Веб рисует все узлы и гасит
  недоступные листья (`MenuNodeView.razor:36`).
- Меню «Окно» (4 подпункта раскладки MDI и список открытых окон) создаётся
  кодом, а не из `iMenu` (`MenuManager.cs:110`–`:120`); оболочки MDI в вебе не
  будет (решение 2026-08-31, `web-migration.md`, этап 1).

## 3. Сводка по категориям

Числа — по 71 пункту с `codeName`. Таблица по каждому пункту — в §9.

| Категория | Пунктов | Веб сейчас |
|---|---:|---|
| Простой журнал | 11 | перенесены все |
| Простой журнал + `ManagerFilter` | 17 | перенесены все |
| Журнал-наследник, по сути простой (`miMassMedia`) | 1 | перенесён |
| Дерево на `FakeContainer` | 7 | перенесены все |
| Дерево на своём контейнере | 6 | перенесены все: «Предмет рекламы» (2026-09-18), 5 журналов акций (2026-09-20) |
| `MasterDetail` | 4 | движка не будет (решение владельца 2026-09-21): `miAgencyTax` и `miHeadOrganizations` перенесены деревом; два журнала оплат — решение отложено |
| Журнал-наследник со своей логикой | 4 | `miStats.Balance` (2026-09-18) и `miAnnouncements` (2026-09-21) перенесены; нет: `miRoller`, `miActPrint` |
| Собственная форма | 6 | нет, этап 3 |
| Диалог → журнал(ы) | 1 | отложено 2026-09-21: в лоб не переносим, нужно своё решение (§10) |
| Мастер: диалоги → карточка акции (новая) | 3 | нет, этап 3 |
| Действие без экрана | 3 | отложено 2026-09-21: пока не делаем (§10) |
| Импорт из файла (новая) | 1 | нет, этап 4 (`miFirmImport`) |
| Отчёт / выгрузка | 4 | нет, этап 4 |
| График (новая) | 1 | перенесён таблицей 2026-09-21; диаграммы отложены (§10) |
| Выход из приложения (новая) | 1 | сделан (2026-09-18): выход из сеанса, как кнопка «Выйти» |
| Папка с `codeName`, без обработчика (новая) | 1 | папка рисуется как группа (`miAccounting`) |
| **Итого** | **71** | |

Новые категории заведены потому, что пункт не ложился ни в одну из заданных:
«мастер» — цепочка модальных форм с выбором фирмы, кончающаяся карточкой акции
(не «собственная форма» и не «диалог → журнал»); «импорт» и «график» — другой
класс работы (файл и Excel на сервере, диаграмма); «выход» и «папка» — не экраны.

**Статус в вебе по числам:**

| Статус | Пунктов |
|---|---:|
| перенесён (32 `SimpleJournal` + 15 `Browser`) | 47 |
| журналы оплат: `MasterDetail` в вебе не будет, решение об экране отложено (2026-09-21) | 2 |
| этап 3 | 11 |
| этап 4 (отчёты, выгрузки, импорт) | 5 |
| отложено (§10) | 4 |
| не экран: `miExit` (сделан выходом из сеанса), `miAccounting` (папка) | 2 |
| **Итого** | **71** |

Один экран за несколькими пунктами: три пункта `miActionJournal`,
`miActionJournalBuh`, `miActionJournalTraffic` — один и тот же экран (`:165`);
`miBalance` и `miBalanceFromRSection` — одна функция `ShowBalance`;
`miPrintGrid` и `miPrintGridFromRSection` — одна `ShowPrintGridForm`;
`miPaymentCommon` и `miPaymentFRS`, `miRollerStatistic` и
`miRollerStatisticWithFilter` — одна функция с разным аргументом. Права у таких
пунктов отдельные (§7), так что в вебе они остаются отдельными пунктами.

## 4. Что своего в журналах-наследниках и собственных контейнерах

Здесь разобрано, что именно делает наследник поверх `JournalForm` или
`FakeContainer`, — чтобы отделить экраны, которые подключаются почти одной
строкой в `MenuRoutes`, от настоящего этапа 3.

### `MassmediasJournal` (`miMassMedia`) — простой журнал

`Client/Forms/MassmediasJournal.cs:14`–`:35`: конструктор с
`Entities.MassMedia`, в `OnLoad` подписка на три события и `RefreshJournal()`
на каждое. Уже перенесён (`MenuRoutes.cs:59`), обоснование — докстрока
`MenuRoutes`.

### `StatBalanceJournalForm` (`miStats.Balance`) — простой журнал с подменой сущности

`Client/Forms/StatBalanceJournalForm.cs:6`–`:21`. Своё — одно: `LoadData`
(`:12`–`:20`): если в отборе `IsGroupByAgency` включён, таблица берёт сущность
`StatsBalanceGroup` (185), иначе `StatsBalance` (160); дальше обычный
`base.LoadData`, который читает `Grid.Entity.GetContent(...)`
(`JournalForm.cs:209`). Без `ManagerFilter` (`MDIForm.cs:816`–`:821`), заголовок
— «Статистика :: » + текст пункта. Обе сущности — `SimpleObjectEntity`, колонки
берутся из данных; у 160 в `iEntityAttribute` 3 атрибута, у 185 — ни одного;
фильтр (`iEntity.filter`) с `IsGroupByAgency` есть у обеих.
**Вывод: дёшево.** Веб читает данные `Journal.razor:208` (`_entity.GetContent`),
там нужна подмена сущности по значению `IsGroupByAgency`.

**Сделано (2026-09-18):** `JournalRoute.EntitySwitch`, подмена в `Journal.razor`;
фильтры сверены по `iEntity.filter` — у 185 те же поля, что у 160, кроме
«Показывать с оплатой» / «Показывать без оплаты» (их у 185 нет); поля менеджера
есть у обеих, но `ManagerFilter` для пункта не включён, как и в десктопе.
Подробности — `web-migration.md`, этап 2.

### `AnnouncementJournalForm` (`miAnnouncements`) — простой журнал с тремя надстройками

`Client/Forms/AnnouncementJournalForm.cs`:

- кнопка «Пометить все как прочитанное» (`:31`–`:48`, обработчик `:50`–`:58`, цикл `:62`–`:83`):
  для каждой строки без `ConfirmationDate` — `Announcement.DoAction(MarkAsRead)`;
- скрыты кнопки «Новый», «Удалить» и «Сумма» — **по индексам** элементов
  панели (`:45`–`:47`);
- всё остальное — обычный журнал сущности 179.

Отдельно: автопоказ журнала по таймеру (`MDIForm.cs:974`–`:983`, интервал 5 мин)
**сейчас отключён** — первая строка `CheckAnnouncements(object, EventArgs)` это
`return;` (`MDIForm.cs:842`, добавлено 2026-03-17 коммитом `903823d1`; код после
неё недостижим, компилятор предупреждает CS0162). Таймер срабатывает вхолостую.
**Вывод: список — почти одной строкой** (сущность 179 без правил). «Добавить» и
удаление в вебе гаснут сами: сущность не объявляет `AddItem`/`DeleteItem`
(принцип «состав кнопок решают метаданные», `web-migration.md`). «Пометить
прочитанным» — действие по строке (`Announcement.cs`, `SetReadMark` приватный,
`DoAction` в UI-половине `Announcement.WinForms.cs`); путь выполнения действия
по строке в вебе появился 2026-09-18 (`ObjectActions`), и **всё перенесено
2026-09-21** — см. `web-migration.md`, этап 2.

### `AudioJournalForm` (`miRoller`) — не одна строка

`Client/Forms/AudioJournalForm.cs`. Список — обычный журнал сущности `Roller`
(20), но панель расширена: «Прослушать/Стоп» (`:52`–`:81`, `MediaControl`,
проигрывание файла ролика), «Сохранить» — копирование файлов роликов в
выбранный каталог (`:83`–`:91`, `:144`–`:153`, `RollersCopyFrm`), «Удалить все»
только для администратора — удаляет **вместе с физическими файлами**
(`:93`–`:110`, `:155`–`:165`, `RollersDeleteFrm`), «Импортировать» — только при
`HasLegacyDBConnectionString` (`:112`–`:128`). Плюс паспорт сущности 20 содержит
кнопку «Загрузить с диска» (`btnLoad`; проверено по `iEntity.passport`). Всё
своё — файлы и звук на клиенте. **Вывод: этап 3 и этап 4** (стриминг аудио —
в плане этапа 4); список без кнопок ничего не даст без загрузки файла.

### `ActJournalForm` (`miActPrint`) — не одна строка

`Client/Forms/ActJournalForm.cs`. Создаётся с `doNotRefresh = true` (`:14`,
`JournalForm.cs:34`): данные не грузятся, пока не задан отбор. Своё —
`PopulateDataGrid` (`:23`–`:62`), постобработка таблицы в памяти: накопление
итогов по акции, гашение повторов `actionId`/`firmName`/`currentDate`,
добавочная строка «Итого» с жирным шрифтом, предупреждение, если во второй
таблице ответа есть строки. Действия строки — печать справки об эфире и медиаплана
(`ActJournalRow.WinForms.cs:12`–`:17`). **Вывод: этап 3 плюс печать этапа 4** —
это не «простой журнал»: результат процедуры перед показом переделывается.

### `AdvertTypeContainer` (`miAdvertSubject`) — `FakeContainer` с двумя переключателями

`Client/Classes/FakeContainers/AdvertTypeContainer.cs:6`–`:36`, действия —
`AdvertTypeContainer.WinForms.cs:11`–`:24`. Это `FakeContainer` со сценарием
«Предметы рекламы» (`iRelationScenario`: id 6, стартовая сущность 17, фильтра
нет) и двумя действиями «с группировкой / без группировки», которые меняют
`ChildEntity` между `AdvertType` (17) и `AdvertTypeChild` (1243). Класс
`internal`, но веб и не обязан его создавать: `Browser.razor:139` строит
`FakeContainer` сам. **Вывод: почти одной строкой** — запись в
`MenuRoutes.Browser` даст экран со стартовой сущностью, но без переключателя.

**Сделано (2026-09-18):** свой контейнер через `BrowserRoute.Factory`, класс
сделан `public`, переключатели — пункты меню корня через
`ObjectActions.ClassActions`. Подробности — `web-migration.md`, этап 2.

### `ActionContainer` (пять пунктов журнала акций) — дерево + переключатели + переименование узлов

`Client/Classes/FakeContainers/ActionContainer.cs:8`–`:79`, действия —
`ActionContainer.WinForms.cs:14`–`:66`. `FakeContainer` со сценарием
(`ConfirmedAction`/`UnconfirmedAction`/`DeletedAction`, стартовые сущности 1255/
1256/1257) плюс:

- три переключателя `ChildEntity` (группы компаний / фирмы / акции,
  `ActionContainer.cs:53`–`:62`, `ActionContainer.WinForms.cs:18`–`:32`);
- `ProcessCreatedChildObject` (`ActionContainer.cs:68`–`:78`) — узлам-акциям
  подставляется имя `Action.CreateNameWithFirmAndStartDatePeriod`;
- свой диалог отбора `ActionJournalFilter` — это то же правило менеджера по
  `userID`, что веб уже умеет как `RestrictManager` (`ActionJournalFilter.cs:25`–`:27`).

Класс `public` и собирается в `FogSoft.Core` (`FogSoft.Core.csproj:90`), веб
может его создать. Но смысл экрана — действия по строке акции (редактирование,
кампании, печать), а не показ дерева: это уже «журнал акций» из плана
контекстного меню. **Вывод: не дёшево.**

**Сделано (2026-09-20):** само дерево и переключатели обошлись разрезом
`DoAction` в ядро плюс записью в `ObjectActions.ClassActions` — то же, что у
`AdvertTypeContainer`; правило менеджера из `ActionJournalFilter` покрыл
существующий признак отбора (`BrowserRoute.ManagerFilter`). Оценка «не дёшево»
относилась к действиям по строке акции, и она в силе: «Свойства» ведут в
`ActionForm` (этап 3), печать — этап 4; в меню они серые. Подробности —
`web-migration.md`, этап 2.

### `MassmediasAndCampaignsContainer` — мёртвый

`Client/Classes/MassmediasAndCampaignsContainer.cs:6`–`:20`: `FakeContainer` со
сценарием «Massmedia and Campaigns» и принудительным `IsFilterable`. Ведёт на
него `miPrintInquire`, которого в `iMenu` нет (§7).

### Остальные обработчики

- **`MasterDetailForm`** (`FogSoft.WinForm/Forms/MasterDetailForm.cs`): две
  таблицы; выбор строки в главной перезагружает подчинённую
  (`grdMaster.DependantGrid`, `MasterDetailForm.cs:40`); начальные значения отбора
  передаются словарём (`ShowInactive=1` у групп компаний, `filterAgencies` у
  журнала оплат). Четыре живых пункта: `miAgencyTax` (Agency 8 → AgencyTax 143),
  `miHeadOrganizations` (HeadCompany 1248 → Firm 16), `miPaymentCommon` и
  `miPaymentFRS` (PaymentCommon 145 → PaymentCommonAction 146).
  **Решение владельца 2026-09-21: своего движка в вебе не будет** — те же четыре
  пункта делаются деревом (слева дерево, справа список детей). Разбор, включая
  то, что сценария связи для этих пар в метаданных нет и заводить его там не
  нужно, — `web-migration.md`, этап 2. **Сделано 2026-09-21 для `miAgencyTax` и
  `miHeadOrganizations`** (сценарий из кода — `CodeScenario`); журналы оплат
  отложены решением владельца.
- **`miPaymentByManager`**: сначала модальный `FrmManagerSelector` (даты,
  агентство, галочки менеджеров), затем **по журналу PaymentCommonAction (146) на
  каждого выбранного менеджера** (`MDIForm.cs:645`–`:663`). Тот же 146 через
  `miPaymentByManagerFromRSection` открывается одним журналом с `ManagerFilter`.
- **`miFirmBalance`** — `FrmFirmIssuesBalance` (наследник `FrmFirmBalance`):
  своя форма: выбор фирмы и агентств, остаток на начало интервала, таблицы акций
  и платежей (`RefreshActionInfo`, `RefreshPaymentInfo`) с итогами.
- **`miRollerStatistic`, `miRollerStatisticWithFilter`** — одна форма
  `RollerStatisticForm` (`:20`): свой отбор (фирма, группа компаний, вид
  рекламы, менеджер, группа станций), три таблицы, воспроизведение роликов,
  Excel, разбивка по менеджерам и дням.
- **`miTariffWindow`** — `TariffWindowGenerationForm`: дерево (сценарий
  `TariffWindows`) плюс собственная таблица окон, переход к дате, Excel.
- **`miTrafficManagement`** — `TrafficManagementForm` на `TrafficGrid`.
- **`miPriceCalculator`** — `PriceCalculatorForm`, 1002 строки, свои процедуры
  расчёта (`pc_PackageDiscountCalculateModel`).
- **`miPrintGrid`, `miPrintGridFromRSection`** — `FrmGridReport`, просмотрщик
  Crystal Reports. **`miExportGrid`** — `ExportGridForm`: выгрузка сеток на диск
  (Crystal/Word/txt). **`miMultiActionMediaPlan`** — диалог `FrmMultiActionMediaPlan`
  (номера акций) → `MediaPlan.CreateInstance(...).Show(true)`, то есть Excel.
- **`VolumeOfRealizationByManager`** — `GraphForm` (диаграммы столбцами и
  круговая) на тех же данных, что `miStats.VolumeOfRealization` (158), с
  `managerID = текущий пользователь` и правилом `ManagerFilter`
  (`MDIForm.cs:823`–`:836`).
- **Три «удаления»** (`deleteDummyRollers`, `deleteDeletedActions`,
  `deleteUnconfirmedActions`): вопрос «Да/Нет» и вызов процедуры
  (`MDIForm.cs:270`–`:307`). Процедуры удаляют данные насовсем
  (`DeleteUnusedDummyRollers.sql`, `DeleteDeletedActions.sql`,
  `DeleteUnconfirmedActions.sql`; последняя, по тексту вопроса в обработчике,
  переносит макеты в журнал удалённых). Вызываются `DataAccessor.ExecuteNonQuery`
  напрямую, не через `DoAction`: проверки права на действие в самом обработчике
  нет.
- **`miFirmImport`**: `OpenFileDialog` и разбор Excel через COM Interop
  (`FirmImporter.cs:33`–`:60`); в плане этапа 4 (`web-migration.md`, п.1).
- **Мастера**: `miCreateUnconfirmedAction` — `Firm.SelectFirm` →
  `ActionOnMassmedia.ShowPassport`; `miMasterCreateActions` — `SelectFirm` →
  `SelectMassmediasStep` → `EditIssuesForm` → `ActionForm`;
  `miComboModulePlacement` — `SelectFirm` → `SelectComboModuleStep` →
  `ComboModulePlacementForm` → `ActionForm` (`MDIForm.cs:526`–`:597`).

## 5. Что подключается дёшево

Порядок — по отношению пользы к работе. Ничего из этого не запускалось.

1. **`miStats.Balance`** — *сделано 2026-09-18.* Маршрут в `MenuRoutes.SimpleJournal` плюс подмена
   сущности 160 → 185 по значению `IsGroupByAgency` (`StatBalanceJournalForm.cs:14`).
   Единственное место, где веб-журнал перестаёт быть «одна сущность на экран»:
   в `JournalRoute` (`MenuRoutes.cs:126`) нужно поле «сущность при включённой
   группировке», в `Journal.razor:208` — её выбор. Пункт сегодня разрешён в меню,
   но открывает «Пока не перенесено» (`web-migration.md`, таблица `miStats.*`).
   Заголовок в десктопе — «Статистика :: » + текст пункта, в вебе по умолчанию
   имя сущности (расхождение уже известно).
2. **`miAnnouncements`** — *сделано 2026-09-21.* Маршрут на сущность 179, действие
   строки `MarkAsRead` через `ObjectActions.ClassActions` и кнопка «Пометить все
   как прочтенное» — свойство маршрута (`JournalRoute.BulkAction`). «Добавить» и
   удаление ничем не гасились — метаданные сами.
3. **`miAdvertSubject`** — *сделано 2026-09-18, с переключателем (не «потеряется»).* Запись в `MenuRoutes.Browser` со сценарием
   «Предметы рекламы» (`RelationScenarios.AdvertTypes`) и корнем «Предметы
   рекламы». Потеряется только переключатель «с группировкой / без»
   (`AdvertTypeContainer.cs:13`–`:20`).
4. **`miExit`** — *сделано 2026-09-18.* В вебе пункт `miExit` ведёт на заглушку
   (`MenuNodeView.razor:60`), хотя кнопка «Выйти» уже есть
   (`MainLayout.razor:28`). Достаточно вызвать тот же `Logout`.

**Технически дёшево, но не делать без решения владельца:** три пункта
«Удаление…». Каждый — вопрос плюс `ExecuteNonQuery` (по 10–15 строк), но
процедуры удаляют данные необратимо, а права на них в вебе проверять нечем:
`MenuAccess` знает только журналы и деревья (`MenuAccess.cs:71`–`:97`,
`:130`–`:137`), проверку по `codeName` пришлось бы добавить. В `ArtvisDev` доступ
у них только личный, по 2 пользователя на пункт (§7).

**Из «журналов с особым поведением» этапа 3** (`web-migration.md`, этап 3, п.4:
`ActJournalForm`, `AudioJournalForm`, `AnnouncementJournalForm`,
`StatBalanceJournalForm`) настоящий этап 3 — два (`ActJournalForm`,
`AudioJournalForm`); `StatBalanceJournalForm` и список
`AnnouncementJournalForm` — малые правки.

## 6. Расхождения `iMenu` ↔ код ↔ веб

### `iMenu` → код (пункт есть, ветки нет)

Один: **`miAccounting`** — папка с `codeName`, ветки нет. Клик по ней в
десктопе ничего не делает (обработчик навешен, `codeName` не совпадает ни с
одним литералом), выпадает её подменю. Безвредно. Все 70 листьев ветку имеют;
15 пунктов `miStats.*` — через префикс.

### Код → `iMenu` (ветка есть, пункта нет — мёртвый код на `ArtvisDev`)

| `codeName` | Ветка | Что делала | В вебе |
|---|---|---|---|
| `miBrand` | `:177` → `ShowBrands:599` | `MasterDetail` Brand (15) → BrandFirm (120) | маршрута нет. **Удалена 2026-09-19** вместе со всеми брэндами (ветка `cleanup/brand`, скрипт `ArtvisDB/Scripts/brand-cleanup-deploy.sql`) |
| `miConfirmationHistory` | `:194` → `:383` | простой журнал ConfirmationHistory (129) | маршрут был (`MenuRoutes.cs:53`). **Удалена 2026-09-20** вместе с журналом, таблицами `ConfirmationHistory` и `iConfirmationType`, сущностью 129 и записью в историю из `IssueIUD`/`ModuleIssueIUD`/`PackModuleIssueID` (ветка `cleanup/confirmation-history`). Режим «грантор» (кнопка, `@grantorID`) не тронут — без журнала |
| `miDisabledWindows` | `:145` → `:449` | дерево `FakeContainer`, сценарий `DisabledWindows` | маршрут был (`MenuRoutes.cs:106`). **Удалена 2026-09-19** с веб-маршрутом, сущностью 10 «Время профилактики» и действием `AddDisabledWindow` (ветка `cleanup/dead-menu-branches`); таблица `DisabledWindow` и проверки в SQL остались |
| `miPayment` | `:181` | псевдоним `miPaymentCommon`/`miPaymentFRS` в общей ветке | нет. **Литерал удалён 2026-09-19** |
| `miPrintInquire` | `:139` → `:378` | `MassmediasAndCampaignsContainer` | нет. **Удалена 2026-09-19** вместе с контейнером, сценарием 18 и сущностью 188 |
| `miUpdateBanksList` | `:220` → `Bank.UpdateBankList` | скачивает `bnk.exe` по `http://cbrates.rbc.ru/bnk/bnk.exe`, запускает его локально и читает файлы (`Bank.WinForms.cs:20`–`:60`) | нет |

Пять веток целиком (`miBrand`, `miConfirmationHistory`, `miDisabledWindows`,
`miPrintInquire`, `miUpdateBanksList`) и один псевдоним внутри живой ветки.
Побочно: приватный метод `ShowAbout` (`MDIForm.cs:320`) не вызывается нигде,
пункта «О программе» в `iMenu` нет.

Наблюдение вне задачи по `miUpdateBanksList`: код скачивает и исполняет чужой
файл по незащищённому http. Переносить в веб в таком виде нельзя; пункт мёртвый,
ветка **удалена 2026-09-19** вместе с `Bank.UpdateBankList` и процедурой `bankListUpdate`: справочник банков ведётся руками (решение владельца).

### Пункты меню, создаваемые кодом

Только «Окно» (`MenuManager.cs:110`–`:120`): «Сверху вниз», «Слева направо»,
«Каскадом», «Упорядочить значки» и список MDI-окон. В вебе не нужно (нет MDI).
«Справки» и «О программе» нет. Горячие клавиши заданы в `iMenu.hotKey` у трёх
пунктов: `miExit` — Alt+X, `miRoller` — Ctrl+Alt+R, `miAnnouncements` —
Ctrl+Shift+A. В вебе их нет и не будет: решение владельца 2026-09-18 (§8, п.10).

### Веб ↔ `iMenu`

- Маршрутов без пункта не осталось: `miDisabledWindows` удалён 2026-09-19, `miConfirmationHistory` — 2026-09-20.
  Безвредны, но если набрать адрес `/journal/129` руками, `MenuAccess`
  вернёт «Доступ закрыт» (маршрут есть, разрешённого пункта нет,
  `MenuAccess.cs:78`–`:80`) — диагностика неверна: к правам это не относится.
- `miExit` — был заглушкой при работающей кнопке «Выйти»; с 2026-09-18 выходит из сеанса (§8, п.9).
- Докстрока `MenuRoutes.cs:9`–`:33` и две фразы в `web-migration.md` (строки 898,
  1670) держат устаревшие «70/22» (§1).
- Заглушка `NotYetPorted.razor` для любого неперенесённого пункта пишет «этап 2
  или этап 3»; пункты этапа 4 (отчёты, выгрузки, импорт) она называет неточно.

## 7. Права

`UserMenuItems` считает `enabled` так: пункт публичный, **или** пользователь —
администратор, **или** есть личная выдача `UserAdditionMenu.isGrant = 1`, **или**
личной записи нет и пункт выдан хотя бы одной группе пользователя
(`GroupMenu` × `GroupMember`). Личный запрет (`isGrant = 0`) перебивает группы.
Формулу проверили на трёх пользователях: число разрешённых пунктов с `codeName`,
посчитанное отдельным запросом, совпало с выдачей самой процедуры (20 = 20 у
`userID` 4, 6, 16).

На `ArtvisDev`: 9 групп; 41 пользователь, из них 40 активных и 5 администраторов
(все активны); 118 строк в `UserAdditionMenu`. Ни один пункт с `codeName` не
публичный (`isPublic = 1` только у 10 разделителей).

Столбцы таблицы §9:

- **Гр.** — в скольких из 9 групп пункт выдан (`GroupMenu`);
- **Лич.** — личные выдачи и запреты `+N/−M` (`UserAdditionMenu`);
- **Польз.** — сколько активных **не-администраторов** из 35 имеют итоговый
  доступ (администраторам доступно всё).

**Не выдан никому** (нет ни групп, ни личных выдач, не публичный — открывается
только администраторам): `miFirmImport`, `miManagerDiscountHistory`,
`miManagerDiscountReason`, `miComboModules`, `miComboModulePlacement`,
`miMultiActionMediaPlan`. Это признак «кандидат в мёртвые» **или** «ещё не
раздали»; отличить по базе нельзя. Для трёх из шести (`miManagerDiscountHistory`,
`miManagerDiscountReason`, `miComboModules`) в вебе маршрут уже есть.

**Только личная выдача, групп нет:** `deleteDummyRollers`,
`deleteDeletedActions`, `deleteUnconfirmedActions` — по 2 пользователя.

Сравнение с продом не делалось — по выдачам он мог разойтись.

## 8. Вопросы владельцу продукта

1. **Мёртвые ветки** (§6) — *2026-09-19: dev = прод 1–2-недельной давности,
   на проде пунктов нет тоже. Удалены `miBrand`, `miDisabledWindows`, `miPayment`,
   `miPrintInquire`, `miUpdateBanksList`; 2026-09-20 — и `miConfirmationHistory`.
   Вопрос закрыт.* Исходный вопрос: `miBrand`, `miConfirmationHistory`,
   `miDisabledWindows`, `miPayment`, `miPrintInquire`, `miUpdateBanksList` — на
   проде их пунктов нет тоже (проверить `iMenu` на `Artvis`)? Удалять ли ветки в
   десктопе и мёртвые маршруты в вебе (`miConfirmationHistory`,
   `miDisabledWindows`) или вернуть пункты в меню?
2. ~~**`miUpdateBanksList`**~~ — **решено 2026-09-19:** справочник банков
   ведётся руками, ветка удалена (§6).
3. ~~**`miPaymentByManager`**~~ — **решено 2026-09-21:** в лоб один в один не
   переносим, экран требует своего интерфейсного решения. Отложен, разбор и
   варианты — §10.
4. ~~**`VolumeOfRealizationByManager`**~~ — **решено 2026-09-21:** пока таблицей
   (журнал 158 с менеджером по умолчанию и правилом `ManagerFilter`), диаграммы
   потом.
5. **Автопоказ сообщений** отключён `return;` с 2026-03-17 (`MDIForm.cs:842`).
   Намеренно ли? Переносить ли опрос в веб, и если да — как (интервал 5 минут в
   десктопе)?
6. ~~**Три «удаления»**~~ — **решено 2026-09-21:** пока не делаем вообще. §10.
7. **Шесть пунктов без выдач** (§7): мёртвые или ещё не розданы? На проде?
8. ~~**Дубли пунктов**~~ — **решено 2026-09-18:** остаются раздельными, права
   выдаются на пункт. Ничего не менять.
9. ~~**`miExit`** в вебе~~ — **решено 2026-09-18:** пункт выходит из сеанса, как
   кнопка «Выйти» (`MainLayout.Logout` → `SecurityManager.Clear()`). Сделано.
10. ~~**Горячие клавиши**~~ — **решено 2026-09-18:** не переносить.
11. ~~**Заголовок журнала**~~ — **решено 2026-09-18:** как в десктопе — свой
    `Caption` маршрута, иначе текст пункта меню, иначе имя сущности. Сделано.
    Оговорка: десктопный `miStats.Balance` добавляет к тексту префикс
    «Статистика :: » — в вебе его нет, заголовок — просто текст пункта.
12. ~~**Пользователи и права**~~ — **решено 2026-09-21:** в qd2 такого экрана
    нет ни в одном из 71 пункта (и мёртвой ветки тоже нет) — пользователи,
    группы, пароли и персональные скидки ведутся отдельным приложением
    `Protector`. Мигрировать его в веб пока не требуется, вопрос закрыт до
    появления причины.

## 9. Полная таблица

Строки идут в порядке меню. «Обработчик» — номер строки ветки в `MDIForm.cs`
и метод с номером строки его определения. `Гр.`, `Лич.`, `Польз.` — §7.

| menuID | Путь в меню | `codeName` | Обработчик | Категория | Сущность / форма | Веб | Гр. | Лич. | Польз. |
|---:|---|---|---|---|---|---|---:|---|---:|
| 68 | Рекламный отдел → Внести макет рекламной акции | `miCreateUnconfirmedAction` | `:159` → CreateMassmediaAction:526 | Мастер: диалоги → карточка акции | `Firm.SelectFirm` → `ActionOnMassmedia.ShowPassport` | этап 3 (акция) | 6 | +3/−0 | 24 |
| 153 | Рекламный отдел → Веерное размещение... | `miMasterCreateActions` | `:161` → MasterCreateAction:543 | Мастер: диалоги → карточка акции | `Firm.SelectFirm` → `SelectMassmediasStep` → `EditIssuesForm` → `ActionForm` | этап 3 (акция, веер) | 5 | +3/−0 | 24 |
| 180 | Рекламный отдел → Размещение комбо-модулями... | `miComboModulePlacement` | `:163` → MasterPlaceComboModules:574 | Мастер: диалоги → карточка акции | `Firm.SelectFirm` → `SelectComboModuleStep` → `ComboModulePlacementForm` → `ActionForm` | этап 3 (акция, комбо) | 0 | — | 0 |
| 175 | Рекламный отдел → Калькулятор цены | `miPriceCalculator` | `:251` → ShowPriceCalculator:622 | Собственная форма | `PriceCalculatorForm` (1002 строки) | этап 3 (п.2 плана) | 5 | +26/−0 | 25 |
| 11 | Рекламный отдел → Журнал подтверждённых рекламных акций | `miActionJournal` | `:165` → ShowMassmediaActions:395 | Дерево на своём контейнере | `ActionContainer(ConfirmedAction)`: 118 / 77 / 1255 | перенесён (Browser, 2026-09-20); действия по строке — этапы 3-4 | 6 | +3/−0 | 24 |
| 158 | Рекламный отдел → Журнал макетов рекламных акций | `miActionJournalUnconfirmed` | `:169` → ShowMassmediaActions:395 | Дерево на своём контейнере | `ActionContainer(UnconfirmedAction)`: 137 / 77 / 1256 | перенесён (Browser, 2026-09-20); действия по строке — этапы 3-4 | 5 | +8/−0 | 24 |
| 157 | Рекламный отдел → Журнал удалённых рекламных акций | `miActionJournalDeleted` | `:172` → ShowMassmediaActions:395 | Дерево на своём контейнере | `ActionContainer(DeletedAction)`: 1229 / 1236 / 1257 | перенесён (Browser, 2026-09-20); действия по строке — этапы 3-4 | 5 | +7/−0 | 23 |
| 145 | Рекламный отдел → Объем реализации (Сводный) | `VolumeOfRealizationByManager` | `:232` → ShowGraphVolumeOfRealizationByPerson:823 | График | `GraphForm` на данных StatsVolumeofRealization (158), `managerID = LoggedUser` | перенесён (SimpleJournal) 2026-09-21: журнал 158 с менеджером по умолчанию; диаграммы отложены | 5 | +3/−0 | 24 |
| 112 | Рекламный отдел → Журнал оплат | `miPaymentFRS` | `:181` → ShowPaymentCommon(true):628 | MasterDetail | PaymentCommon (145) → PaymentCommonAction (146), `filterAgencies=true` | решение отложено (2026-09-21) | 6 | +3/−0 | 24 |
| 113 | Рекламный отдел → Журнал оплат по менеджерам | `miPaymentByManagerFromRSection` | `:192` → ShowCommonOrderByManagerFromRSection:665 | Простой журнал + ManagerFilter | PaymentCommonAction (146) | перенесён (SimpleJournal) | 6 | +3/−0 | 24 |
| 114 | Рекламный отдел → Баланс для всех фирм-заказчиков | `miBalanceFromRSection` | `:186` → ShowBalance:438 | Простой журнал + ManagerFilter | BalanceIssues (184) | перенесён (SimpleJournal) | 6 | +3/−0 | 24 |
| 62 | Рекламный отдел → Сетка вещания | `miPrintGridFromRSection` | `:196` → ShowPrintGridForm:670 | Отчёт / выгрузка | `FrmGridReport` (Crystal-просмотрщик) | этап 4 | 6 | +4/−0 | 24 |
| 71 | Рекламный отдел → Журнал использования роликов | `miRollerStatisticWithFilter` | `:204` → ShowRollerStatistic(true):702 | Собственная форма | `RollerStatisticForm` (менеджер по умолчанию — текущий) | этап 3 | 6 | +3/−0 | 24 |
| 178 | Рекламный отдел → Журнал использования бонусов | `miBonusesStat` | `:257` → inline:258 | Простой журнал | StatBonuses (1269) | перенесён (SimpleJournal) | 4 | — | 17 |
| 16 | Рекламный отдел → Фирмы-заказчики | `miFirm` | `:179` → ShowFirms:605 | Простой журнал | Firm (16) | перенесён (SimpleJournal) | 8 | +3/−0 | 30 |
| 169 | Рекламный отдел → Группа компаний | `miHeadOrganizations` | `:249` → ShowHeadCompanies:610 | MasterDetail | HeadCompany (1248) → Firm (16), `ShowInactive=1` | перенесён деревом (2026-09-21) | 7 | — | 28 |
| 39 | Рекламный отдел → Предмет рекламы | `miAdvertSubject` | `:155` → ShowAdvertSubjects:509 | Дерево на своём контейнере | `AdvertTypeContainer` (сценарий «Предметы рекламы»: 17 / 1243) | перенесён (Browser), переключатели в меню корня; проверено вживую 2026-09-18 | 7 | +7/−0 | 26 |
| 14 | Рекламный отдел → Банки | `miBank` | `:175` → ShowBanks:400 | Простой журнал | Bank (2) | перенесён (SimpleJournal) | 8 | +3/−0 | 30 |
| 107 | Рекламный отдел → Сообщения | `miAnnouncements` | `:222` → ShowAnnouncements:410 | Журнал-наследник со своей логикой | `AnnouncementJournalForm` (Announcement, 179) | перенесён (SimpleJournal + кнопка «Пометить все»); проверено вживую 2026-09-21 | 7 | +3/−0 | 27 |
| 22 | Рекламный отдел → Выход | `miExit` | `:137` → ApplicationExit:985 | Выход из приложения | `Application.Exit()` | сделан: выход из сеанса (`MenuNodeView`); проверено вживую 2026-09-18 | 7 | +3/−0 | 27 |
| 23 | Режиссёр → Журнал рекламных роликов | `miRoller` | `:157` → ShowRollers:520 | Журнал-наследник со своей логикой | `AudioJournalForm` (Roller, 20) | этап 3 + этап 4 (аудио) | 8 | +2/−1 | 27 |
| 70 | Трафик → Журнал использования роликов | `miRollerStatistic` | `:202` → ShowRollerStatistic(false):702 | Собственная форма | `RollerStatisticForm` (свой отбор, 3 таблицы, аудио, Excel) | этап 3 | 2 | +3/−0 | 8 |
| 72 | Трафик → Трафик-менеджмент | `miTrafficManagement` | `:206` → ShowTrafficManagement:713 | Собственная форма | `TrafficManagementForm` (`TrafficGrid`) | этап 3 | 2 | — | 5 |
| 64 | Трафик → Сетка вещания | `miPrintGrid` | `:196` → ShowPrintGridForm:670 | Отчёт / выгрузка | `FrmGridReport` (Crystal-просмотрщик) | этап 4 | 2 | — | 5 |
| 152 | Трафик → Экспорт сеток вещания | `miExportGrid` | `:234` → ExportGrid:309 | Отчёт / выгрузка | `ExportGridForm` (файлы на диск) | этап 4 | 1 | — | 5 |
| 73 | Трафик → Журнал переносов | `miTransferJournal` | `:210` → ShowTransferJournal:708 | Простой журнал | TransferLog (141) | перенесён (SimpleJournal) | 2 | — | 5 |
| 119 | Трафик → Журнал подтверждённых рекламных акций | `miActionJournalTraffic` | `:165` → ShowMassmediaActions:395 | Дерево на своём контейнере | `ActionContainer(ConfirmedAction)`: 118 / 77 / 1255 | перенесён (Browser, 2026-09-20); действия по строке — этапы 3-4 | 2 | — | 5 |
| 181 | Трафик → График размещения по нескольким акциям | `miMultiActionMediaPlan` | `:208` → ShowMultiActionMediaPlan:723 | Отчёт / выгрузка | `FrmMultiActionMediaPlan` → `MediaPlan.CreateInstance(...).Show(true)` (Excel) | этап 4 | 0 | — | 0 |
| 88 | Статистика → Объем реализации → Сводный (размещение рекламы) | `miStats.VolumeOfRealization` | `:751` → ShowStatsJournal:745 | Простой журнал + ManagerFilter | StatsVolumeofRealization (158) | перенесён (SimpleJournal) | 3 | — | 8 |
| 127 | Статистика → Объем реализации → За период (с разбивкой по месяцам) | `miStats.VolumeRealizationByMonth` | `:782` → ShowStatsJournal:745 | Простой журнал + ManagerFilter | StatVolumeOfRealiztionByMonth (201) | перенесён (SimpleJournal) | 3 | — | 8 |
| 154 | Статистика → Объем реализации → Факторный анализ продаж | `miStats.FactorAnalysis` | `:808` → ShowStatsJournal:745 | Простой журнал + ManagerFilter | StatsFactorAnalysis (225) | перенесён (SimpleJournal) | 1 | — | 5 |
| 129 | Статистика → Объем реализации → Выручка от размещения рекламных модулей | `miStats.ModuleFinancy` | `:789` → ShowStatsJournal:745 | Простой журнал + ManagerFilter | StatModuleFinancy (203) | перенесён (SimpleJournal) | 2 | — | 5 |
| 131 | Статистика → Объем реализации → Выручка от размещения пакетных рекламных модулей | `miStats.PackModuleFinancy` | `:796` → ShowStatsJournal:745 | Простой журнал + ManagerFilter | StatPackModuleFinancy (205) | перенесён (SimpleJournal) | 2 | — | 5 |
| 95 | Статистика → % заполнения | `miStats.FillPercentage` | `:771` → ShowStatsJournal:745 | Простой журнал | StatsFillPercentage (163), без ManagerFilter | перенесён (SimpleJournal) | 2 | — | 5 |
| 97 | Статистика → Фактическое размещение → Cпонсорских программ | `miStats.SponsorBusiness` | `:778` → ShowStatsJournal:745 | Простой журнал + ManagerFilter | StatsSponsorBusiness (170) | перенесён (SimpleJournal) | 3 | +1/−0 | 8 |
| 128 | Статистика → Фактическое размещение → Рекламных модулей | `miStats.ModuleLoading` | `:785` → ShowStatsJournal:745 | Простой журнал + ManagerFilter | StatModuleLoading (202) | перенесён (SimpleJournal) | 3 | +1/−0 | 8 |
| 130 | Статистика → Фактическое размещение → Пакетных рекламных модулей | `miStats.PackModuleLoading` | `:792` → ShowStatsJournal:745 | Простой журнал + ManagerFilter | StatPackModuleLoading (204) | перенесён (SimpleJournal) | 3 | +1/−0 | 8 |
| 147 | Статистика → Аналитические показатели → Средняя скидка по радиостанциям | `miStats.AvgDiscount` | `:799` → ShowStatsJournal:745 | Простой журнал + ManagerFilter | StatAvgDiscount (218) | перенесён (SimpleJournal) | 1 | — | 5 |
| 149 | Статистика → Аналитические показатели → Объем продаж в секундах | `miStats.VolumeOfRealizationSec` | `:802` → ShowStatsJournal:745 | Простой журнал + ManagerFilter | StatVolumeOfRealizationSec (219) | перенесён (SimpleJournal) | 1 | — | 5 |
| 46 | Бухгалтерия | `miAccounting` | — (ветки нет) | Папка с codeName, без обработчика | папка «Бухгалтерия» | — (папка рисуется как группа) | 3 | +1/−0 | 9 |
| 48 | Бухгалтерия → Выписать акт выполненных работ | `miActPrint` | `:216` → ShowActJournal:349 | Журнал-наследник со своей логикой | `ActJournalForm` (ActJournalRow, 156) | этап 3 + этап 4 (печать) | 3 | — | 8 |
| 77 | Бухгалтерия → Журнал оплат | `miPaymentCommon` | `:181` → ShowPaymentCommon(false):628 | MasterDetail | PaymentCommon (145) → PaymentCommonAction (146), `filterAgencies=false` | решение отложено (2026-09-21) | 3 | +1/−0 | 9 |
| 84 | Бухгалтерия → Журнал оплат по менеджерам | `miPaymentByManager` | `:190` → ShowCommonOrderByManager:645 | Диалог → журнал(ы) | `FrmManagerSelector` → по журналу PaymentCommonAction (146) на каждого выбранного менеджера | отложено 2026-09-21 (§10) | 3 | — | 8 |
| 85 | Бухгалтерия → Баланс для конкретной фирмы-заказчика | `miFirmBalance` | `:188` → ShowFirmBalance:638 | Собственная форма | `FrmFirmIssuesBalance` (← `FrmFirmBalance`) | этап 3 (п.3 плана) | 3 | — | 8 |
| 86 | Бухгалтерия → Баланс для всех фирм-заказчиков | `miBalance` | `:184` → ShowBalance:438 | Простой журнал + ManagerFilter | BalanceIssues (184) | перенесён (SimpleJournal) | 3 | — | 8 |
| 122 | Бухгалтерия → Журнал подтверждённых рекламных акций | `miActionJournalBuh` | `:165` → ShowMassmediaActions:395 | Дерево на своём контейнере | `ActionContainer(ConfirmedAction)`: 118 / 77 / 1255 | перенесён (Browser, 2026-09-20); действия по строке — этапы 3-4 | 2 | — | 8 |
| 92 | Бухгалтерия → Специальные отчёты → Cальдо расчётов по всем фирмам-заказчикам в разрезе агентств | `miStats.Balance` | `:765` → ShowStatBalance:816 | Журнал-наследник со своей логикой | `StatBalanceJournalForm`: StatsBalance (160) / StatsBalanceGroup (185) | перенесён (SimpleJournal + подмена сущности); проверено вживую 2026-09-18 | 1 | — | 5 |
| 101 | Бухгалтерия → Специальные отчёты → Развёрнутое итоговое сальдо расчетов в разрезе агентств | `miStats.BalanceAgency` | `:768` → ShowStatsJournal:745 | Простой журнал + ManagerFilter | StatsBalanceAgency (161) | перенесён (SimpleJournal) | 1 | — | 5 |
| 103 | Бухгалтерия → Специальные отчёты → Сводный журнал долгов фирм-заказчиков (по менеджерам в разрезе агентств) | `miStats.BalanceManager` | `:775` → ShowStatsJournal:745 | Простой журнал + ManagerFilter | StatsBalanceManager (168) | перенесён (SimpleJournal) | 1 | — | 5 |
| 150 | Бухгалтерия → Специальные отчёты → Отчет по типам оплат | `miStats.VolumeByPaymentType` | `:805` → ShowStatsJournal:745 | Простой журнал + ManagerFilter | StatVolumeByPaymentType (222) | перенесён (SimpleJournal) | 1 | — | 5 |
| 24 | Администрация → Радиостанции | `miMassMedia` | `:143` → ShowMassMedia:443 | Журнал-наследник, по сути простой | `MassmediasJournal` → MassMedia (9) | перенесён (SimpleJournal) | 2 | — | 5 |
| 126 | Администрация → Группы Радиостанций | `miGroupMassmedia` | `:228` → ShowMassmediaGroupJournal:344 | Простой журнал | MassmediaGroup (195) | перенесён (SimpleJournal) | 2 | — | 5 |
| 36 | Администрация → Рекламные тарифы | `miTariff` | `:149` → ShowTariff:473 | Дерево на FakeContainer | сценарий Tariff | перенесён (Browser) | 2 | — | 5 |
| 38 | Администрация → Рекламные модули | `miModules` | `:153` → ShowModules:497 | Дерево на FakeContainer | сценарий Module | перенесён (Browser) | 2 | — | 5 |
| 67 | Администрация → Пакетные модули | `miPackModules` | `:198` → ShowPackModules:676 | Дерево на FakeContainer | сценарий PackModules | перенесён (Browser) | 2 | — | 5 |
| 179 | Администрация → Комбо-модули | `miComboModules` | `:200` → ShowComboModules:689 | Дерево на FakeContainer | сценарий Combo Modules | перенесён (Browser) | 0 | — | 0 |
| 35 | Администрация → Тарифы для спонсоров | `miSponsorTariff` | `:147` → ShowSponsorTariff:461 | Дерево на FakeContainer | сценарий Sponsor programm | перенесён (Browser) | 2 | — | 5 |
| 37 | Администрация → Скидки | `miDiscount` | `:151` → ShowDiscounts:485 | Дерево на FakeContainer | сценарий Discount | перенесён (Browser) | 2 | — | 5 |
| 118 | Администрация → Пакетные скидки | `miPackageDiscounts` | `:224` → ShowPackageDicounts:326 | Дерево на FakeContainer | сценарий PackageDiscount | перенесён (Browser) | 2 | — | 5 |
| 76 | Администрация → Генерация рекламных окон | `miTariffWindow` | `:214` → ShowTariffWindowJournal:356 | Собственная форма | `TariffWindowGenerationForm` (дерево TariffWindows + `windowGrid`) | этап 3 | 2 | — | 5 |
| 155 | Администрация → Текст для отчётов | `miReportPartText` | `:236` → ShowReportPartText:405 | Простой журнал | ReportPartText (1227) | перенесён (SimpleJournal) | 1 | — | 5 |
| 75 | Администрация → Агентства и налоги | `miAgencyTax` | `:212` → ShowAgencyTaxJournal:737 | MasterDetail | Agency (8) → AgencyTax (143) | перенесён деревом (2026-09-21) | 2 | — | 5 |
| 138 | Администрация → Ввод остатков | `miSpecialActions` | `:230` → ShowSpecialAction:315 | Простой журнал + ManagerFilter | SpecialAction (207) | перенесён (SimpleJournal) | 2 | +2/−0 | 7 |
| 26 | Администрация → Тип оплаты | `miPaymentType` | `:141` → ShowPaymentTypes:389 | Простой журнал | PaymentType (5) | перенесён (SimpleJournal) | 2 | — | 5 |
| 176 | Администрация → Менеджерские скидки | `miManagerDiscountHistory` | `:253` → inline:254 | Простой журнал | ManagerDiscountHistory (1266) | перенесён (SimpleJournal) | 0 | — | 0 |
| 177 | Администрация → Причины выдачи менеджерской скидки | `miManagerDiscountReason` | `:255` → inline:256 | Простой журнал | ManagerDiscountReason (1267) | перенесён (SimpleJournal) | 0 | — | 0 |
| 120 | Администрация → Удаленные рекламные выпуски | `miLog` | `:226` → inline:227 | Простой журнал | LogDeletedIssue (193) | перенесён (SimpleJournal) | 2 | — | 5 |
| 163 | Администрация → Удаление роликов-пустышек | `deleteDummyRollers` | `:238` → DeleteDummyRollers:270 | Действие без экрана | вопрос + `DeleteUnusedDummyRollers` | отложено 2026-09-21 (§10) | 0 | +2/−0 | 2 |
| 164 | Администрация → Очистка журнала удаленных рекламных акций | `deleteDeletedActions` | `:240` → DeleteDeletedActions:283 | Действие без экрана | вопрос + `DeleteDeletedActions` | отложено 2026-09-21 (§10) | 0 | +2/−0 | 2 |
| 165 | Администрация → Удаление макетов рекламных акций | `deleteUnconfirmedActions` | `:242` → DeleteUnconfirmedActions:296 | Действие без экрана | вопрос + `DeleteUnconfirmedActions` | отложено 2026-09-21 (§10) | 0 | +2/−0 | 2 |
| 166 | Администрация → Импорт фирм | `miFirmImport` | `:244` → inline:246 → `FirmImporter.Import` | Импорт из файла | `OpenFileDialog` + Excel COM Interop | этап 4 (`FirmImporter`, п.1) | 0 | — | 0 |

## 10. Отложено

Не «решено не переносить» (такие вещи вычёркиваются и исчезают из плана), а
«вернёмся». У каждого пункта — дата решения и что именно мешает сделать его
сейчас.

### Три «удаления» — не делаем пока (2026-09-21)

`deleteDummyRollers`, `deleteDeletedActions`, `deleteUnconfirmedActions`.
Технически это по 10–15 строк: вопрос «Да/Нет» и один `ExecuteNonQuery`. Плюс
понадобилась бы проверка права на пункт-без-экрана (`MenuAccess` умеет журналы
и деревья, для `codeName` нужен свой метод — данные для него уже собираются).
Решение владельца продукта: **пока не делаем вообще**. Права на них и в десктопе
выданы лично двум пользователям на пункт, групповых нет ни одной.

Напоминание на будущее: `DeleteUnconfirmedActions` вопреки названию ничего не
удаляет — переносит макеты в журнал удалённых (таймаут 300 с, в транзакции).
Две другие удаляют необратимо.

### «Журнал оплат по менеджерам» (`miPaymentByManager`) — нужно своё решение (2026-09-21)

**Что это в десктопе.** Модальный `FrmManagerSelector`: интервал дат, агентство,
кнопка «Обновить информацию» и чек-лист менеджеров. По «Ок» открывается
**по журналу PaymentCommonAction (146) на каждого** отмеченного менеджера.

**Зачем он нужен — выяснено по процедуре, а не по догадке.** Список менеджеров
в диалоге даёт `PaymentCommonActionUsers`: он возвращает не всех, а **только тех,
у кого в этом интервале реально были оплаты** (с учётом агентства и прав
смотрящего). То есть диалог отвечает на вопрос «кому вообще приходили оплаты за
период», которого бухгалтер заранее не знает. Имя менеджера дальше живёт
в заголовке окна, а не в гриде.

**Почему «один в один» в вебе плохо:** одно окно — одна вкладка, и выбор пяти
менеджеров означал бы пять вкладок. Владелец продукта (2026-09-21): в лоб не
переносим, экран требует своего интерфейсного решения; пункт отложен.

**Что выяснено, чтобы не начинать с нуля.** Журнал 146 (`PaymentCommonActions`)
уже принимает `@managerID`, `@startOfInterval`, `@endOfInterval`, `@agencyID`.
Но менеджера он **не возвращает**: итоговый `SELECT` отдаёт `psoa.*`, `name`,
`firmName`, `agencyName`, `paymentTypeName`; менеджер — это `Action.userID`, и
его в выборке нет. Колонки «Менеджер» нет и в атрибутах сущности 146.

**Варианты, между которыми выбирать:**

1. **Одна таблица вместо диалога и пяти окон.** Добавить в `PaymentCommonActions`
   имя менеджера (join к `User`) и колонку в метаданные сущности 146 — тогда
   бухгалтер открывает один журнал за период, видит колонку «Менеджер»,
   сортирует по ней и выгружает в Excel; вопрос «кому приходили оплаты»
   отвечается самой таблицей. Минус: правка процедуры и метаданных в базе,
   общей с десктопом (десктопный журнал 146 тоже получит колонку — вероятно, к
   лучшему, но это решение).
2. **Дерево «менеджер → его оплаты»** на уже готовом движке: мастер — список
   менеджеров с оплатами за период (процедура `PaymentCommonActionUsers` уже
   есть), деталь — журнал 146 с `@managerID`. Ближе к десктопной логике и не
   трогает общую базу, но нужно проверить, что ключ родителя дойдёт до
   процедуры детали под нужным именем (`userID` против `@managerID`).
3. Оставить только соседний `miPaymentByManagerFromRSection` — тот же журнал 146
   с правилом менеджера, он уже перенесён, — и признать пункт избыточным.

### «Объём реализации по менеджерам» — отложены диаграммы (2026-09-21)

**Сам пункт перенесён 2026-09-21** — таблицей: журнал 158 с
`managerID = текущий пользователь` и правилом `ManagerFilter`, ровно те данные,
что показывает десктопный `GraphForm` (`MDIForm.cs:779`–`:792`). Отложены именно
диаграммы (столбцы и круг): «красоту наведём потом». Когда дойдёт — это новая
работа и библиотека графиков в вебе, которой сейчас нет.

### Автопоказ сообщений — вопрос владельцу открыт (§8, п.5)

В десктопе опрос отключён `return;` с 2026-03-17. Журнал «Сообщения» переносится
без него.

## Как воспроизвести

Меню и права (Windows-аутентификация, `sqlcmd -S ".\sqlexpress" -d ArtvisDev -E -f 65001`):

```sql
SELECT menuID, name, parentID, position, codeName, isObsolete FROM iMenu ORDER BY parentID, position;
EXEC UserMenuItems @userID = 4, @languageCode = 'ru';  -- итоговый enabled для пользователя
SELECT menuID, COUNT(DISTINCT groupID) FROM GroupMenu GROUP BY menuID;
SELECT menuID, isGrant, COUNT(*) FROM UserAdditionMenu GROUP BY menuID, isGrant;
```

Формула «Польз.» — условие из `UserMenuItems` (§7), отдельно по активным
не-администраторам.
