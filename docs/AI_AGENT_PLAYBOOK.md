# AI Agent Playbook for qd2

## How to approach a new task

1. Read `.github/copilot-instructions.md` first.
2. Find existing similar behavior before editing.
3. Identify all affected layers:
   - WinForms UI (`Client/Forms`, `Client/Controls`)
   - C# business/domain classes (`Client/Classes`)
   - data access (`FogSoft.WinForm/DataAccess`)
   - stored procedures/SQL objects (`ArtvisDB/dbo/*`)
   - logging (`Client/app.config`, DAL logging points)
4. Produce a short plan before code changes.
5. Keep changes minimal and targeted.
6. Validate build capability for environment and run relevant manual scenarios.
7. Document assumptions and risks explicitly.

## Proposing simplifications (required since 2026-09-18)

"Keep changes minimal" (step 5) governs what you *do* without asking. It does
**not** mean staying silent about bad design. The product owner changed the
rule: while reading desktop code — especially code about to be moved to the
web (`docs/tasks/web-migration.md`), but also along the way in any task — you
**must propose** fixes of defects and simplifications of the existing desktop
architecture whenever they would make the web migration easier.

- Propose, don't apply: what gets simpler, what is lost, cost, what it touches
  in the desktop. The product owner decides.
- Code already migrated to the web is out of scope.
- Until a decision is made, migrate as is; record decisions in
  `docs/tasks/web-migration.md`.

## Task templates

### Bug fix task template

- Symptom:
- Expected behavior:
- Repro steps:
- Affected screen/form:
- Logs/screenshots/data samples:
- Suspected files/procedures:
- Suspected root cause:
- Validation plan:
- Risky assumptions:

### New UI feature task template

- Form/control to change:
- User action (button/menu/grid event):
- Expected UI behavior:
- Validation/error messages:
- Database impact (if any):
- Grid behavior (selection, check-all, sorting, width/perf):
- Manual test scenarios:
- Risks/assumptions:

### Групповые действия в вебе: чекбоксы, а не выделение строк мышью

В десктопном гриде пачку выбирают Ctrl/Shift-кликом по строкам и жмут Delete. **В вебе
так не делаем** (решение владельца продукта 2026-09-22): Ctrl+клик и Shift+клик заняты
самим браузером, а клик по строке у нас открывает карточку — жест вышел бы
двусмысленным. Кроме того, списки виртуализированы: выделение, которого не видно на
экране, для необратимого действия недопустимо.

Конвенция:

- колонка чекбоксов в списке, «отметить все» в шапке — по **всему** текущему списку (с
  учётом отбора и сортировки), а не по отрисованным строкам;
- Shift-клик по чекбоксу отмечает диапазон — привычка из десктопа без конфликта с кликом
  по строке;
- само действие — кнопкой в тулбаре экрана с числом: «Удалить (7)», рядом «Снять
  выделение»; кнопки видны только при непустом выборе;
- один вопрос на всю пачку с числом объектов, затем тихое выполнение по каждому
  (`silenceFlag: true`) со сбором отказов;
- итоги: при успехе ничего не показывать (строки и так исчезли), при отказах — показ
  готовой таблицы (см. раздел выше);
- выделение хранится по ссылкам на строки данных: переживает сортировку, сбрасывается при
  приходе нового набора.

Образец — массовое удаление: `ObjectList.razor` (выбор), `ObjectActions.DeleteSelectedAsync`
(выполнение), кнопки в `ScreenToolbar` журнала и древовидного экрана. Включается флагом
`iEntity.isMassDeleteAllowed`.

### Именованные паспорта (`iPassport`): форма без формы

Кроме паспорта сущности (`iEntity.passport` — карточка объекта) в системе есть **второй,
независимый набор паспортов**: таблица `iPassport`, ключ `codeName`. Это описания форм,
не привязанные ни к какой сущности: набор полей для параметров операции. Загружает их
`PassportLoader.Load(codeName)` (кэш в памяти; процедура `PassportRetrieve` либо общая
выгрузка словарей).

**Зачем это знать.** Диалог «спросить параметры и выполнить» не нужно верстать руками:
поля объявляются в метаданных, а код получает готовый словарь значений. На `ArtvisDev`
таких паспортов **17**.

Два способа применения:

1. **`UniversalPassportForm`** (`Client/Forms/UniversalPassportForm.cs`) — три конструктора:
   - `(parameters, passportName, procedureName, caption, validate)` — собранные значения
     уходят прямо в хранимую процедуру (`ExecuteNonQuery`). Форма ради формы не пишется
     вообще: метаданные + имя процедуры;
   - `(parameters, passportName, applyChanges, caption, validate)` — вместо процедуры свой
     обработчик;
   - `(po, passportName, caption, entity, ds, validate, applyChanges)` — плюс объект-шаблон и
     набор данных для справочников (так сделаны «Добавить тариф массово» и «Изменить похожие
     тарифы»).
   Проверка ввода — делегат `ValidateDataDelegate`, возвращает `false` и форма не закрывается.

2. **Своя форма поверх именованного паспорта**: `PassportForm`-наследник передаёт
   `PassportLoader.Load("…")` в базовый конструктор и добавляет своё поведение. Так живут
   `CampaignDaysForm` (`CampaignDaysDelete`), `ChangePositioningForm`
   (`IssueChangePositioning`), `RollerSubstitutionForm` (`RollerSubstitute`),
   `FrmWindowTariffTemplate` (`TrafficTemplate`), `TariffWindowsDisabledStatusForm`,
   `ChartSettingsForm`, `FormGridHighlight` и другие.

**Что это значит для веба.** Компонент `Passport.razor` принимает XML параметром (`Xml`) и
набор данных (`Data`), то есть умеет нарисовать любой такой паспорт. Значит перенос подобного
диалога — это не новый экран, а обвязка: взять XML по имени, показать, проверить, вызвать
ядро. Делая такой диалог, делай его механизмом, а не под один случай: желающих много.

**Ловушки.**
- `PassportLoader` кэширует XML в статическом словаре. В отличие от `[WEB-01]` в
  `IMPROVEMENTS.md`, здесь это безопасно: в паспорте нет ничего персонального, права в него
  не вшиты.
- Имена — как записаны в базе, включая опечатки: паспорт подсветки журнала называется
  `JounalHighlight`, без «r». Искать по `iPassport`, а не по догадке.
- Новый именованный паспорт — это строка метаданных, то есть сид-скрипт и деплой на все базы
  заказчиков. Прежде чем заводить, проверь, нет ли подходящего среди семнадцати.

### Служебные таблицы в интерфейсе: виртуальная сущность, а не строка в `iEntity`

Любой грид системы — и десктопный `SmartGrid`, и веб-`ObjectList` — рисует колонки
по `Entity`. Когда надо показать **техническую** таблицу (итоги массовой операции,
список ошибок, разовый отчёт), соблазн велик завести под неё строку в `iEntity`.
Так исторически и появилась сущность 157 «Ошибки» (`ErrTmplGen`) с парой колонок
на все случаи жизни — из-за неё разные операции показывают итоги в одних и тех же
полях, хотя у ролика и у тарифа они разные.

**Правило: сущность для такой таблицы собирается в коде**, через
`EntityManager.CreateVirtualEntity(entityId, name, codeName, pkColumn, attributes)`:
она не пишется в базу, не попадает в кэш сущностей и живёт ровно столько, сколько
показ. Колонки объявляются под конкретную операцию. Идентификатор — отрицательный,
по нему сразу видно, что строки в `iEntity` за ним нет.

Где уже так сделано:

- `SmartGrid.ShowDeleteErrors` — ошибки массового удаления (сущность −5001);
- `ActionOnMassmedia.WinForms.cs` — итоги активации акции (три разные таблицы);
- веб: `TableDialog` (−5100) — общий показ готовой таблицы, веб-аналог
  `Globals.ShowSimpleJournal(entity, caption, DataTable)`; список при этом
  переводится в режим `ObjectList.ReadOnly` (строка результата — не доменный
  объект: ни карточки по клику, ни меню действий).

Показ готовой таблицы нужен часто: в десктопе таких мест одиннадцать (ошибки
клонирования, смены типа оплаты, удаления, позиционирования, импорта фирм,
добавления окон и другие). Появилась двенадцатая — переиспользуй механизм, а не
заводи очередную строку метаданных.

### Ожидание в вебе: обращения к базе — под `BusyService.RunAsync`

Ядро читает и пишет синхронно (`GetContent`, `Update`, `Delete` → `DataAccessor`), а
Blazor Server отдаёт браузеру отрисовку только когда обработчик события вернул
управление. Синхронный вызов базы прямо в обработчике — это экран, который до конца
запроса ничего не показывает и не отвечает. В десктопе здесь был бы курсор ожидания
(`Globals.SetWaitCursor`); в вебе его аналог — `BusyService` (решение владельца продукта
2026-09-22).

**Правило для нового веб-кода:**

- любое обращение к базе из обработчика экрана, диалога или действия оборачивается в
  `await Busy.RunAsync(() => ...)` (есть вариант с результатом: `RunAsync<T>`). Сервис
  сначала показывает слой ожидания и отпускает circuit, чтобы слой ушёл в браузер, и
  только потом выполняет работу;
- **показ диалога не оборачивается никогда.** Ожидание ответа пользователя — не
  «система занята»: под `RunAsync` — только подготовка до диалога и выполнение после;
- загрузка при открытии экрана — в `OnParametersSetAsync`/`OnInitializedAsync` через
  `RunAsync`, а не в синхронном `OnParametersSet`. Если на один переход приходит два
  прохода параметров (так у `Journal.razor`: `?menu=` приходит отдельно), отметку
  «загружено» ставить **до** чтения, иначе второй проход запустит тяжёлую процедуру
  повторно;
- вложенные вызовы допустимы (счётчик), но пачку последовательных обращений лучше
  оборачивать одним `RunAsync`, а не каждое отдельно.

Слой (`MainLayout.razor`) накрывает всё окно, включая меню и открытые диалоги, и сразу
блокирует мышь; курсор и полоса появляются через 300 мс, спиннер — через 1 с, поэтому
быстрые операции не мигают. Клавиатуру слой не блокирует.

Где уже так сделано: `Journal.razor`, `Browser.razor` (загрузка и перечитка),
`PassportDialog`/`NamedPassportDialog` (справочники и сохранение), `ObjectActions`
(удаление, клоны, пересчёт), `ObjectSelector.razor` (список выбора).

### Stored procedure change template

- Procedure name:
- Current input parameters:
- Current output parameters:
- Proposed contract change (if any):
- Affected tables/views/functions:
- C# caller methods/forms/classes:
- Metadata-driven callers (entity/action/module):
- Transaction/locking risks:
- Performance expectations:
- Validation SQL and UI scenarios:

### Report/statistics task template

- Report/stat name:
- Filters:
- Grouping/sorting:
- Expected columns and formulas:
- Source procedures/tables:
- C# caller path (form/class):
- Edge cases (empty periods, partial data, rights):
- Validation examples (input -> expected output):

### Performance investigation template

- Scenario and timing symptom:
- Log samples (`logs/qd2.log`):
- Procedure names and parameters:
- Date range/data volume:
- Expected diagnostics to capture:
- Query-plan/index considerations:
- Candidate bottlenecks (UI, DAL, SQL):
- Validation before/after metrics:

### Payment/allocation logic template

- Business rule to enforce:
- Affected forms/classes/procedures:
- Affected tables:
- Allocation/recalculation trigger:
- Transaction scope:
- Ordering/rounding rules:
- Edge cases (partial payments, over-allocation, zero balances):
- Validation dataset and expected results:

## Template-based advertising issue generation

Quick investigation map:
- Form open path:
  - `CampaignForm.tbbTemplate_Click` -> `FrmTemplate`.
  - `EditIssuesForm` inherits `CampaignForm`, so same toolbar/template entry point is used.
- Template execution path:
  - template mode (`EditMode.Template`) -> `CampaignForm.grid_CellClicked` -> `FrmGenerator`.
  - add/remove switch is `IssueTemplate.IsModeAdd`.
- Mode-specific execution:
  - Campaign mode: `FrmGenerator` uses `_campaign` add/delete logic.
  - Fan placement mode (`TariffWithRangeGrid`): `FrmGenerator` uses range delegates (`AddIssuesRange`, `DeleteIssuesRange`).

Key files for this flow:
- `Client/Forms/FrmTemplate.cs`
- `Client/Forms/FrmGenerator.cs`
- `Client/Forms/CampaignForm.cs`
- `Client/Forms/CreateActionMaster/EditIssuesForm.cs`
- `Client/Controls/TariffWithRangeGrid.cs`
- `Client/Classes/IssueTemplate.cs`
- SQL: `AddRangeIssues.sql`, `MasterIssueDelete.sql`, `TariffWindowWithRange.sql`

Fan placement safety checklist:
- Confirm remove mode is available in range/fan placement template flow.
- Reuse existing stored procedures (`AddRangeIssues`, `MasterIssueDelete`) instead of duplicating business logic.
- Keep delete criteria constrained to current action context + selected slot/roller/position.
- Keep recalculation (`Action.Recalculate` / `Campaign.RecalculateAction`) and UI refresh chain intact after template operations.
- Verify no behavior regressions for non-range campaign template flow.

## Definition of done for AI changes

- Build check attempted and environment limitations stated (if build cannot run locally).
- No unrelated refactoring.
- Existing behavior preserved unless explicitly requested.
- Stored procedure callers checked (explicit and metadata-driven when relevant).
- Manual validation steps documented.
- Risky assumptions/open questions listed.
