# Combat Retail Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace inventory/restock/sale simulation with a deterministic idle-combat loop where the default clerk Maomao throws equipped products at time- and event-driven customers, successful service grants revenue and random product drops, and every store uses one interior background instead of shelves.

**Architecture:** Build a pure combat domain beside the legacy retail domain, expose it through a dedicated Application service and immutable snapshots, then switch WPF and Skia surfaces before removing runtime calls to procurement, sales, shelves and employee roles. A data-driven skeletal animation layer separates rigs, skins, 24-frame clips and product attachment sockets; 0.2.0 ships only neutral `maomao-default`, while future collectible characters can reuse the contract without adding gacha systems now. Product collection is account-wide, loadouts are store-specific, customer pools are selected from injected real-local-time segments plus active events, and each store scene draws one configured background image.

**Tech Stack:** .NET 10, C# 14, deterministic domain simulation, WPF, SkiaSharp pixel rendering, SQLite JSON payload persistence, xUnit.

---

## Frozen design contract

### Core loop

1. A customer enters with demand HP, speed, reward, tags, weaknesses and resistances.
2. Maomao automatically targets the customer nearest the service boundary and throws an equipped product.
3. Projectile impact deals product power modified by mastery, tags and customer resistance; Maomao contributes neutral base values and no special effect.
4. Demand HP reaching zero means successfully served, not killed. The store receives revenue and rolls a product drop.
5. A customer reaching the boundary with demand remaining leaves unsatisfied. The store records one miss, but there is no game-over or permanent punishment.
6. First product copy unlocks it; duplicate copies automatically fill mastery progress.
7. The player only equips or replaces store products, expands slots and chooses store investments.

### Small decision surface

- Each store starts with 3 product slots and can grow to 6.
- Products are account-wide unlocks and may be used by multiple stores, but only once in the same store.
- Every active store uses one default Maomao and the store loadout; there are no employee roles or per-character equipment inventories.
- Products expose power, attack interval, revenue modifier and one effect sentence.
- Customers expose demand HP, speed, base reward and one trait sentence.
- No random affixes, dismantling, ammunition, inventory capacity, recruitment, employee management or per-character equipment screen.

### Progression rules

- First copy grants mastery level 1.
- Copies required for the next level are `3 + 2 * (currentLevel - 1)`.
- Mastery cap is 20. Scaling reaches +66.5% base power and +28.5% base revenue at level 20, avoiding exponential income inflation.
- Rarity controls drop weight and base budget, not random stat rolls.
- Damage products improve throughput; revenue products improve cash per customer; utility products add splash, slow, vulnerability or drop bonuses.
- Cash remains meaningful through fixed store operating cost, slot expansion, store growth and new stores.

### Character, animation and store boundaries

- 0.2.0 contains exactly one playable clerk definition: `maomao-default`.
- Maomao has neutral attack cadence and no profession, skill, rarity, passive effect or recruitment path.
- Every store automatically owns one Maomao; character acquisition, draw currency, banners, pity and character duplicates are outside 0.2.0.
- The combat engine consumes generic `CharacterCombatStats` and never switches on a profession enum, allowing future collectible characters without rewriting combat.
- Rig, skin, animation clip and gameplay definition are separate assets. A new character may reuse the humanoid rig and clips while replacing only skin parts and metadata.
- Every clip has exactly 24 logical frames. Runtime skeletal interpolation generates poses and snaps joints to integer pixels, avoiding 24 full PNG frames per action.
- The humanoid rig includes root, pelvis, torso, head, upper/lower arms, hands, upper/lower legs and feet; the throwing hand exposes a `product_socket` attachment.
- Required Maomao clips are idle, walk, wind-up, throw, recovery and celebrate. Required customer clips are walk, hit reaction, served and leave.
- The store scene contains no shelf geometry, shelf hit targets or shelf upgrades. Each store configuration supplies one `InteriorBackgroundKey`; 0.2.0 may use distinct placeholder PNGs.
- Store format controls pressure, elite chance, reward multiplier and category drop weights.
- Each opened store runs its own combat lane and loadout while sharing cash and collection.
- Player level unlocks slots, customer tiers and investment depth; product discovery comes from drops.
- Customer spawn pools use injected real local time segments (`05–09`, `09–17`, `17–22`, `22–05`) without displaying a clock or creating game time.
- Active events add, remove or reweight customer archetypes after the time pool is selected; elite arrivals remain top popups on the street page.
- Only active program runtime advances combat; no offline reward and no game clock.

## 0.2.0 internal delivery stages

| Stage | Outcome | Real content floor |
| --- | --- | --- |
| A | Combat and skeletal-animation vertical slice replaces active sales in one store | Maomao; humanoid rig; 10 required clips; placeholder store backgrounds; 24 products; 12 customers; time/event pools |
| B | Collection, mastery, loadout UI, multi-store and schema v8 | Maomao only; 24 products; 12 customers; per-store backgrounds and drop pools |
| C | Content reachability, long-run balance, full regression and packaging | 24 products, 12 customers, 11 event modifiers; every product reachable |

The active version remains 0.1.34 throughout stages A–C. Only after all three stages pass full verification is the product version changed directly to 0.2.0. Collectible characters and gacha are a later project after 0.2.0; this plan only preserves the extension boundary.

## File map

- `src/HajimaoDesktopShop.Domain/Combat/StoreCombatEngine.cs`: pure targeting, cooldown, projectile, impact, service and escape rules.
- `src/HajimaoDesktopShop.Domain/Combat/StoreCombatState.cs`: active customers, Maomao cooldown and projectiles without profession state.
- `src/HajimaoDesktopShop.Domain/Combat/CombatEvent.cs`: typed rendering/Application events.
- `src/HajimaoDesktopShop.Domain/Collections/ProductCollection.cs`: unlock, copies and mastery.
- `src/HajimaoDesktopShop.Domain/Collections/StoreProductLoadout.cs`: validated 3–6 product slots.
- `src/HajimaoDesktopShop.Application/Business/Combat/BusinessCombatService.cs`: per-store orchestration, rewards and drops.
- `src/HajimaoDesktopShop.Application/Business/Combat/CombatSnapshot.cs`: immutable UI/rendering projection.
- `src/HajimaoDesktopShop.Application/Catalog/ProductCombatDefinition.cs`: product combat content record.
- `src/HajimaoDesktopShop.Application/Catalog/CustomerArchetypeDefinition.cs`: customer and drop-table record.
- `src/HajimaoDesktopShop.Application/Catalog/CharacterDefinition.cs`: generic future-facing character metadata; only `maomao-default` ships.
- `src/HajimaoDesktopShop.Application/Business/Combat/CustomerSpawnPoolService.cs`: real-time-segment and event-weighted customer selection.
- `src/HajimaoDesktopShop.Application/Catalog/StoreInteriorDefinition.cs`: store-to-background mapping.
- `src/HajimaoDesktopShop.Application/Persistence/CombatSaveData.cs`: schema v8 collection, loadout and combat state.
- `src/HajimaoDesktopShop.Infrastructure/Configuration/JsonCombatContentCatalog.cs`: strict JSON loading and reference checks.
- `src/HajimaoDesktopShop.Rendering/Animation/SkeletalRig.cs`: bones, parent links, pivots, z-order and sockets.
- `src/HajimaoDesktopShop.Rendering/Animation/SkeletalAnimationClip.cs`: 24-frame bone transforms and animation events.
- `src/HajimaoDesktopShop.Rendering/Animation/SkeletalAnimator.cs`: deterministic interpolation and integer pixel snapping.
- `src/HajimaoDesktopShop.Rendering/Combat/BusinessShopCombatChoreography.cs`: combat events mapped to rig clips.
- `src/HajimaoDesktopShop.Rendering/Combat/ProductProjectileRenderer.cs`: throw arcs, impact, reward and drop feedback.
- `src/HajimaoDesktopShop.Rendering/Interiors/StoreInteriorRenderer.cs`: one background bitmap per store, with no shelf objects.
- `src/HajimaoDesktopShop.Desktop/ViewModels/Market/StoreLoadoutViewModel.cs`: equip and comparison commands.
- `src/HajimaoDesktopShop.Desktop/ViewModels/Market/ProductCollectionViewModel.cs`: discovery and mastery view.

## Task 1: Build the extensible skeletal animation foundation

**Files:**
- Create: `src/HajimaoDesktopShop.Rendering/Animation/BoneTransform.cs`
- Create: `src/HajimaoDesktopShop.Rendering/Animation/SkeletalRig.cs`
- Create: `src/HajimaoDesktopShop.Rendering/Animation/SkeletalAnimationClip.cs`
- Create: `src/HajimaoDesktopShop.Rendering/Animation/SkeletalAnimator.cs`
- Create: `src/HajimaoDesktopShop.Rendering/Animation/CharacterSkin.cs`
- Create: `src/HajimaoDesktopShop.Infrastructure/Configuration/JsonCharacterAnimationCatalog.cs`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/characters/rigs/humanoid.json`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/characters/animations/humanoid-clips.json`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/characters/maomao/parts.png`
- Create during authoring, exclude from release payload: `artifacts/animation-reference/maomao/*.png`
- Test: `tests/HajimaoDesktopShop.Rendering.Tests/Animation/SkeletalAnimatorTests.cs`
- Test: `tests/HajimaoDesktopShop.Infrastructure.Tests/Configuration/JsonCharacterAnimationCatalogTests.cs`

- [x] Write failing tests for acyclic parent links, unique bone/socket IDs, valid skin parts, exactly 24 logical frames per clip, interpolation, frame wrapping, integer-pixel output and deterministic repeated evaluation.
- [x] Run `dotnet test tests/HajimaoDesktopShop.Rendering.Tests -c Release --filter FullyQualifiedName~SkeletalAnimatorTests`; require RED because the animation types do not exist.
- [x] Add these exact core contracts:

```csharp
public sealed record SkeletalRig(
    string Id,
    IReadOnlyList<RigBone> Bones,
    IReadOnlyList<RigSocket> Sockets);

public sealed record RigBone(
    string Id, string? ParentId, float PivotX, float PivotY, int ZIndex);

public sealed record RigSocket(
    string Id, string BoneId, float OffsetX, float OffsetY);

public sealed record SkeletalAnimationClip(
    string Id,
    int LogicalFrameCount,
    IReadOnlyDictionary<string, IReadOnlyList<BoneKeyframe>> BoneTracks,
    IReadOnlyList<AnimationMarker> Markers);
```

- [x] Define `humanoid-v1` with root, pelvis, torso, head, left/right upper arm, lower arm, hand, upper leg, lower leg and foot; bind `product_socket` to the throwing hand.
- [x] Approve one in-game Maomao seed frame first. Use it as the identity, silhouette, palette, costume and proportion anchor for every later motion.
- [x] Generate the throw action as one complete 24-frame reference strip, never as 24 isolated image requests. Treat AI output as motion reference only: reject it from shipping if transparency, slot containment or anatomy gates fail.
- [x] Build the transparent edit canvas, use the installed `imagegen` skill for the one-strip edit request, normalize with one shared scale and bottom-center anchor, lock frame 1 when the clip starts from the base pose, and render the preview with these commands:

```powershell
$spriteTools = 'C:\Users\86427\.codex\plugins\cache\openai-curated-remote\game-studio\0.1.2\scripts'
$spritePython = 'C:\Users\86427\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
& $spritePython "$spriteTools\build_sprite_edit_canvas.py" --seed artifacts/animation-reference/maomao/seed.png --out artifacts/animation-reference/maomao/throw-edit-canvas.png --frames 24 --slot-size 64 --canvas-size 1536
& $spritePython "$spriteTools\normalize_sprite_strip.py" --input artifacts/animation-reference/maomao/throw-raw.png --out-dir artifacts/animation-reference/maomao/throw --frames 24 --frame-size 64
Copy-Item artifacts/animation-reference/maomao/throw/01.png artifacts/animation-reference/maomao/seed.png -Force
& $spritePython "$spriteTools\normalize_sprite_strip.py" --input artifacts/animation-reference/maomao/throw-raw.png --out-dir artifacts/animation-reference/maomao/throw --frames 24 --frame-size 64 --anchor artifacts/animation-reference/maomao/seed.png --lock-frame1
& $spritePython "$spriteTools\render_sprite_preview_sheet.py" --frames-dir artifacts/animation-reference/maomao/throw --out artifacts/animation-reference/maomao/throw-preview.png --columns 8
```

- [x] Translate approved motion principles into skeletal bone keyframes instead of shipping full reference strips. Author exactly 24 logical frames for Maomao idle, walk, wind-up, throw, recovery and celebrate, plus customer walk, hit, served and leave clips. Put `release_product` on the throw clip and never infer projectile release from wall time.
- [x] Render bones in z-order with nearest-neighbor sampling and integer pixel snapping. The rig file contains geometry; `maomao/parts.png` contains skin pixels; clips contain transforms only.
- [x] Render all ten runtime clips for all 24 frames in the automated resource gate. Require visible in-bounds pixels, preserved transparency and a moving `product_socket` at the release marker.
- [x] Run focused Rendering and catalog tests; require GREEN, keep reference strips out of the portable package, and validate that adding a second test skin needs no animator code change.

## Task 2: Content, customer pools and store interiors

**Files:**
- Create: `src/HajimaoDesktopShop.Application/Catalog/ProductCombatDefinition.cs`
- Create: `src/HajimaoDesktopShop.Application/Catalog/CustomerArchetypeDefinition.cs`
- Create: `src/HajimaoDesktopShop.Application/Catalog/CharacterDefinition.cs`
- Create: `src/HajimaoDesktopShop.Application/Catalog/StoreInteriorDefinition.cs`
- Create: `src/HajimaoDesktopShop.Application/Business/Combat/CustomerSpawnPoolService.cs`
- Create: `src/HajimaoDesktopShop.Infrastructure/Configuration/JsonCombatContentCatalog.cs`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Config/product-combat.json`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/customers/customer-archetypes.json`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/customers/customer-spawn-pools.json`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/characters/characters.json`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/interiors/interiors.json`
- Create: `src/HajimaoDesktopShop.Desktop/Assets/Content/interiors/placeholders/*.png`
- Test: `tests/HajimaoDesktopShop.Infrastructure.Tests/Configuration/JsonCombatContentCatalogTests.cs`
- Test: `tests/HajimaoDesktopShop.Application.Tests/Business/Combat/CustomerSpawnPoolServiceTests.cs`

- [x] Write failing tests for unique IDs, positive values, valid resistance range, valid product references, one `maomao-default`, one background per store, four non-overlapping real-time segments, event weight modifiers and the 24/12 minimum content floor.
- [x] Run `dotnet test tests/HajimaoDesktopShop.Infrastructure.Tests -c Release --filter FullyQualifiedName~JsonCombatContentCatalogTests`; require RED because the loader is absent.
- [x] Add the combat content records and validated JSON contracts.

```csharp
public sealed record ProductCombatDefinition(
    string ProductId, int BasePower, int AttackIntervalTicks,
    int RevenueModifierPermille, ProductEffectKind Effect,
    int EffectStrengthPermille, string[] Tags, int DropWeight);

public sealed record CustomerArchetypeDefinition(
    string Id, int DemandHp, int MovementPermillePerTick,
    long BaseRewardCents, string[] Tags,
    IReadOnlyDictionary<string, int> ResistancePermille,
    IReadOnlyDictionary<string, int> ProductDropWeights);

public sealed record CharacterDefinition(
    string Id, string RigId, string SkinId, int BaseAttackIntervalTicks);

public sealed record StoreInteriorDefinition(
    string StoreId, string BackgroundAssetPath);
```

- [x] Add only `maomao-default` with neutral values and no effects. Stage A adds 24 mechanically distinct products, 12 customer archetypes and one background assignment per configured store. All stores may share the same storage-efficient placeholder until final art replaces it; do not use duplicate gameplay content rows.
- [x] Select the base pool from injected local hour using `05–09`, `09–17`, `17–22`, `22–05`, then apply active-event additions/removals/weights. Real time changes only future spawns and never advances simulation or awards offline income.
- [x] Rerun the focused tests and require GREEN.

## Task 3: Deterministic combat engine

**Files:**
- Create: `src/HajimaoDesktopShop.Domain/Combat/ProductCombatStats.cs`
- Create: `src/HajimaoDesktopShop.Domain/Combat/CustomerCombatStats.cs`
- Create: `src/HajimaoDesktopShop.Domain/Combat/StoreCombatState.cs`
- Create: `src/HajimaoDesktopShop.Domain/Combat/CombatEvent.cs`
- Create: `src/HajimaoDesktopShop.Domain/Combat/StoreCombatEngine.cs`
- Test: `tests/HajimaoDesktopShop.Domain.Tests/Combat/StoreCombatEngineTests.cs`

- [x] Write failing tests for nearest-boundary targeting, cooldown, projectile travel, resistance, splash, service, escape, empty loadout and deterministic replay.
- [x] Run `dotnet test tests/HajimaoDesktopShop.Domain.Tests -c Release --filter FullyQualifiedName~StoreCombatEngineTests`; require RED.
- [x] Implement the pure deterministic combat contract.

```csharp
public sealed class StoreCombatEngine
{
    public StoreCombatTickResult Tick(
        StoreCombatState state,
        CharacterCombatStats maomao,
        IReadOnlyList<ProductCombatStats> loadout,
        CustomerSpawnRequest? spawn);
}

public sealed record StoreCombatTickResult(
    StoreCombatState State,
    IReadOnlyList<CombatEvent> Events);
```

- [x] Emit only `CustomerSpawned`, `ProductThrown`, `ProductHit`, `CustomerServed` and `CustomerEscaped`; keep WPF, Skia, money, shelves, professions, gacha, stock and wall time outside Domain.
- [x] Rerun twice and require identical serialized state for identical seed and inputs.

## Task 4: Collection, drops and loadouts

**Files:**
- Create: `src/HajimaoDesktopShop.Domain/Collections/ProductCollection.cs`
- Create: `src/HajimaoDesktopShop.Domain/Collections/StoreProductLoadout.cs`
- Create: `src/HajimaoDesktopShop.Application/Business/Combat/ProductDropService.cs`
- Create: `src/HajimaoDesktopShop.Application/Business/Combat/ProductLoadoutService.cs`
- Test: `tests/HajimaoDesktopShop.Domain.Tests/Collections/ProductCollectionTests.cs`
- Test: `tests/HajimaoDesktopShop.Application.Tests/Business/Combat/ProductDropServiceTests.cs`

- [x] Write RED tests for first-copy unlock, duplicate mastery, level-20 cap, 3–6 slots, same-store uniqueness and cross-store reuse.
- [x] Add consistent collection, mastery and loadout records and formula.

```csharp
public sealed record ProductCollectionEntry(string ProductId, int MasteryLevel, int StoredCopies);
public sealed record StoreProductLoadout(string StoreId, int UnlockedSlots, IReadOnlyList<string> ProductIds);
public static int CopiesRequired(int level) => level >= 20 ? int.MaxValue : 3 + (2 * (level - 1));
```

- [x] Implement at most one normal drop per served customer and one independent elite bonus roll; record every roll source for diagnostics.
- [x] Run Domain collection and Application combat tests; require GREEN.

## Task 5: Active session and schema v8

**Files:**
- Create: `src/HajimaoDesktopShop.Application/Business/Combat/BusinessCombatService.cs`
- Create: `src/HajimaoDesktopShop.Application/Business/Combat/CombatSnapshot.cs`
- Create: `src/HajimaoDesktopShop.Application/Persistence/CombatSaveData.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/BusinessSession.cs`
- Modify: `src/HajimaoDesktopShop.Application/Persistence/GameSaveData.cs`
- Create: `src/HajimaoDesktopShop.Infrastructure/Persistence/LegacyGameSaveV7.cs`
- Modify: `src/HajimaoDesktopShop.Infrastructure/Persistence/SqliteGameSaveStore.cs`
- Test: `tests/HajimaoDesktopShop.Application.Tests/Business/Combat/BusinessCombatServiceTests.cs`
- Test: `tests/HajimaoDesktopShop.Infrastructure.Tests/Persistence/SqliteGameSaveStoreTests.cs`

- [x] Write RED tests: stable store-ID tick order, reward only on service, no reward on escape, drop/loadout persistence and no closed-program progression.
- [ ] Add this schema payload:

```csharp
public sealed record CombatSaveData(
    ProductCollectionSaveData Collection,
    IReadOnlyList<StoreProductLoadoutSaveData> Loadouts,
    IReadOnlyList<StoreCombatStateSaveData> Stores,
    ulong RandomState);
```

- [x] Migrate v7 by preserving cash, level, stores and growth; create one `maomao-default` per store; archive the old employee roster only in compatibility data with no active effect; unlock registered products; turn stock quantity into copies; remove shelves/restock policies; cancel inbound orders and refund paid base cost once.
- [x] Switch the desktop simulation loop to `BusinessCombatService`. Keep old types compilable only for migration until the 0.2.0 cutover is verified.
- [x] Run focused Application and SQLite tests; require GREEN.

## Task 6: Combat rendering, backgrounds and 24-frame rig playback

**Files:**
- Create: `src/HajimaoDesktopShop.Rendering/Combat/BusinessShopCombatChoreography.cs`
- Create: `src/HajimaoDesktopShop.Rendering/Combat/ProductProjectileRenderer.cs`
- Create: `src/HajimaoDesktopShop.Rendering/Interiors/StoreInteriorRenderer.cs`
- Modify: `src/HajimaoDesktopShop.Rendering/BusinessShopSceneRenderer.cs`
- Test: `tests/HajimaoDesktopShop.Rendering.Tests/Combat/BusinessShopCombatChoreographyTests.cs`
- Test: `tests/HajimaoDesktopShop.Rendering.Tests/BusinessShopSceneRendererTests.cs`

- [x] Write tests for state-driven customer position, one background draw and zero shelf draws, 24-frame clips, release markers, projectile interpolation, impact and reduced motion.
- [x] Render only immutable combat state/events; Rendering does not calculate targets, damage or drops.
- [x] Draw one configured background, customers, Maomao, products, demand bars, damage, coins and drop feedback within the pixel budget.
- [x] Run all Rendering tests and require GREEN without exceeding the atlas budget.

## Task 7: Loadout UI without operational clutter

**Files:**
- Create: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/StoreLoadoutViewModel.cs`
- Create: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/ProductCollectionViewModel.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/MarketViewModel.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/Windows/ManagementWindow.xaml`
- Test: `tests/HajimaoDesktopShop.Desktop.Tests/ViewModels/Market/StoreLoadoutViewModelTests.cs`
- Test: `tests/HajimaoDesktopShop.Desktop.Tests/Windows/ManagementWindowTests.cs`

- [x] Test equipped slots, replacement comparison, mastery progress, customer trait text and absence of legacy operational controls.
- [x] Keep three top-level sections; place `当前装备` and `商品图鉴` inside battle strategy.
- [x] Make equip/replace one action, provide one recommended combination, and keep Maomao fixed without role management.
- [x] Run Desktop layout and 420×280 store containment tests; require GREEN.

## Task 8: Retire shelves, employee roles and the retail loop

**Files:**
- Modify: `src/HajimaoDesktopShop.Application/Business/Analysis/StoreEconomyAnalysisService.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Simulation/BusinessDayReport.cs`
- Modify: `src/HajimaoDesktopShop.Application/Business/Investments/InvestmentReturnCalculator.cs`
- Modify: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/StoreEconomyViewModel.cs`
- Delete after reference audit: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/ProductManagementViewModel.cs`
- Delete after reference audit: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/ProductManagementItemViewModel.cs`
- Delete after migration verification: `src/HajimaoDesktopShop.Desktop/ViewModels/Market/EmployeeManagementViewModel.cs`
- Delete after renderer replacement: `src/HajimaoDesktopShop.Rendering/Interactions/BusinessShopEmployeeChoreography.cs`

- [x] Replace player-facing reports with encountered, served, escaped, damage, service revenue and drops.
- [x] Replace shelf investment with automatic product-slot progression and direct new-store selection; do not retain shelf levels under a new label.
- [x] Audit active Desktop and Rendering paths; legacy matches remain only in migration/compatibility layers.
- [x] Run 1/7/30/90/365 deterministic scenarios; require progress without offline gains, deadlocks or repetitive clicking.

## Task 9: Version, verify and package

**Files:**
- Modify: `Directory.Build.props`
- Modify: `CHANGELOG.md`
- Modify: `README.md`
- Modify: `docs/roadmap.md`
- Modify: `tests/HajimaoDesktopShop.Release.Tests/ReleasePackagingContractTests.cs`

- [x] Keep 0.1.34 during implementation. Set 0.2.0 only after Tasks 1–8 pass and the active session no longer calls sale/restock/shelf/employee-role systems.
- [x] Run `scripts/test-all.ps1 -Configuration Release`; require zero failures.
- [x] Run `scripts/build-portable.ps1 -Version 0.2.0 -PrunePrevious` and verify the root EXE is 0.2.0.0 and equals the ZIP entry by SHA-256.
- [x] Run `scripts/clean-workspace.ps1`; require zero `bin`/`obj`, empty staging and preserved root EXE.

## Self-review

- Every requested concept is covered: no stock/sales/shelf loop, Maomao-only combat, no professions, time/event spawn pools, per-store background, random product drops, product slots, skeletal throwing animation and long-term idle progression.
- Domain owns rules, Application owns content/money/save, Rendering consumes immutable events, and Desktop only issues loadout commands.
- Store-level loadouts, one neutral Maomao, automatic duplicate upgrades and no random affixes prevent a new micromanagement problem.
- Every milestone has a real content floor; no sample-only release is allowed.
- Schema v8 conversion preserves meaningful progress without reintroducing offline settlement or game time; injected real local time only selects future customer pools.
- Rig, skin, clip and character metadata are independent; future collectible characters do not require profession switches or animator rewrites, but no gacha system is included now.
- Type names and signatures are consistent across all tasks; no incomplete implementation placeholders remain.
