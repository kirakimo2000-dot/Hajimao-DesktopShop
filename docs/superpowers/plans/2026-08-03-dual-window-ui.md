# Playable Dual-Window UI Implementation Plan

> **Execution note:** Follow TDD for ViewModel behavior and build-smoke RED/GREEN for WPF integration. Window code-behind may contain only OS/window mechanics; all gameplay commands flow through Application services.

**Goal:** Deliver v0.4.0 as the first locally playable WPF build: a 420×280 desktop shop continues simulating, while a 1180×720 management window lets the player restock, change prices, and control speed.

**Architecture:** Desktop is the composition root. A shared `GameViewModel` consumes detached `SimulationSnapshot` values and sends commands to `ShopGameService`/`ShopSimulation`. `SimulationLoop` owns the background pulse. Both windows reuse `ShopSceneControl`. Window services isolate drag, snap, lock and Win32 click-through mechanics.

**Visual system:** Dark warm pixel dashboard, 4/8 px spacing, hard 2 px borders, stable 150–200 ms hover/focus feedback, visible labels (no emoji icons), text plus color for stock/errors. Page overrides live under `design-system/hajimao-desktop-shop/pages/`.

---

### Task 1: Testable presentation model

**Files:**
- Create: `tests/HajimaoDesktopShop.Desktop.Tests/HajimaoDesktopShop.Desktop.Tests.csproj`
- Modify: `HajimaoDesktopShop.slnx`
- Create: `tests/HajimaoDesktopShop.Desktop.Tests/ViewModels/GameViewModelTests.cs`
- Create: `src/HajimaoDesktopShop.Desktop/ViewModels/GameViewModel.cs`
- Create: `src/HajimaoDesktopShop.Desktop/ViewModels/ProductItemViewModel.cs`
- Create: `src/HajimaoDesktopShop.Desktop/ViewModels/CustomerVisualViewModel.cs`
- Create: `src/HajimaoDesktopShop.Desktop/ViewModels/EmployeeItemViewModel.cs`

- [x] Test refresh maps cash, time, customer/employee state, stock text and finance totals.
- [x] Test restock, price ±10 cents and speed commands call the application boundary and update feedback.
- [x] Run Desktop tests and observe missing presentation types.
- [x] Implement detached UI rows and commands; run selected and full Desktop tests.

### Task 2: Runtime adapters and composition

**Files:**
- Create: `tests/HajimaoDesktopShop.Infrastructure.Tests/Simulation/SeededRandomSourceTests.cs`
- Create: `src/HajimaoDesktopShop.Infrastructure/Simulation/SeededRandomSource.cs`
- Create: `src/HajimaoDesktopShop.Desktop/Services/SimulationLoop.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/App.xaml`
- Modify: `src/HajimaoDesktopShop.Desktop/App.xaml.cs`

- [x] Add RED tests for a repeatable seeded random adapter and stoppable background simulation loop.
- [x] Compose JSON catalog, new game, simulation, background one-second loop and UI refresh timer in `App`.
- [x] Show a clear startup error and exit if configuration cannot load.

### Task 3: Semantic theme and reusable shop scene

**Files:**
- Create: `src/HajimaoDesktopShop.Desktop/Themes/Colors.xaml`
- Create: `src/HajimaoDesktopShop.Desktop/Themes/Controls.xaml`
- Create: `src/HajimaoDesktopShop.Desktop/Controls/ShopSceneControl.xaml`
- Create: `src/HajimaoDesktopShop.Desktop/Controls/ShopSceneControl.xaml.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/App.xaml`

- [x] Centralize all color, spacing, button, focus and typography resources.
- [x] Draw three shelf zones, counter, cashier/restocker and state-positioned customers with integer-aligned WPF shapes.
- [x] Reuse exactly the same scene control in both windows.

### Task 4: DesktopShopWindow and ManagementWindow

**Files:**
- Delete: `src/HajimaoDesktopShop.Desktop/MainWindow.xaml`
- Delete: `src/HajimaoDesktopShop.Desktop/MainWindow.xaml.cs`
- Create: `src/HajimaoDesktopShop.Desktop/Windows/DesktopShopWindow.xaml`
- Create: `src/HajimaoDesktopShop.Desktop/Windows/DesktopShopWindow.xaml.cs`
- Create: `src/HajimaoDesktopShop.Desktop/Windows/ManagementWindow.xaml`
- Create: `src/HajimaoDesktopShop.Desktop/Windows/ManagementWindow.xaml.cs`
- Create: `src/HajimaoDesktopShop.Desktop/Services/WindowInteractionService.cs`

- [x] Desktop window is transparent, borderless, topmost, draggable when unlocked, corner-snapped, lockable and click-through capable.
- [x] Management window shows navigation, shared live scene, product cards, alerts/tasks, finance and pause/1x/2x/4x controls.
- [x] All controls have text labels, tooltips/accessibility names, keyboard focus and visible hover/pressed states.
- [x] Build the full solution and run a launch smoke test.

### Task 5: Version checkpoint

**Files:**
- Modify: `Directory.Build.props`
- Modify: `task_plan.md`
- Modify: `findings.md`
- Modify: `progress.md`
- Modify: `CHANGELOG.md`
- Create: `docs/progress/v0.4.0-phase-4.md`

- [x] Run all automated tests and full build.
- [x] Launch both windows, exercise restock/price/speed, and capture visual evidence.
- [x] Audit packages and workspace hygiene.
- [x] Mark Phase 4 complete only with fresh evidence; summarize and begin SQLite/Skia content integration.
