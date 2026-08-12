# Hajimao DesktopShop Content Library and Market Events Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a data-driven content library containing 24 famous store identities, 120 products, 96 employee profiles and 96 meaningful events while keeping the player-facing decision surface capped at three routine options.

**Architecture:** Application defines immutable content contracts and a deterministic event runtime; Infrastructure validates and merges versioned JSON content packs; Desktop projects concise brand proposals and event text without exposing formulas. Rich metadata shares compact pixel-atlas assets instead of creating one large image per item. This plan starts after the v0.1.23 brand/format identity and schema-v7 groundwork in `2026-08-12-store-type-portfolio.md`.

**Tech Stack:** .NET 10, C# 14, WPF, CommunityToolkit.Mvvm, SkiaSharp sprite atlases, System.Text.Json, SQLite schema v7, xUnit.

---

### Task 1: Add a versioned multi-pack content catalog with hard volume gates

**Files:**
- Create: `src/HajimaoDesktopShop.Application/Catalog/ContentManifest.cs`
- Create: `src/HajimaoDesktopShop.Application/Catalog/GameContentCatalog.cs`
- Create: `src/HajimaoDesktopShop.Application/Catalog/EmployeeProfileDefinition.cs`
- Create: `src/HajimaoDesktopShop.Application/Business/Events/MarketEventDefinition.cs`
- Create: `src/HajimaoDesktopShop.Infrastructure/Configuration/JsonGameContentCatalog.cs`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/content-manifest.json`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/stores/store-brands-global.json`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/products/beverages-snacks.json`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/products/fresh-prepared.json`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/products/household-care.json`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/products/lifestyle-home.json`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/employees/employee-profiles.json`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/events/market-events.json`
- Create: `tests/HajimaoDesktopShop.Infrastructure.Tests/Configuration/JsonGameContentCatalogTests.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/HajimaoDesktopShop.Desktop.csproj`
- Modify: `src/HajimaoDesktopShop.Desktop/App.xaml.cs`

- [ ] **Step 1: Write failing manifest and reference validation tests**

```csharp
[Fact]
public async Task LoadAsync_RequiresThePlayableContentVolumeAndValidReferences()
{
    var content = await LoadShippedCatalogAsync();

    Assert.True(content.StoreBrands.Count >= 12);
    Assert.True(content.Products.Count >= 40);
    Assert.True(content.EmployeeProfiles.Count >= 32);
    Assert.True(content.MarketEvents.Count >= 32);
    Assert.All(content.StoreBrands, brand =>
        Assert.Contains(content.StoreFormats, format => format.Id == brand.FormatId));
    Assert.All(content.Products, product => Assert.False(string.IsNullOrWhiteSpace(product.IconKey)));
}
```

Also prove duplicate IDs across packs, a missing localization key, an unknown format/category/asset reference and a count below the manifest minimum each throw `InvalidDataException` naming the offending ID.

- [ ] **Step 2: Run RED**

Run: `dotnet test tests/HajimaoDesktopShop.Infrastructure.Tests -c Release --filter FullyQualifiedName~JsonGameContentCatalogTests`

Expected: compilation fails because `JsonGameContentCatalog` and content contracts do not exist.

- [ ] **Step 3: Implement one immutable aggregate and merge packs by stable ID**

```csharp
public sealed record ContentManifest(
    int SchemaVersion,
    int MinimumStoreBrandCount,
    int MinimumProductCount,
    int MinimumEmployeeProfileCount,
    int MinimumMarketEventCount,
    IReadOnlyList<string> PackFiles);

public sealed record GameContentCatalog(
    IReadOnlyList<StoreFormatDefinition> StoreFormats,
    IReadOnlyList<StoreBrandDefinition> StoreBrands,
    IReadOnlyList<ProductDefinition> Products,
    IReadOnlyList<EmployeeProfileDefinition> EmployeeProfiles,
    IReadOnlyList<MarketEventDefinition> MarketEvents);
```

`JsonGameContentCatalog.LoadAsync()` reads `content-manifest.json`, resolves every pack below `Assets/Content`, rejects paths outside that directory, merges by content kind and validates all references before returning the aggregate. The Desktop project copies only manifest-listed JSON and atlas files to output.

Author the v0.1.24 validation tranche in the listed production packs: 12 brands, 40 products, 32 employee profiles and 32 events. These are complete records using the same schemas as the final catalog, not provisional IDs. Later tasks expand the same files to the v0.1.25 gate.

- [ ] **Step 4: Run GREEN and commit**

Run the selected Infrastructure tests, then `dotnet test tests/HajimaoDesktopShop.Infrastructure.Tests -c Release --nologo`.

Commit: `feat(content): add validated multi-pack catalog`

### Task 2: Author 24 famous store identities without coupling names to algorithms

**Files:**
- Modify: `src/HajimaoDesktopShop.Desktop/Assets/Content/stores/store-brands-global.json`
- Modify: `src/HajimaoDesktopShop.Desktop/Assets/Config/store-formats.json`
- Modify: `src/HajimaoDesktopShop.Application/Catalog/StoreBrandDefinition.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/StorePortfolio/StoreOpeningProposalService.cs`
- Modify: `tests/HajimaoDesktopShop.Infrastructure.Tests/Configuration/JsonStoreContentCatalogTests.cs`
- Create: `tests/HajimaoDesktopShop.Application.Tests/Business/StorePortfolio/StoreOpeningProposalServiceTests.cs`

- [ ] **Step 1: Write failing brand-roster and proposal tests**

```csharp
[Fact]
public void GlobalRoster_ContainsTwentyFourNamedStoresAcrossEightFormats()
{
    Assert.Equal(24, _catalog.StoreBrands.Count);
    Assert.True(_catalog.StoreBrands.Select(x => x.FormatId).Distinct().Count() >= 8);
    Assert.Contains(_catalog.StoreBrands, x => x.DisplayName == "7-Eleven");
    Assert.Contains(_catalog.StoreBrands, x => x.DisplayName == "银座三越");
}

[Fact]
public void ExpansionProposal_ReturnsThreeBrandsAcrossAtLeastTwoFormats()
{
    var result = _service.CreateExpansionProposals(_portfolio, seed: 7411);

    Assert.Equal(3, result.Count);
    Assert.True(result.Select(x => x.FormatId).Distinct().Count() >= 2);
}
```

Prove proposals are stable for the same seed/state, automatically change after their configured expiry day, do not use player level, and never expose a refresh command.

- [ ] **Step 2: Run RED**

Run: `dotnet test tests/HajimaoDesktopShop.Application.Tests -c Release --filter FullyQualifiedName~StoreOpeningProposalServiceTests`

Expected: FAIL because the brand roster and deterministic three-proposal policy are absent.

- [ ] **Step 3: Author the locked 24-brand roster**

The roster contains exactly these display identities for the first content gate: `7-Eleven`, `FamilyMart`, `Lawson`, `Circle K`, `ALDI`, `Lidl`, `Walmart`, `Carrefour`, `Costco`, `Sam's Club`, `METRO`, `MUJI`, `IKEA`, `Daiso`, `Matsumoto Kiyoshi`, `Watsons`, `Boots`, `银座三越`, `Harrods`, `Galeries Lafayette`, `Macy's`, `Don Quijote`, `Target`, and `AEON`.

Each record contains:

```json
{
  "id": "seven-eleven",
  "displayName": "7-Eleven",
  "region": "global",
  "formatId": "convenience",
  "facadeStyleKey": "facade-convenience-a",
  "referenceNote": "real-world-name",
  "distributionStatus": "review-required"
}
```

Do not store logos, slogans, mascots or copied trade dress. `StoreOpeningProposalService` ranks by portfolio format gap, affordability, active market tags and recent proposal history, then deterministically selects three brands across at least two formats.

- [ ] **Step 4: Run GREEN and commit**

Run the brand catalog and proposal tests.

Commit: `feat(content): add global store identity roster`

### Task 3: Expand products from ten samples to 120 useful economic resources

**Files:**
- Modify: `src/HajimaoDesktopShop.Application/Catalog/ProductDefinition.cs`
- Modify: `src/HajimaoDesktopShop.Infrastructure/Configuration/JsonProductCatalog.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/Assets/Content/products/beverages-snacks.json`
- Modify: `src/HajimaoDesktopShop.Desktop/Assets/Content/products/fresh-prepared.json`
- Modify: `src/HajimaoDesktopShop.Desktop/Assets/Content/products/household-care.json`
- Modify: `src/HajimaoDesktopShop.Desktop/Assets/Content/products/lifestyle-home.json`
- Remove: `src/HajimaoDesktopShop.Desktop/Assets/Config/products.json`
- Modify: `tests/HajimaoDesktopShop.Infrastructure.Tests/Configuration/JsonProductCatalogTests.cs`
- Modify: `tests/HajimaoDesktopShop.Application.Tests/Catalog/ProductDefinitionTests.cs`
- Create: `tests/HajimaoDesktopShop.Application.Tests/Business/Simulation/ProductAssortmentScenarioTests.cs`

- [ ] **Step 1: Write failing category, margin and reachability tests**

```csharp
[Fact]
public void ProductRoster_HasTenProductsInEachOfTwelveEconomicCategories()
{
    var groups = _catalog.Products.GroupBy(x => x.CategoryId).ToArray();

    Assert.Equal(12, groups.Length);
    Assert.All(groups, group => Assert.Equal(10, group.Count()));
    Assert.All(_catalog.Products, product => Assert.True(product.WholesalePriceCents < product.InitialSalePriceCents));
}
```

The twelve locked categories are beverages, snacks, staples, bakery, dairy, prepared-food, frozen, household, personal-care, wellness, stationery-lifestyle and home-gift. Tests require at least three margin bands, three inventory-cost bands, all three shelf kinds, and at least one reachable product per format at player level 1.

- [ ] **Step 2: Run RED**

Run: `dotnet test tests/HajimaoDesktopShop.Infrastructure.Tests -c Release --filter FullyQualifiedName~JsonProductCatalogTests`

Expected: existing catalog returns 10 products and lacks category/region/icon metadata.

- [ ] **Step 3: Extend the product contract and author four 30-item packs**

```csharp
public ProductDefinition(
    string id,
    string name,
    string categoryId,
    long wholesalePriceCents,
    long initialSalePriceCents,
    int capacity,
    string shelfKind,
    int requiredPlayerLevel,
    string iconKey,
    IReadOnlyList<string> regionTags)
```

Each of the 120 products must differ in at least two economically relevant fields among category, margin, unit cost, capacity, shelf, level and region tags. Preserve the ten current IDs and values where possible so v6/v7 saves retain products. The runtime merges four packs and sorts by stable ID; UI continues to show only unlocked products relevant to the selected store.

- [ ] **Step 4: Run GREEN, balance scenarios and commit**

Run catalog tests and `ProductAssortmentScenarioTests`; assert every format sells at least four categories by day 30 and no product is permanently unreachable.

Commit: `feat(content): expand product assortment to 120 items`

### Task 4: Replace the eight-name generator with 96 employee profiles

**Files:**
- Modify: `src/HajimaoDesktopShop.Application/Catalog/EmployeeProfileDefinition.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/Assets/Content/employees/employee-profiles.json`
- Modify: `src/HajimaoDesktopShop.Application/Business/Employees/EmployeeOperationsService.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Employees/EmployeeCandidate.cs`
- Modify: `src/HajimaoDesktopShop.Application/Persistence/BusinessSaveData.cs`
- Modify: `tests/HajimaoDesktopShop.Application.Tests/Business/Employees/EmployeeOperationsServiceTests.cs`
- Modify: `tests/HajimaoDesktopShop.Application.Tests/Business/BusinessSessionTests.cs`

- [ ] **Step 1: Write failing diversity and determinism tests**

```csharp
[Fact]
public void ShippedProfiles_CoverSixRolesAndTwelveRegionalNamePools()
{
    Assert.Equal(96, _catalog.EmployeeProfiles.Count);
    Assert.Equal(6, _catalog.EmployeeProfiles.SelectMany(x => x.AllowedRoles).Distinct().Count());
    Assert.True(_catalog.EmployeeProfiles.Select(x => x.RegionTag).Distinct().Count() >= 12);
}
```

Prove the visible candidate pool remains exactly three, the same random state yields the same three profile IDs, hired employees persist profile ID/name/role/efficiency/wage, and exhausted recent-profile history avoids immediate duplicates without an exposed reroll button.

- [ ] **Step 2: Run RED**

Run: `dotnet test tests/HajimaoDesktopShop.Application.Tests -c Release --filter FullyQualifiedName~EmployeeOperationsServiceTests`

Expected: existing service has only eight hard-coded names and no profile catalog.

- [ ] **Step 3: Inject profiles and keep candidate calculation automatic**

```csharp
public sealed record EmployeeProfileDefinition(
    string Id,
    string DisplayName,
    string RegionTag,
    string AppearanceKey,
    IReadOnlyList<EmployeeRole> AllowedRoles,
    int EfficiencyBiasPermille,
    int WageBiasPermille,
    string BackgroundTextKey);
```

`EmployeeOperationsService` receives the immutable profile catalog. It selects a profile, allowed role, efficiency and wage in the existing deterministic random stream. Players still see three candidates and the concise efficiency/wage comparison; profile biases and generation math remain internal.

- [ ] **Step 4: Run GREEN and commit**

Run employee tests, save/restore tests and Application tests.

Commit: `feat(content): add 96 employee identity profiles`

### Task 5: Implement deterministic background market events

**Files:**
- Modify: `src/HajimaoDesktopShop.Application/Business/Events/MarketEventDefinition.cs`
- Create: `src/HajimaoDesktopShop.Application/Business/Events/MarketEventEffect.cs`
- Create: `src/HajimaoDesktopShop.Application/Business/Events/ActiveMarketEvent.cs`
- Create: `src/HajimaoDesktopShop.Application/Business/Events/MarketEventScheduler.cs`
- Create: `src/HajimaoDesktopShop.Application/Business/Events/MarketEventSnapshot.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/Assets/Content/events/market-events.json`
- Modify: `src/HajimaoDesktopShop.Application/Business/Simulation/BusinessSimulation.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Simulation/BusinessSimulationSnapshot.cs`
- Modify: `src/HajimaoDesktopShop.Application/Persistence/BusinessSaveData.cs`
- Create: `tests/HajimaoDesktopShop.Application.Tests/Business/Events/MarketEventSchedulerTests.cs`
- Modify: `tests/HajimaoDesktopShop.Application.Tests/Business/Simulation/BusinessSimulationTests.cs`

- [ ] **Step 1: Write failing scheduling and economic-effect tests**

```csharp
[Fact]
public void AdvanceMinute_ActivatesEligibleEventOnceAndExpiresAtExactMinute()
{
    var scheduler = CreateScheduler(seed: 711, gameMinute: 0);

    scheduler.AdvanceMinutes(120);
    var active = Assert.Single(scheduler.GetSnapshot().ActiveEvents);
    scheduler.AdvanceMinutes(active.RemainingMinutes);

    Assert.Empty(scheduler.GetSnapshot().ActiveEvents);
}
```

Test fixed-seed replay, cooldowns, incompatible tags, scope target validation, save/restore exactness and integer effects for traffic, purchase probability, product-category weight, procurement cost and employee efficiency. Prove cash changes only through existing sales/cost commands, never directly from event text.

- [ ] **Step 2: Run RED**

Run: `dotnet test tests/HajimaoDesktopShop.Application.Tests -c Release --filter FullyQualifiedName~MarketEvent`

Expected: compilation fails because the event subsystem does not exist.

- [ ] **Step 3: Implement tagged definitions and a deterministic scheduler**

```csharp
public sealed record MarketEventDefinition(
    string Id,
    MarketEventScope Scope,
    IReadOnlyList<string> EligibilityTags,
    int DurationMinutes,
    int CooldownMinutes,
    string Headline,
    string EffectSummaryTemplate,
    IReadOnlyList<MarketEventEffect> Effects,
    IReadOnlyList<MarketEventChoice> Choices);
```

The scheduler owns random state, active events, cooldowns and recent history. `BusinessSimulation` asks for a modifier snapshot and applies it to existing demand, product selection, procurement quote and effective employee efficiency. All arithmetic uses integer permille/basis points and is included in deterministic save state.

- [ ] **Step 4: Author 96 non-duplicate events, run GREEN and commit**

The roster contains at least 12 events for each of global market, street, store, product/category and employee scopes. The remaining 36 cover weather/season, supply, commuting, festivals, local competition and trend combinations. Two events are duplicates if they share the same scope, eligibility, effects and duration even when wording differs; validation rejects such duplicates.

Run selected event/simulation tests and all Application tests.

Commit: `feat(gameplay): add deterministic market event runtime`

### Task 6: Project events as concise text and expose choices only when strategic

**Files:**
- Create: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/MarketEventTickerViewModel.cs`
- Create: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/MarketEventDecisionViewModel.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/MarketOverviewViewModel.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/NextActionViewModel.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/MarketViewModel.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/Windows/ManagementWindow.xaml`
- Modify: `src/HajimaoDesktopShop.Desktop/Controls/BusinessDesktopShopSurfaceControl.xaml`
- Create: `tests/HajimaoDesktopShop.Desktop.Tests/ViewModels/Market/MarketEventTickerViewModelTests.cs`
- Modify: `tests/HajimaoDesktopShop.Desktop.Tests/ViewModels/Market/NextActionViewModelTests.cs`
- Modify: `tests/HajimaoDesktopShop.Desktop.Tests/Windows/ManagementWindowTests.cs`

- [ ] **Step 1: Write failing text and option-cap tests**

```csharp
[Fact]
public void PassiveEvent_ShowsCauseDirectionAndDurationWithoutCommand()
{
    _viewModel.Refresh(Event("commuter-surge", "早高峰提前", "通勤客流上升，排队压力增大", 120));

    Assert.Equal("早高峰提前：通勤客流上升，排队压力增大（剩余 2 小时）", _viewModel.CurrentText);
    Assert.Empty(_viewModel.Commands);
}
```

Prove the overview retains only three recent messages, passive events expose no button, one strategic event replaces the right-rail next action with exactly two choices, and routine management never exceeds three visible options.

- [ ] **Step 2: Run RED**

Run: `dotnet test tests/HajimaoDesktopShop.Desktop.Tests -c Release --filter FullyQualifiedName~MarketEvent`

Expected: compilation fails because event view models are absent.

- [ ] **Step 3: Implement one-line passive projection and one decision slot**

The street surface renders only `CurrentText`; it does not show exact modifiers. Management overview stores the latest three completed/active messages. `NextActionViewModel` gives an unresolved strategic event priority over onboarding/progression until the player chooses or the documented default fires. No event gets a claim/reward/reroll button.

- [ ] **Step 4: Run GREEN and commit**

Run selected Desktop tests and `dotnet test tests/HajimaoDesktopShop.Desktop.Tests -c Release --nologo` without launching the application.

Commit: `feat(desktop): explain market events with concise text`

### Task 7: Expand pixel resources through atlases and 24-frame variants

**Files:**
- Modify: `src/HajimaoDesktopShop.Rendering/PixelArt/PixelSpriteAtlas.cs`
- Modify: `src/HajimaoDesktopShop.Rendering/PixelArt/PixelArtBudget.cs`
- Create: `src/HajimaoDesktopShop.Rendering/PixelArt/ContentSpriteKey.cs`
- Create: `src/HajimaoDesktopShop.Rendering/Assets/PixelArt/content-atlas.png`
- Modify: `src/HajimaoDesktopShop.Rendering/HajimaoDesktopShop.Rendering.csproj`
- Modify: `src/HajimaoDesktopShop.Rendering/CommercialStreetSceneRenderer.cs`
- Modify: `src/HajimaoDesktopShop.Rendering/BusinessShopSceneRenderer.cs`
- Modify: `tools/pixel-assets/build_market_atlas.py`
- Modify: `tools/pixel-assets/optimize_market_atlas.py`
- Create: `tests/HajimaoDesktopShop.Rendering.Tests/PixelArt/ContentSpriteAtlasTests.cs`
- Modify: `tests/HajimaoDesktopShop.Rendering.Tests/CommercialStreetSceneRendererTests.cs`
- Modify: `tests/HajimaoDesktopShop.Rendering.Tests/BusinessShopSceneRendererTests.cs`

- [ ] **Step 1: Write failing atlas coverage and storage-budget tests**

```csharp
[Fact]
public void ShippedContent_ResolvesEveryIconFacadeAndAppearanceKey()
{
    Assert.All(_content.Products, item => Assert.True(_atlas.Contains(item.IconKey)));
    Assert.All(_content.StoreBrands, item => Assert.True(_atlas.Contains(item.FacadeStyleKey)));
    Assert.All(_content.EmployeeProfiles, item => Assert.True(_atlas.Contains(item.AppearanceKey)));
}
```

Also prove every animated human appearance resolves exactly 24 logical frames, nearest-neighbor sampling remains enabled, and all new compressed atlas files stay below the size budget recorded in `PixelArtBudget`.

- [ ] **Step 2: Run RED**

Run: `dotnet test tests/HajimaoDesktopShop.Rendering.Tests -c Release --filter FullyQualifiedName~ContentSpriteAtlasTests`

Expected: missing content sprite keys and incomplete resource coverage.

- [ ] **Step 3: Build combinatorial variants instead of one PNG per record**

Store facades combine eight base formats with palette, awning and text-sign variants. Product icons share category sheets. Employees combine body, hair, uniform and accessory layers on the existing 24-frame timeline. Real-world names are rendered as text signs; copied logos and packaging are excluded.

- [ ] **Step 4: Run GREEN, inspect atlas dimensions and commit**

Run:

```powershell
dotnet test tests/HajimaoDesktopShop.Rendering.Tests -c Release --nologo
Get-Item src/HajimaoDesktopShop.Rendering/Assets/PixelArt/market-atlas.png,src/HajimaoDesktopShop.Rendering/Assets/PixelArt/content-atlas.png | Select-Object Name,Length
```

Expected: all Rendering tests pass; both reported lengths are at or below their named byte ceilings in `PixelArtBudget`. Record atlas dimensions and compressed bytes in the v0.1.25 progress report.

Commit: `feat(rendering): add compact rich-content pixel atlases`

### Task 8: Prove content richness does not create UI or balance noise

**Files:**
- Create: `tests/HajimaoDesktopShop.Desktop.Tests/Progression/RichContentLongTermScenarioTests.cs`
- Modify: `tests/HajimaoDesktopShop.Desktop.Tests/Progression/LongTermProgressionScenarioRunner.cs`
- Create: `docs/progress/v0.1.24-content-event-foundation.md`
- Create: `docs/progress/v0.1.25-rich-content.md`
- Modify: `CHANGELOG.md`
- Modify: `Directory.Build.props`

- [ ] **Step 1: Add deterministic 30/90/180/365-day content scenarios**

Each route must encounter at least six brands, eight product categories, twelve distinct employee profiles and twenty distinct events by day 365. Assert cash never becomes negative, event effects expire, proposal cards stay at three, employee candidates stay at three and at least one future investment remains executable.

- [ ] **Step 2: Add content-quality gates**

Fail when more than 20% of events share the same economic effect bundle, one brand appears in over 25% of proposals across fixed seeds, any product has zero sales across all format scenarios, or one format dominates revenue, net profit, cash pressure and lost-sales rate simultaneously.

- [ ] **Step 3: Run full verification**

Run:

```powershell
dotnet test HajimaoDesktopShop.slnx -c Release --nologo
dotnet build HajimaoDesktopShop.slnx -c Release --nologo
git diff --check
git status --short
```

Expected: zero failed tests, zero build errors/warnings introduced by this work, no whitespace errors and only intentional source/content/documentation changes.

- [ ] **Step 4: Version, document and clean**

Release v0.1.24 after catalog/event runtime tests pass with the 12/40/32/32 validation tranche. Release v0.1.25 only after the shipped catalog reaches 24 brands, 120 products, 96 employee profiles and 96 validated events plus the full long-term gates. Remove generated build output and staging packages; do not create a release archive until requested.

Commit: `docs: complete rich content gameplay milestone`
