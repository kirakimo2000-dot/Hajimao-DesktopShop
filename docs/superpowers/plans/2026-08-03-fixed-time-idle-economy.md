# Fixed-Time Idle Economy Implementation Plan

> **Superseded:** This plan was replaced by `2026-08-03-v0.1.0-gameplay-foundation.md` after the project version was rebased to 0.1.0 and the gameplay foundation scope expanded.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver Hajimao 1.1.0 with fixed real-time 1x operation, compatible v1 save migration, bounded offline settlement, and an explainable return report.

**Architecture:** Remove player speed from Application contracts instead of merely hiding WPF controls. Persist current saves as schema v2; Infrastructure owns conversion of schema v1 JSON and discards the legacy speed value. A pure Application idle-settlement service advances the existing deterministic simulation in a bounded batch, while Desktop only coordinates startup and presents the resulting immutable report.

**Tech Stack:** .NET 10, C# 14, WPF, CommunityToolkit.Mvvm, SQLite, System.Text.Json, xUnit.

---

## File map

| Responsibility | Files |
| --- | --- |
| Fixed-time contract | Modify `src/HajimaoDesktopShop.Application/Simulation/SimulationClock.cs`, `ShopSimulation.cs`, `SimulationSnapshot.cs`; delete `SimulationSpeed.cs` |
| Current save schema | Modify `src/HajimaoDesktopShop.Application/Persistence/GameSaveData.cs` |
| Legacy save conversion | Create `src/HajimaoDesktopShop.Infrastructure/Persistence/LegacyGameSaveV1.cs`; modify `SqliteGameSaveStore.cs` |
| Idle settlement | Create `src/HajimaoDesktopShop.Application/Idle/IdleSettlementPolicy.cs`, `IdleSettlementReport.cs`, `IdleSettlementService.cs` |
| Desktop presentation | Create `src/HajimaoDesktopShop.Desktop/ViewModels/IdleSettlementReportViewModel.cs`, `Windows/IdleSettlementWindow.xaml`, `Windows/IdleSettlementWindow.xaml.cs`; modify `App.xaml.cs`, `GameViewModel.cs`, `ManagementWindow.xaml` |
| Automated verification | Modify affected tests and create `tests/HajimaoDesktopShop.Application.Tests/Idle/IdleSettlementServiceTests.cs` |
| Version/docs | Modify `Directory.Build.props`, `README.md`, `CHANGELOG.md`, `docs/architecture/technical-foundation.md`, `docs/roadmap.md`; create `docs/progress/v1.1.0-fixed-time-idle.md` |

The repository is not currently a Git repository, so this plan uses verified file checkpoints instead of fictional commit commands. If Git is initialized before execution, create one commit after each task passes its listed tests.

### Task 1: Remove speed from public simulation contracts

**Files:**
- Create: `tests/HajimaoDesktopShop.Application.Tests/Simulation/FixedTimeContractTests.cs`
- Modify: `tests/HajimaoDesktopShop.Application.Tests/Simulation/SimulationClockTests.cs`
- Modify: `tests/HajimaoDesktopShop.Application.Tests/Simulation/ShopSimulationBatchTests.cs`
- Modify: `src/HajimaoDesktopShop.Application/Simulation/SimulationClock.cs`
- Modify: `src/HajimaoDesktopShop.Application/Simulation/ShopSimulation.cs`
- Modify: `src/HajimaoDesktopShop.Application/Simulation/SimulationSnapshot.cs`
- Modify: `src/HajimaoDesktopShop.Application/Persistence/GameSaveData.cs`
- Delete: `src/HajimaoDesktopShop.Application/Simulation/SimulationSpeed.cs`
- Modify: `tests/HajimaoDesktopShop.Application.Tests/Persistence/GameSaveDataTests.cs`
- Modify: `tests/HajimaoDesktopShop.Desktop.Tests/Services/AutosaveCoordinatorTests.cs`
- Modify: `tests/HajimaoDesktopShop.Infrastructure.Tests/Persistence/SqliteGameSaveStoreTests.cs`
- Modify: `tests/HajimaoDesktopShop.Rendering.Tests/DesktopShopRendererTests.cs`
- Modify: `tests/HajimaoDesktopShop.Rendering.Tests/ShopSceneRendererTests.cs`

- [ ] **Step 1: Add a failing API contract test**

```csharp
[Fact]
public void PublicContracts_DoNotExposePlayerControlledSpeed()
{
    Assert.Null(typeof(ShopSimulation).GetMethod("SetSpeed"));
    Assert.Null(typeof(SimulationSnapshot).GetProperty("Speed"));
    Assert.DoesNotContain(
        typeof(SimulationSaveData).GetProperties(),
        property => property.Name == "Speed");
}
```

- [ ] **Step 2: Run the contract test and verify RED**

```powershell
dotnet test tests/HajimaoDesktopShop.Application.Tests --filter FullyQualifiedName~FixedTimeContractTests
```

Expected: FAIL because all three speed members still exist.

- [ ] **Step 3: Reduce `SimulationClock` to a fixed one-tick clock**

```csharp
public sealed class SimulationClock
{
    public SimulationClock(long gameMinute = 0)
    {
        if (gameMinute < 0) throw new ArgumentOutOfRangeException(nameof(gameMinute));
        GameMinute = gameMinute;
    }

    public long GameMinute { get; private set; }

    public void AdvanceRealSecond(Action processTick)
    {
        ArgumentNullException.ThrowIfNull(processTick);
        processTick();
        GameMinute++;
    }
}
```

- [ ] **Step 4: Remove speed from Application state**

Delete `SimulationSpeed.cs`; remove `ShopSimulation.SetSpeed`; construct `SimulationClock` with only `GameMinute`; remove `Speed` from `SimulationSnapshot` and `SimulationSaveData`; adjust snapshot/save constructors and all production/test call sites.

- [ ] **Step 5: Replace speed tests with fixed-time invariants**

```csharp
[Fact]
public void AdvanceRealSecond_ExecutesExactlyOneTick()
{
    var calls = 0;
    var clock = new SimulationClock();
    clock.AdvanceRealSecond(() => calls++);
    Assert.Equal(1, calls);
    Assert.Equal(1, clock.GameMinute);
}
```

Retain `AdvanceRealSeconds(30)` coverage and assert exactly 30 game minutes and 30 simulation ticks.

- [ ] **Step 6: Run Application, Rendering and compilation-dependent tests**

```powershell
dotnet test tests/HajimaoDesktopShop.Application.Tests -c Release
dotnet test tests/HajimaoDesktopShop.Rendering.Tests -c Release
```

Expected: both projects PASS; no remaining production reference to `SimulationSpeed`.

### Task 2: Migrate schema v1 saves to fixed-time schema v2

**Files:**
- Create: `src/HajimaoDesktopShop.Infrastructure/Persistence/LegacyGameSaveV1.cs`
- Modify: `src/HajimaoDesktopShop.Infrastructure/Persistence/SqliteGameSaveStore.cs`
- Modify: `tests/HajimaoDesktopShop.Infrastructure.Tests/Persistence/SqliteGameSaveStoreTests.cs`
- Modify: `tests/HajimaoDesktopShop.Application.Tests/Persistence/GameSaveDataTests.cs`

- [ ] **Step 1: Set current save schema to 2 and add a failing legacy migration test**

Build a temporary v1 database with `PRAGMA user_version = 1`, `schema_version = 1`, and a payload containing `"speed": 4`. Load it through `SqliteGameSaveStore`, then assert:

```csharp
Assert.Equal(2, migrated!.SchemaVersion);
Assert.Equal(88, migrated.Simulation.GameMinute);
Assert.Equal(7, migrated.Shop.Products.Single().Quantity);

await using var verify = new SqliteConnection($"Data Source={database.Path};Pooling=False");
await verify.OpenAsync();
await using var payloadCommand = verify.CreateCommand();
payloadCommand.CommandText = "SELECT payload_json FROM game_save WHERE slot = 1;";
var payload = Assert.IsType<string>(await payloadCommand.ExecuteScalarAsync());
Assert.DoesNotContain("speed", payload, StringComparison.OrdinalIgnoreCase);
```

- [ ] **Step 2: Run the migration test and verify RED**

```powershell
dotnet test tests/HajimaoDesktopShop.Infrastructure.Tests --filter FullyQualifiedName~VersionOne
```

Expected: FAIL because version 1 is rejected when current schema is 2.

- [ ] **Step 3: Add an Infrastructure-only legacy DTO**

```csharp
internal sealed record LegacySimulationSaveDataV1(
    long GameMinute,
    int Speed,
    long Tick,
    long NextCustomerId,
    int CompletedSales,
    IReadOnlyList<CustomerSaveData> Customers,
    IReadOnlyList<long> CheckoutQueue,
    long? CashierCustomerId,
    IReadOnlyList<RestockTaskSaveData> RestockQueue,
    RestockTaskSaveData? ActiveRestockTask,
    string? LastRestockFailure);
```

Define the matching legacy root/shop records in the same file. Convert every field except `Speed` into current `GameSaveData`.

- [ ] **Step 4: Implement transactional `MigrateFromOneToTwoAsync`**

Read the slot-1 payload, deserialize the v1 DTO, construct schema-v2 data, update `game_save.schema_version`, `payload_json`, and `saved_at_utc`, then set `PRAGMA user_version = 2` in the same transaction. Change startup migration sequencing to `0→1→2` for new databases and `1→2` for existing databases.

- [ ] **Step 5: Cover all legacy speed values**

Use test data for `0`, `1`, `2`, and `4`; each must produce the same schema-v2 fixed-time state. A malformed payload must roll back without changing `PRAGMA user_version` or the original row.

- [ ] **Step 6: Run persistence tests**

```powershell
dotnet test tests/HajimaoDesktopShop.Infrastructure.Tests -c Release
dotnet test tests/HajimaoDesktopShop.Application.Tests -c Release
```

Expected: all tests PASS; both new and migrated databases report `user_version = 2`.

### Task 3: Remove speed controls and presentation state

**Files:**
- Modify: `src/HajimaoDesktopShop.Desktop/Windows/ManagementWindow.xaml`
- Modify: `src/HajimaoDesktopShop.Desktop/ViewModels/GameViewModel.cs`
- Modify: `tests/HajimaoDesktopShop.Desktop.Tests/ViewModels/GameViewModelTests.cs`

- [ ] **Step 1: Add a failing ViewModel surface test**

```csharp
[Fact]
public void ViewModel_DoesNotExposePlayerSpeedCommand()
{
    Assert.Null(typeof(GameViewModel).GetProperty("SetSpeedCommand"));
    Assert.Null(typeof(GameViewModel).GetProperty("CurrentSpeedText"));
}
```

- [ ] **Step 2: Run the test and verify RED**

```powershell
dotnet test tests/HajimaoDesktopShop.Desktop.Tests --filter FullyQualifiedName~DoesNotExposePlayerSpeedCommand
```

Expected: FAIL because both properties exist.

- [ ] **Step 3: Remove speed state and commands from `GameViewModel`**

Delete `_currentSpeed`, `CurrentSpeed`, `CurrentSpeedText`, `SetSpeedCommand`, `SetSpeed`, and `FormatSpeed`. Preserve lock and click-through coverage by renaming the combined test to `WindowCommands_UpdatePresentationState`.

- [ ] **Step 4: Replace the management header controls**

Remove the simulation XML namespace and all four speed buttons. Use a non-interactive status label:

```xml
<Border Grid.Column="2" Style="{DynamicResource PixelPanel}" Padding="10,6">
    <TextBlock Text="现实时间经营 · 持续 1x"
               Foreground="{DynamicResource Brush.TextMuted}"
               AutomationProperties.Name="现实时间固定经营" />
</Border>
```

- [ ] **Step 5: Run Desktop tests and inspect compiled XAML**

```powershell
dotnet test tests/HajimaoDesktopShop.Desktop.Tests -c Release
dotnet build src/HajimaoDesktopShop.Desktop -c Release
```

Expected: PASS with zero warnings/errors; management XAML contains no command parameter for speed.

### Task 4: Add bounded, explainable offline settlement

**Files:**
- Create: `src/HajimaoDesktopShop.Application/Idle/IdleSettlementPolicy.cs`
- Create: `src/HajimaoDesktopShop.Application/Idle/IdleSettlementReport.cs`
- Create: `src/HajimaoDesktopShop.Application/Idle/IdleSettlementService.cs`
- Create: `tests/HajimaoDesktopShop.Application.Tests/Idle/IdleSettlementServiceTests.cs`

- [ ] **Step 1: Write failing duration and economy tests**

Cover: negative/future clock produces zero; 59.9 seconds floors to 59; 12 hours clamps to 8 hours; settlement advances game time and sales through the same `ShopSimulation`; report deltas equal before/after snapshots; stockouts naturally stop revenue.

```csharp
var report = service.Settle(simulation, savedAt, savedAt.AddHours(12));
Assert.Equal(TimeSpan.FromHours(12), report.ActualAwayDuration);
Assert.Equal(TimeSpan.FromHours(8), report.SettledDuration);
Assert.Equal(28_800, report.ProcessedRealSeconds);
```

- [ ] **Step 2: Run idle tests and verify RED**

```powershell
dotnet test tests/HajimaoDesktopShop.Application.Tests --filter FullyQualifiedName~IdleSettlementServiceTests
```

Expected: FAIL because the `Idle` namespace does not exist.

- [ ] **Step 3: Add immutable policy and report contracts**

```csharp
public sealed record IdleSettlementPolicy(TimeSpan MaximumAwayDuration)
{
    public static IdleSettlementPolicy Default { get; } = new(TimeSpan.FromHours(8));
}

public sealed record IdleSettlementReport(
    TimeSpan ActualAwayDuration,
    TimeSpan SettledDuration,
    int ProcessedRealSeconds,
    int CompletedSales,
    long RevenueCents,
    long StockPurchaseCostCents,
    long GrossProfitCents,
    int EndingLowStockProducts);
```

- [ ] **Step 4: Implement settlement through the existing simulation boundary**

```csharp
var actual = nowUtc <= savedAtUtc ? TimeSpan.Zero : nowUtc - savedAtUtc;
var settled = actual < _policy.MaximumAwayDuration ? actual : _policy.MaximumAwayDuration;
var seconds = checked((int)Math.Floor(settled.TotalSeconds));
var before = simulation.GetSnapshot();
if (seconds > 0) simulation.AdvanceRealSeconds(seconds);
var after = simulation.GetSnapshot();
```

Build the report only from before/after immutable snapshots. Do not directly mutate cash, inventory or employees.

- [ ] **Step 5: Add a 28,800-tick budget test**

Run the default-cap settlement with deterministic random input and assert it completes within 2 seconds on the test machine while all cash/inventory/customer boundaries remain valid.

- [ ] **Step 6: Run Application tests**

```powershell
dotnet test tests/HajimaoDesktopShop.Application.Tests -c Release
```

Expected: PASS; online and offline advancement share exactly one simulation code path.

### Task 5: Integrate startup settlement and return report

**Files:**
- Create: `src/HajimaoDesktopShop.Desktop/ViewModels/IdleSettlementReportViewModel.cs`
- Create: `src/HajimaoDesktopShop.Desktop/Windows/IdleSettlementWindow.xaml`
- Create: `src/HajimaoDesktopShop.Desktop/Windows/IdleSettlementWindow.xaml.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/App.xaml.cs`
- Create: `tests/HajimaoDesktopShop.Desktop.Tests/ViewModels/IdleSettlementReportViewModelTests.cs`

- [ ] **Step 1: Write a failing report formatting test**

```csharp
var viewModel = new IdleSettlementReportViewModel(report);
Assert.Equal("离开 2小时15分钟", viewModel.AwayDurationText);
Assert.Equal("+¥32.50", viewModel.RevenueText);
Assert.Equal("缺货/低库存 3", viewModel.StockWarningText);
```

- [ ] **Step 2: Run the formatting test and verify RED**

```powershell
dotnet test tests/HajimaoDesktopShop.Desktop.Tests --filter FullyQualifiedName~IdleSettlementReportViewModelTests
```

Expected: FAIL because the ViewModel does not exist.

- [ ] **Step 3: Implement a presentation-only ViewModel and pixel-styled window**

The window shows actual away duration, settled duration when capped, sales, revenue, purchase cost, gross profit, and low-stock count. It has one `继续经营` button and no gameplay mutation commands.

- [ ] **Step 4: Settle before starting the live loop**

In `App.OnStartup`, after restoring `ShopSimulation` and before creating `SimulationLoop`, call `IdleSettlementService.Settle(_simulation, savedGame.SavedAtUtc, DateTimeOffset.UtcNow)` on `Task.Run`. Show the report only when at least 300 real seconds were processed. The normal 5-second autosave then persists the advanced state.

- [ ] **Step 5: Protect startup failure boundaries**

If settlement fails, keep the migrated save intact, start normal fixed-time play, and display `离线结算未完成，已按原存档继续营业` through `GameViewModel.ReportSystemMessage`. Do not discard or recreate the save.

- [ ] **Step 6: Run Desktop tests and manual startup acceptance**

```powershell
dotnet test tests/HajimaoDesktopShop.Desktop.Tests -c Release
dotnet run --project src/HajimaoDesktopShop.Desktop -c Release
```

Acceptance: a copied v1 save with 2x/4x restores at fixed 1x; a timestamp 10 minutes in the past displays the report; closing the report does not pause the shop.

### Task 6: Update versioned documentation and compatibility notes

**Files:**
- Modify: `Directory.Build.props`
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/architecture/technical-foundation.md`
- Modify: `docs/roadmap.md`
- Create: `docs/progress/v1.1.0-fixed-time-idle.md`

- [ ] **Step 1: Set `VersionPrefix` to `1.1.0`**

```xml
<VersionPrefix>1.1.0</VersionPrefix>
```

- [ ] **Step 2: Update current-facing docs**

README and technical foundation must say fixed real-time 1x, eight-hour default offline cap, schema-v1 compatibility and return report. Keep `docs/progress/v1.0.0-first-playable-demo.md` and the 1.0.0 Changelog entry unchanged as historical truth.

- [ ] **Step 3: Write the phase report with evidence**

Record test counts, build status, migration fixtures, 28,800-tick timing, manual v1 save acceptance, CPU/memory sample, remaining risks and the 1.2.0 next plan.

- [ ] **Step 4: Scan current docs for stale player-speed promises**

```powershell
rg -n "暂停/1x/2x/4x|切换速度|速度控制" README.md docs/architecture docs/product-vision.md docs/roadmap.md
```

Expected: no current-facing promise remains; matches are allowed only inside versioned historical progress/plan records and the gap audit.

### Task 7: Release verification, packaging and cleanup

**Files:**
- Update: `docs/progress/v1.1.0-fixed-time-idle.md`
- Create: `artifacts/HajimaoDesktopShop-1.1.0-win-x64/`
- Create: `artifacts/HajimaoDesktopShop-1.1.0-win-x64.zip`

- [ ] **Step 1: Run the full Release gate**

```powershell
dotnet test HajimaoDesktopShop.slnx -c Release
dotnet build HajimaoDesktopShop.slnx -c Release --no-restore
dotnet list HajimaoDesktopShop.slnx package --vulnerable --include-transitive
```

Expected: all tests PASS, zero warnings/errors, no known vulnerable package.

- [ ] **Step 2: Verify forbidden production references**

```powershell
rg -n "SimulationSpeed|SetSpeedCommand|CurrentSpeedText" src
```

Expected: no matches. `Speed` may appear only in `LegacyGameSaveV1.cs` and its migration tests.

- [ ] **Step 3: Publish the self-contained package**

```powershell
dotnet publish src/HajimaoDesktopShop.Desktop -c Release -r win-x64 --self-contained true -o artifacts/HajimaoDesktopShop-1.1.0-win-x64
```

Zip the directory, record byte size and SHA-256 in the phase report, and confirm the executable file version is 1.1.0.0.

- [ ] **Step 4: Perform live/save acceptance without mutating the user's active save**

Use a copied database in an isolated temporary LocalApplicationData test profile. Verify new game, v1 migration, 10-minute offline return, eight-hour cap, 5-second autosave, exit/restart continuity, lock, click-through and management opening.

- [ ] **Step 5: Clean the workspace**

Remove only generated `bin/`, `obj/`, temporary acceptance profiles and unreferenced scratch captures after verifying their resolved paths are inside the project/test temp directories. Preserve both 1.0.0 and 1.1.0 release artifacts and all version reports.

## Final self-review checklist

- [ ] Every player-facing and public Application speed control is removed.
- [ ] Every schema-v1 speed value migrates to the same fixed-time schema-v2 state.
- [ ] Offline gains obey the same inventory, customer and employee rules as online play.
- [ ] System clock rollback cannot create negative or duplicated progress.
- [ ] Eight-hour cap is visible in the return report.
- [ ] Historical 1.0 records remain factually unchanged.
- [ ] 1.1.0 has tests, Changelog, phase summary, next plan, package hash and a clean workspace.
