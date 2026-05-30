# AI Agent Playbook for qd2

## How to approach a new task

1. Read `/tmp/workspace/MikeFog/qd2/.github/copilot-instructions.md`.
2. Locate concrete callers before editing:
   - Proc calls: `DataAccessor.LoadDataSet(`, `ExecuteNonQuery(`, `ExecuteScalar(` in `/tmp/workspace/MikeFog/qd2/Client` and `/tmp/workspace/MikeFog/qd2/FogSoft.WinForm`.
   - Metadata routing: `ArtvisDB/dbo/Stored Procedures/ProcedureConfigurationRetrieve.sql` + tables `iStoredProcedure`, `iModuleProcedure`, `iTableAlias`.
3. Enumerate touched layers explicitly (UI form/control, class, proc, table/type).
4. Make a minimal patch only in affected files.
5. Validate what is possible in this environment:
   - Build check: `dotnet build qd2.sln -c Debug` (expected to fail in Linux without .NET Framework targeting packs).
   - No test project exists; run manual scenario checklist for changed modules.
6. Report what was verified vs. what remains uncertain.

## Task templates

### Bug fix task template

- Symptom:
- Expected behavior:
- Repro steps:
- Affected screen/form (exact path):
- Logs/screenshots/data samples:
- Suspected files/procedures/tables:
- Suspected root cause:
- Validation plan:
- Risky assumptions:

### New UI feature task template

- Form/control to change (exact file path):
- User action (button/menu/grid event):
- Expected UI behavior:
- Validation/error messages:
- Database impact (procedure/table/type names):
- Grid behavior (selection, check-all, sorting, width/perf):
- Manual test scenarios:
- Risks/assumptions:

### Stored procedure change template

- Procedure name:
- Current input parameters:
- Current output parameters:
- Proposed contract change (if any):
- Affected tables/views/functions:
- C# caller methods/forms/classes:
- Metadata-driven callers (entity/action/module):
- Metadata tables checked (`iStoredProcedure`, `iModuleProcedure`, `iTableAlias`):
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
- Related report creator path (`Client/Forms/GridReport/GridReportCreator.cs` or equivalent):
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

### Security-sensitive change template

- Authentication impact (`FogSoft.WinForm/Classes/SecurityManager.cs`):
- Connection string/config impact (`Client/app.config`, `ConnectionStringsProtector`):
- Logging data exposure impact (`DataAccessor.BuildExecScript`, exception parameter logging):
- Rights/menu impact (`GetUserData`, `UserMenuItems`, `Group*`/`User*` tables):
- Data leakage/regression checks:

### Payment/allocation logic template

- Business rule to enforce:
- Affected forms/classes/procedures:
- Affected tables:
- Allocation/recalculation trigger:
- Transaction scope:
- Ordering/rounding rules:
- Edge cases (partial payments, over-allocation, zero balances):
- Validation dataset and expected results:

## Definition of done for AI changes

- Build check attempted and environment limitations stated (if build cannot run locally).
- No unrelated refactoring.
- Existing behavior preserved unless explicitly requested.
- Stored procedure callers checked (explicit and metadata-driven when relevant).
- Manual validation steps documented.
- Risky assumptions/open questions listed.
- High-risk areas reviewed when relevant (transaction scope, MD5 auth path, sensitive logging, connection string handling).
