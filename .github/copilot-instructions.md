# Copilot instructions for qd2

## Quick context
- Legacy WinForms app (`Client`) + shared framework (`FogSoft.WinForm`) + SQL Server DB project (`ArtvisDB`).
- DAL entrypoint: `FogSoft.WinForm/DataAccess/DataAccessor.cs`.
- Main shell: `Client/Forms/MDIForm.cs` (`MdiForm` class).

## Where to look first
- UI/event handlers: `/tmp/workspace/MikeFog/qd2/Client/Forms` and `/tmp/workspace/MikeFog/qd2/Client/Controls`
- Business/domain classes: `/tmp/workspace/MikeFog/qd2/Client/Classes`
- Metadata-driven DB mapping: `ArtvisDB/dbo/Stored Procedures/ProcedureConfigurationRetrieve.sql`, tables `iStoredProcedure`, `iModuleProcedure`, `iTableAlias`
- Explicit proc calls: search `DataAccessor.LoadDataSet(`, `ExecuteNonQuery(`, `ExecuteScalar(`.

## Change rules
- Keep existing patterns (`DataSet`/`DataTable`, event-driven forms, `ErrorManager.PublishError`).
- Do not introduce new architecture/frameworks.
- Do not change stored procedure contracts unless all callers (explicit + metadata-driven) are updated.
- Keep patches minimal; avoid unrelated refactors.

## High-risk areas (verify explicitly)
- Transactions: `DataAccessor.BeginTransaction/CommitTransaction/RollbackTransaction` and multi-step flows in `Client/Forms/ActionForm.cs`.
- Security: password hashing uses `MD5CryptoServiceProvider` in `FogSoft.WinForm/Classes/SecurityManager.cs`.
- Sensitive logging: `DataAccessor` appends parameter values and generated exec scripts to exceptions/log scope (`BuildExecScript`, `exp.Data.Add(...)`).
- Credentials/config: connection string is in `Client/app.config` (`<connectionStrings>`).

## Build/validation constraints
- Linux `dotnet build qd2.sln -c Debug` fails without .NET Framework 3.5/4.8 targeting packs.
- No test projects are present; validate by targeted manual UI + SQL scenarios.
