# Employee Task Feedback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a deterministic employee-duty system that exposes each employee's real task, target, remaining time, and rest state, then drives gameplay contribution, scene position, and employee detail feedback from the same immutable snapshot.

**Architecture:** A pure task planner in Application receives store demand plus employee attendance and chooses duties from fixed role priorities. `BusinessSimulation` owns the per-minute orchestration and applies only the chosen duties; offline settlement already advances this same simulation pipeline. Rendering and Desktop consume the task snapshot without inventing business state, while Domain and persistence remain unchanged because priorities are role rules and current tasks are derived runtime state.

**Tech Stack:** .NET 10, C# 14, xUnit, WPF, SkiaSharp, CommunityToolkit.Mvvm.

---

## File structure

- Create `src/HajimaoDesktopShop.Application/Business/Employees/EmployeeTaskKind.cs`: finite duty vocabulary.
- Create `src/HajimaoDesktopShop.Application/Business/Employees/EmployeeTaskSnapshot.cs`: immutable task, target, remaining-time, and rest projection.
- Create `src/HajimaoDesktopShop.Application/Business/Employees/EmployeeTaskPriorityCatalog.cs`: one fixed ordered duty list per role.
- Create `src/HajimaoDesktopShop.Application/Business/Simulation/EmployeeTaskPlanner.cs`: pure deterministic decision function.
- Modify `src/HajimaoDesktopShop.Application/Business/Employees/EmployeeOperationsSnapshot.cs`: append task and priority data to each employee snapshot.
- Modify `src/HajimaoDesktopShop.Application/Business/Employees/EmployeeOperationsService.cs`: report non-mutating scheduled/rest availability and accept task projections when snapshotting.
- Modify `src/HajimaoDesktopShop.Application/Business/Simulation/BusinessSimulation.cs`: plan and apply checkout, cleaning, customer service, rest, and procurement-monitoring duties in the real Tick.
- Modify `src/HajimaoDesktopShop.Rendering/Interactions/BusinessShopEmployeeChoreography.cs`: derive positions from duties and targets.
- Modify `src/HajimaoDesktopShop.Rendering/Interactions/BusinessShopEmployeePose.cs`: retain the chosen duty alongside the pose.
- Modify `src/HajimaoDesktopShop.Desktop/ViewModels/Market/EmployeeCardViewModel.cs`: format task and priority projections.
- Modify `src/HajimaoDesktopShop.Desktop/ViewModels/Market/MarketViewModel.cs`: project live task data into object details.
- Modify `src/HajimaoDesktopShop.Desktop/Windows/ManagementWindow.xaml`: show task and priority on existing employee cards.
- Modify focused xUnit files in Application, Rendering, and Desktop before each production change.

### Task 1: Duty vocabulary and role priorities

**Files:**
- Create: `src/HajimaoDesktopShop.Application/Business/Employees/EmployeeTaskKind.cs`
- Create: `src/HajimaoDesktopShop.Application/Business/Employees/EmployeeTaskSnapshot.cs`
- Create: `src/HajimaoDesktopShop.Application/Business/Employees/EmployeeTaskPriorityCatalog.cs`
- Test: `tests/HajimaoDesktopShop.Application.Tests/Business/Employees/EmployeeTaskPriorityCatalogTests.cs`

- [ ] **Step 1: Write failing priority tests**

```csharp
[Theory]
[InlineData(EmployeeRole.Cashier, EmployeeTaskKind.Checkout)]
[InlineData(EmployeeRole.Restocker, EmployeeTaskKind.Restock)]
[InlineData(EmployeeRole.Cleaner, EmployeeTaskKind.Clean)]
[InlineData(EmployeeRole.SalesAssistant, EmployeeTaskKind.CustomerService)]
public void GetPriorities_starts_with_the_roles_primary_duty(
    EmployeeRole role,
    EmployeeTaskKind expected) =>
    Assert.Equal(expected, EmployeeTaskPriorityCatalog.GetPriorities(role)[0]);
```

- [ ] **Step 2: Run RED**

Run: `dotnet test tests/HajimaoDesktopShop.Application.Tests/HajimaoDesktopShop.Application.Tests.csproj -c Release --filter EmployeeTaskPriorityCatalogTests`

Expected: compilation fails because task types do not exist.

- [ ] **Step 3: Implement minimal immutable vocabulary**

```csharp
public enum EmployeeTaskKind { Idle, Rest, Checkout, Restock, Clean, CustomerService }

public sealed record EmployeeTaskSnapshot(
    EmployeeTaskKind Kind,
    string? TargetKey,
    string? TargetName,
    int? RemainingMinutes)
{
    public bool IsResting => Kind == EmployeeTaskKind.Rest;
}
```

`EmployeeTaskPriorityCatalog.GetPriorities(EmployeeRole)` returns cached read-only arrays with cashier→checkout, restocker/buyer→restock, cleaner→clean, sales assistant→customer service, and manager→checkout first. Every role ends with `Idle` as a deterministic fallback.

- [ ] **Step 4: Run GREEN and commit**

Run the focused test command; expect all priority tests to pass.

Commit: `git commit -am "feat: define employee duty priorities"`

### Task 2: Pure deterministic task planner

**Files:**
- Create: `src/HajimaoDesktopShop.Application/Business/Simulation/EmployeeTaskPlanner.cs`
- Test: `tests/HajimaoDesktopShop.Application.Tests/Business/Simulation/EmployeeTaskPlannerTests.cs`

- [ ] **Step 1: Write failing planner tests**

Cover these separate behaviors with one assertion focus per test:

```csharp
Assert.Equal(EmployeeTaskKind.Checkout, Plan(cashier, checkoutDemand).Kind);
Assert.Equal(EmployeeTaskKind.Rest, Plan(restingCashier, checkoutDemand).Kind);
Assert.Equal(EmployeeTaskKind.Restock, Plan(restocker, inboundOrder).Kind);
Assert.Equal(EmployeeTaskKind.Clean, Plan(cleaner, dirtyStore).Kind);
Assert.Equal(EmployeeTaskKind.CustomerService, Plan(assistant, serviceDemand).Kind);
Assert.Equal(EmployeeTaskKind.Checkout, Plan(managerOnly, checkoutDemand).Kind);
```

Also prove checkout and the selected inbound order are claimed by at most one employee, role specialists are considered before manager fallback, and input order does not change results.

- [ ] **Step 2: Run RED**

Run: `dotnet test tests/HajimaoDesktopShop.Application.Tests/HajimaoDesktopShop.Application.Tests.csproj -c Release --filter EmployeeTaskPlannerTests`

Expected: compilation fails because planner inputs and `Plan` do not exist.

- [ ] **Step 3: Implement the pure planner**

```csharp
internal static IReadOnlyDictionary<string, EmployeeTaskSnapshot> Plan(
    IReadOnlyList<EmployeeTaskWorker> workers,
    StoreTaskDemand demand)
```

Normalize workers into specialist-first, employee-ID order; map resting workers directly to `Rest`, unpaid workers to `Idle`, and paid workers to the first available task in `EmployeeTaskPriorityCatalog`. Checkout and one procurement target are exclusive; cleaning and customer service may be shared. Preserve demand target key/name and non-negative remaining minutes in the result.

- [ ] **Step 4: Run GREEN, refactor, and commit**

Run the focused tests twice: once after minimal implementation and once after extracting small claim helpers. Expect all planner tests to pass.

Commit: `git add ... && git commit -m "feat: plan deterministic employee duties"`

### Task 3: Apply duties in the real simulation and expose snapshots

**Files:**
- Modify: `src/HajimaoDesktopShop.Application/Business/Employees/EmployeeOperationsSnapshot.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Employees/EmployeeOperationsService.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Simulation/BusinessSimulation.cs`
- Test: `tests/HajimaoDesktopShop.Application.Tests/Business/Simulation/BusinessSimulationTests.cs`
- Test: `tests/HajimaoDesktopShop.Application.Tests/Business/Offline/OfflineSettlementServiceTests.cs`

- [ ] **Step 1: Write failing simulation tests**

Add tests proving: an active checkout exposes product target and remaining minutes; an off-shift or exhausted employee exposes `Rest`; a cleaner's `Clean` assignment raises cleanliness; a cashier serving checkout is excluded from customer-service contribution; a manager covers checkout if no cashier is available; and a restocker exposes the nearest pending order target and its real remaining delivery time.

- [ ] **Step 2: Run RED**

Run the two focused classes. Expected failures: snapshots have no task properties and operations are still selected directly by role.

- [ ] **Step 3: Project tasks from EmployeeOperationsService**

Append optional immutable members to `EmployeeOperationsEmployeeSnapshot` so existing call sites remain source-compatible:

```csharp
EmployeeTaskSnapshot? CurrentTask = null,
IReadOnlyList<EmployeeTaskKind>? TaskPriorities = null
```

Add an internal attendance query that distinguishes ready, resting, and unpaid without exposing WPF or persistence types. Ensure every snapshot publishes a non-null task and the cached read-only priority list.

- [ ] **Step 4: Route BusinessSimulation through the planner**

For each store minute: resolve attendance/payroll, construct demand from active checkout/queue, cleanliness, visitors, and the nearest pending procurement order; call the planner; apply cleaning only to `Clean`, service only to `CustomerService`, and checkout only to `Checkout`; then refresh the projected task state after mutations. The initial snapshot uses the same decision path without advancing or charging time.

- [ ] **Step 5: Prove offline parity**

Create identical seeded simulations, advance one through `AdvanceRealSeconds` and the other through `OfflineSettlementService.Settle`, then compare business totals plus every employee task kind/target/remaining-time tuple.

- [ ] **Step 6: Run GREEN and commit**

Run Application tests; expect 0 failures.

Commit: `git add ... && git commit -m "feat: drive store operations from employee duties"`

### Task 4: Task-driven scene choreography

**Files:**
- Modify: `src/HajimaoDesktopShop.Rendering/Interactions/BusinessShopEmployeeChoreography.cs`
- Modify: `src/HajimaoDesktopShop.Rendering/Interactions/BusinessShopEmployeePose.cs`
- Test: `tests/HajimaoDesktopShop.Rendering.Tests/Interactions/BusinessShopEmployeeChoreographyTests.cs`
- Test: `tests/HajimaoDesktopShop.Rendering.Tests/Interactions/BusinessShopInteractionMapTests.cs`

- [ ] **Step 1: Write failing pose tests**

Prove the same employee appears at the register for checkout, moves toward ambient/chilled/frozen shelf anchors for restocking, traverses the floor while cleaning, stays in the rest area while resting, and retains 24 distinct logical position steps where the route spans enough pixels. Also prove hit targets follow the task-driven pose.

- [ ] **Step 2: Run RED**

Run: `dotnet test tests/HajimaoDesktopShop.Rendering.Tests/HajimaoDesktopShop.Rendering.Tests.csproj -c Release --filter "BusinessShopEmployeeChoreographyTests|BusinessShopInteractionMapTests"`

Expected: role-only choreography produces the same route regardless of task.

- [ ] **Step 3: Implement minimal task choreography**

Use `CurrentTask.Kind` and `TargetKey` only. Checkout anchors at the register; restock targets the matching shelf; clean traverses the floor; customer service patrols the customer aisle; rest holds a stable staff-area pose. Continue using `CharacterMotion` and the existing 24-frame atlas API; add no speed setting or business mutation.

- [ ] **Step 4: Run GREEN and commit**

Run all Rendering tests; expect 0 failures.

Commit: `git add ... && git commit -m "feat: animate employees by current duty"`

### Task 5: Employee feedback in existing UI

**Files:**
- Modify: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/EmployeeCardViewModel.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/MarketViewModel.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/Windows/ManagementWindow.xaml`
- Test: `tests/HajimaoDesktopShop.Desktop.Tests/ViewModels/Market/EmployeeManagementViewModelTests.cs`
- Test: `tests/HajimaoDesktopShop.Desktop.Tests/ViewModels/Market/MarketViewModelTests.cs`

- [ ] **Step 1: Write failing view-model tests**

Assert Chinese text for checkout target plus remaining time, resting state, and ordered role priorities. Assert a selected employee object's live status updates after the next snapshot without reselection.

- [ ] **Step 2: Run RED**

Run the two focused Desktop test classes. Expected: task/priority properties do not exist and detail text contains only condition/shift.

- [ ] **Step 3: Implement projection-only UI**

Add `TaskText` and `PriorityText` to the card, format snapshots through a small private mapping, include task in `StatusText`, and include priorities in `ActionHintText`. Add two text rows to the existing employee card template. Do not add a speed control, timer, repository call, or task decision to WPF.

- [ ] **Step 4: Run GREEN and commit**

Run all Desktop tests; expect 0 failures.

Commit: `git add ... && git commit -m "feat: show live employee duties"`

### Task 6: Version, report, verification, and delivery

**Files:**
- Modify: `Directory.Build.props`
- Modify: `CHANGELOG.md`
- Modify: `docs/roadmap.md`
- Create: `docs/progress/v0.1.15-employee-task-feedback.md`
- Create: desktop TXT report through the repository report workflow.

- [ ] **Step 1: Update release metadata**

Set `VersionPrefix` to `0.1.15`. Document completed rules, red/green evidence, known gaps, fixed real `1x`, no schema change, and the next `0.1.16` plan.

- [ ] **Step 2: Run fresh full verification**

Run:

```powershell
dotnet test HajimaoDesktopShop.slnx -c Release --nologo
dotnet build HajimaoDesktopShop.slnx -c Release --no-restore --nologo
```

Expected: all test projects pass; build exits 0 with 0 warnings and 0 errors.

- [ ] **Step 3: Package and smoke-test**

Use the existing portable-only release script for `0.1.15`, launch the packaged executable with an isolated data directory, wait long enough for SQLite/log creation, verify the process remains responsive, then terminate only that verified process and remove smoke data.

- [ ] **Step 4: Publish and clean**

Use the repository's fast GitHub workflow to push one reviewed branch, merge one PR, publish one release, and retain only the final portable ZIP plus checksum under `artifacts/release/0.1.15`. Remove worktrees, `bin`, `obj`, test results, logs, databases, and temporary captures after final verification.

## Self-review

- Spec coverage: tasks 1–3 cover real task/target/time/rest and role priorities; Offline task 3 proves one decision pipeline; task 4 drives positions; task 5 displays both task and priority; task 6 covers versioning, report, runnable delivery, and cleanup.
- Placeholder scan: no unfinished markers remain; each behavior-changing task includes a precise RED command, production contract, GREEN command, and commit.
- Type consistency: `EmployeeTaskKind`, `EmployeeTaskSnapshot`, `CurrentTask`, `TaskPriorities`, `EmployeeTaskWorker`, and `StoreTaskDemand` are introduced before their consumers and retain identical names throughout.
- Boundary check: no Domain, SQLite schema, or save migration is needed because current assignments are deterministic projections of already-saved runtime state and role priorities are static game rules.
