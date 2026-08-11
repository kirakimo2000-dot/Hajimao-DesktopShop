# Project instructions

- Target Windows 10 2004 or newer and .NET 10; the desktop UI uses WPF.
- Product identity: a traditional desktop-placement incremental shop-management game, not a conventional full-screen management game.
- Visual identity: pixel art with crisp integer scaling, readable silhouettes, restrained animation, and no texture smoothing for scene sprites.

## Architecture

- Keep business rules out of XAML code-behind and control event handlers.
- Domain must not reference WPF, SQLite, SkiaSharp, Serilog, or other infrastructure packages.
- Application orchestrates use cases, simulation ticks, and ports; it references Domain only.
- Infrastructure implements persistence, configuration, repositories, and logging.
- Rendering owns SkiaSharp scene drawing and must not own business state.
- Desktop composes dependencies and owns windows, view models, controls, and themes.
- Every module must have one responsibility and communicate through explicit interfaces, commands, queries, events, or immutable snapshots.
- Do not use mutable global state, service locators, UI-owned business state, direct repository calls from views, or circular project references.
- Prefer vertical feature folders inside the established layers; do not create a single catch-all manager, service, or utility class.

## Delivery

- Add or change behavior with tests first. Run `dotnet test HajimaoDesktopShop.slnx` before completion.
- Phase 1 is limited to one convenience store, ten products, three shelf types, customer purchase flow, cashier/restocker roles, finance, SQLite saves, and two main windows.
- Deliver in small, runnable versions. The active version is defined by `VersionPrefix` in `Directory.Build.props`.
- Every version must update `CHANGELOG.md` and add a stage report under `docs/progress/` containing completed work, verification evidence, known gaps, and the next plan.
- Do not silently change product rules, visual direction, save compatibility, simulation timing, window behavior, or the active milestone. Ask the user before implementation when these requirements are missing or ambiguous.
- At the end of each stage, remove generated build outputs, temporary captures, unused assets, obsolete plans, local databases, and logs. Never delete source or user assets as cleanup.
- Before handoff, verify no unintended `bin/`, `obj/`, `TestResults/`, `logs/`, temporary files, or generated databases remain in the workspace.

## Lean execution workflow

- Work in the primary agent by default. Do not spawn subagents unless the user explicitly requests them or one bounded, independent task has a clear wall-clock advantage.
- Unless the user explicitly requests a larger team, a version may use at most one implementation agent and one combined reviewer. Do not run separate specification, quality, fix, and final-review agents for each module.
- During implementation, run only the smallest relevant tests. A subagent or reviewer must not run the full solution unless it is the single designated release verifier.
- Run the full solution once on the unchanged release candidate immediately before publication. Treat GitHub CI as the independent second gate; do not repeat the same local full suite while CI is evaluating that commit.
- If no code or build input changed after a passing verification, reuse that evidence instead of rerunning it. Documentation-only link fixes require only their focused checks.
- Produce one changelog/progress update, one stage review, one GitHub publication, and one cleanup pass per version.
- Plans must default to direct execution or `executing-plans`; they must not require `subagent-driven-development` for ordinary iteration.
- Extra review rounds require a concrete high-risk reason such as save migration, destructive cleanup, security/privacy boundaries, or a reproduced runtime failure. State that reason before starting the extra review.
