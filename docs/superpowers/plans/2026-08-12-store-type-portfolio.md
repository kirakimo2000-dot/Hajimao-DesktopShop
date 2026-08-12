# Hajimao DesktopShop Store Brand Portfolio Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace level-sequenced fixed stores with repeatable player-selected famous store identities backed by reusable economic formats, including first-store proposals, explainable profit/risk differences, unlimited street instances, and save-compatible long-term portfolio progression.

**Architecture:** Domain owns stable store instance, brand and format identities plus explainable demand sensitivity math. Application owns brand-to-format mapping, opening proposals, dynamic instance creation, simulation integration and immutable snapshots. Infrastructure loads validated brand/format content and migrates schema v6 saves to v7. Desktop presents four starter proposals and three expansion proposals only after the gameplay kernel is proven. Rendering consumes facade keys and remains independent of economic rules.

**Tech Stack:** .NET 10, C# 14, WPF, CommunityToolkit.Mvvm, SkiaSharp, System.Text.Json, SQLite, xUnit.

**Design source:** `docs/design/store-type-portfolio-design.md`

---

## Task 1: Separate store instance, famous brand identity and reusable economic format

**Files:**
- Create: `src/HajimaoDesktopShop.Domain/Shops/StoreBrandId.cs`
- Create: `src/HajimaoDesktopShop.Domain/Shops/StoreFormatId.cs`
- Modify: `src/HajimaoDesktopShop.Domain/Shops/ShopDefinition.cs`
- Modify: `src/HajimaoDesktopShop.Domain/Shops/OpenShopResult.cs`
- Modify: `src/HajimaoDesktopShop.Domain/Shops/RetailBusiness.cs`
- Modify: `src/HajimaoDesktopShop.Domain/Shops/RetailBusinessStoreState.cs`
- Test: `tests/HajimaoDesktopShop.Domain.Tests/Shops/RetailBusinessTests.cs`

- [ ] Write failing tests proving two different `ShopId` instances can share one `StoreBrandId`, multiple brands can share one `StoreFormatId`, opening has no player-level check, duplicate instance IDs remain atomic, and insufficient cash mutates nothing.
- [ ] Run `dotnet test tests/HajimaoDesktopShop.Domain.Tests -c Release --filter FullyQualifiedName~RetailBusinessTests` and record RED.
- [ ] Change `ShopDefinition` to carry `ShopId`, `StoreBrandId`, `StoreFormatId`, display name, `StreetOrdinal` and opening cost. Remove `RequiredPlayerLevel`.
- [ ] Remove `LevelLocked` from `OpenShopStatus`; retain unknown instance, duplicate instance and insufficient-funds outcomes.
- [ ] Keep the wallet and individual `Shop` state unchanged so type content never enters financial entities as mutable global state.
- [ ] Run the selected Domain tests, then `dotnet test tests/HajimaoDesktopShop.Domain.Tests -c Release --nologo`.

## Task 2: Add validated format/brand content and dynamic opening proposals

**Files:**
- Create: `src/HajimaoDesktopShop.Application/Catalog/StoreFormatDefinition.cs`
- Create: `src/HajimaoDesktopShop.Application/Catalog/StoreBrandDefinition.cs`
- Create: `src/HajimaoDesktopShop.Application/Catalog/StoreTimeProfile.cs`
- Create: `src/HajimaoDesktopShop.Application/Business/StorePortfolio/StoreBrandSnapshot.cs`
- Create: `src/HajimaoDesktopShop.Application/Business/StorePortfolio/StoreOpeningProposal.cs`
- Create: `src/HajimaoDesktopShop.Application/Business/StorePortfolio/StoreOpeningProposalService.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/BusinessGameService.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/BusinessSnapshot.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/BusinessSession.cs`
- Replace: `src/HajimaoDesktopShop.Application/Business/StoreCatalogItemSnapshot.cs`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Config/store-formats.json`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Config/store-brands.json`
- Create: `src/HajimaoDesktopShop.Infrastructure/Configuration/JsonStoreContentCatalog.cs`
- Create: `tests/HajimaoDesktopShop.Infrastructure.Tests/Configuration/JsonStoreContentCatalogTests.cs`
- Modify: `tests/HajimaoDesktopShop.Application.Tests/Business/BusinessGameServiceTests.cs`

- [ ] Write catalog validation tests for four initial format IDs and at least twelve brand IDs, including `seven-eleven`; validate unique stable IDs, brand-to-format references, display names, regions, distribution status, facade keys, positive multipliers, complete `ambient/chilled/frozen` weights, valid recommended strategies and non-negative costs/reserves.
- [ ] Write service tests proving new games receive four starter-safe brands while later `GetStoreOpeningProposals()` returns exactly three brands across at least two formats, repeated brands are allowed across instances, next instance IDs are `store-0002`, `store-0003`, and quote cost is `BaseOpeningCostCents + 40_000 * openStoreCount`.
- [ ] Run the selected Application/Infrastructure tests and record RED.
- [ ] Load formats and brands from separate JSON files in `App.OnStartup`; leave products in their existing catalog until the content-library plan replaces the single product document.
- [ ] Change new-session creation to require an explicit starter brand ID. The first definition is `store-0001`, ordinal 1, with its mapped format and zero opening cost.
- [ ] Make `BusinessStoreSnapshot` expose `StoreBrandId`, `StoreFormatId` and `StreetOrdinal`; compute product capacity from format capacity multiplier before applying existing shelf development capacity.
- [ ] Replace fixed `DesktopGameContent.Shops` with type-independent opening cash, level curve, employee templates and an instance assignment factory.
- [ ] Run all Application and Infrastructure tests.

## Task 3: Make format economics explainable inside the existing simulation

**Files:**
- Create: `src/HajimaoDesktopShop.Domain/Demand/DemandSensitivity.cs`
- Create: `src/HajimaoDesktopShop.Domain/Demand/DemandTimeCurve.cs`
- Modify: `src/HajimaoDesktopShop.Domain/Demand/DemandContext.cs`
- Modify: `src/HajimaoDesktopShop.Domain/Demand/DemandModel.cs`
- Modify: `src/HajimaoDesktopShop.Application/Game/ProductSnapshot.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/BusinessGameService.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Simulation/BusinessSimulation.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Simulation/BusinessSimulationSnapshot.cs`
- Test: `tests/HajimaoDesktopShop.Domain.Tests/Demand/DemandModelTests.cs`
- Test: `tests/HajimaoDesktopShop.Application.Tests/Business/Simulation/BusinessSimulationTests.cs`

- [ ] Write independent demand tests for base, price, service, queue, cleanliness and time multipliers. Every `DemandBreakdown` component must remain attributable and final demand must remain clamped to `0..10_000`.
- [ ] Write deterministic simulation tests proving discount has more arrivals but a larger high-price penalty, boutique accepts high-margin pricing but loses more demand from poor service/cleanliness, and commuter demand moves from off-peak to morning/evening peaks.
- [ ] Write weighted product-choice tests proving shelf-kind weights only affect in-stock unlocked products and consume deterministic random state reproducibly.
- [ ] Run the selected tests and record RED.
- [ ] Extend `DemandContext` with immutable sensitivities and an explicit time curve; apply multipliers to individual adjustments rather than to final profit.
- [ ] Add format-specific `DemandWeightPermille` to `ProductSnapshot`; replace uniform product selection with deterministic weighted selection.
- [ ] Do not add daily disasters, direct profit multipliers, hidden free stock or offline settlement.
- [ ] Run all Domain and Application tests.

## Task 4: Remove the one-visitor and five-store long-term ceilings

**Files:**
- Modify: `src/HajimaoDesktopShop.Domain/Streets/CommercialStreetTrafficModel.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Street/CommercialStreetTrafficService.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Street/CommercialStreetSnapshot.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Simulation/BusinessSimulation.cs`
- Test: `tests/HajimaoDesktopShop.Domain.Tests/Streets/CommercialStreetTrafficModelTests.cs`
- Test: `tests/HajimaoDesktopShop.Application.Tests/Business/Street/CommercialStreetTrafficServiceTests.cs`
- Modify: `tests/HajimaoDesktopShop.Application.Tests/Business/Simulation/BusinessSimulationTests.cs`

- [ ] Write tests for 1, 2, 5, 6, 20 and 100 storefronts. Five or more maps to `CommercialStreetTier.Block` without throwing.
- [ ] Write deterministic routing tests for `ceil(storeCount * 0.6)` visitor opportunities, shared-demand rolls, attraction-weighted routing and the possibility of multiple visitors reaching one store in a minute.
- [ ] Prove a level-10 player with one open store still has exactly one storefront and one-store content width.
- [ ] Run the selected tests and record RED.
- [ ] Remove player level from street-size derivation. Preserve level only in the business/player snapshot for product progression.
- [ ] Calculate shared traffic from mean current store demand with 3% per additional-store synergy capped at 18%; keep weather as the existing visible and economic modifier.
- [ ] Expose the number of visitor opportunities in the street snapshot for diagnostics and balancing.
- [ ] Run Domain, Application and Rendering tests.

## Task 5: Replace level-gated progression and single fixed opening candidate

**Files:**
- Replace: `src/HajimaoDesktopShop.Application/Business/Investments/StoreOpeningInvestmentAdvisor.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Investments/StoreInvestmentService.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Investments/CapitalAllocationAdvisor.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Investments/CapitalAllocationSnapshot.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Investments/InvestmentCandidate.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Investments/InvestmentKind.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Progression/LongTermProgressionService.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Progression/ProgressionGoalSnapshot.cs`
- Test: `tests/HajimaoDesktopShop.Application.Tests/Business/Investments/StoreOpeningInvestmentAdvisorTests.cs`
- Test: `tests/HajimaoDesktopShop.Application.Tests/Business/Investments/CapitalAllocationAdvisorTests.cs`
- Test: `tests/HajimaoDesktopShop.Application.Tests/Business/Progression/LongTermProgressionServiceTests.cs`

- [ ] Write tests proving capital allocation still exposes at most three top-level directions and `ExpandStreet` contains exactly three brand proposals instead of selecting one hidden fixed store.
- [ ] Prove no opening choice is level locked; insufficient opening cost is blocked, while enough cost but less than recommended reserve is executable with high cash pressure.
- [ ] Prove the progression loop alternates among stabilize weakest store, build expansion reserve and choose next type without a terminal third-store or level-10 goal.
- [ ] Run the selected tests and record RED.
- [ ] Give opening candidates separate prospective instance ID, brand ID and format ID. Revalidate proposal, quote and ordinal at execution time.
- [ ] Keep investment tracking attached to the newly created instance ID after a successful opening.
- [ ] Remove `RequiredPlayerLevel` and `LevelLocked` presentation paths from store-opening contracts only; product unlock levels remain unchanged.
- [ ] Run all Application tests.

## Task 6: Persist store brand/format identity and migrate schema v6 to v7

**Files:**
- Modify: `src/HajimaoDesktopShop.Application/Persistence/GameSaveData.cs`
- Modify: `src/HajimaoDesktopShop.Application/Persistence/BusinessSaveData.cs`
- Create: `src/HajimaoDesktopShop.Infrastructure/Persistence/LegacyGameSaveV6.cs`
- Modify: `src/HajimaoDesktopShop.Infrastructure/Persistence/SqliteGameSaveStore.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/BusinessSession.cs`
- Test: `tests/HajimaoDesktopShop.Application.Tests/Persistence/GameSaveDataTests.cs`
- Test: `tests/HajimaoDesktopShop.Infrastructure.Tests/Persistence/SqliteGameSaveStoreTests.cs`
- Modify: `tests/HajimaoDesktopShop.Application.Tests/Business/BusinessSessionTests.cs`

- [ ] Write a v6 migration fixture with three stores, employees, runtime queues, orders, auto-restock, promotions, growth and investment tracking.
- [ ] Prove migration maps legacy IDs to `store-0001..0003`, assigns `seven-eleven/familymart/lawson` brands with the neutral `convenience` format, preserves order, and rewrites every foreign store ID consistently.
- [ ] Prove a v7 save/restore round trip preserves repeated brands, shared formats and street ordinals.
- [ ] Run selected persistence tests and record RED.
- [ ] Add `StoreBrandId`, `StoreFormatId`, `StreetOrdinal` and optional event state groundwork to `BusinessStoreSaveData`/`BusinessSimulationSaveData`; bump only save schema to 7.
- [ ] Implement the transactional SQLite payload migration and keep database `user_version` aligned with payload schema.
- [ ] Restore existing saves without showing first-store selection.
- [ ] Run Application and Infrastructure test suites twice to detect order/random-state drift.

## Task 7: Add first-store and expansion selection only after the kernel passes

**Files:**
- Modify: `src/HajimaoDesktopShop.Desktop/Services/DesktopBusinessSessionFactory.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/Services/DesktopBusinessSessionStartResult.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/App.xaml.cs`
- Create: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/StoreBrandChoiceCardViewModel.cs`
- Create: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/StarterStoreSelectionViewModel.cs`
- Create: `src/HajimaoDesktopShop.Desktop/Windows/StarterStoreSelectionWindow.xaml`
- Create: `src/HajimaoDesktopShop.Desktop/Windows/StarterStoreSelectionWindow.xaml.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/InvestmentPortfolioViewModel.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/InvestmentCandidateCardViewModel.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/Windows/ManagementWindow.xaml`
- Modify: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/OnboardingViewModel.cs`
- Test: `tests/HajimaoDesktopShop.Desktop.Tests/Services/DesktopBusinessSessionFactoryTests.cs`
- Create: `tests/HajimaoDesktopShop.Desktop.Tests/ViewModels/Market/StarterStoreSelectionViewModelTests.cs`
- Modify: `tests/HajimaoDesktopShop.Desktop.Tests/ViewModels/Market/InvestmentPortfolioViewModelTests.cs`
- Modify: `tests/HajimaoDesktopShop.Desktop.Tests/Windows/ManagementWindowTests.cs`

- [ ] Write view-model tests proving new games cannot start without a valid type, restored games skip selection, and one command produces exactly one starter choice result.
- [ ] Write expansion tests proving the top-level card expands to three concise brand proposals, cancellation changes nothing, and executing a choice refreshes street/store navigation.
- [ ] Keep each card to name, earning logic, primary risk, recommended strategy and cash consequence; do not add long tutorial paragraphs.
- [ ] Ensure the simulation loop, autosave and desktop window start only after first-store selection completes.
- [ ] Change onboarding guidance to refer to the selected type's recommended strategy without auto-applying it.
- [ ] Run all Desktop tests without launching or controlling the user's desktop.

## Task 8: Add restrained type-specific storefronts and long-term balance gates

**Files:**
- Modify: `src/HajimaoDesktopShop.Application/Business/Street/CommercialStreetSnapshot.cs`
- Modify: `src/HajimaoDesktopShop.Rendering/CommercialStreetSceneRenderer.cs`
- Modify: `tests/HajimaoDesktopShop.Rendering.Tests/CommercialStreetSceneRendererTests.cs`
- Modify: `tests/HajimaoDesktopShop.Desktop.Tests/Progression/LongTermProgressionScenarioRunner.cs`
- Create: `tests/HajimaoDesktopShop.Desktop.Tests/Progression/StoreBrandPortfolioScenarioTests.cs`
- Modify: `CHANGELOG.md`
- Create: `docs/progress/v0.1.23-store-type-kernel.md`
- Create: `docs/progress/v0.1.24-store-type-selection.md`
- Modify: `Directory.Build.props`

- [ ] Pass `StoreBrandId`, `StoreFormatId` and `FacadeStyleKey` to rendering snapshots. Render original pixel facades and text signs; do not copy real-world logos or add one large image per brand.
- [ ] Prove storefront hit testing, camera offset and content width still use instance order and work beyond five stores.
- [ ] Add seeded 30-day tests for all four starter types under their recommended strategies.
- [ ] Add 30/90/180/365-day single-type and mixed-portfolio scenarios. Assert non-negative cash, zero unexplained wage failures, continued investment options and distinct economic signatures.
- [ ] Treat a universally dominant type/strategy as a failed balance gate; adjust only `store-types.json` parameters and record before/after checkpoints.
- [ ] Complete v0.1.23 after Tasks 1-6 with changelog and progress report. Execute the separate content/event plan in v0.1.24, then complete the restrained selection/rendering surface and rich content gate in v0.1.25. Keep all releases in the `0.1.x` series.
- [ ] Run `dotnet test HajimaoDesktopShop.slnx -c Release --nologo`, `dotnet build HajimaoDesktopShop.slnx -c Release --nologo`, repository cleanup checks, and `git status --short` before claiming completion.
