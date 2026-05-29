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

## Definition of done for AI changes

- Build check attempted and environment limitations stated (if build cannot run locally).
- No unrelated refactoring.
- Existing behavior preserved unless explicitly requested.
- Stored procedure callers checked (explicit and metadata-driven when relevant).
- Manual validation steps documented.
- Risky assumptions/open questions listed.
