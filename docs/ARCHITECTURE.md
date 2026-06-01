# Project overview

`qd2` is a legacy desktop system for planning and operating advertising campaigns (mainly radio/mass media), tariff/price calculations, issue scheduling, payment distribution, and operational reporting/statistics.

Core business domains visible in code and DB objects:
- Campaigns/actions and campaign recalculation
- Radio stations / mass media and tariff windows
- Price lists/tariffs and package pricing
- Issues/broadcast scheduling and substitutions
- Payments and payment allocation to actions/orders
- Firms/head companies/agencies and managers
- Reports/statistics (`rpt_*`, `stat_*` procedures)
- Users/security/rights and menu access

Main runtime entry points:
- `Client/Classes/Launcher.cs` (`Main`) initializes logging/culture/global handlers, then starts `MdiForm`.
- `Client/Forms/MDIForm.cs` is the main container and menu router to domain forms/journals.

# Solution structure

- `qd2.sln` — root solution.
- `Client/` — primary WinForms app:
  - `Client/Forms/` — forms (campaigns, payments, reports, tariffs, etc.).
  - `Client/Controls/` — reusable grids/editors (`SmartGrid`, issue grids, template controls).
  - `Client/Classes/` — business/domain classes (Campaign/Action/Payment/etc.).
- `FogSoft.WinForm/` — shared framework:
  - `DataAccess/` — DB access and transaction orchestration (`DataAccessor`, `DbExecutionScope`).
  - `Classes/` — entity metadata, security, configuration, error handling.
  - `Controls/`, `Passport/` — UI/passport metadata infrastructure.
- `Microsoft.ApplicationBlocks.Data/` — SqlHelper implementation used by DAL.
- `ArtvisDB/` — SQL Server project with DB objects:
  - `dbo/Stored Procedures/`, `dbo/Tables/`, `dbo/Functions/`, `dbo/Views/`, `dbo/User Defined Types/`.
- `ConnectionStringsProtector/` — utility executable.
- `Lib/` — third-party binaries and XML docs.

# Runtime architecture

Typical runtime flow:
1. User action in a form/control (button/menu/check/grid event).
2. UI validation inside event handler.
3. Call into domain/business object and/or `DataAccessor`.
4. Stored procedure execution (metadata-driven or explicit proc name).
5. Data returned as `DataSet`/`DataTable`/`DataView` or output parameters.
6. UI grid/form refresh.
7. Error handling/logging via `ErrorManager` + log4net.

Concrete examples:
- Campaign manager discount flow (`Client/Forms/ActionForm.cs`):
  - `SetManagerDiscount` wraps `campaign.SetFinalPrice(...)` + `campaign.Action.Recalculate(...)` in `DataAccessor.BeginTransaction/CommitTransaction` with rollback on exception.
- Price calculation flow (`Client/Forms/PriceCalculatorForm.cs`):
  - UI inputs -> `grdPriceCalculator.LoadData(...)` -> `ApplyCalculation(...)` -> package discount via `pc_PackageDiscountCalculateModel` + TVP parameter (`dbo.pc_SelectedMassmedia`) -> totals update.
- Payment allocation flow (`Client/Forms/PaymentCandidatesForm.cs`, `Client/Classes/PaymentCommon.cs`):
  - Candidate actions loaded through metadata action (`DataAccessor.DoAction`), selected rows converted into payment-action updates via `PresentationObject.Update()`.
- Report flow (`Client/Forms/GridReport/GridReportCreator.cs`):
  - Calls `rpt_Grid_v3` and `stat_FillPercentage`, binds results to report grid/export layers.

# Database access architecture

Main classes and responsibilities:
- `FogSoft.WinForm/DataAccess/DataAccessor.cs`
  - Core execution APIs: `LoadDataSet`, `ExecuteNonQuery`, `ExecuteScalar`, `DoAction`.
  - Parameter preparation and metadata-based action resolution.
  - Injects logged user context (`loggedUserId`) when needed.
  - Handles transactions (both external `BeginTransaction` and local transaction scopes).
  - Supports TVP values via `TvpValue`.
  - Builds executable SQL script text for diagnostics (`BuildExecScript`).
- `FogSoft.WinForm/DataAccess/DbExecutionScope.cs`
  - Measures proc execution time and logs slow calls (threshold from `ConfigurationUtil.StoredProcExecutionTimeThreshold`).
- `Microsoft.ApplicationBlocks.Data/SqlHelper` usage
  - Underlying ADO.NET command execution and parameter cache behavior.

Stored procedure calling patterns:
- Explicit call: `DataAccessor.LoadDataSet("ProcName", params)` or `ExecuteNonQuery/ExecuteScalar`.
- Metadata-driven call: `DataAccessor.PrepareParameters(entity)` + `DataAccessor.DoAction(parameters)`.
  - Mapping from entity/action/module to proc is loaded from `ProcedureConfigurationRetrieve.sql` result.

Parameter/output patterns:
- Parameters passed as `Dictionary<string, object>`.
- Output parameters often initialized as `null` then read back from dictionary after call.
- TVP usage example: `PriceCalculatorForm` passes `new TvpValue(dataTable, "dbo.pc_SelectedMassmedia")`.

Transaction patterns:
- Explicit business transactions in UI/business flow (e.g., `ActionForm`, issue grids, pack module grid).
- DAL also creates local SQL transactions for some execution branches.
- Risk: nested/overlapping transactional assumptions if flow mixes manual transaction and lower-level calls.

Logged user propagation:
- `SecurityManager.LoggedUser` set after login (`GetUserData` proc), and used by DAL/business operations.
- Log context property `user` set in `MDIForm` (`GlobalContext.Properties["user"] = ...`).

Long-running SQL diagnostics:
- `DbExecutionScope` logs procedure/exec script + timing when threshold exceeded.
- Threshold configured in `Client/app.config` (`StoredProcExecutionTimeThresholdMS`).

Common risks:
- Hidden proc coupling via metadata mappings (`iModuleProcedure`/`iStoredProcedure`).
- Output parameter contract changes breaking caller expectations.
- Connection/transaction lifetime assumptions in legacy mixed patterns.
- Command timeout differences across calls (some calls use custom timeout values).

# Stored procedures and SQL objects

SQL scripts location:
- `ArtvisDB/dbo/Stored Procedures/*.sql`
- `ArtvisDB/dbo/Tables/*.sql`
- `ArtvisDB/dbo/User Defined Types/*.sql`
- `ArtvisDB/dbo/Functions/*.sql`, `Views/*.sql`

Important naming patterns observed:
- `*IUD` for insert/update/delete-style operations.
- `rpt_*` for reporting datasets.
- `stat_*` for statistical datasets.
- `sp_*` appears in some utility/validation procedures.
- Domain-specific names (`Campaign*`, `Payment*`, `Tariff*`, `Roller*`, etc.).

Key metadata proc:
- `ArtvisDB/dbo/Stored Procedures/ProcedureConfigurationRetrieve.sql`
  - Produces entity/action/module -> stored procedure and table alias configuration.
  - Reads metadata tables `iStoredProcedure`, `iModuleProcedure`, `iTableAlias`.

Examples of C# to proc mappings:
- Security/menu metadata: `GetUserData`, `UserMenuItems`, `EntityInfoRetrieve`.
- Campaign/action: `ActionRecalculate`, `MoveIssues2NewCampaign`, `CampaignIssuesTransfers`.
- Pricing/tariffs: `GetPriceByPeriod`, `TariffWindowRetrieveByDate`, `GenerateTariffWindowByTemplate`.
- Payments: `PaymentAction_CorrectByActionTotalPrice`, payment candidate actions (metadata-based).
- Reports/statistics: `rpt_Grid_v3`, `stat_FillPercentage`, `rpt_OrderActionBill`.
- Price calculator: `pc_PackageDiscountCalculateModel`.

Safe SQL modification rules:
- Search both explicit callers and metadata-driven callers before changes.
- Keep parameter names/types/order/output semantics stable unless all callers are updated.
- Validate impacted forms/reports manually with representative data.
- For reporting procs, verify filters/grouping/paging behavior and performance.

How to search usage:
- C#: search `DataAccessor.LoadDataSet(`, `ExecuteNonQuery(`, `ExecuteScalar(` by proc name.
- Metadata: inspect `ProcedureConfigurationRetrieve.sql`, `iStoredProcedure.sql`, `iModuleProcedure.sql`.
- SQL dependencies: inspect other procs/views/functions in `ArtvisDB` for referenced object names.

Patterns seen:
- Heavy use of temp/intermediate sets in reporting and scheduling procs (object names indicate multi-stage reporting/statistics design).
- TVP usage present (e.g., `dbo.pc_SelectedMassmedia` for calculator package discount).

# UI architecture

Main forms/controls:
- `Client/Forms/MDIForm.cs` — main shell/menu orchestration.
- `Client/Forms/ActionForm.cs`, `CampaignForm.cs` — campaign/action operations.
- `Client/Forms/PriceCalculatorForm.cs` + `Client/Controls/TemplateEditorControl.cs` — pricing calculator UI.
- `Client/Forms/PaymentCandidatesForm.cs` — payment allocation selection.
- `Client/Forms/GridReport/*` — reporting/filter/export UX.
- Reusable grids in `Client/Controls/*` and `FogSoft.WinForm/Controls/*`.

Grid/event patterns:
- Form events wired in constructors/load handlers.
- Grid refresh via `DataSource` assignment to `DataView`/`DataTable`.
- Selection/check logic with explicit handlers (e.g., check-all in price calculator snapshots).

Validation patterns:
- Inline checks in event handlers (date ranges, required selections, value guards).
- Errors surfaced via `FogSoft.WinForm.Forms.MessageBox` and `ErrorManager.PublishError`.

Long-running operation patterns:
- `UseWaitCursor`/`Cursor.Current` around DB-heavy operations.
- `Application.DoEvents()` used in some handlers before heavy execution.

Common UI risks:
- Grid rebinding side effects and expensive redraws.
- Check-all / multiselect synchronization bugs.
- Event re-entrancy when recalculation triggers multiple handlers.
- Performance hits when updating large DataTables repeatedly.

# Business modules

## Campaigns / actions

Purpose:
- Create/edit/manage advertising actions and campaign lines, with recalculation and issue movement logic.

Main classes/forms:
- `Client/Classes/Action.cs`, `ActionOnMassmedia.cs`, `Campaign.cs`
- `Client/Forms/ActionForm.cs`, `CampaignForm.cs`

Main stored procedures:
- `ActionRecalculate`, `MoveIssues2NewCampaign`, `CampaignIssuesTransfers`, `GetMonthes`, `GetMassmedias` (plus metadata-driven action CRUD).

Important tables:
- Campaign/action domain tables in `ArtvisDB/dbo/Tables` (campaign, action, issue linkage tables; exact confirmation recommended per deployment schema).

Typical flow:
- UI edit -> domain object update -> recalculation proc(s) -> grid/stat refresh.

Known invariants/risks:
- Discount/final price updates often require immediate recalculation.
- Transaction boundaries are critical for multi-campaign updates.

## Template-based advertising issue generation

Scope:
- Covers template add/remove flow initiated from campaign screens and from fan placement editing (`EditIssuesForm` + `TariffWithRangeGrid`).

Entry points and form opening:
- `CampaignForm.tbbTemplate_Click` creates `FrmTemplate` with current `IssueTemplate`.
- `EditIssuesForm` inherits `CampaignForm`, so it uses the same `tbbTemplate_Click` entry point.
- Template mode is activated by `CampaignForm.SetEditMode` (`EditMode.Template`) when toolbar template button is checked.

`FrmTemplate` and `FrmGenerator` interaction:
- `FrmTemplate` edits template date interval, weekday/odd-even rule, and add/remove mode (`IssueTemplate.IsModeAdd`).
- In template edit mode, clicking a tariff cell triggers `CampaignForm.grid_CellClicked`.
- `grid_CellClicked` calls `IssueTemplate.SetTime(windowDate)` and opens `FrmGenerator`.
- `FrmGenerator.Generate()` iterates template dates (`IssueTemplate.MoveNext`) and dispatches:
  - add path: `AddIssues()`;
  - remove path: `DeleteIssues()`.

Template representation (UI + code):
- Day selection and odd/even mode: `FrmTemplate` controls + `IssueTemplate.Day2AddMode`, `IssueTemplate.WeekDays`, `IssueTemplate.IsOdd`.
- Date boundaries: `IssueTemplate.StartDate` / `FinishDate`.
- Selected position/window time comes from clicked grid cell (`ITariffWindow.WindowDate`) and is injected via `IssueTemplate.SetTime`.
- Station/campaign scope is defined by active grid context:
  - normal campaign mode: current `Campaign` (single massmedia campaign line);
  - fan placement mode: action-wide range grid (`TariffWithRangeGrid`) covering all campaigns of the action.

How selected template positions become issues:
- For normal campaign mode:
  - `FrmGenerator.AddSimpleIssue` -> `Campaign.AddIssue(...)` -> `RollerIssue.Update()` -> DB write via `IssueIUD` path.
- For fan placement mode (`TariffWithRangeGrid`):
  - `FrmGenerator` uses delegates:
    - add: `TariffWithRangeGrid.AddIssuesRange(date)` -> proc `AddRangeIssues`;
    - remove: `TariffWithRangeGrid.DeleteIssuesRange(date)` -> proc `MasterIssueDelete`.

Insert/remove/recalculate/refresh flow:
- Add (campaign): issue inserted per date; on generator finish `Campaign.RecalculateAction()`.
- Remove (campaign): `CampaignOnSingleMassmedia.GetIssuesForDate(date)` + `issue.Delete(true)`; on finish recalculation runs.
- Add (fan placement): `AddRangeIssues` inserts one issue per campaign/massmedia in action context; `_action.Recalculate()` is called.
- Remove (fan placement): `MasterIssueDelete` removes matching issue per campaign/massmedia in action context; `_action.Recalculate()` is called.
- UI refresh:
  - `CampaignForm.grid_CellClicked` refreshes grid when template intersects visible date range.
  - For `TariffWithRangeGrid`, `GridRefreshed` event in `EditIssuesForm` updates added issues grid and action statistics.

Responsible methods/classes:
- Add issues by template:
  - `CampaignForm.grid_CellClicked`
  - `FrmGenerator.Generate` / `AddIssues`
  - `Campaign.AddIssue`, `TariffWithRangeGrid.AddIssuesRange`
- Delete/remove issues by template:
  - `FrmGenerator.DeleteIssues`
  - `CampaignOnSingleMassmedia.GetIssuesForDate` + `Issue.Delete(true)` (campaign mode)
  - `TariffWithRangeGrid.DeleteIssuesRange` (fan placement mode)
- Apply template to campaign:
  - `CampaignForm` + `FrmTemplate` + `FrmGenerator` (campaign-backed constructors)
- Apply template to existing fan-placement issues:
  - `CampaignForm` with range-grid branch -> `FrmGenerator` delegate constructor
- Refresh after changes:
  - `CampaignForm.RefreshGrid`, `CampaignForm.CampaignStatusChanged`
  - `EditIssuesForm.TariffGridRefreshed`, `ShowCurrentIssues`, `Action.DisplayData`

Database objects involved:
- Stored procedures:
  - `AddRangeIssues` (fan placement add)
  - `MasterIssueDelete` (fan placement delete)
  - `TariffWindowWithRange` (range grid data source)
  - `IssuesByDate` (campaign-mode delete candidate retrieval)
  - `ActionRecalculate` (recalculation after modifications)
  - `IssueIUD` (low-level insert/delete execution)
- Main tables touched by this flow:
  - `Action`, `Campaign`, `Issue`, `TariffWindow`, `Tariff`.

Mode/parameter differences (normal vs fan placement):
- Normal campaign mode is driven by `_campaign` object in `FrmGenerator`.
- Fan placement mode is driven by `TariffWithRangeGrid` delegates and `ActionID`-scoped procedures.
- Distinguishing parameters for fan placement procs:
  - `@actionID`, `@issueDate`, `@rollerID`, `@positionID`, optional `@grantorID`;
  - add also uses `@rollerDuration`, `@considerUnconfirmed`.

How `EditIssuesForm` uses template mechanism:
- `EditIssuesForm` hosts `TariffWithRangeGrid` and inherits template toolbar behavior from `CampaignForm`.
- Template dialog in range/fan placement context allows choosing both add and remove modes.
- Range branch in `CampaignForm.grid_CellClicked` now passes both add and remove delegates into `FrmGenerator`.

Business rules to preserve in this flow:
- Do not delete unrelated issues (delete criteria remain date+roller+position within current action scope).
- Do not affect other actions (procedures are scoped by `@actionID`).
- Preserve station/massmedia filtering (`Campaign` rows under the current action, `TariffWindowWithRange` mm intersection logic).
- Preserve date/time boundaries (30-minute window selection via clicked tariff window datetime).
- Preserve confirmed/unconfirmed behavior (`considerUnconfirmed` handling in range add; existing campaign delete path unchanged).
- Preserve recalculation (`RecalculateAction`/`Action.Recalculate`) after modifications.
- Preserve existing UI refresh chain and stats updates.

Open questions:
- In campaign-mode delete (`IssuesByDate` + exact datetime match), behavior for multiple issues in the same date/time slot should be confirmed by domain owner.

## Price calculation

Purpose:
- Compute campaign pricing for selected mass media and schedule parameters, including package discount.

Main classes/forms:
- `Client/Forms/PriceCalculatorForm.cs`
- `Client/Classes/PriceCalculatorClasses.cs`
- `Client/Controls/TemplateEditorControl.cs`

Main stored procedures:
- `pc_PackageDiscountCalculateModel`.

Important tables/types:
- TVP type `dbo.pc_SelectedMassmedia` (used from C# via `TvpValue`).

Typical flow:
- User sets period/schedule/spots/duration -> data load + per-row calculation -> package discount proc -> totals.

Known invariants/risks:
- Date/day-selection logic (weekday mask vs even/odd days) affects totals.
- Snapshot restore/edit can desync with current UI state if not applied consistently.

## Tariffs / price lists

Purpose:
- Maintain tariff windows and pricing templates for mass media and pack modules.

Main classes/forms:
- `Client/Classes/MassmediaPricelist.cs`, `PackModulePricelist.cs`, `ModulePricelist.cs`
- `Client/Forms/FrmWindowTariffTemplate.cs`

Main stored procedures:
- `TariffWindowRetrieveByDate`, `TariffWindowWithAdvertTypeRetrieve`, `GenerateTariffWindowByTemplate`, `TariffWindowMassDelete`, `PackModuleTariffWindowsRetrieve`.

Important tables:
- Tariff window and related pricing tables in `ArtvisDB/dbo/Tables`.

Typical flow:
- Select period/object -> retrieve windows -> edit/save/delete with validations.

Known invariants/risks:
- Linked windows and advert type constraints (`CheckLinkedWindows`, related checks).
- Date-range overlap and batch update effects.

## Issues / broadcasts

Purpose:
- Manage aired/planned issues, substitutions, and positioning.

Main classes/forms:
- `Client/Classes/Issue.cs`, `CampaignOnSingleMassmedia.cs`, `CampaignRoller.cs`, `ModuleIssue.cs`
- `Client/Controls/RollerIssuesGrid3.cs`, `ProgramIssuesGrid2.cs`

Main stored procedures:
- `RollerSubstitute`, `RollerSubstitutionPassport`, `IssuesByDate`, `IssueChangePositioningPassport`.

Important tables:
- Issue/roller linkage tables and scheduling tables in `ArtvisDB/dbo/Tables`.

Typical flow:
- Grid edits/substitutions -> transactional save -> issue list/grid refresh.

Known invariants/risks:
- Positioning and substitutions can impact downstream reporting and campaign totals.
- Multi-row issue changes usually require transactional integrity.

## Payments / payment allocation

Purpose:
- Register payments and allocate sums across actions/orders.

Main classes/forms:
- `Client/Classes/PaymentCommon.cs`, `PaymentStudioOrder.cs`
- `Client/Forms/PaymentCandidatesForm.cs`

Main stored procedures:
- `PaymentAction_CorrectByActionTotalPrice`, candidate selection/retrieval procs via metadata actions.

Important tables:
- Payment and payment-allocation tables in `ArtvisDB/dbo/Tables`.

Typical flow:
- Load candidates -> distribute available payment remainder -> update payment-action links -> refresh balances.

Known invariants/risks:
- Allocation order affects remainder distribution.
- Recalculation/correction procs may be required after action total changes.

## Firms / head companies / agencies

Purpose:
- Maintain legal entities and relationships used in actions/contracts/payments.

Main classes/forms:
- `Client/Classes/Agency.cs`, firm-related classes/forms (`FirmPassportForm`, balance forms)
- `Client/Forms/FirmPassportForm.cs`, `FrmFirmIssuesBalance.cs`, `FrmFirmStudioOrderBalance.cs`

Main stored procedures:
- `AgencyPainting`, `firms`, `FirmManagers`, `FirmStudioOrderManagers`, `sp_CheckFirmINN`, `sp_SayAdminThatFirmInnDouble`.

Important tables:
- Firms/agencies/head-company tables in `ArtvisDB/dbo/Tables`.

Typical flow:
- Passport form validation -> save/update -> related balances/references refresh.

Known invariants/risks:
- INN uniqueness checks and duplicate notifications.
- Relationship integrity across firm/agency/action/payment entities.

## Reports / statistics

Purpose:
- Provide planning/monitoring reports and operational statistics.

Main classes/forms:
- `Client/Forms/GridReport/FrmGridReport.cs`
- `Client/Forms/GridReport/GridReportCreator.cs`
- `Client/Forms/RollerStatisticForm.cs`

Main stored procedures:
- `rpt_Grid_v3`, `rpt_Grid` (legacy creator), `stat_FillPercentage`, `rpt_OrderActionBill`, `OnAirInquireReport`.

Important tables:
- Report source domain tables under campaign/issues/payments/tariffs.

Typical flow:
- Filter input -> report proc call(s) -> bind/export.

Known invariants/risks:
- Filter semantics and grouping must match business expectations.
- Performance sensitivity for wide date ranges and large datasets.

## Users / security / rights

Purpose:
- Authenticate users, load rights/menu, enforce action availability.

Main classes/forms:
- `FogSoft.WinForm/Classes/SecurityManager.cs`
- `FogSoft.WinForm/Classes/MenuManager.cs`
- `Client/Classes/AdvertAgUser.cs`
- Login UI controls in `FogSoft.WinForm/Controls/LoginCtl.cs`

Main stored procedures:
- `GetUserData`, `UserMenuItems`, `GetUserDiscount`, `UserAgencies`, `CheckRatioForUser`.

Important tables:
- User/rights/menu metadata tables in `ArtvisDB`.

Typical flow:
- Login -> load user profile/rights -> set context -> menu/action enablement checks.

Known invariants/risks:
- Rights flags gate critical operations (discount changes, reports, etc.).
- User context must be set before data operations relying on logged user.

# Logging and diagnostics

Framework and config:
- log4net configured in `Client/app.config`.
- Rolling file appender writes `logs/qd2.log` (size-based rotation).
- Pattern includes `%property{user}` and `%property{cid}`.

Context properties:
- `user` is explicitly assigned in `MDIForm` after successful login.
- `cid` is in pattern; explicit assignment is not obvious in current code scan (open question).

SQL execution logging:
- `DbExecutionScope` logs slow proc calls with elapsed time and timeout.
- Threshold is configurable (`StoredProcExecutionTimeThresholdMS`).

How to diagnose slow procedures:
1. Lower/confirm threshold in app config for investigation.
2. Reproduce scenario from UI.
3. Inspect `logs/qd2.log` for DAL entries (proc or generated `exec ...` script and timings).
4. Re-run the captured script in SSMS with actual execution plan/statistics.

Where to add temporary diagnostics safely:
- Prefer existing log points in DAL and immediate caller handlers.
- Avoid changing stored procedure contracts for diagnostics-only needs.

# Build, run, and validation

Build/tooling:
- Expected tooling: Windows + Visual Studio / MSBuild with .NET Framework 3.5 and 4.8 targeting packs.
- Legacy automation exists in `AdvertAg.build` (NAnt invoking `C:\WINDOWS\Microsoft.NET\Framework\v3.5\msbuild.exe`).

Known prerequisite constraints:
- Linux sandbox `dotnet build` fails due missing .NET Framework reference assemblies.

Run locally:
- Build `qd2.sln`, run `Client` startup project.
- Ensure SQL Server DB (`GrifMedia` in default config) and connection string are valid for environment.

Validate typical UI change (manual):
1. Start app and authenticate.
2. Open affected form from `MDIForm` menu.
3. Execute changed user scenario.
4. Verify grid updates, calculations, and action enablement.
5. Check for exceptions and inspect `logs/qd2.log`.

Validate DB/stored procedure change (manual):
1. Identify all C# and metadata-driven callers.
2. Execute representative UI flows that invoke the proc.
3. Validate returned tables/columns/output params.
4. Compare before/after behavior on edge-case inputs.
5. Inspect performance/timing logs for regressions.

Automated tests:
- No dedicated test project was found in this repository scan.

Safest manual validation checklist when no tests exist:
- Smoke login/menu load.
- Smoke core modules touched (campaigns/payments/reports/tariffs).
- Verify save/edit/delete scenario touched by change.
- Verify at least one report/stat relevant to changed data.
- Check log for DAL errors/timeouts.

# Coding conventions

Observed conventions:
- PascalCase for types/methods/properties; camelCase/private fields with `_` prefix.
- Heavy use of `DataSet`/`DataTable`/`DataView` for transport/binding.
- Error handling: try/catch in UI handlers + `ErrorManager.PublishError(ex)`.
- Null handling: defensive checks before object/row usage; dictionary params may carry `null` for output placeholders.
- Transaction style: explicit begin/commit/rollback in business-critical multi-step operations.
- SQL style: procedure-centric architecture with metadata-driven mappings and named parameter dictionaries.
- Comments: mixed RU/EN, mostly practical inline notes.
- Formatting: legacy Visual Studio C# style; avoid broad formatting churn.

# AI-agent safety rules

- Always inspect existing similar implementation before adding or changing logic.
- Prefer minimal, targeted patches.
- Do not invent architecture not present in repository.
- Do not introduce new frameworks/libraries without explicit request.
- Do not change public behavior unless explicitly requested.
- Do not change SQL schema without explicit request.
- Do not change stored procedure contracts without updating all callers.
- When modifying reports/statistics, trace both C# caller and stored procedure.
- When modifying payment/campaign logic, check transaction and recalculation side effects.
- When modifying UI grids, verify binding/event side effects and performance.
- Before finalizing a task, provide a concise validation checklist.

# Open questions

1. Business-level canonical description of each entity/table (campaign/payment/report schema semantics) needs domain-owner confirmation.
2. Exact production build pipeline currently used (VS version, CI workflow, SQL deploy steps) is not fully documented in-repo.
3. `%property{cid}` logging context appears configured but setter location is unclear.
4. Which stored procedures are considered contract-stable API vs. internal implementation is not explicitly documented.
5. Canonical end-to-end regression checklist by module (campaigns/payments/reports) should be confirmed by product owner/QA.
