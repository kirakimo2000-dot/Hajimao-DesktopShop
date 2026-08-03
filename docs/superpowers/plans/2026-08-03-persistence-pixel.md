# Persistence and Pixel Presentation Implementation Plan

> **Execution note:** Apply RED → GREEN → REFACTOR to every state-restoration and rendering behavior. Persistence is an adapter behind Application contracts; WPF remains the composition root.

**Goal:** Deliver v0.5.0 with automatic SQLite saves, exact simulation restoration, desktop placement restoration, a reusable SkiaSharp pixel scene, and basic muted-by-default-capable sound feedback.

**Architecture:** Application owns versioned immutable save contracts and capture/restore behavior. Infrastructure stores one atomic JSON save payload inside a migrated SQLite schema and stores window placement separately. Rendering consumes only detached `SimulationSnapshot` values. Desktop coordinates timers, windows and sound without owning gameplay rules.

---

### Task 1: Restorable domain and simulation state

**Files:**
- Modify: `src/HajimaoDesktopShop.Domain/Shops/Shop.cs`
- Modify: `src/HajimaoDesktopShop.Domain/Inventory/InventorySlot.cs`
- Create: `src/HajimaoDesktopShop.Application/Persistence/GameSaveData.cs`
- Modify: `src/HajimaoDesktopShop.Application/Game/ShopGameService.cs`
- Modify: `src/HajimaoDesktopShop.Application/Simulation/SimulationClock.cs`
- Modify: `src/HajimaoDesktopShop.Application/Simulation/ShopSimulation.cs`
- Add tests under Domain/Application test projects.

- [x] RED: restoring cash, totals, price and quantity is unsupported.
- [x] GREEN: restore validated shop state without replaying transactions.
- [x] RED: simulation time, speed, customers, employee work and queues cannot round-trip.
- [x] GREEN: capture and restore the full deterministic simulation state.

### Task 2: SQLite schema, migrations and repository

**Files:**
- Create: `src/HajimaoDesktopShop.Application/Persistence/IGameSaveStore.cs`
- Create: `src/HajimaoDesktopShop.Infrastructure/Persistence/SqliteGameSaveStore.cs`
- Add: `tests/HajimaoDesktopShop.Infrastructure.Tests/Persistence/SqliteGameSaveStoreTests.cs`

- [x] RED: new database initialization, round-trip and v0→v1 migration tests fail.
- [x] GREEN: transactional save/load and idempotent schema migration pass.
- [x] Reject unsupported future save schemas with a clear message.

### Task 3: Desktop autosave and placement restore

**Files:**
- Create: `src/HajimaoDesktopShop.Desktop/Services/AutosaveCoordinator.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/App.xaml.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/Windows/DesktopShopWindow.xaml.cs`
- Add Desktop tests.

- [x] RED/GREEN: coordinator coalesces periodic saves and flushes on exit.
- [x] Load save before composing the game; save every 5 seconds and on shutdown.
- [x] Restore on-screen desktop left/top and lock state; never persist click-through as enabled.

### Task 4: SkiaSharp pixel scene

**Files:**
- Create: `tests/HajimaoDesktopShop.Rendering.Tests/`
- Create renderer files under `src/HajimaoDesktopShop.Rendering/`.
- Modify: `src/HajimaoDesktopShop.Desktop/Controls/ShopSceneControl.xaml(.cs)`

- [x] RED/GREEN: renderer produces a fixed logical 420×180 scene with deterministic pixel samples.
- [x] Draw floor, three shelf zones, counter, two employees, customers and state markers.
- [x] Use nearest-neighbor/integer coordinates and reuse the renderer in both windows.

### Task 5: Basic sound and v0.5 checkpoint

**Files:**
- Create Desktop sound service and generated local WAV assets.
- Modify ViewModel/App wiring and phase documentation.

- [x] Play short local feedback for restock, price change and completed sale; expose mute.
- [x] Run all tests, full build, package audit and workspace hygiene.
- [x] Launch, save, restart, verify exact restoration and visually inspect both windows.
- [x] Publish v0.5.0 phase summary and start final Demo acceptance.
