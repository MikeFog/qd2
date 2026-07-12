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
- Logging guide: `docs/LOGGING.md`
- SmartGrid control reference: `docs/smartgrid.md`
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