# Copilot instructions for qd2

## Repository context
- Legacy **Windows Forms** desktop application for advertising/radio campaign operations, pricing, issues scheduling, payments, and reporting.
- Main technologies: **C#**, **.NET Framework 3.5/4.8**, **MS SQL Server stored procedures**, **log4net**.
- Main projects: `Client` (UI + business flows), `FogSoft.WinForm` (framework/DAL/passport engine), `Microsoft.ApplicationBlocks.Data` (SqlHelper), `ArtvisDB` (SQL objects).

## Architecture at a glance
- UI event handlers in forms/controls call domain classes (`Client/Classes/*`) and shared framework classes.
- Data access is centralized in `FogSoft.WinForm/DataAccess/DataAccessor.cs`.
- A large part of CRUD/actions is metadata-driven via entity/action mappings (`ProcedureConfigurationRetrieve.sql`, `iStoredProcedure`, `iModuleProcedure`, `iTableAlias`).
- Some calls use explicit stored procedure names (`DataAccessor.LoadDataSet/ExecuteNonQuery/ExecuteScalar`) in business classes/forms.

## Build and validation
- Preferred environment: **Windows + Visual Studio/MSBuild** with .NET Framework targeting packs.
- In Linux sandbox, `dotnet build qd2.sln -c Debug` fails (missing .NET Framework 3.5/4.8 reference assemblies).
- Validate changes by targeted manual scenario checks in app UI and SQL verification.

## Coding and change conventions
- Keep existing style (legacy WinForms + DataSet/DataTable patterns).
- Reuse existing entity/action/data access patterns; do not introduce new architecture layers/frameworks.
- Keep patches minimal and grouped by related files; avoid many tiny unrelated edits.
- Preserve Russian UI/business naming where already present.

## SQL/stored procedure rules
- SQL scripts are under `ArtvisDB/dbo/*`.
- Before changing a proc, find all C# callers (`DataAccessor.*("ProcName"...)`) and metadata mappings.
- Do not change proc parameter contracts/output semantics unless all callers are updated.
- Be careful with transaction-sensitive flows (`DataAccessor.BeginTransaction/CommitTransaction/RollbackTransaction`).
- Keep long-running proc diagnostics intact (`DbExecutionScope`, `StoredProcExecutionTimeThresholdMS`).

## WinForms UI rules
- Follow existing event-driven patterns in forms and custom grids (`SmartGrid`, `GenericGridView`).
- Preserve wait-cursor/error handling style (`UseWaitCursor`, `ErrorManager.PublishError`).
- For grid changes, verify selection, check-all behavior, data binding side effects, and performance.

## Logging/diagnostics rules
- Keep log4net configuration behavior (`Client/app.config`, `logs/qd2.log`).
- Maintain user context logging (`GlobalContext.Properties["user"]` set after login).
- When adding diagnostics, keep them temporary, scoped, and removable.

## What not to do
- Do not refactor unrelated legacy code.
- Do not rename DB objects/classes/methods without explicit request.
- Do not change schema/stored procedure contracts without explicit task scope.
- Do not assume modern ORM/service architecture; document and follow existing patterns.
