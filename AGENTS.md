# qd2 repository instructions

This is a legacy Windows Forms advertising system.

Primary stack:
- C# / .NET Framework / Windows Forms
- MS SQL Server
- stored-procedure centric data access
- main application project: `Client`
- shared framework: `FogSoft.WinForm`
- SQL project: `ArtvisDB`

## Read first

Before any non-trivial change, read:
1. `docs/ARCHITECTURE.md`
2. `docs/AI_AGENT_PLAYBOOK.md`
3. `docs/business-logic.md` for domain meaning
4. `docs/LOGGING.md` if the task touches diagnostics
5. The most relevant file under `docs/scenarios/` for issue/template/campaign flows

Treat `docs/` as first-class project knowledge. Prefer it over model memory when it is available.

## What the docs are for

- `docs/ARCHITECTURE.md` - codebase layout, runtime flow, and data-access structure.
- `docs/AI_AGENT_PLAYBOOK.md` - task templates, validation expectations, and scenario notes.
- `docs/business-logic.md` - plain-language business meaning of campaigns, actions, and placement.
- `docs/LOGGING.md` - log4net and timing/diagnostic conventions.
- `docs/IMPROVEMENTS.md` - non-bug ideas and known rough edges; use for context, not as a change list.
- `docs/scenarios/*.md` - deep dives for specific user flows; read the matching scenario before changing that flow.
- `graphify-out/GRAPH_REPORT.md` - generated knowledge graph snapshot; useful for orientation, but it can be stale.

## Change rules

- Keep patches minimal and targeted.
- Do not refactor unrelated legacy code.
- Find an existing similar implementation before editing.
- Identify affected layers before changing behavior:
  - WinForms UI
  - domain/business classes
  - data access
  - stored procedures and SQL objects
  - logging and diagnostics
- Do not change stored procedure contracts unless all callers are checked.
- For SQL changes, search both explicit callers and metadata-driven callers.
- For grid/UI changes, check selection, rebind behavior, re-entrancy, and performance.
- Preserve existing Russian domain naming where it already exists in code or UI.

## Validation expectations

- Attempt a build when the environment allows it.
- If the build cannot run in this environment, say so clearly.
- Validate the relevant manual scenario, not just compilation.
- Document assumptions and risks for anything that is not obvious.

## Good default workflow

1. Read the relevant docs.
2. Find the closest existing code path.
3. Make the smallest safe change.
4. Verify the change.
5. Report what changed and what remains risky.
