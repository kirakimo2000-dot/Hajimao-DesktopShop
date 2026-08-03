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
