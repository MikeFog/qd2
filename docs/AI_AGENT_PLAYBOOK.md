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
