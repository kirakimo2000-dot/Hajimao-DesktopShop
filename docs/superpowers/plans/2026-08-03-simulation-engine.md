# Deterministic Shop Simulation Implementation Plan

> **Execution note:** Execute inline with strict RED → GREEN → REFACTOR. Do not add wall-clock timers or WPF dependencies here; Desktop will host the engine later. Persist every checkpoint in `progress.md` because this directory has no Git history.

**Goal:** Deliver v0.3.0 as a deterministic, testable simulation engine where game time advances at pause/1x/2x/4x, customers visibly traverse the complete purchase state machine, and cashier/restocker employees consume explicit task queues.

**Architecture:** `SimulationClock` converts one real-second pulse into zero, one, two, or four pure ticks. `ShopSimulation` owns transient actors and queues but delegates every economic mutation to `ShopGameService`. `IRandomSource` makes spawning and product choice deterministic in tests. Snapshots are detached records shared safely with Rendering and Desktop.

**Gameplay contract:** One simulation tick equals one game minute. A customer advances at most one state per tick. Checkout is FIFO and only succeeds through the cashier queue. Restocking is explicit work queued for the restocker; no employee scans every product each frame.

**Tech Stack:** .NET 10, C# 14, xUnit; no UI or timer framework dependencies.

---

### Task 1: Clock and speed contract

**Files:**
- Create: `tests/HajimaoDesktopShop.Application.Tests/Simulation/SimulationClockTests.cs`
- Create: `src/HajimaoDesktopShop.Application/Simulation/SimulationSpeed.cs`
- Create: `src/HajimaoDesktopShop.Application/Simulation/SimulationClock.cs`

- [x] Test that paused/1x/2x/4x execute exactly 0/1/2/4 ticks and advance matching game minutes.
- [x] Test invalid enum values are rejected without changing the current speed.
- [x] Run selected tests and observe missing-type RED.
- [x] Implement the smallest synchronous clock; run selected and full Application regression.

### Task 2: Actor and task snapshot contracts

**Files:**
- Create: `src/HajimaoDesktopShop.Application/Simulation/Customers/CustomerState.cs`
- Create: `src/HajimaoDesktopShop.Application/Simulation/Customers/CustomerSnapshot.cs`
- Create: `src/HajimaoDesktopShop.Application/Simulation/Employees/EmployeeRole.cs`
- Create: `src/HajimaoDesktopShop.Application/Simulation/Employees/EmployeeState.cs`
- Create: `src/HajimaoDesktopShop.Application/Simulation/Employees/EmployeeSnapshot.cs`
- Create: `src/HajimaoDesktopShop.Application/Simulation/SimulationSnapshot.cs`
- Create: `src/HajimaoDesktopShop.Application/Simulation/IRandomSource.cs`

- [x] Define explicit customer states in acceptance tests: Entering, SeekingProduct, Queueing, CheckingOut, Leaving.
- [x] Define Cashier and Restocker roles with Idle/Working states in the snapshot contract.
- [x] Require all snapshot records to be immutable and free of Domain mutable objects.

### Task 3: Customer state machine and cashier FIFO

**Files:**
- Create: `tests/HajimaoDesktopShop.Application.Tests/Simulation/ShopSimulationCustomerTests.cs`
- Create: `src/HajimaoDesktopShop.Application/Simulation/ShopSimulation.cs`

- [x] Seed inventory through `ShopGameService` and use a scripted random source.
- [x] Test a customer traverses Entering → SeekingProduct → Queueing → CheckingOut → Leaving, then a successful sale changes cash and inventory.
- [x] Test customers do not buy out-of-stock products and checkout order remains FIFO.
- [x] Run RED, implement one-state-per-tick transitions and an explicit checkout queue, then run GREEN.

### Task 4: Restocker task queue

**Files:**
- Create: `tests/HajimaoDesktopShop.Application.Tests/Simulation/ShopSimulationRestockerTests.cs`
- Modify: `src/HajimaoDesktopShop.Application/Simulation/ShopSimulation.cs`

- [x] Test `QueueRestock(productId, quantity)` creates work without immediately changing inventory.
- [x] Test the restocker completes queued tasks in order through `ShopGameService.PurchaseStock`.
- [x] Test failed purchase leaves the employee idle and exposes the failure in the scene snapshot.
- [x] Run RED, implement minimal queue processing, run selected and full Application regression.

### Task 5: Version checkpoint

**Files:**
- Modify: `Directory.Build.props`
- Modify: `task_plan.md`
- Modify: `findings.md`
- Modify: `progress.md`
- Modify: `CHANGELOG.md`
- Create: `docs/progress/v0.3.0-phase-3.md`

- [x] Run all tests and require zero failures.
- [x] Build the complete solution and require 0 warnings / 0 errors.
- [x] Audit direct/transitive NuGet packages and inspect workspace hygiene.
- [x] Mark Phase 3 complete only with fresh evidence, summarize progress, and start the dual-window plan.
