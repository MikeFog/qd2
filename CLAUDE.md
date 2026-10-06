# qd2 Claude Code instructions

This is a legacy Windows Forms advertising agency system.

Primary stack:
- C# / .NET Framework / Windows Forms
- MS SQL Server
- Stored procedure centric architecture
- Main application project: Client
- Shared framework: FogSoft.WinForm
- SQL project: ArtvisDB

Before making non-trivial changes:
1. Read `docs/ARCHITECTURE.md`.
2. Read `docs/AI_AGENT_PLAYBOOK.md`.
3. Find existing similar implementation before editing.
4. Identify all affected layers: WinForms UI, business classes, DAL, SQL stored procedures, logging.
5. Produce a short plan before code changes.
6. Keep changes minimal and targeted.
7. Do not introduce new frameworks or large refactoring unless explicitly requested.
8. Do not change stored procedure contracts without checking all C# and metadata-driven callers.
9. For SQL changes, search both C# callers and metadata mappings.
10. For UI grid changes, check event re-entrancy, rebinding, selection/check-all behavior and performance.
11. For payment/campaign logic, check transaction boundaries and recalculation side effects.
12. Before final answer, provide a concise validation checklist.

Important project documents:
- Architecture map: `docs/ARCHITECTURE.md`
- AI workflow/playbook: `docs/AI_AGENT_PLAYBOOK.md`
- Improvement candidates (non-bugs, future work): `docs/IMPROVEMENTS.md`
- Business-rule errors (`RAISERROR('Ключ')` + таблица `iMessage`, как добавить правило): `docs/business-errors.md`
- UI rules for user messages (titles, buttons, texts; desktop and web): `docs/UI_RULES.md` — read before adding or changing any message box / info dialog / menu item name; also lists known deviations.
- Logging guide: `docs/LOGGING.md`
- SmartGrid control reference: `docs/smartgrid.md`
- Tariff grid family reference: `docs/tariffgrid.md` — all placement/window grids (`TariffGrid` hierarchy, `TariffWithRangeGrid`, `ComboModuleGrid`, `TrafficGrid`): hosts, procedures, cell semantics, prod timings, ranked performance defects П-1…П-17. Read before touching any grid, placement form or template generator. Web design: `docs/tasks/web-tariffgrid.md`.
- Window merging reference: `docs/window-merging.md` — two distinct "склейка" mechanisms: `TariffUnion` (pricelist-level tariff continuation) and `TariffWindow.windowPrevId`/`windowNextId` (per-day window chains); entry points, readers, and known defects.
- `broadcastStart` reference: `docs/broadcast-start.md` — legacy "broadcast day start" field in `Pricelist`/`SponsorProgramPricelist`; full inventory of 49 dependent DB objects and 11 C# files (several shared with web via FogSoft.Core) grouped by removal cost, data-state evidence that the field is dormant, and a staged removal plan. Read before touching anything that shifts `issueDate` by `broadcastStart`.
- Media plan («График размещения») reference: `docs/mediaplan.md` — all 21 print actions + menu/ActionForm entry points, two sheet layouts (by campaign / by agency-station), block layout, SQL (`MediaPlanRetrieve_v2`), settings, pitfalls, real usage from logs. Web port plan: `docs/tasks/web-mediaplan.md`.
- Mass operations on selected tariff-grid windows: `docs/mass-window-operations.md` — Del / Insert / Ctrl+R (roller replace) / roller checklist in linear `CampaignForm` and veer `EditIssuesForm`; where enabled, implementation differences, why linear replace goes per issue.
- Action/campaign forms reference: `docs/action-forms.md` — how actions and campaigns are created and edited in the desktop (`ActionForm`, the `CampaignForm` "superform" in five modes, veer `EditIssuesForm`, combo `ComboModulePlacementForm`, three creation paths), feature matrices, usage stats, defects Д-1…Д-16. Web plan (action page with placement tabs by kind of place, one creation wizard): `docs/tasks/web-action-forms.md`.
- Roller types (`iRollerActionType`) inventory: `docs/roller-types.md` — all 11 types × placement rules, grid/fill statistics, DJin export order, auto-framing of agitation, findings Н-1…Н-12 and a checklist for adding a new type. Customer summary: `docs/roller-types-customer.md`. Read before adding or changing a roller type.
## Scenario maps

Detailed scenario investigations are stored in:
- `docs/scenarios/issue-add-click-to-db.md` — adding an advertising issue by clicking a cell in `_tariffGrid` / `RollerIssuesGrid3`, from UI click to `IssueIUD`, `ActionRecalculate`, and UI refresh.
- `docs/scenarios/range-issue-add-click-to-db.md` — adding issues across all massmedia in one click via `TariffWithRangeGrid` / `AddRangeIssues`.
- `docs/scenarios/template-issue-generation.md` — bulk issue generation via `FrmTemplate` / `FrmTemplate2` / `FrmGenerator`; covers Simple and TimePeriod templates, prime/non-prime split, linear vs range comparison, and gap analysis for range TimePeriod support.
- `docs/scenarios/campaign-edit-form-load.md` — data-loading chain when initializing the campaign edit form (`CampaignForm`): pricelist → `TariffWindowRetrieve` grid → `Grid` issue marking → rollers/stats; entry points, SQL procedures, and where original-window time enters the layout.
- `docs/scenarios/template3-roller-distribution.md` — Шаблон №3 (`FrmTemplate3`): multiple rollers with individual quotas, price estimate reusing `PriceCalculatorGrid`, and `RollerAllocationQueue` round-robin distribution shared by linear (`FrmGenerator`) and range (`TariffWithRangeGrid`) generation paths.
Before changing issue creation logic, read the relevant scenario map first.

Communication style:
- Be concise.
- Prefer targeted patches.
- Explain risky assumptions.
- Do not do unrelated formatting churn.