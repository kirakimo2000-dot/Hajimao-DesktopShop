# User-Friendly Portable Package Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship v0.1.16 as a Windows portable ZIP whose extracted root contains exactly one friendly executable, while preserving the existing multi-file MSI pipeline.

**Architecture:** `build-release.ps1` will create two independent publish outputs. The portable output uses .NET 10 compressed single-file self-extraction and is renamed to `Hajimao DesktopShop.exe`; the installer output remains the current self-contained multi-file layout consumed by WiX. Release contract tests lock the separation and smoke tests validate the exact extracted layout before launching it.

**Tech Stack:** .NET 10, WPF, PowerShell 7/Windows PowerShell, xUnit, WiX 6, GitHub Actions.

---

### Task 1: Lock the friendly portable contract

**Files:**
- Modify: `tests/HajimaoDesktopShop.Release.Tests/ReleasePackagingContractTests.cs`
- Modify: `scripts/test-release.ps1`

- [ ] **Step 1: Write failing release contract tests**

Add assertions that the build script contains distinct `portable` and `installer-publish` directories plus these publish properties:

```csharp
Assert.Contains("PublishSingleFile=true", script, StringComparison.OrdinalIgnoreCase);
Assert.Contains("IncludeAllContentForSelfExtract=true", script, StringComparison.OrdinalIgnoreCase);
Assert.Contains("EnableCompressionInSingleFile=true", script, StringComparison.OrdinalIgnoreCase);
Assert.Contains("installer-publish", script, StringComparison.OrdinalIgnoreCase);
Assert.Contains("Hajimao DesktopShop.exe", script, StringComparison.Ordinal);
```

Add smoke-script assertions for an exact one-file extracted portable layout and the friendly executable name.

- [ ] **Step 2: Run the focused tests and verify RED**

Run:

```powershell
dotnet test tests/HajimaoDesktopShop.Release.Tests/HajimaoDesktopShop.Release.Tests.csproj -c Release --filter FullyQualifiedName~ReleasePackagingContractTests
```

Expected: failures because the current script publishes hundreds of files to one directory and uses `HajimaoDesktopShop.Desktop.exe`.

- [ ] **Step 3: Update smoke validation minimally**

After extraction, require exactly one root file and launch:

```powershell
$portableFiles = @(Get-ChildItem -LiteralPath $portableExtract -File -Recurse)
if ($portableFiles.Count -ne 1) {
    throw "Portable archive must contain exactly one file; actual: $($portableFiles.Count)."
}
$portableExecutable = Join-Path $portableExtract 'Hajimao DesktopShop.exe'
```

- [ ] **Step 4: Keep the test RED for missing build behavior**

Run the focused tests again. Expected: smoke-source assertions pass while build-script assertions remain RED.

### Task 2: Split portable and installer publication

**Files:**
- Modify: `scripts/build-release.ps1`

- [ ] **Step 1: Create two owned staging directories**

Keep `portable` for the player-facing single-file output and create `installer-publish` for the WiX bind path.

- [ ] **Step 2: Publish the portable executable as one compressed bundle**

Use:

```powershell
dotnet publish src/HajimaoDesktopShop.Desktop/HajimaoDesktopShop.Desktop.csproj `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None -p:DebugSymbols=false `
    -o $portableDir --nologo
```

Remove PDB files, rename the only application host to `Hajimao DesktopShop.exe`, and fail unless that is the sole remaining file.

- [ ] **Step 3: Publish the existing MSI payload independently**

Run the current non-single-file self-contained publish into `$installerPublishDir`, strip PDB files there, and pass that directory to WiX through `PublishDir`.

- [ ] **Step 4: Verify versions and archive only the friendly executable**

Read `FileVersionInfo` from both publish entry points, require the active version, and pass only the portable executable path to `Compress-Archive`.

- [ ] **Step 5: Run focused tests and verify GREEN**

Run the release contract test project. Expected: all tests pass.

### Task 3: Version and documentation

**Files:**
- Modify: `Directory.Build.props`
- Modify: `tests/HajimaoDesktopShop.Release.Tests/ReleasePackagingContractTests.cs`
- Modify: `CHANGELOG.md`
- Modify: `docs/ROADMAP.md`
- Create: `docs/progress/v0.1.16-friendly-portable-package.md`

- [ ] **Step 1: Write the failing version expectation**

Change the release test to expect `0.1.16` and run it against `0.1.15` to verify RED.

- [ ] **Step 2: Raise `VersionPrefix` to 0.1.16 and verify GREEN**

Update only the three-part patch version; keep schema v6 and all gameplay rules unchanged.

- [ ] **Step 3: Document the release**

Record the single-file layout, dual-publish boundary, verification evidence, known single-file extraction behavior, cleanup, and next plan. Move store identity to v0.1.17 in the roadmap.

### Task 4: Full release verification and cleanup

**Files:**
- Verify: `HajimaoDesktopShop.slnx`
- Verify: `artifacts/release/0.1.16/*`

- [ ] **Step 1: Run all Release tests and build**

```powershell
dotnet test HajimaoDesktopShop.slnx -c Release --nologo
dotnet build HajimaoDesktopShop.slnx -c Release --no-restore --nologo
```

Expected: all tests pass, zero warnings, zero errors.

- [ ] **Step 2: Build and smoke-test the actual release**

```powershell
pwsh -File scripts/build-release.ps1 -Version 0.1.16
pwsh -File scripts/test-release.ps1 -Version 0.1.16
```

Expected: ZIP contains exactly `Hajimao DesktopShop.exe`; portable and MSI runtime smoke checks pass.

- [ ] **Step 3: Inspect archive size and checksum**

Verify the ZIP entry count is one, executable version is 0.1.16, and SHA-256 matches the release manifest/checksum file.

- [ ] **Step 4: Clean generated workspace files**

Remove owned trial/staging outputs and generated `bin`, `obj`, `TestResults`, logs, databases, and temporary captures. Preserve only v0.1.16 release artifacts, source, tests, docs, and required assets.

