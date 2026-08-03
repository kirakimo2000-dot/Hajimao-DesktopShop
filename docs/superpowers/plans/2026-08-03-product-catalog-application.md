# Product Catalog and Application Boundary Implementation Plan

> **Execution note:** Follow strict RED → GREEN → REFACTOR for every behavior. This directory is not a Git repository, so `progress.md` and this checklist are the recovery checkpoints.

**Goal:** Complete Phase 2 with a ten-product JSON catalog contract and an application service that lets every future UI purchase stock, change prices, and query immutable shop state without owning domain rules.

**Architecture:** `Domain.Shop` remains the transaction boundary. `Application` defines catalog contracts, commands, and read-only snapshots. `Infrastructure` implements JSON loading. Desktop will later compose these adapters; neither WPF nor rendering types enter these layers.

**Tech Stack:** .NET 10, C# 14, System.Text.Json, xUnit.

---

### Task 1: Aggregate-owned price changes

**Files:**
- Modify: `tests/HajimaoDesktopShop.Domain.Tests/Shops/ShopTests.cs`
- Create: `src/HajimaoDesktopShop.Domain/Shops/PriceChangeResult.cs`
- Modify: `src/HajimaoDesktopShop.Domain/Shops/Shop.cs`

- [x] Add a test proving a registered product price changes and unknown/invalid requests do not mutate state.
- [x] Run `dotnet test tests/HajimaoDesktopShop.Domain.Tests --filter FullyQualifiedName~ChangePrice` and observe missing API failure.
- [x] Add `PriceChangeStatus` (`Success`, `UnknownProduct`, `InvalidPrice`) and `Shop.TryChangePrice(ProductId, Money)`.
- [x] Run the selected tests and full Domain regression.
- [x] Prove and implement Domain-owned revenue, purchase-cost, and gross-profit totals for consistent snapshots.

### Task 2: Application game service and immutable snapshots

**Files:**
- Create: `tests/HajimaoDesktopShop.Application.Tests/Game/ShopGameServiceTests.cs`
- Create: `src/HajimaoDesktopShop.Application/Catalog/ProductDefinition.cs`
- Create: `src/HajimaoDesktopShop.Application/Game/ProductSnapshot.cs`
- Create: `src/HajimaoDesktopShop.Application/Game/ShopSnapshot.cs`
- Create: `src/HajimaoDesktopShop.Application/Game/ShopGameService.cs`

- [x] Test initialization from catalog definitions, purchase, price change, and a fresh immutable query snapshot.
- [x] Run selected Application tests and observe missing types.
- [x] Implement the minimal service. It owns one `Shop`, converts string IDs and cent values at the boundary, and returns domain operation results plus immutable snapshot records.
- [x] Include snapshot totals for cash, inventory, revenue, expenses, and gross profit so both windows consume the same data.
- [x] Run selected and all Application tests.

### Task 3: JSON catalog adapter and ten-product content

**Files:**
- Create: `tests/HajimaoDesktopShop.Infrastructure.Tests/HajimaoDesktopShop.Infrastructure.Tests.csproj`
- Modify: `HajimaoDesktopShop.slnx`
- Create: `tests/HajimaoDesktopShop.Infrastructure.Tests/Configuration/JsonProductCatalogTests.cs`
- Create: `src/HajimaoDesktopShop.Application/Catalog/IProductCatalog.cs`
- Create: `src/HajimaoDesktopShop.Infrastructure/Configuration/JsonProductCatalog.cs`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Config/products.json`
- Modify: `src/HajimaoDesktopShop.Desktop/HajimaoDesktopShop.Desktop.csproj`

- [x] Test that the shipped file loads exactly ten unique, valid products across three shelf kinds.
- [x] Run the selected test and observe missing adapter/content failure.
- [x] Implement async JSON loading with explicit validation and actionable exceptions.
- [x] Add ten convenience-store products and copy the JSON to output.
- [x] Run Infrastructure tests and validate the output copy.

### Task 4: Phase checkpoint

**Files:**
- Modify: `task_plan.md`
- Modify: `findings.md`
- Modify: `progress.md`
- Modify: `CHANGELOG.md`
- Create: `docs/progress/v0.2.0-phase-2.md`

- [x] Run `dotnet test HajimaoDesktopShop.slnx`.
- [x] Run `dotnet build HajimaoDesktopShop.slnx` and require 0 warnings / 0 errors.
- [x] Run `dotnet list HajimaoDesktopShop.slnx package --vulnerable --include-transitive`.
- [x] Mark Phase 2 complete only after all checks pass; summarize progress and the Phase 3 simulation plan.
