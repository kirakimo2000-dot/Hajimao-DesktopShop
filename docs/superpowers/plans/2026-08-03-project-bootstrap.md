# Hajimao Desktop Shop Project Bootstrap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建立面向 Windows 的 .NET 10 WPF 分层解决方案，为第一阶段经营游戏开发提供可编译、可测试的骨架。

**Architecture:** 采用 Domain、Application、Infrastructure、Rendering、Desktop 五层结构，并以单向项目引用约束业务逻辑边界。Desktop 作为组合根，Infrastructure 和 Rendering 作为外部能力适配层。

**Tech Stack:** .NET 10, WPF, CommunityToolkit.Mvvm, SkiaSharp, Microsoft.Data.Sqlite, System.Text.Json, Serilog, xUnit

---

### Task 1: Create the solution and projects

**Files:**
- Create: `HajimaoDesktopShop.slnx`
- Create: `src/HajimaoDesktopShop.Domain/HajimaoDesktopShop.Domain.csproj`
- Create: `src/HajimaoDesktopShop.Application/HajimaoDesktopShop.Application.csproj`
- Create: `src/HajimaoDesktopShop.Infrastructure/HajimaoDesktopShop.Infrastructure.csproj`
- Create: `src/HajimaoDesktopShop.Rendering/HajimaoDesktopShop.Rendering.csproj`
- Create: `src/HajimaoDesktopShop.Desktop/HajimaoDesktopShop.Desktop.csproj`
- Create: `tests/HajimaoDesktopShop.Domain.Tests/HajimaoDesktopShop.Domain.Tests.csproj`
- Create: `tests/HajimaoDesktopShop.Application.Tests/HajimaoDesktopShop.Application.Tests.csproj`

- [x] **Step 1: Generate the seven projects**

```powershell
dotnet new classlib -n HajimaoDesktopShop.Domain -o src/HajimaoDesktopShop.Domain --no-restore
dotnet new wpf -n HajimaoDesktopShop.Desktop -f net10.0 -o src/HajimaoDesktopShop.Desktop --no-restore
dotnet new xunit -n HajimaoDesktopShop.Domain.Tests -o tests/HajimaoDesktopShop.Domain.Tests --no-restore
```

- [x] **Step 2: Target .NET 10**

Set non-UI projects to `<TargetFramework>net10.0</TargetFramework>` and Rendering/Desktop to `<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>`.

- [x] **Step 3: Add all projects to the solution**

```powershell
dotnet sln HajimaoDesktopShop.slnx add (Get-ChildItem -Recurse -Filter *.csproj | ForEach-Object FullName)
```

Expected: seven projects are listed by `dotnet sln HajimaoDesktopShop.slnx list`.

### Task 2: Enforce dependency boundaries

**Files:**
- Modify: `src/*/*.csproj`
- Modify: `tests/*/*.csproj`

- [x] **Step 1: Add production references**

```text
Application -> Domain
Infrastructure -> Application, Domain
Rendering -> Domain
Desktop -> Application, Infrastructure, Rendering
```

- [x] **Step 2: Add test references**

```text
Domain.Tests -> Domain
Application.Tests -> Application
```

- [x] **Step 3: Verify the graph**

Run:

```powershell
Get-ChildItem -Recurse -Filter *.csproj |
  ForEach-Object { dotnet list $_.FullName reference }
```

Expected: Domain has no project references and no reverse dependency enters Desktop.

### Task 3: Centralize build and package settings

**Files:**
- Create: `Directory.Build.props`
- Create: `Directory.Packages.props`

- [x] **Step 1: Enable shared compiler settings**

```xml
<LangVersion>12.0</LangVersion>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<Deterministic>true</Deterministic>
```

- [x] **Step 2: Centralize package versions**

Add CommunityToolkit.Mvvm, Microsoft.Data.Sqlite, SkiaSharp.Views.WPF, Serilog.Extensions.Hosting, xUnit, the test SDK, and coverlet to `Directory.Packages.props`.

- [x] **Step 3: Restore and build**

Run: `dotnet restore HajimaoDesktopShop.slnx` then `dotnet build HajimaoDesktopShop.slnx --no-restore`.

Expected: restore and build complete with zero errors.

### Task 4: Prepare feature directories and documentation

**Files:**
- Create: `README.md`
- Create: `AGENTS.md`
- Create: `docs/architecture/technical-foundation.md`
- Create: `docs/skills.md`
- Create: `.gitignore`

- [x] **Step 1: Create domain and adapter directories**

Create the folders listed in `docs/architecture/technical-foundation.md` for business features, simulation, persistence, rendering, WPF UI, assets, and JSON configuration.

- [x] **Step 2: Record architecture rules**

Document layer ownership, dependency direction, first-phase scope, and forbidden cross-layer dependencies.

- [x] **Step 3: Record applicable skills**

Document the workflow, testing, debugging, verification, UI, and asset-production skills that match this WPF project.

- [x] **Step 4: Run the complete test command**

Run: `dotnet test HajimaoDesktopShop.slnx --no-build`.

Expected: command exits with code 0; projects currently contain no behavior tests.
